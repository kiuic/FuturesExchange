using InternalExchangeDatamodel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using PublicExchangeDatamodel;

namespace ExchangeDataAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeDataController : ControllerBase
    {
        [HttpGet]
        [Route("GetOrderBookSnapshot")]
        public async Task<OrderBookSnapshot> GetOrderBookSnapshot([FromQuery] string book)
        {
            var proxy = ServiceProxy.Create<IExchangeState>(new Uri("fabric:/FuturesExchange/ExchangeState"));
            return await proxy.GetOrderBookSnapshot(book);
        }

        [HttpGet]
        [Route("GetTradableCities")]
        public async Task<List<string>> GetTradableCities()
        {
            var proxy = ServiceProxy.Create<IExchangeState>(new Uri("fabric:/FuturesExchange/ExchangeState"));
            return await proxy.GetTradableCities();
        }
    }
}
