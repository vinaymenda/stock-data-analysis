using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyzer.Core.Models
{
    public class Wave
    {
        public Wave() { points = new List<DailyData>(); }

        private List<DailyData> points;
        
        public void Add(DailyData point)
        {
            points.Add(point);
        }

        public void Add(Wave wave)
        {
            points.AddRange(wave.points);
        }

        public decimal? Min() => points.Min(wp => wp.GetRSI());

        public decimal? Max() => points.Max(wp => wp.GetRSI());

        public DailyData Lowest() => points.FirstOrDefault(wp => wp.GetRSI() <= Min());

        public DailyData Highest() => points.FirstOrDefault(wp => wp.GetRSI() >= Max());

        public DailyData First() => points.OrderBy(p => p.Position).FirstOrDefault();

        public DailyData Last() => points.OrderBy(p => p.Position).LastOrDefault();
    }
}
