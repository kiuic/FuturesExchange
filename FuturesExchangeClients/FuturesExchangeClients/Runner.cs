using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PublicExchangeDatamodel;

namespace FuturesExchangeClients
{
    internal class Runner
    {

        private static readonly Dictionary<uint, Dictionary<string, TradingClient>> tradingClients = new Dictionary<uint, Dictionary<string, TradingClient>>();
        private static readonly Dictionary<uint, Dictionary<string, Thread>> tradingClientThreads = new Dictionary<uint, Dictionary<string, Thread>>();

        static void Main()
        {
            process();
            while(true)
            {
                // La Bleyage
            }
        }

        static async Task process()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:8123/");
            listener.Start();
            Console.WriteLine("Server is listening on http://localhost:8123/...");

            while (true)
            {

                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string requestBody = await reader.ReadToEndAsync();
                        CreateTradingClientRequest createTradingClientRequest = Shared.Deserialize<CreateTradingClientRequest>(requestBody);
                        response.StatusCode = (int)CreateTradingClient(createTradingClientRequest);
                        response.Close();
                    }
                }
            }
        }

        static HttpStatusCode CreateTradingClient(CreateTradingClientRequest request)
        {

            uint userID = request.UserID;
            if (tradingClients.ContainsKey(userID))
            {
                return HttpStatusCode.Conflict;
            }

            tradingClients.Add(userID, new Dictionary<string, TradingClient>());
            tradingClientThreads.Add(userID, new Dictionary<string, Thread>());

            List<String> tradableCities = Shared.GetTradableCities(new HttpClient()).Result;

            foreach (string tradableCity in tradableCities)
            {
                TradingClient tradingClient = request.GetType().Equals("M") ? new MarketMaker(tradableCity, userID) : new RetailTrader(tradableCity, userID);
                Thread thread = new Thread(() => tradingClient.run());
                tradingClients[userID].Add(tradableCity, tradingClient);
                tradingClientThreads[userID].Add(tradableCity, thread);
                thread.Start();
            }
            return HttpStatusCode.OK;
        }
    }
}
