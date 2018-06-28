using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CryptoSignalsMailing
{
    public static class FunctionSignals
    {
        static readonly HttpClient Client = new HttpClient();

        [FunctionName("FunctionSignals")]
        public static async Task Run([TimerTrigger("0 5 6,10,14,18,22 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            // for testing use this cron expression (every minute): "0 */1 * * * *"

            log.Info($"My Crypto Signal trigger function executed at: {DateTime.Now}");

            // Setup of HttpClient only needed when function is running for the first time.
            if (Client.BaseAddress == null)
            {  
                Client.BaseAddress = new Uri("https://min-api.cryptocompare.com");
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var maInfo4h = await CryptoApi.GetMovingAverage(Client, "BTC", 4, 21);
            var priceYesterday = await CryptoApi.GetCoinPriceYesterday(Client, "BTC");
            var priceCurrent = await CryptoApi.GetCoinPrice(Client, "BTC");

            var prc = ((priceCurrent - maInfo4h) / maInfo4h) * 100;

            var roundedPrc = Math.Round(prc, 1);

            var text = $"API called, BTC price: {priceCurrent} ; yesterday: {priceYesterday} ; 21MA in 4h: {maInfo4h} ; Prc: {roundedPrc}";

            log.Info(text);

            var subject = "Alert BTC price";

            await SendGridMail.SendEmail("mvdruijt@gmail.com", "mvdr@dummy.nl", subject, text);

            log.Info("SendEmail has been called");
        }
    }
}
