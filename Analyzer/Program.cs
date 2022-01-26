using Analyzer.Core.Models;
using CsvHelper;
using StockApis.Alphavantage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Analyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var api = new AlphavantageApi("WNVSX2JIWEYIJHOK");
            var items = await api.GetTimeSeries("TATASTEEL.BSE");
            var reference = await FindLowerReference(items.Where(item => item.Position > 30));
            var divergence = await GetDivergence(reference);
            var rsi = items.Select(res => new { res.Date, RSI = res.GetRSI(), Close = res.Close }).ToList();
            

            //using (var writer = new StreamWriter("D:\\proj\\file.csv"))
            //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            //{
            //    csv.WriteRecords(rsi);
            //}




        }

        static async Task<DailyData> FindLowerReference(IEnumerable<DailyData> items)
        {
            foreach(var item in items)
            {
                var isReference = IsLowerReference(item);
                if (isReference) { return item; }                
            }
            return null;
        }

        static bool IsLowerReference(DailyData item, decimal? cutOff = 30)
        {
            if (item.GetRSI() < cutOff)
            {
                int count = 3;
                var next = item.Next;
                bool broken = false;
                while (count > 0)
                {
                    if (next.GetRSI() > 30) { broken = true; break; }
                    next = next.Next;
                    count--;
                }
                if (broken) { return false; }
                else { return true; }
            }
            return false;
        }

        static async Task<DailyData> GetDivergence(DailyData reference)
        {
            // tODo: divergance should at least be 5 days after reference

            var acceptablePrice = reference.Close + (0.01m * reference.Close);
            var acceptableRSI = reference.GetRSI() + (0.05m * reference.GetRSI());

            var next = reference.Next;            
            while (next != null)
            {
                if (IsLowerReference(next, reference.GetRSI())) 
                {
                    reference = next;
                }
                else if(next.Close  < acceptablePrice && next.GetRSI() > acceptableRSI)
                {
                    return next;
                }
                next = next.Next;
            }
            return null;
        }

        static async Task<DailyData> FindHigherReference(IEnumerable<DailyData> items)
        {
            //TODO: reference point (>70)   
            return null;
        }
    }
}
