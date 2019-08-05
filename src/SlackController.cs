using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlackCryptoPriceBot
{
    public class SlackController : Controller
    {
        private static HttpClient _httpClient = new HttpClient();

        private string cryptoCompareApiKey = "{your-crypto-compare-api-key-here}";

        [HttpPost]
        [Route("api/slack/commands")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> ReceiveSlackSlashCommand([FromForm] IDictionary<string, string> slackParameters)
        {
            var responseUrl = slackParameters["response_url"];
            var text = slackParameters["text"];

            var parts = text.Split(" ");

            if (parts.Length == 3)
            {
                var cryptoSymbol = parts[0].ToUpper(); // extract the crypto symbol, e.g. BTC, ETH, LTC
                var targetCurrency = parts[2].ToUpper(); // extract the currency

                // Call the cryptocompare API
                var responseMessage = await _httpClient.GetAsync(
                    $"https://min-api.cryptocompare.com/data/price?fsym={cryptoSymbol}&tsyms={targetCurrency}&api_key={cryptoCompareApiKey}");

                // read the response
                var content = await responseMessage.Content.ReadAsStringAsync();

                // parse the json
                var jToken = JToken.Parse(content);

                var price = jToken[targetCurrency].Value<decimal>();

                // create slack message
                var slackMessage = new
                {
                    blocks = new[]
                    {
                        new
                        {
                            type = "section",
                            text = new
                            {
                                type = "mrkdwn",
                                text = $":chart_with_upwards_trend: *1 {cryptoSymbol}* currently equals *{price:N} {targetCurrency}*"
                            }
                        }

                    }
                };

                
                // send to slack
                var httpResponseMessage = await _httpClient.PostAsync(responseUrl,
                    new StringContent(JsonConvert.SerializeObject(slackMessage), Encoding.UTF8, "application/json"));


            }
            else
            {
                // create slack message
                var slackMessage = new
                {
                    blocks = new[]
                    {
                        new
                        {
                            type = "section",
                            text = new
                            {
                                type = "mrkdwn",
                                text = $"Need help? Here's an example of how to use the command: \n\n`/cryptoprice btc in usd`"
                            }
                        }

                    }
                };

                // send to slack
                var httpResponseMessage = await _httpClient.PostAsync(responseUrl,
                    new StringContent(JsonConvert.SerializeObject(slackMessage), Encoding.UTF8, "application/json"));
            }

            return new OkResult();
        }

    }
}
