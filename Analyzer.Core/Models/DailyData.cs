using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyzer.Core.Models
{
    public class DailyData
    {
        private int position;

        public DailyData(int position) { this.position = position; }

        public int Position => position;

        public DateTime? Date { get; set; }

        public decimal? Open { get; set; }

        public decimal? Low { get; set; }

        public decimal? Close { get; set; }

        public decimal? High { get; set; }

        public decimal? Volume { get; set; }

        public DailyData Previous { get; set; }

        public DailyData Next { get; set; }

        private decimal? change => this.Close - Previous?.Close;

        public decimal? Gain => change > 0 ? change : 0;

        public decimal? Loss => change < 0 ? change * -1 : 0;

        public decimal? GetAvgGain()
        {
            if (position < 15)
            {
                return null;
            }
            else if (position == 15)
            {
                decimal? gains = 0;
                DailyData prev = Previous;
                while (prev != null)
                {
                    if (prev.Gain != null) { gains += prev.Gain; }
                    prev = prev.Previous;
                }
                return gains / 14;
            }
            else
            {
                return (Previous.GetAvgGain() * 13 + Gain) / 14;
            }
        }

        public decimal? GetAvgLoss()
        {
            if (position < 15)
            {
                return null;
            }
            else if (position == 15)
            {
                decimal? losses = 0;
                DailyData previous = Previous;
                while (previous != null)
                {
                    if (previous.Loss != null) { losses += previous.Loss; }
                    previous = previous.Previous;
                }
                return losses / 14;
            }
            else
            {
                return (Previous.GetAvgLoss() * 13 + Loss) / 14;
            }
        }

        public decimal? GetRSI()
        {
            var avgGain = GetAvgGain();
            var avgLoss = GetAvgLoss();

            if (avgGain == 0) { return 0; }
            if (avgGain + avgLoss == 0) { return null; }
            return 100 * (avgGain / (avgGain + avgLoss));
        }

        private decimal? GetRSIChange()
            => this.GetRSI() - Previous?.GetRSI();

        public decimal? GetTurbulenceRatio(DailyData reference)
        {
            if (reference?.Next == null) { return null; }

            var eligiblePoints = new List<DailyData>() { this };
            var dp = reference.Next;
            while (dp != this)
            {
                eligiblePoints.Add(dp);
                dp = dp.Next;
            }

            var avgRSIChange = eligiblePoints.Average(p => p.GetRSIChange());
            var avgStockChange = eligiblePoints.Average(p => p.change);

            if (avgStockChange >= 0)
            {
                // +ve change in stock, check the ratio
                return Math.Abs((decimal)(avgStockChange != 0 ? avgRSIChange / avgStockChange : 0));
            }
            else
            {
                // -ve change in stock, only check if RSI has increased enough
                var thresholdRSI = 1.5m * reference.GetRSI();
                return (avgRSIChange >= thresholdRSI) ? decimal.MaxValue : decimal.MinValue;
            }
        }

        // this.Close/Prev.Close > 1.9 OR this.Close/Prev.Close < 0.55 , highlight! 
    }
}

