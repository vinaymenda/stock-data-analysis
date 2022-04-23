using System;
using System.Collections.Generic;
using System.Text;

namespace Analyzer.Core.Models
{
    public class TrendIndicator
    {
        public DateTime Date { get; set; }

        public decimal? BullishIndicator { get; set; }

        public decimal? BearishIndicator { get; set; }

        public string BullishStocks { get; set; }

        public string BearishStocks { get; set; }
    }
}
