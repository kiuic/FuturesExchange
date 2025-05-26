using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using PublicExchangeDatamodel;
using TradingAgent.Interfaces;

namespace TradingAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TradingController : ControllerBase
    {
        [HttpGet]
        [Route("GetAllOrders")] 
        public async Task<HashSet<OrderStatusResponse>> GetOrderResponses([FromQuery] uint userID)
        {
            var actorID = new ActorId(userID);
            var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
            return await proxy.GetAllOrders(new CancellationToken());
        }

        [HttpGet]
        [Route("GetPnl")]
        public async Task<PnlResult> GetPnl([FromQuery] uint userID, [FromQuery] string book)
        {
            var actorID = new ActorId(userID);
            var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
            return await proxy.GetPnl(book, new CancellationToken());
        }

        [HttpPost]
        [Route("SendOrderRequest")]
        public async Task<OrderStatusResponse> SendOrderRequest([FromBody] OrderRequest orderRequest)
        {
            try
            {
                orderRequest.Price = Math.Round(orderRequest.Price, 2);
                var actorID = new ActorId(orderRequest.UserID);
                var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
                return await proxy.SendOrderRequest(orderRequest, new CancellationToken());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendOrderRequest: {ex.Message}");
                return new OrderStatusResponse(
                    OrderStatus.ERROR,
                    orderRequest.UserID,
                    999999,
                    orderRequest.Book,
                    orderRequest.OrderType,
                    orderRequest.OrderSide,
                    orderRequest.Price,
                    orderRequest.Volume,
                    0,
                    0);
            }
        }

        [HttpPost]
        [Route("CancelOrderRequest")]
        public async Task CancelOrderRequest([FromBody] CancelOrderRequest cancelOrderRequest)
        {
            try
            {
                var actorID = new ActorId(cancelOrderRequest.UserID);
                var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
                await proxy.SendCancelOrderRequest(cancelOrderRequest, new CancellationToken());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CancelOrderRequest: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("CancelAllOrders")]
        public async Task CancelOrderRequest([FromQuery] uint userID, [FromQuery] string book)
        {
            try
            {
                var actorID = new ActorId(userID);
                var proxy = ActorProxy.Create<ITradingAgent>(actorID, new Uri("fabric:/FuturesExchange/TradingAgentActorService"));
                await proxy.SendCancelAllOrders(book, new CancellationToken());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CancelOrderRequest: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("AddTradingAgent")]
        public async Task<IActionResult> AddTradingAgent([FromBody] CreateTradingClientRequest createTradingClientRequest)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var serializedObj = Serialize<CreateTradingClientRequest>(createTradingClientRequest);
                    var jsonContent = new StringContent(serializedObj, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("http://localhost:8123", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendCreateTradingClientRequest: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        internal static string Serialize<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
