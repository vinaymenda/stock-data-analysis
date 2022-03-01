using System;
using System.Collections.Generic;
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
            return 100 * ( avgGain/ (avgGain + avgLoss));
        }

        // this.Close/Prev.Close > 1.9 OR this.Close/Prev.Close < 0.55 , highlight! 
    }
}

