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
            DateTime? startReferenceFrom = new DateTime(2018, 04, 01);
            DateTime? startDivergenceFrom = startReferenceFrom.HasValue ?  startReferenceFrom.Value.AddDays(60) : DateTime.Today.AddDays(-15);
            Operations? operation = Operations.FindDivergenceStocks;
            string path = @"D:\per\candle-data\ratios.csv";

            var bullishResultSet = new List<DivergencePoint>();
            var bearishResultSet = new List<DivergencePoint>();
            var shortListedBullishResultSet = new List<DivergencePoint>();
            var shortListedBearishResultSet = new List<DivergencePoint>();

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
                        var items = await excelApi.GetTimeSeries(stock.Code);
                        var refService = new ReferenceService(items, startReferenceFrom); 
                        
                        var bullishPoints = new DivergenceService(stock, items, refService).GetBullishDivergentPoints();
                        var bearishPoints = new DivergenceService(stock, items, refService).GetBearishDivergentPoints();
                        
                        var shortListedBullishPoints = bullishPoints.Where(p => p.DataPoint.Date >= startDivergenceFrom);
                        var shortListedBearishPoints = bearishPoints.Where(p => p.DataPoint.Date >= startDivergenceFrom);
                        if (shortListedBullishPoints.Count() > 0)
                        {
                            Console.WriteLine($"Divergence found for {stock.Code}: {string.Join(",", shortListedBullishPoints.Select(p => p.DataPoint.Date.Value.ToString("dd-MM-yyyy")))}");
                            bullishResultSet.AddRange(bullishPoints);
                            shortListedBullishResultSet.AddRange(shortListedBullishPoints);
                        }
                        if (shortListedBearishPoints.Count() > 0)
                        {
                            Console.WriteLine($"Divergence found for {stock.Code}: {string.Join(",", shortListedBearishPoints.Select(p => p.DataPoint.Date.Value.ToString("dd-MM-yyyy")))}");
                            bearishResultSet.AddRange(bearishPoints);
                            shortListedBearishResultSet.AddRange(shortListedBearishPoints);
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
                    
                    var bullishGroups = shortListedBullishResultSet.GroupBy(x => x.DataPoint.Date);
                    foreach(var group in bullishGroups)
                    {
                        var listOfStocks = group.AsEnumerable();
                        var ti = new TrendIndicator()
                        {
                            Date = group.Key.Value,
                            BullishIndicator = listOfStocks.Sum(x => x.DataPoint.Close * x.Stock.NumberOfShares),
                            BullishStocks = string.Join("; ", listOfStocks.Select(x => x.Stock.Code))
                        };
                        list.Add(ti);
                    }

                    var bearishGroups = shortListedBearishResultSet.GroupBy(x => x.DataPoint.Date);
                    foreach(var group in bearishGroups)
                    {
                        var listOfStocks = group.AsEnumerable();
                        var ti = list.FirstOrDefault(li => li.Date == group.Key.Value);
                        if(ti == null)
                        {
                            var item = new TrendIndicator()
                            {
                                Date = group.Key.Value,
                                BearishIndicator = listOfStocks.Sum(x => x.DataPoint.Close * x.Stock.NumberOfShares),
                                BearishStocks = string.Join("; ", listOfStocks.Select(x => x.Stock.Code))
                            };
                            list.Add(item);
                        }
                        else
                        {
                            ti.BearishIndicator = listOfStocks.Sum(x => x.DataPoint.Close * x.Stock.NumberOfShares);
                            ti.BearishStocks = string.Join("; ", listOfStocks.Select(x => x.Stock.Code));
                        }
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

            var groups = points.GroupBy(p => p.Stock.Code);
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
