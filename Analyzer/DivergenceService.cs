using Analyzer.Core.Models;
using System.Collections.Generic;

namespace Analyzer
{
    public class DivergenceService
    {
        private IEnumerable<DailyData> _dataPoints;
        private ReferenceService _refService;

        public DivergenceService(IEnumerable<DailyData> dataPoints, ReferenceService referenceService)
        {
            _dataPoints = dataPoints;
            _refService = referenceService;
        }

        public IEnumerable<DailyData> GetDivergentPoints()
        {
            var divergentPoints = new List<DailyData>();
            foreach (var pt in _dataPoints)
            {
                var reference = _refService.GetReference(pt);
                if (reference == null) { continue; }
                var acceptablePrice = reference.Close + (0.01m * reference.Close);
                var acceptableRSI = reference.GetRSI() + (0.05m * reference.GetRSI());
                if (pt.Close < acceptablePrice && pt.GetRSI() > acceptableRSI
                    && (pt.Position - reference.Position >= 5))
                {
                    divergentPoints.Add(pt);
                }
            }
            return divergentPoints;
        }
    }
}
