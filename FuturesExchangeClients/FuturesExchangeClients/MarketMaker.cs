using PublicExchangeDatamodel;
using System;

namespace FuturesExchangeClients
{
    internal class MarketMaker : TradingClient
    {

        private readonly double spread;

        public MarketMaker(string book, uint userID) : base(book, userID)
        {
            this.spread = 0.3;
        }

        protected override double calculatePrice(OrderSide orderSide)
        {
            HttpClient httpClient = new HttpClient();
            CityTemperature cityTemperature = Shared.GetCurrentTemperature(httpClient, book).Result;
            if (orderSide == OrderSide.BUY)
            {
                return cityTemperature.Temperature - Random.Shared.NextDouble() * spread;
            } 
            else
            {
                return cityTemperature.Temperature + Random.Shared.NextDouble() * spread;
            }
        }

        protected override uint getVolume()
        {
            return (uint)(Random.Shared.Next(10) + 1);
        }

        protected override Boolean shouldCancelAllOrders()
        {
            return Random.Shared.NextDouble() > 0.999;
        }
    }
}
