using Analyzer.Core.Models;
using System.Collections.Generic;

namespace Analyzer
{
    public class DivergenceService
    {
        private IEnumerable<DailyData> _dataPoints;
        private ReferenceService _refService;
        private string _stock;

        public DivergenceService(string stock, IEnumerable<DailyData> dataPoints, ReferenceService referenceService)
        {
            _dataPoints = dataPoints;
            _refService = referenceService;
            _stock = stock;
        }

        public IEnumerable<DivergencePoint> GetBullishDivergentPoints()
        {
            var divergentPoints = new List<DivergencePoint>();
            foreach (var pt in _dataPoints)
            {
                var reference = _refService.GetTroughReference(pt);
                if (reference == null) { continue; }                
                var turbulenceRatio = pt.GetBullishTurbulenceRatio(reference);                                   
                if (SatisfiesBullishDivergence(pt, reference) && turbulenceRatio > 20)
                {
                    var divergence = new DivergencePoint() { DataPoint = pt, Reference = reference, Stock = _stock };
                    divergentPoints.Add(divergence);
                }
            }
            return divergentPoints;
        }

        public IEnumerable<DivergencePoint> GetBearishDivergentPoints()
        {
            var divergentPoints = new List<DivergencePoint>();
            foreach (var pt in _dataPoints)
            {
                var reference = _refService.GetCrestReference(pt);
                if (reference == null) { continue; }
                var turbulenceRatio = pt.GetBearishTurbulenceRatio(reference);
                if (SatisfiesBearishDivergence(pt, reference) && turbulenceRatio > 20)
                {
                    var divergence = new DivergencePoint() { DataPoint = pt, Reference = reference, Stock = _stock };
                    divergentPoints.Add(divergence);
                }
            }
            return divergentPoints;
        }

        public bool SatisfiesBullishDivergence(DailyData pt, DailyData reference)
        {
            var acceptablePrice = reference.Close + (0.01m * reference.Close);
            var acceptableRSI = ReferenceService.LOWER_CUTOFF;
            return pt.Close < acceptablePrice && pt.GetRSI() > acceptableRSI
                    && (pt.Position - reference.Position >= 5);
        }

        public bool SatisfiesBearishDivergence(DailyData pt, DailyData reference)
        {
            var acceptablePrice = reference.Close - (0.01m * reference.Close);
            var acceptableRSI = ReferenceService.HIGHER_CUTOFF;
            return pt.Close > acceptablePrice && pt.GetRSI() < acceptableRSI
                    && (pt.Position - reference.Position >= 5);
        }
    }
}
