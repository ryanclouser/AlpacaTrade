using System;
using System.Collections.Generic;
using System.Linq;
using TicTacTec.TA.Library;
using static AlpacaTrade.Bar;

namespace AlpacaTrade
{
    class TA
    {
        public static double[] ATR(List<Candle> results, int period = 20)
        {
            int begin, element;

            var high = new double[results.Count];
            var low = new double[results.Count];
            var close = new double[results.Count];

            for (int x = 0; x < results.Count; ++x)
            {
                high[x] = (double)results[x].High;
                low[x] = (double)results[x].Low;
                close[x] = (double)results[x].Close;
            }

            double[] result = new double[close.Length];
            Core.Atr(0, close.Length - 1, high, low, close, period, out begin, out element, result);

            double[] atr = new double[element];
            Array.Copy(result, 0, atr, 0, element);

            // reverse the order to make things easier
            atr.Reverse();

            return atr;
        }
    }
}