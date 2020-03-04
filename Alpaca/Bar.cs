using System;
using System.Collections.Generic;
using Alpaca.Markets;

namespace AlpacaTrade
{
    public class Bar
    {
        public class Candle : ICloneable
        {
            public DateTime Date { get; set; }
            public Decimal Open { get; set; }
            public Decimal High { get; set; }
            public Decimal Low { get; set; }
            public Decimal Close { get; set; }
            public long Volume { get; set; }

            public Decimal Median()
            {
                return Math.Abs((Open + Close) / 2);
            }

            public Decimal Mean()
            {
                return (Open + High + Low + Close) / 4;
            }

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        public static List<Candle> ConvertAlpacaBars(List<IAgg> bars)
        {
            var results = new List<Candle>();

            foreach (var c in bars)
            {
                results.Add(new Candle { Date = Date.UnixTimeToEastern(c.Time), Open = c.Open, High = c.High, Low = c.Low, Close = c.Close, Volume = c.Volume });
            }

            return results;
        }

        public static List<Candle> Resample(List<Candle> candles, int granularity)
        {
            if (granularity < 60)
            {
                throw (new Exception("Minimum granularity is 60s"));
            }

            if ((granularity % 60) != 0)
            {
                throw (new Exception("Granularity must be a multiple of 60"));
            }

            var results = new List<Candle>();

            Candle last = null;

            foreach (var c in candles)
            {
                var dt = c.Date;
                dt = dt.AddSeconds(-dt.Second).AddMilliseconds(-dt.Millisecond);
                dt = dt.AddMinutes(-(dt.Minute % (granularity / 60)));

                if (last == null || last.Date != dt)
                {
                    last = new Candle { Date = dt, Open = c.Open, High = c.High, Low = c.Low, Close = c.Close, Volume = c.Volume };
                    results.Add(last);
                }
                else
                {
                    if (c.High > last.High)
                    {
                        last.High = c.High;
                    }
                    if (c.Low < last.Low)
                    {
                        last.Low = c.Low;
                    }

                    last.Close = c.Close;
                    last.Volume += c.Volume;
                }
            }

            return results;
        }
    }
}