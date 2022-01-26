using Analyzer.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StockApis.Alphavantage.Models
{
    public class TimeSeriesResponse
    {
        [JsonProperty("Meta Data")]
        public object Metadata { get; set; }

        [JsonProperty("Time Series (Daily)")]
        public Dictionary<DateTime?, TimeSeries> DailyTimeSeries { get; set; }

        public IEnumerable<DailyData> ToDailyData()
        {
            var dataPoints = new List<DailyData>();

            DailyData previous = null;
            int position = 1;
            foreach(var item in DailyTimeSeries.OrderBy(x => x.Key))
            {
                var dt = item.Key;
                var ts = item.Value;
                var dataPoint = new DailyData(position)
                {
                    Date = dt,
                    Close = ts.Close,
                    High = ts.High,
                    Low = ts.Low,
                    Open = ts.Open,
                    Volume = ts.Volume, 
                    Previous = previous
                };
                if (previous != null) { previous.Next = dataPoint; }
                previous = dataPoint;
                dataPoints.Add(dataPoint);
                position++;
            }

            return dataPoints;
        }
    }
}
