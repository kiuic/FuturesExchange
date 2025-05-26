using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PublicExchangeDatamodel;

namespace FuturesExchangeClients
{
    internal abstract class TradingClient
    {
        private static readonly uint SLEEP_TIMER_MS = 2000;
        private static readonly int DEFAULT_VOLUME = 3;
        private static readonly double CANCEL_ORDER_PROBABILITY = 0.8;

        protected readonly string book;
        protected readonly uint userID;

        public TradingClient(string book, uint userID)
        {
            this.book = book;
            this.userID = userID;
        }

        public async Task run()
        {
            using (var httpClient = new HttpClient())
            {
                while (true)
                {
                    OrderBookSnapshot orderBookState = Shared.GetOrderBookSnapshot(httpClient, book).Result;
                    registerOrderBookState(orderBookState);

                    if (shouldCancelAllOrders())
                    {
                        Shared.SendCancelAllOrdersRequest(httpClient, userID, book).Wait();
                        continue;
                    }

                    if (!shouldSendOrderRequest())
                    {
                        continue;
                    }

                    OrderSide orderSide = this.getOrderSide();
                    double price = calculatePrice(orderSide);
                    uint volume = getVolume();
                    OrderRequest orderRequest = new OrderRequest(this.book, OrderType.LIMIT, orderSide, price, volume, this.userID);
                    Shared.SendOrderRequest(httpClient, orderRequest).Wait();

                    await sleep();
                }
            }
        }

        protected virtual OrderSide getOrderSide()
        {
            return Random.Shared.NextDouble() > 0.5 ? OrderSide.BUY : OrderSide.SELL;
        }

        protected virtual uint getVolume()
        {
            return (uint)(Random.Shared.Next(DEFAULT_VOLUME) + 1);
        }

        protected virtual Boolean shouldCancelAllOrders()
        {
            return Random.Shared.NextDouble() < CANCEL_ORDER_PROBABILITY;
        }

        protected virtual async Task sleep()
        {
            Thread.Sleep((int)(Random.Shared.NextDouble() * SLEEP_TIMER_MS + SLEEP_TIMER_MS));
        }

        protected virtual void registerOrderResponse(OrderStatusResponse orderStatusResponse)
        {
            Console.WriteLine(orderStatusResponse);
        }

        protected virtual void registerOrderBookState(OrderBookSnapshot orderBookState)
        {
            Console.WriteLine(orderBookState);
        }

        protected virtual bool shouldSendOrderRequest()
        {
            return true;
        }
        protected abstract double calculatePrice(OrderSide orderSide);

    }
}
