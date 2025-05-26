using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TradingAgent.Interfaces;
using PublicExchangeDatamodel;
using System.Runtime.ConstrainedExecution;
using InternalExchangeDatamodel;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TradingAgent
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class TradingAgent : Actor, ITradingAgent
    {

        private readonly static string CURRENT_ORDER_ID = "CURRENT_ORDER_ID";
        private readonly static string ORDER_STATUSES = "ORDER_STATUSES";

        public TradingAgent(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        public async Task<OrderStatusResponse> SendOrderRequest(OrderRequest orderRequest, CancellationToken cancellationToken)
        {
            orderRequest.Price = Math.Round(orderRequest.Price, 2);
            uint currentOrderId = await this.StateManager.GetStateAsync<uint>(CURRENT_ORDER_ID, cancellationToken);
            HashSet<OrderStatusResponse> orderResponses = await this.StateManager.GetStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES, cancellationToken);
            OrderStatusResponse orderResponse = CreateOrderStatusResponse(orderRequest, currentOrderId, OrderStatus(orderRequest, orderResponses));
            if (orderResponse.OrderStatus == PublicExchangeDatamodel.OrderStatus.ACTIVE)
            {
                await SendOrder(orderRequest, currentOrderId);
            }
            orderResponses.Add(orderResponse);
            await this.StateManager.SetStateAsync(CURRENT_ORDER_ID, currentOrderId + 1);
            await this.StateManager.SetStateAsync(ORDER_STATUSES, orderResponses);
            await this.StateManager.SaveStateAsync(cancellationToken);
            return orderResponse;
        }

        public async Task SendCancelOrderRequest(CancelOrderRequest cancelOrderRequest, CancellationToken cancellationToken)
        {
            ServicePartitionKey partitionKey = new ServicePartitionKey(cancelOrderRequest.Book.ToUpperInvariant());
            var matchingEngineProxy = ServiceProxy.Create<IMatchingEngine>(new Uri("fabric:/FuturesExchange/MatchingEngine"), partitionKey);
            await matchingEngineProxy.CancelOrder(cancelOrderRequest);
        }

        public async Task SendCancelAllOrders(string book, CancellationToken cancellationToken)
        {
            HashSet<OrderStatusResponse> orderResponses = await this.StateManager.GetStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES, cancellationToken);
            foreach (var order in orderResponses)
            {
                if (order.Book == book && order.OrderStatus == PublicExchangeDatamodel.OrderStatus.ACTIVE)
                {
                    var cancelOrderRequest = new CancelOrderRequest(book, order.UserID, order.OrderID);
                    await SendCancelOrderRequest(cancelOrderRequest, cancellationToken);
                }
            }
        }

        public async Task<HashSet<OrderStatusResponse>> GetAllOrders(CancellationToken cancellationToken)
        {
            return await this.StateManager.GetStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES);
        }

        public async Task RegisterTrade(Trade trade, CancellationToken cancellationToken)
        {
            var userID = this.GetActorId().GetLongId();
            var orderID = Convert.ToInt64(trade.BuyUserID) == userID ? trade.BuyOrderID : trade.SellOrderID;

            HashSet<OrderStatusResponse> orderResponses = await this.StateManager.GetStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES, cancellationToken);
            OrderStatusResponse orderStatusResponse = orderResponses.First(n => n.UserID == userID && n.OrderID == orderID);
            orderResponses.Remove(orderStatusResponse);

            orderStatusResponse.AverageFilledPrice = (orderStatusResponse.AverageFilledPrice * orderStatusResponse.FilledVolume + trade.Price * trade.Volume) / (orderStatusResponse.FilledVolume + trade.Volume);
            orderStatusResponse.FilledVolume += trade.Volume;
            if (orderStatusResponse.FilledVolume == orderStatusResponse.Volume)
            {
                orderStatusResponse.OrderStatus = PublicExchangeDatamodel.OrderStatus.FILLED;
            }

            orderResponses.Add(orderStatusResponse);
            await this.StateManager.SetStateAsync(ORDER_STATUSES, orderResponses);
            await this.StateManager.SaveStateAsync(cancellationToken);
        }

        public async Task RegisterOrderCancellation(uint orderID, CancellationToken cancellationToken)
        {
            var userID = this.GetActorId().GetLongId();
            HashSet<OrderStatusResponse> orderResponses = await this.StateManager.GetStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES, cancellationToken);
            OrderStatusResponse orderStatusResponse = orderResponses.First(n => n.UserID == userID && n.OrderID == orderID);
            orderResponses.Remove(orderStatusResponse);
            orderStatusResponse.OrderStatus = PublicExchangeDatamodel.OrderStatus.CANCELLED;
            orderResponses.Add(orderStatusResponse);
            await this.StateManager.SetStateAsync(ORDER_STATUSES, orderResponses);
            await this.StateManager.SaveStateAsync(cancellationToken);
        }

        public async Task<PnlResult> GetPnl(string book, CancellationToken cancellationToken)
        {
            HashSet<OrderStatusResponse> allOrders = await GetAllOrders(cancellationToken);
            var ordersInBook = allOrders.Where(n => n.Book == book);
            uint totalLongVolume = 0;
            uint totalShortVolume = 0;
            double totalDebit = 0.0;
            double totalCredit = 0.0;
            foreach (var order in ordersInBook)
            {
                if (order.OrderSide == OrderSide.BUY)
                {
                    totalLongVolume += order.FilledVolume;
                    totalDebit += order.AverageFilledPrice * order.FilledVolume;
                }
                else
                {
                    totalShortVolume += order.FilledVolume;
                    totalCredit += order.AverageFilledPrice * order.FilledVolume;
                }
            }
            double signedOutstandingVolume = (double) totalLongVolume - (double) totalShortVolume;
            CityTemperature cityTemperature = await GetCityTemperature(book);
            double impliedPnl = signedOutstandingVolume * cityTemperature.Temperature + totalCredit - totalDebit;

            return new PnlResult(book, totalLongVolume, totalShortVolume, signedOutstandingVolume, totalDebit, totalCredit, impliedPnl);
        }

        protected async override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, $"Trading Agent for userID: {this.GetActorId()} activated.");
            await this.StateManager.TryAddStateAsync<uint>(CURRENT_ORDER_ID, 0);
            await this.StateManager.TryAddStateAsync<HashSet<OrderStatusResponse>>(ORDER_STATUSES, []);
            await this.StateManager.SaveStateAsync();
        }

        private OrderStatus OrderStatus(OrderRequest order, HashSet<OrderStatusResponse> orderStatusResponses)
        {
            return PublicExchangeDatamodel.OrderStatus.ACTIVE;
            //if (order.OrderSide == OrderSide.BUY)
            //{
            //    return orderStatusResponses.Any<OrderStatusResponse>(orderStatusResponse => orderStatusResponse.OrderSide == OrderSide.SELL && orderStatusResponse.Price <= order.Price && orderStatusResponse.OrderStatus == PublicExchangeDatamodel.OrderStatus.ACTIVE) 
            //        ? PublicExchangeDatamodel.OrderStatus.ERROR 
            //        : PublicExchangeDatamodel.OrderStatus.ACTIVE;
            //} else
            //{
            //    return orderStatusResponses.Any<OrderStatusResponse>(orderStatusResponse => orderStatusResponse.OrderSide == OrderSide.BUY && orderStatusResponse.Price >= order.Price && orderStatusResponse.OrderStatus == PublicExchangeDatamodel.OrderStatus.ACTIVE)
            //        ? PublicExchangeDatamodel.OrderStatus.ERROR
            //        : PublicExchangeDatamodel.OrderStatus.ACTIVE;
            //}
        }

        private OrderStatusResponse CreateOrderStatusResponse(OrderRequest orderRequest, uint orderId, OrderStatus orderStatus)
        {
            return new OrderStatusResponse(orderStatus,
                orderRequest.UserID,
                orderId,
                orderRequest.Book,
                orderRequest.OrderType,
                orderRequest.OrderSide,
                orderRequest.Price,
                orderRequest.Volume,
                0,
                0.0);
        }

        private async Task<MatchingEngineResponse> SendOrder(OrderRequest orderRequest, uint orderID) {
            Order order = new Order( orderRequest.Book, 
                orderRequest.OrderType,
                orderRequest.OrderSide, 
                orderRequest.Price, 
                orderRequest.Volume, 
                orderRequest.UserID, 
                orderID);
            ServicePartitionKey partitionKey = new ServicePartitionKey(orderRequest.Book.ToUpperInvariant());
            var matchingEngineProxy = ServiceProxy.Create<IMatchingEngine>(new Uri("fabric:/FuturesExchange/MatchingEngine"), partitionKey);
            return await matchingEngineProxy.SendOrder(order);
        }

        private async Task<CityTemperature> GetCityTemperature(string city)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"http://localhost:8286/temperaturedata/getcurrentcitytemperature?city={city}");
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                return Deserialize<CityTemperature>(responseContent);
            }
        }

        private static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}
