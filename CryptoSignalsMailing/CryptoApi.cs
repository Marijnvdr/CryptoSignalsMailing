using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace CryptoSignalsMailing
{
    public static class CryptoApi
    {
        public static async Task<string> GetCoinPrice(HttpClient client, string currency)
        {
            var response = await client.GetAsync($"/data/price?fsym={currency}&tsyms=USD,EUR");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                return obj.USD.Value.ToString();
            }
            else
            {
                return "ERR";
            }
        }
    }
}
