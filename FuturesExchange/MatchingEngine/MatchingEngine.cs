using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternalExchangeDatamodel;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using PublicExchangeDatamodel;
using TradingAgent.Interfaces;

namespace MatchingEngine
{
    internal sealed class MatchingEngine : StatefulService, IMatchingEngine
    {

        private static readonly string BUY_ORDERS_QUEUE = "BUY_ORDERS_QUEUE";
        private static readonly string SELL_ORDERS_QUEUE = "SELL_ORDERS_QUEUE";
        private static readonly string CANCEL_ORDERS_QUEUE = "CANCEL_ORDERS_QUEUE";
        private static readonly string ORDER_BOOK = "ORDER_BOOK";
        private static readonly string BUY_ORDERS = "BUY_ORDERS";
        private static readonly string SELL_ORDERS = "SELL_ORDERS";

        public MatchingEngine(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<MatchingEngineResponse> SendOrder(Order order)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                var ordersQueue = await OrdersQueue(order.OrderSide);
                await ordersQueue.EnqueueAsync(tx, order);
                await tx.CommitAsync();
            }
            return new MatchingEngineResponse(order, OrderStatus.ACTIVE);
        }

        public async Task CancelOrder(CancelOrderRequest cancelOrderRequest)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                var cancelOrderQueue = await StateManager.GetOrAddAsync<IReliableQueue<CancelOrderRequest>>(CANCEL_ORDERS_QUEUE);
                await cancelOrderQueue.EnqueueAsync(tx, cancelOrderRequest);
                await tx.CommitAsync();
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Tick();
                await Task.Delay(1000, cancellationToken);
            }
        }

        private async Task Tick()
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                BuySellOrders ordersFromOrderQueues = await GetAllOrdersFromOrderQueues(tx);
                BuySellOrders ordersFromOrderBook = await GetAllOrdersFromOrderBook(tx);

                List<Order> buyOrdersSorted = CreateSortedOrderList(tx, ordersFromOrderBook.buyOrders, ordersFromOrderQueues.buyOrders, Comparer<Order>.Create((order1, order2) => order2.Price.CompareTo(order1.Price)));
                List<Order> sellOrdersSorted = CreateSortedOrderList(tx, ordersFromOrderBook.sellOrders, ordersFromOrderQueues.sellOrders, Comparer<Order>.Create((order1, order2) => order1.Price.CompareTo(order2.Price)));
                BuySellOrders sortedOrders = new BuySellOrders(buyOrdersSorted, sellOrdersSorted);

                List<CancelOrderRequest> cancelOrderRequests = await GetAllCancelOrderRequestsFromQueue(tx);
                await RemoveCancelledOrders(tx, sortedOrders, cancelOrderRequests);

                List<Trade> trades = MatchOrders(sortedOrders);
                await UpdateOrderBook(tx, sortedOrders);
                await tx.CommitAsync();
                foreach (Trade trade in trades)
                {
                    await SendTradeUpdates(trade);
                }
                await SendOrderBookSnapshot(sortedOrders);
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        private async Task RemoveCancelledOrders(ITransaction tx, BuySellOrders buySellOrders, List<CancelOrderRequest> cancelOrderRequests)
        {
            foreach (CancelOrderRequest cancelOrderRequest in cancelOrderRequests)
            {
                int removed = buySellOrders.buyOrders.RemoveAll(order => order.UserID == cancelOrderRequest.UserID && order.OrderID == cancelOrderRequest.OrderID);
                removed += buySellOrders.sellOrders.RemoveAll(order => order.UserID == cancelOrderRequest.UserID && order.OrderID == cancelOrderRequest.OrderID);
                if (removed > 0) {
                    await SendCancelOrderUpdate(cancelOrderRequest);
                }
            }
        }

        private List<Trade> MatchOrders(BuySellOrders sortedOrders)
        {
            List<Trade> trades = new List<Trade>();
            while (sortedOrders.buyOrders.Count > 0 && sortedOrders.sellOrders.Count > 0)
            {
                Order bestBuy = sortedOrders.buyOrders.First();
                Order bestSell = sortedOrders.sellOrders.First();
                if (bestBuy.Price < bestSell.Price)
                {
                    break;
                }
                sortedOrders.buyOrders.Remove(bestBuy);
                sortedOrders.sellOrders.Remove(bestSell);

                double price = (bestBuy.Price + bestSell.Price) / 2;
                uint volume = Math.Min(bestBuy.Volume, bestSell.Volume);
                Trade trade = new Trade(bestBuy.Book, bestBuy.UserID, bestBuy.OrderID, bestSell.UserID, bestSell.OrderID, price, volume);
                bestBuy.Volume -= volume;
                bestSell.Volume -= volume;
                if (bestBuy.Volume > 0)
                {
                    sortedOrders.buyOrders.Insert(0, bestBuy);
                }
                if (bestSell.Volume > 0)
                {
                    sortedOrders.sellOrders.Insert(0, bestSell);
                }
                trades.Add(trade);

            }
            return trades;
        }

        private async Task SendTradeUpdates(Trade trade)
        {
            var buyActorID = new ActorId(trade.BuyUserID);
            var buyProxy = ActorProxy.Create<ITradingAgent>(buyActorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
            await buyProxy.RegisterTrade(trade, new CancellationToken());

            var sellActorID = new ActorId(trade.SellUserID);
            var sellProxy = ActorProxy.Create<ITradingAgent>(sellActorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
            await sellProxy.RegisterTrade(trade, new CancellationToken());
        }

        private async Task SendOrderBookSnapshot(BuySellOrders buySellOrders)
        {
            if (buySellOrders.buyOrders.Count == 0 && buySellOrders.sellOrders.Count == 0)
            {
                return;
            }
            string book = "";
            if (buySellOrders.buyOrders.Count > 0)
            {
                book = buySellOrders.buyOrders.First().Book;
            }
            else if (buySellOrders.sellOrders.Count > 0)
            {
                book = buySellOrders.sellOrders.First().Book;
            }
            SortedDictionary<double, uint> buyOrdersVolumesPerLevel = new SortedDictionary<double, uint>();
            foreach (Order buyOrder in buySellOrders.buyOrders)
            {
                if (buyOrdersVolumesPerLevel.ContainsKey(buyOrder.Price))
                {
                    buyOrdersVolumesPerLevel[buyOrder.Price] += buyOrder.Volume;
                } else
                {
                    buyOrdersVolumesPerLevel[buyOrder.Price] = buyOrder.Volume;
                }
            }

            SortedDictionary<double, uint> sellOrdersVolumesPerLevel = new SortedDictionary<double, uint>();
            foreach (Order sellOrder in buySellOrders.sellOrders)
            {
                if (sellOrdersVolumesPerLevel.ContainsKey(sellOrder.Price))
                {
                    sellOrdersVolumesPerLevel[sellOrder.Price] += sellOrder.Volume;
                }
                else
                {
                    sellOrdersVolumesPerLevel[sellOrder.Price] = sellOrder.Volume;
                }
            }
            var proxy = ServiceProxy.Create<IExchangeState>(new Uri("fabric:/FuturesExchange/ExchangeState"));
            await proxy.RegisterOrderBookSnapshot(new OrderBookSnapshot(book, buyOrdersVolumesPerLevel, sellOrdersVolumesPerLevel));
        }

        private async Task SendCancelOrderUpdate(CancelOrderRequest cancelOrderRequest)
        {
            var actorID = new ActorId(cancelOrderRequest.UserID);
            var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
            await proxy.RegisterOrderCancellation(cancelOrderRequest.OrderID, new CancellationToken());
        }

        private async Task<IReliableQueue<Order>> OrdersQueue(OrderSide orderSide)
        {
            return orderSide == OrderSide.BUY
                    ? await StateManager.GetOrAddAsync<IReliableQueue<Order>>(BUY_ORDERS_QUEUE)
                    : await StateManager.GetOrAddAsync<IReliableQueue<Order>>(SELL_ORDERS_QUEUE);
        }

        private async Task<BuySellOrders> GetAllOrdersFromOrderBook(ITransaction tx)
        {
            var orderBook = await this.StateManager.GetOrAddAsync<IReliableDictionary<String, List<Order>>>(ORDER_BOOK);
            List<Order> buyOrdersUnsorted = await orderBook.GetOrAddAsync(tx, BUY_ORDERS, []);
            List<Order> sellOrdersUnsorted = await orderBook.GetOrAddAsync(tx, SELL_ORDERS, []);
            return new BuySellOrders(buyOrdersUnsorted, sellOrdersUnsorted);
        }

        private async Task UpdateOrderBook(ITransaction tx, BuySellOrders buySellOrders)
        {
            var orderBook = await this.StateManager.GetOrAddAsync<IReliableDictionary<String, List<Order>>>(ORDER_BOOK);
            await orderBook.SetAsync(tx, BUY_ORDERS, buySellOrders.buyOrders);
            await orderBook.SetAsync(tx, SELL_ORDERS, buySellOrders.sellOrders);
        }

        private async Task<BuySellOrders> GetAllOrdersFromOrderQueues(ITransaction tx)
        {
            return new BuySellOrders(await GetAllOrdersFromOrderQueue(tx, OrderSide.BUY), await GetAllOrdersFromOrderQueue(tx, OrderSide.SELL));
        }

        private async Task<List<Order>> GetAllOrdersFromOrderQueue(ITransaction tx, OrderSide orderSide)
        {
            List<Order> ordersUnsorted = new List<Order>();
            var ordersQueue = await OrdersQueue(orderSide);
            var order = await ordersQueue.TryDequeueAsync(tx);
            while (order.HasValue)
            {
                ordersUnsorted.Add(order.Value);
                order = await ordersQueue.TryDequeueAsync(tx);
            }
            return ordersUnsorted;
        }

        private async Task<List<CancelOrderRequest>> GetAllCancelOrderRequestsFromQueue(ITransaction tx)
        {
            List<CancelOrderRequest> cancelOrderRequests = new List<CancelOrderRequest>();
            var cancelOrdersQueue = await StateManager.GetOrAddAsync<IReliableQueue<CancelOrderRequest>>(CANCEL_ORDERS_QUEUE);
            var cancelOrder = await cancelOrdersQueue.TryDequeueAsync(tx);
            while (cancelOrder.HasValue)
            {
                cancelOrderRequests.Add(cancelOrder.Value);
                cancelOrder = await cancelOrdersQueue.TryDequeueAsync(tx);
            }
            return cancelOrderRequests;
        }

        private List<Order> CreateSortedOrderList(ITransaction tx, List<Order> ordersFromOrderBook, List<Order> ordersFromOrderQueue, IComparer<Order> comparer)
        {
            List<Order> sortedOrders = new List<Order>();
            sortedOrders.AddRange(ordersFromOrderBook);
            sortedOrders.AddRange(ordersFromOrderQueue);
            sortedOrders.Sort(comparer);
            return sortedOrders;
        }

        private sealed record BuySellOrders(List<Order> buyOrders, List<Order> sellOrders);
    }
}
