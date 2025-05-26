using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using PublicExchangeDatamodel;

namespace FuturesExchangeClients
{
    public class Shared
    {

        private static readonly string SEND_ORDER_REQUEST_URL = "http://localhost:8188/trading/sendorderrequest";
        private static readonly string CANCEL_ORDER_REQUEST_ULR = "http://localhost:8188/trading/cancelorderrequest";
        private static readonly string GET_ALL_ORDERS_URL = "http://localhost:8188/trading/getallorders";
        private static readonly string CANCEL_ALL_ORDERS_URL = "http://localhost:8188/trading/cancelallorders";
        private static readonly string GET_PNL_URL = "http://localhost:8188/trading/getpnl";
        private static readonly string GET_TRADABLE_CITIES = "http://localhost:8448/exchangedata/gettradablecities";
        private static readonly string GET_ORDERBOOK_SNAPSHOT = "http://localhost:8448/exchangedata/getorderbooksnapshot";
        private static readonly string GET_CURRENT_TEMPERATURE = "http://localhost:8286/temperaturedata/getcurrentcitytemperature";

        internal static async Task<OrderStatusResponse> SendOrderRequest(HttpClient httpClient, OrderRequest orderRequest)
        {
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(orderRequest),
                Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync(SEND_ORDER_REQUEST_URL, jsonContent);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<OrderStatusResponse>(responseContent);
        }

        internal static async Task SendCancelOrderRequest(HttpClient httpClient, CancelOrderRequest cancelOrderRequest)
        {
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(cancelOrderRequest),
                Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync(CANCEL_ORDER_REQUEST_ULR, jsonContent);
            response.EnsureSuccessStatusCode();
        }

        internal static async Task SendCancelAllOrdersRequest(HttpClient httpClient, uint userID, string book)
        {
            var response = await httpClient.GetAsync($"{CANCEL_ALL_ORDERS_URL}?userid={userID}&book={book}");
            response.EnsureSuccessStatusCode();
        }

        internal static async Task<HashSet<OrderStatusResponse>> GetAllOrders(HttpClient httpClient, uint userID)
        {
            var response = await httpClient.GetAsync($"{GET_ALL_ORDERS_URL}?userid={userID}");
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<HashSet<OrderStatusResponse>>(responseContent);
        }

        internal static async Task<PnlResult> GetPnl(HttpClient httpClient, uint userID, string book)
        {
            var response = await httpClient.GetAsync($"{GET_PNL_URL}?userid={userID}&book={book}");
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<PnlResult>(responseContent);
        }

        internal static async Task<List<string>> GetTradableCities(HttpClient httpClient)
        {
            var response = await httpClient.GetAsync(GET_TRADABLE_CITIES);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<List<string>>(responseContent);
        }

        internal static async Task<OrderBookSnapshot> GetOrderBookSnapshot(HttpClient httpClient, string book)
        {
            var response = await httpClient.GetAsync($"{GET_ORDERBOOK_SNAPSHOT}?book={book}");
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<OrderBookSnapshot>(responseContent);
        }

        internal static async Task<CityTemperature> GetCurrentTemperature(HttpClient httpClient, string city)
        {
            var response = await httpClient.GetAsync($"{GET_CURRENT_TEMPERATURE}?city={city}");
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return Deserialize<CityTemperature>(responseContent);
        }

        internal static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}
