﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Analyzer.Core.Models
{
    public class DivergencePoint
    {
        public Stock Stock { get; set; }

        public DailyData DataPoint { get; set; }

        public DailyData Reference { get; set; }
    }
}
