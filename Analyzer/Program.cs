using Analyzer.Core;
using Analyzer.Core.Models;
using Analyzer.Core.Services;
using CsvHelper;
using ServiceStack;
using StockApis.Alphavantage;
using StockApis.ExcelSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // inputs
            DateTime? startReferenceFrom = null;
            DateTime? startDivergenceFrom = startReferenceFrom.HasValue ?  startReferenceFrom.Value.AddDays(60) : DateTime.Today.AddDays(-15);
            Operations? operation = Operations.FindDivergenceDates;
            string path = @"D:\per\candle-data\ratios.csv";

            var bullishResultSet = new List<DivergencePoint>();
            var bearishResultSet = new List<DivergencePoint>();       

            using (var excelApi = new ExcelSheetsApi())
            {
                var stocks = await excelApi.GetAllStocks();

                Parallel.ForEach(stocks,
                    new ParallelOptions() { MaxDegreeOfParallelism = 10 },
                    async (stock) =>
                {
                    try
                    {
                        Console.WriteLine($"Processing stock [{stock}]");
                        var items = await excelApi.GetTimeSeries(stock);
                        var refService = new ReferenceService(items, startReferenceFrom); 
                        
                        var bullishPoints = new DivergenceService(stock, items, refService).GetBullishDivergentPoints();
                        var bearishPoints = new DivergenceService(stock, items, refService).GetBearishDivergentPoints();
                        
                        var shortListedBullishPoints = bullishPoints.Where(p => p.DataPoint.Date >= startDivergenceFrom);
                        var shortListedBearishPoints = bearishPoints.Where(p => p.DataPoint.Date >= startDivergenceFrom);
                        if (shortListedBullishPoints.Count() > 0)
                        {
                            Console.WriteLine($"Divergence found for {stock}: {string.Join(",", shortListedBullishPoints.Select(p => p.DataPoint.Date.Value.ToString("dd-MM-yyyy")))}");
                            bullishResultSet.AddRange(bullishPoints);
                        }
                        if (shortListedBearishPoints.Count() > 0)
                        {
                            Console.WriteLine($"Divergence found for {stock}: {string.Join(",", shortListedBearishPoints.Select(p => p.DataPoint.Date.Value.ToString("dd-MM-yyyy")))}");
                            bearishResultSet.AddRange(bearishPoints);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Error processing stock {stock}: {ex}");
                    }
                }
                );
            }


            switch(operation)
            {              
                case Operations.FindDivergenceStocks:
                    var list = new List<TrendIndicator>();
                    
                    var groups = bullishResultSet.GroupBy(x => x.DataPoint.Date);

                    foreach(var group in groups)
                    {
                        var listOfStocks = group.AsEnumerable();
                        var ti = new TrendIndicator()
                        {
                            Date = group.Key.Value,
                            BullishIndicator = listOfStocks.Sum(x => x.DataPoint.Close),
                            BullishStocks = string.Join("; ", listOfStocks.Select(x => x.Stock))
                        };
                        list.Add(ti);
                    }

                    File.WriteAllText(path, list.ToCsv());

                    break;

                case Operations.FindDivergenceDates:
                default:
                    // add closing for the first & last divergent point
                    await SendReportAsync(bullishResultSet, "Utha lo!");
                    await SendReportAsync(bearishResultSet, "Bech do!");
                    break;
            }            
        }

        public static async Task SendReportAsync(List<DivergencePoint> points, string subject)
        {
            var body = new StringBuilder();
            body.Append(@"<table width=""900"" style=""border: 1px solid black"">");
            body.Append(@"<thead><th style=""border: 1px solid black; padding: 10px;"">Stock</th><th style=""border: 1px solid black; padding: 10px;"">Divergent Points</th><th style=""border: 1px solid black; padding: 10px;"">Reference Points</th></thead>");

            var groups = points.GroupBy(p => p.Stock);
            foreach (var group in groups)
            {
                body.Append("<tr>");
                body.Append($"<td style='border: 1px solid black; padding: 10px;'>{group.Key}</td>");
                body.Append($"<td style='border: 1px solid black; padding: 10px;'>{string.Join(", ", group.Select(p => p.DataPoint.Date.Value.ToString("dd-MM-yyyy")))}</td>");
                body.Append($"<td style='border: 1px solid black; padding: 10px;'>{string.Join(", ", group.Select(p => p.Reference.Date.Value).Distinct().Select(d => d.ToString("dd -MM-yyyy")))}</td>");
                body.Append("</tr>");

            }
            body.Append("</table>");

            var mailMessage = new MailMessage();
            mailMessage.To.Add(ConfigurationSettings.Instance.Get("divergence:emailRecipients"));
            mailMessage.Subject = subject;
            mailMessage.Body = body.ToString();
            mailMessage.IsBodyHtml = true;
            await EmailService.SendAsync(mailMessage);
        }
    }
}
