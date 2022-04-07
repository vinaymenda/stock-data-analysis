using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyzer
{
    public class ReferenceService
    {
        private IEnumerable<DailyData> _dataPoints;
        private IEnumerable<Wave> _waves;

        public ReferenceService(IEnumerable<DailyData> points)
        {
            _dataPoints = points;
            _waves = FindWaves(_dataPoints.OrderBy(dt => dt.Date).FirstOrDefault(dt => dt.Date >= DateTime.Today.AddMonths(-3)));
        }

        List<Wave> FindWaves(DailyData start)
        {
            var waves = new List<Wave>();

            var point = start;
            var wave = FindWave(start);
            while (wave != null)
            {
                waves.Add(wave);
                point = wave.Last().Next;
                wave = FindWave(point);
            }

            return waves;
        }

        Wave FindWave(DailyData item)
        {
            var dataPoint = item;
            while (dataPoint != null && dataPoint.GetRSI() > 25) { dataPoint = dataPoint.Next; }

            if (dataPoint != null)
            {
                // wave started for RSI
                var wave = new Wave();
                while (dataPoint != null && dataPoint.GetRSI() <= 25) { wave.Add(dataPoint); dataPoint = dataPoint.Next; }

                // wave has completed but we need to ensure there are at least 5 points > 30
                if (AreAbove(dataPoint, 25, 5))
                {
                    // the wave has truly completed
                    return wave; 
                }
                else
                {
                    var nextWave = FindWave(dataPoint);
                    wave.Add(nextWave);
                    return wave;
                }
            }
            else
            {
                // no reference found
                return null;
            }
        }

        bool AreAbove(DailyData reference, decimal cutoff, int numberOfPoints)
        {
            int counter = 1;
            DailyData dataPoint = reference;
            while (counter <= numberOfPoints)
            {
                if (dataPoint == null) { break; }
                if (dataPoint.GetRSI() <= cutoff) { return false; }
                dataPoint = dataPoint.Next;
                counter++;
            }
            return true;
        }

        public DailyData GetReference(DateTime? dt)
        {
            return _waves.OrderByDescending(x => x.First().Date)
                .FirstOrDefault(wave => wave.Last().Date < dt)
                ?.Lowest();
        }

        public DailyData GetReference(DailyData pt)
            => GetReference(pt.Date);

        public IEnumerable<DateTime?> GetReferences()
            => _waves.Select(wv => wv.Lowest().Date);
    }
}
