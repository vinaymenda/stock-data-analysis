using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Analyzer
{
    public class ReferenceService
    {
        public const decimal LOWER_CUTOFF = 25m;
        public const decimal HIGHER_CUTOFF = 75m;
        private IEnumerable<DailyData> _dataPoints;
        private IEnumerable<Wave> _troughWaves;
        private IEnumerable<Wave> _crestWaves;

        public ReferenceService(IEnumerable<DailyData> points, DateTime? startFrom)
        {
            _dataPoints = points;            
            var startPoint = _dataPoints
                .OrderBy(dt => dt.Date)
                .FirstOrDefault(dt => dt.Date >= (startFrom.HasValue ? startFrom.Value : DateTime.Today.AddMonths(-3))
                                && dt.Position >= 20);
            _troughWaves = FindTroughWaves(startPoint);
            _crestWaves = FindCrestWaves(startPoint);
        }

        List<Wave> FindTroughWaves(DailyData start)
        {
            var waves = new List<Wave>();

            var wave = FindTroughWave(start);
            while (wave != null)
            {
                waves.Add(wave);
                var point = wave.Last().Next;
                wave = FindTroughWave(point);
            }

            return waves;
        }

        List<Wave> FindCrestWaves(DailyData start)
        {
            var waves = new List<Wave>();

            var wave = FindCrestWave(start);
            while (wave != null)
            {
                waves.Add(wave);
                var point = wave.Last().Next;
                wave = FindCrestWave(point);
            }

            return waves;
        }

        Wave FindTroughWave(DailyData item)
        {
            var dataPoint = item;
            while (dataPoint != null && dataPoint.GetRSI() > LOWER_CUTOFF) { dataPoint = dataPoint.Next; }

            if (dataPoint != null)
            {
                // wave started for RSI
                var wave = new Wave();
                while (dataPoint != null && dataPoint.GetRSI() <= LOWER_CUTOFF) { wave.Add(dataPoint); dataPoint = dataPoint.Next; }

                // wave has completed but we need to ensure there are at least 5 points > 30
                if (AreBeyond(dataPoint, LOWER_CUTOFF, 5))
                {
                    // the wave has truly completed
                    return wave;
                }
                else
                {
                    var nextWave = FindTroughWave(dataPoint);
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

        Wave FindCrestWave(DailyData item)
        {
            var dataPoint = item;
            while (dataPoint != null && dataPoint.GetRSI() < HIGHER_CUTOFF) { dataPoint = dataPoint.Next; }

            if (dataPoint != null)
            {
                // wave started for RSI
                var wave = new Wave();
                while (dataPoint != null && dataPoint.GetRSI() >= HIGHER_CUTOFF) { wave.Add(dataPoint); dataPoint = dataPoint.Next; }

                // wave has completed but we need to ensure there are at least 5 points below cutoff
                if (AreBeyond(dataPoint, HIGHER_CUTOFF, 5, false))
                {
                    // the wave has truly completed
                    return wave;
                }
                else
                {
                    var nextWave = FindCrestWave(dataPoint);
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

        bool AreBeyond(DailyData reference, decimal cutoff, int numberOfPoints, bool upwards = true)
        {
            int counter = 1;
            DailyData dataPoint = reference;
            while (counter <= numberOfPoints)
            {
                if (dataPoint == null) { break; }
                if (upwards) { if (dataPoint.GetRSI() <= cutoff) { return false; } }
                else { if (dataPoint.GetRSI() >= cutoff) { return false; } }
                dataPoint = dataPoint.Next;
                counter++;
            }
            return true;
        }

        public DailyData GetTroughReference(DateTime? dt)
            => _troughWaves.OrderByDescending(x => x.First().Date)
                .FirstOrDefault(wave => wave.Last().Date < dt)
                ?.Lowest();

        public DailyData GetCrestReference(DateTime? dt)
            => _crestWaves.OrderByDescending(x => x.First().Date)
                .FirstOrDefault(wave => wave.Last().Date < dt)
                ?.Highest();

        public DailyData GetTroughReference(DailyData pt)
            => GetTroughReference(pt.Date);

        public DailyData GetCrestReference(DailyData pt)
            => GetCrestReference(pt.Date);
    }
}
