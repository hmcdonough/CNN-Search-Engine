using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebCrawlerLibrary
{
    public class WebCrawlerEntity : TableEntity
    {
        public WebCrawlerEntity(string pageUrl, string title, string date, int num, string fullTitle)
        {
            this.PartitionKey = title;
            this.RowKey = System.Uri.EscapeDataString(pageUrl);
            this.url = pageUrl;
            this.date = date;
            this.num = num;
            this.title = fullTitle;
        }

        public WebCrawlerEntity() { }

        public string url { get; set; }
        public string date { get; set; }
        public int num { get; set; }
        public string title { get; set; }

    }
}