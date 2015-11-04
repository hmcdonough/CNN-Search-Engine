using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerLibrary
{
    public class infoEntity : TableEntity
    {
        public infoEntity() { }

        public infoEntity(string type, string information)
        {
            this.PartitionKey = "info";
            this.RowKey = type;
            this.description = information;
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public string description { get; set; }
        public string date { get; set; }
    }
}