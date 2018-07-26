using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using CryptoSignalsMailing.Models;

namespace CryptoSignalsMailing
{
    public static class FunctionSignals
    {
        static readonly HttpClient Client = new HttpClient();

        // On Azure UTC time is used (+2), so TimerTrigger 4,16 means 6h,18h
        // the first 5 means 5 minutes after the whole hour: so actually 6:05 and 18:05
        [FunctionName("FunctionSignals")]
        public static async Task Run([TimerTrigger("0 5 4,16 * * *")]TimerInfo myTimer, TraceWriter log)
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

            var coinsBitfinexShortable = new string[] { "BTC", "BTG", "DSH", "EOS", "ETC", "ETH", "IOT", "LTC", "NEO", "OMG", "XMR", "XRP", "ZEC" };

            var coins = new string[] { "ADA", "AION", "ARK", "BNB", "BTC", "BTG", "CLOAK", "DASH", "EOS", "ETC", "ETH", "GAS", "HT", "ICX", "IOT", "KCS",
            "LSK", "LTC", "NANO", "NEO", "OMG", "QTUM", "TRX", "VEN", "XLM", "XMR", "XRP", "ZEC", "ZIL" };

            var coinsInfoSignalUp = new List<MaSignalsModel>();
            var coinsInfoSignalDown = new List<MaSignalsModel>();
            var coinsSkipped = new List<string>();

            var chartInHours = 4;

            foreach (var coin in coins)
            {
                try
                {
                    var maInfo4h = await CryptoApi.GetMovingAverage(Client, coin, chartInHours, 21);
                    var priceYesterday = await CryptoApi.GetCoinPriceYesterday(Client, coin);
                    var priceCurrent = await CryptoApi.GetCoinPrice(Client, coin);

                    if (maInfo4h > 0 && priceCurrent > 0 && priceYesterday > 0)
                    {
                        var prc = ((priceCurrent - maInfo4h) / maInfo4h) * 100;
                        var roundedPrc = Math.Round(prc, 1);
                        var isShortable = Array.IndexOf(coinsBitfinexShortable, coin) > -1;

                        var coinMaSignal = new MaSignalsModel()
                        {
                            Name = coin,
                            MovingAverage = maInfo4h,
                            ChartInHours = chartInHours,
                            PriceCurrent = priceCurrent,
                            PriceYesterday = priceYesterday,
                            PercentageCurrentPriceToMa = roundedPrc,
                            IsShortable = isShortable
                        };

                        if (priceCurrent > maInfo4h && priceYesterday < maInfo4h)
                        {
                            coinsInfoSignalUp.Add(coinMaSignal);
                        }

                        if (priceCurrent < maInfo4h && priceYesterday > maInfo4h)
                        {
                            coinsInfoSignalDown.Add(coinMaSignal);
                        }
                    }
                    else
                    {
                        coinsSkipped.Add(coin);
                        log.Info($"API call failed for : {coin} ; Coin is skipped");
                    }
                }
                catch
                {
                    coinsSkipped.Add(coin);
                    log.Info($"Unexpected exception while processing : {coin} ; Coin is skipped");
                }
            }

            log.Info($"Up Signals found: {coinsInfoSignalUp.Count} ; Down Signals found: {coinsInfoSignalDown.Count}");

            if (coinsInfoSignalUp.Count > 0 || coinsInfoSignalDown.Count > 0 || coinsSkipped.Count > 0)
            {
                var plainText = string.Empty;
                var htmlText = string.Empty;

                if (coinsInfoSignalUp.Count > 0)
                {
                    plainText = "21Ma in 4h up (buy signal): "; 
                    htmlText = "<h3>Cross up 21 MA in 4h chart (BUY signal)</h3> ";

                    foreach (var coin in coinsInfoSignalUp)
                    {
                        plainText += $"{coin.Name}, ";
                        htmlText += $"<p style='color:Green'>{coin.Name}</p>";
                    }
                    htmlText += "<br>";
                }
                if (coinsInfoSignalDown.Count > 0)
                {
                    plainText += "21Ma in 4h down (sell signal): ";
                    htmlText = "<h3>Cross down 21 MA in 4h chart (SELL signal)</h3> ";

                    foreach (var coin in coinsInfoSignalDown)
                    {
                        plainText += $"{coin.Name}, ";
                        htmlText += $"<p style='color:Red'>{coin.Name}</p>";
                    }
                }
                if (coinsSkipped.Count > 0)
                {
                    plainText += "skipped: ";
                    htmlText = "<h3>Skipped</h3> ";

                    foreach (var coin in coinsSkipped)
                    {
                        plainText += $"{coin}, ";
                        htmlText += $"<p style='color:Grey'>{coin}</p>";
                    }
                }

                var subject = "Alert Moving Average signals";

                await SendGridMail.SendEmail("mvdruijt@gmail.com", "mvdr@dummy.nl", subject, plainText, htmlText);

                log.Info("SendEmail has been called");
            }
        }
    }
}
