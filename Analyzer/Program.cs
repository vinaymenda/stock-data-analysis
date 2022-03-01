using Analyzer.Core;
using Analyzer.Core.Models;
using CsvHelper;
using StockApis.Alphavantage;
using StockApis.ExcelSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Analyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var excelApi = new ExcelSheetsApi())
            {
                var stocks = await excelApi.GetAllStocks(); // new List<string>() { "539834" }

                Parallel.ForEach(stocks, async (stock) =>
                {
                    Console.WriteLine($"Processing stock [{stock}]");
                    var items = await excelApi.GetTimeSeries(stock);

                    Console.WriteLine($"Found {items.Count()} points for {stock}");
                    var refService = new ReferenceService(items);
                    Console.WriteLine($"Reference Points: ");
                    foreach (var dt in refService.GetReferences())
                    {
                        Console.WriteLine(dt.Value.ToString("dd-MM-yyyy"));
                    }
                    var points = new DivergenceService(items, refService).GetDivergentPoints();
                    Console.WriteLine($"Divergent Points: ");
                    foreach (var point in points)
                    {
                        Console.WriteLine(point.Date.Value.ToString("dd-MM-yyyy"));
                    }
                }
                );             
            }       
        }

        static void Print(IEnumerable<DailyData> items)
        {
            var rsi = items.Select(res => new { res.Date, res.Open, Close = res.Close, Gain = res.GetAvgGain(), Loss = res.GetAvgLoss(), RSI = res.GetRSI() }).ToList();
            using (var writer = new StreamWriter("D:\\proj\\file.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(rsi);

            }
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

        static async Task<DailyData> FindHigherReference(IEnumerable<DailyData> items)
        {
            //TODO: reference point (>70)   
            return null;
        }
    }
}
