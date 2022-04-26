using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace StockApis.ExcelSheets
{
    public class ExcelSheetsApi : IStockApi, IDisposable
    {
        private List<DataRow> _rows;
        private List<Stock> _numberOfShares;        

        public ExcelSheetsApi()
        {
            Task.Run(async () => await GoogleDriveHelper.DownloadFiles("1JXEqnLV1cFpthoN6RI67LhRVUCJKhdy2", "App_data")).Wait();
            _rows = GetDataRows();
            _numberOfShares = GetNumberOfShares();
        }

        public void Dispose()
        {
            File.Delete(@"App_Data\Daily Data - BSE - 2021.xlsx");
            for (int i = 1; i <= DateTime.Today.Month; i++)
            {
                File.Delete($"App_data\\Daily Data - BSE - 2022-{i:00}.xlsx");
            }
        }        

        public async Task<IEnumerable<DailyData>> GetTimeSeries(string symbol)
        {
            var filteredRows = _rows
                .Where(row => row.ItemArray[1].ToString() == symbol)
                .ToList();

            filteredRows.Sort(delegate(DataRow row1, DataRow row2) 
            {
                var dt1 = Convert.ToDateTime(row1.ItemArray[0]);
                var dt2 = Convert.ToDateTime(row2.ItemArray[0]);
                return dt1.CompareTo(dt2);
            });

            var dataPoints = new List<DailyData>();
            DailyData previous = null;
            int position = 1;
            foreach (var row in filteredRows)
            {
                var dataPoint = ToDailyData(row, position, previous);
                if (previous != null) { previous.Next = dataPoint; }
                previous = dataPoint;
                dataPoints.Add(dataPoint);
                position++;
            }

            return dataPoints;
        }

        public async Task<IEnumerable<Stock>> GetAllStocks()
        {
            var stocks = new List<Stock>();
            var stockNames = _rows
                .Select(row => row.ItemArray[1].ToString())
                .Distinct();
            foreach(var name in stockNames)
            {
                var stock = new Stock()
                {
                    Code = name,
                    NumberOfShares = _numberOfShares.FirstOrDefault(x => x.Code == name)?.NumberOfShares
                };
                stocks.Add(stock);
            }
            return stocks;
        }

        private List<DataRow> GetDataRows()
        {
            var rows = new List<DataRow>();
            AddRows(rows, "Daily Data - BSE - 2018");
            AddRows(rows, "Daily Data - BSE - 2019");
            AddRows(rows, "Daily Data - BSE - 2020");
            AddRows(rows, "Daily Data - BSE - 2021");
            for(int i = 1; i <= DateTime.Today.Month; i++)
            {
                AddRows(rows, $"Daily Data - BSE - 2022-{i:00}");                
            }
            return rows;
        }

        private void AddRows(List<DataRow> rows, string fileName)
        {
            var dt = GetDataTable($"App_data\\{fileName}.xlsx");
            foreach (DataRow row in dt.Rows)
            {
                rows.Add(row);
            }
        }

        private List<Stock> GetNumberOfShares()
        {
            var stocks = new List<Stock>();
            var dt = GetDataTable(@"App_data\_in\numberofshares.xlsx");
            foreach(DataRow row in dt.Rows)
            {
                var stock = new Stock()
                {
                    Code = row.ItemArray[0].ToString(),
                    NumberOfShares = Convert.ToInt64(row.ItemArray[1])
                };
                stocks.Add(stock);
            }
            return stocks;
        }

        private DataTable GetDataTable(string fileName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var config = new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            };
            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {

                    // 2. Use the AsDataSet extension method
                    var result = reader.AsDataSet(config);
                    // The result of each spreadsheet is in result.Tables
                    var dt = result.Tables[0];
                    return dt;
                }
            }
        }

        private DailyData ToDailyData(DataRow row, int position, DailyData previous)
        {
            return new DailyData(position)
            {
                Date = Convert.ToDateTime(row.ItemArray[0]),
                Close = Convert.ToDecimal(row.ItemArray[9]),
                High = Convert.ToDecimal(row.ItemArray[7]),
                Low = Convert.ToDecimal(row.ItemArray[8]),
                Open = Convert.ToDecimal(row.ItemArray[6]),
                Volume = Convert.ToDecimal(row.ItemArray[13]),
                Previous = previous
            };
        }
    }
}
