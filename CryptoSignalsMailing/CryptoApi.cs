using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System;

namespace CryptoSignalsMailing
{
    public static class CryptoApi
    {
        public static async Task<double> GetCoinPrice(HttpClient client, string currency)
        {
            var response = await client.GetAsync($"/data/price?fsym={currency}&tsyms=USD,EUR");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                return obj.USD.Value;
            }
            else
            {
                return 0;
            }
        }

        public static async Task<double> GetCoinPriceYesterday(HttpClient client, string currency)
        {
            var response = await client.GetAsync($"/data/histohour?fsym={currency}&tsym=USD&limit=6&aggregate=4");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                return obj.Data[0].open.Value;
            }
            else
            {
                return 0;
            }
        }

        public static async Task<double> GetMovingAverage(HttpClient client, string currency, int hours, int candleCount)
        {
            var response = await client.GetAsync($"/data/histohour?fsym={currency}&tsym=USD&limit={candleCount - 1}&aggregate={hours}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(json);

                var total = 0;
                var count = 0;

                foreach (var candle in obj.Data)
                {
                    total += candle.open.Value;
                    count++;
                }

                if (count > 0)
                {
                    double avg = total / count;
                    return Math.Round(avg, 4);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
