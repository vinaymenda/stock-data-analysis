using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StockApis.ExcelSheets
{
    public class GoogleDriveHelper
    {
        private static RestClient client = new RestClient("https://www.googleapis.com/drive/v2");
        private const string apiKey = "AIzaSyBMz5J-WvSoo275NXYn5HrAJt1zoeYF2HY";        

        public static async Task DownloadFiles(string driveFolder, string downloadLocation)
        {
            // https://drive.google.com/drive/folders/1JXEqnLV1cFpthoN6RI67LhRVUCJKhdy2?usp=sharing
            // https://www.googleapis.com/drive/v2/files?q=%271JXEqnLV1cFpthoN6RI67LhRVUCJKhdy2%27%20in%20parents&key=AIzaSyBMz5J-WvSoo275NXYn5HrAJt1zoeYF2HY

            var req = new RestRequest($"files?q=%27{driveFolder}%27%20in%20parents&key={apiKey}", Method.GET);
            var res = await client.ExecuteAsync(req);

            var itemType = new { downloadUrl = string.Empty, title = string.Empty };
            var responseType = new { items = new[] { itemType }.ToList() };
            var content = JsonConvert.DeserializeAnonymousType(res.Content, responseType);

            foreach (var item in content.items)
            {
                new WebClient().DownloadFile(item.downloadUrl, $"{downloadLocation}/{item.title}");
            }
        }
    }
}
