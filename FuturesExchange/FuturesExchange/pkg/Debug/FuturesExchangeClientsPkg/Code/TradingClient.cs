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
                    await sleep();
                    if (shouldCancelAllOrder())
                    {
                        Shared.SendCancelAllOrdersRequest(httpClient, userID, book).Wait();
                    }
                    OrderSide orderSide = this.orderSide();
                    OrderRequest orderRequest = new OrderRequest(this.book, OrderType.LIMIT, orderSide, calculatePrice(orderSide), getVolume(), this.userID);
                    Shared.SendOrderRequest(httpClient, orderRequest).Wait();
                }
            }
        }


        protected OrderSide orderSide()
        {
            return Random.Shared.NextDouble() > 0.5 ? OrderSide.BUY : OrderSide.SELL;
        }

        protected virtual uint getVolume()
        {
            return (uint)(Random.Shared.Next(3) + 1);
        }

        protected virtual Boolean shouldCancelAllOrder()
        {
            return Random.Shared.NextDouble() > 0.8;
        }

        protected async Task sleep()
        {
            Thread.Sleep((int)(Random.Shared.NextDouble() * SLEEP_TIMER_MS + SLEEP_TIMER_MS));
        }

        abstract protected double calculatePrice(OrderSide orderSide);

    }
}
