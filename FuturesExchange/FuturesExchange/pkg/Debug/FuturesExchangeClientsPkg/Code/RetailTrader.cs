using PublicExchangeDatamodel;

namespace FuturesExchangeClients
{
    internal class RetailTrader : TradingClient
    {

        private readonly double spread;

        public RetailTrader(string book, uint userID) : base(book, userID)
        {
            this.spread = 0.3;
        }

        protected override double calculatePrice(OrderSide orderSide)
        {
            HttpClient httpClient = new HttpClient();
            CityTemperature cityTemperature = Shared.GetCurrentTemperature(httpClient, book).Result;
            if (orderSide == OrderSide.BUY)
            {
                return cityTemperature.Temperature + Random.Shared.NextDouble() * spread;
            }
            else
            {
                return cityTemperature.Temperature - Random.Shared.NextDouble() * spread;
            }
        }
    }
}
