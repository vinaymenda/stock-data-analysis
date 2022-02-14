using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Interfaces
{
    public interface IStockApi
    {
        Task<IEnumerable<DailyData>> GetTimeSeries(string symbol);
    }
}
