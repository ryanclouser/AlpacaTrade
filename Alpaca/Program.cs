using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Alpaca.Markets;
using static AlpacaTrade.Bar;

namespace AlpacaTrade
{
    class Program
    {
        class Settings
        {
            public string AlpacaPaperKey = string.Empty;
            public string AlpacaPaperSecret = string.Empty;

            public string AlpacaKey = string.Empty;
            public string AlpacaSecret = string.Empty;
            public bool Paper = true;

            public List<string> Symbols = new List<string>();
            public long PositionSize = 1;
            public long MaxDayTrades = 3;
        }

        static Settings settings;
        static AlpacaTradingClient trading;
        static RestSharp.RestClient client = new RestSharp.RestClient("https://api.polygon.io/v2");

        static async Task WaitMarketOpen()
        {
            while (!(await trading.GetClockAsync()).IsOpen)
            {
                await Task.Delay(60000);
            }
        }

        static async Task<List<IOrder>> GetOrders(string symbol)
        {
            var orders = await trading.ListOrdersAsync(OrderStatusFilter.Open);
            return new List<IOrder>(orders.Where(o => o.Symbol == symbol));
        }

        static async Task<IPosition> GetPosition(string symbol)
        {
            var positions = await trading.ListPositionsAsync();
            return positions.FirstOrDefault(p => p.Symbol == symbol);
        }

        static async Task CancelOrders(List<IOrder> orders)
        {
            foreach (var o in orders)
            {
                await trading.DeleteOrderAsync(o.OrderId);
            }
        }

        public static List<Candle> GetPolygonBars(string symbol, string timespan, DateTime from, DateTime to)
        {
            var request = new RestRequest("/aggs/ticker/{ticker}/range/{multiplier}/{timespan}/{from}/{to}", Method.GET);
            request.AddHeader("Content-Type", "application/json");

            request.Timeout = 5000;
            request.ReadWriteTimeout = 5000;

            request.AddUrlSegment("ticker", symbol);
            request.AddUrlSegment("multiplier", 1);
            request.AddUrlSegment("timespan", timespan);
            request.AddUrlSegment("from", from.ToString("yyyy-MM-dd"));
            request.AddUrlSegment("to", to.ToString("yyyy-MM-dd"));

            request.AddQueryParameter("apiKey", settings.AlpacaKey);    // use the live key for real time data

            var response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var candles = new List<Candle>();

                JObject o = JObject.Parse(response.Content);

                if ((string)o["status"] == "OK")
                {
                    foreach (JObject c in (JArray)o["results"])
                    {
                        var next = new Candle();
                        next.Open = (decimal)c["o"];
                        next.High = (decimal)c["h"];
                        next.Low = (decimal)c["l"];
                        next.Close = (decimal)c["c"];
                        next.Volume = (long)c["v"];
                        next.Date = Date.UnixTimeStampToDateTime((long)c["t"] / 1000);
                        candles.Add(next);
                    }

                    // oldest to newest
                    candles.Sort((x, y) => x.Date.CompareTo(y.Date));
                    return candles;
                }
            }

            return null;
        }

        static async Task Update(string symbol, IAccount account, IPosition position, List<IOrder> orders, List<Candle> candles)
        {
            // hourly candles
            var hourly = Resample(candles, 3600);

            // must calculate indicators before reversing
            var atr = TA.ATR(hourly, 20);

            // reverse the order to make things easier
            hourly.Reverse();

            // no position
            if (position == null)
            {
                // no active orders
                if (orders.Count == 0)
                {
                    // first candle of the day closed down
                    if (hourly[1].Date.Hour == 9 && hourly[1].Close < hourly[1].Open)
                    {
                        // entry price
                        decimal entry = hourly[1].Low;

                        // make sure there is enough cash to take the trade
                        if (account.TradableCash >= settings.PositionSize * entry)
                        {
                            // make sure we do not violate the PDT rules
                            if (settings.MaxDayTrades == 0 || account.DayTradeCount < settings.MaxDayTrades)
                            {
                                Console.WriteLine("[{0}] Placing buy order for ${1} at ${2}", DateTime.Now, symbol, entry);

                                // buy limit at low
                                await trading.PostOrderAsync(symbol, settings.PositionSize, OrderSide.Buy, OrderType.Limit, TimeInForce.Day, entry);
                            }
                        }
                    }
                }
            }
            // filled
            else
            {
                // profitable (up half an ATR)
                if (position.AssetCurrentPrice > position.CostBasis + (decimal)(atr[1] * 0.5))
                {
                    Console.WriteLine("[{0}] Selling ${1} position at ${2}", DateTime.Now, symbol, position.AssetCurrentPrice);

                    if (orders.Count > 0)
                    {
                        // cancel existing orders
                        await CancelOrders(orders);

                        // must wait a few seconds otherwise order errors may occur
                        await Task.Delay(2000);
                    }

                    // liquidate
                    await trading.DeletePositionAsync(symbol);
                }
            }
        }

        static async Task Main(string[] args)
        {
            bool created = false;

            using (var m = new Mutex(true, "AlpacaTrade", out created))
            {
                if (!created)
                    return;

                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"settings.json"));
                }
                catch (Exception)
                {
                    settings = new Settings();
                    File.WriteAllText(@"settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));

                    Console.WriteLine("Defaults have been saved to 'settings.json'");
                    Console.ReadKey();
                    return;
                }

                var c = new AlpacaTradingClientConfiguration();
                c.KeyId = settings.Paper ? settings.AlpacaPaperKey : settings.AlpacaKey;
                c.SecurityId = new SecretKey(settings.Paper ? settings.AlpacaPaperSecret : settings.AlpacaSecret);
                c.ApiEndpoint = new Uri(settings.Paper ? "https://paper-api.alpaca.markets" : "https://api.alpaca.markets");
                c.ApiVersion = ApiVersion.V2;

                trading = new AlpacaTradingClient(c);

                while (true)
                {
                    await WaitMarketOpen();

                    foreach (var symbol in settings.Symbols)
                    {
                        var account = await trading.GetAccountAsync();

                        if (!account.IsTradingBlocked)
                        {
                            var orders = await GetOrders(symbol);
                            var position = await GetPosition(symbol);

                            var candles = GetPolygonBars(symbol, "minute", DateTime.Now.AddDays(-7), DateTime.Now.AddDays(1));

                            if (candles != null)
                            {
                                await Update(symbol, account, position, orders, candles);
                            }
                        }
                    }

                    await Task.Delay(10000);
                }
            }
        }
    }
}