using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PublicExchangeDatamodel;

namespace TemperatureData.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TemperatureDataController : ControllerBase
    {
        // VERY BAD PRACTICE :(
        private static readonly string API_KEY = "596939d60emsh17dede5c0ed3951p18cf6djsn7674766f4fde";
        private static readonly string API_HOST = "cities-temperature.p.rapidapi.com";
        
        private static readonly ConcurrentDictionary<string, CityTemperature> cityTemperatures = new ConcurrentDictionary<string, CityTemperature>();

        [HttpGet]
        [Route("GetCurrentCityTemperature")]
        public async Task<CityTemperature> GetCurrentCityTemperature([FromQuery] string city)
        {
            if (cityTemperatures.ContainsKey(city))
            {
                return cityTemperatures[city];
            }

            CityTemperature cityTemperature = await getTemperature(city);
            cityTemperatures[city] = cityTemperature;
            Timer timer = new Timer(_ =>
            {
                cityTemperatures.Remove(city, out CityTemperature removed);
            }, null, 1000 * 1000, Timeout.Infinite);
            return cityTemperature;
        }

        private async Task<CityTemperature> getTemperature(string city)
        {

            // Mock API
            if (true)
            {
                return new CityTemperature(city, Random.Shared.NextDouble() * 20.0);
            }

            using HttpClient client = new HttpClient();
            string url = $"https://cities-temperature.p.rapidapi.com/weather/v1/current?location={city}";

            client.DefaultRequestHeaders.Add("x-rapidapi-host", API_HOST);
            client.DefaultRequestHeaders.Add("x-rapidapi-key", API_KEY);

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;
            CityTemperature cityTemperature = new CityTemperature(city, Math.Round(double.Parse(root.GetProperty("temperature").ToString()), 2));
            return cityTemperature;
        }
    }
}
