using Newtonsoft.Json;
using RestSharp;
using StockApis.Alphavantage.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Core.Models;

namespace StockApis.Alphavantage
{
    public class AlphavantageApi
    {
        private readonly string _apiKey;
        private readonly RestClient _restClient; 
        public AlphavantageApi(string apiKey)
        {
            _apiKey = apiKey;
            _restClient = new RestClient("https://www.alphavantage.co/query");
        }

        public async Task<IEnumerable<DailyData>> GetTimeSeries(string symbol)
        {
            var query = $"?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact&apikey={_apiKey}";
            var req = new RestRequest(query, Method.GET);
            var res = await _restClient.ExecuteAsync(req);
            var content = JsonConvert.DeserializeObject<TimeSeriesResponse>(res.Content);
            return content.ToDailyData();
        }
    }
}
