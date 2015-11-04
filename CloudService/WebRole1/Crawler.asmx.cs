using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Configuration;
using WebCrawlerLibrary;
using System.Web.Script.Services;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Web.Script.Serialization;
using System.Globalization;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        public WebService1()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference("hmcdblob");
            blob = container.GetBlockBlobReference("TitlesWithSpaces");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();
            infoTable = tableClient.GetTableReference("infotable");
            infoTable.CreateIfNotExists();
            errorTable = tableClient.GetTableReference("errortable");
            errorTable.CreateIfNotExists();
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            startStopQueue = queueClient.GetQueueReference("startstop");
            startStopQueue.CreateIfNotExists();
            cache = new Dictionary<string, List<TitleEntity>>();
        }

        public static CloudTable table;
        public static CloudTable infoTable;
        public static CloudTable errorTable;
        public static CloudQueue startStopQueue;
        public static Dictionary<string, List<TitleEntity>> cache;
        private static Trie trie;
        public CloudBlobContainer container;
        public CloudBlockBlob blob;
        private static string filenameForBlob;

        //sends a message to start the crawler
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StartCrawling()
        {
            CloudQueueMessage message = new CloudQueueMessage("started");
            startStopQueue.AddMessage(message);
            return "Crawler has started";
        }

        //sends a message to stop the crawler
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StopCrawling()
        {
            CloudQueueMessage message = new CloudQueueMessage("stopped");
            startStopQueue.AddMessage(message);
            return "Crawler has stopped";
        }

        //clears the table + queue
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearIndex()
        {
            //wipes table
            CloudQueueMessage message = new CloudQueueMessage("clearing");
            startStopQueue.AddMessage(message);
            return "The index has been cleared, please wait a minute before starting again";
        }

        //gets a page title given a url and returns it
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<TitleEntity> GetUrlsForTitle(string title)
        {
            if (cache.Count > 100)
            {
                cache = new Dictionary<string, List<TitleEntity>>();
            }
            else if (cache.ContainsKey(title))
            {
                return cache[title];
            }
            List<WebCrawlerEntity> listOfTitles = new List<WebCrawlerEntity>();
            string[] titles = title.Split(' ');
            foreach (string s in titles)
            {
                string temp = s.ToLower();
                TableQuery<WebCrawlerEntity> titlesQuery = new TableQuery<WebCrawlerEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, temp));
                foreach (WebCrawlerEntity entity in table.ExecuteQuery(titlesQuery))
                {
                    listOfTitles.Add(entity);
                }
            }

            //LINQ statement here
            var urlsToReturn = listOfTitles
                .GroupBy(x => x.RowKey)
                .Select(x => new Tuple<string, int, string, string>(x.Key, x.ToList().Count, x.ToList()[0].title, x.ToList()[0].date))
                .OrderByDescending(x => x.Item2)
                .ThenByDescending(x => x.Item4).Take(18).ToList();


            List<TitleEntity> tuples = new List<TitleEntity>();
            foreach (Tuple<string, int, string, string> temp in urlsToReturn)
            {
                TitleEntity temp2 = new TitleEntity(temp);
                tuples.Add(temp2);
            }
            cache.Add(title, tuples);
            return tuples;
        }

        //Generic method for getting information from infoTable
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetX(string x)
        {
            string state = "";
            TableQuery<infoEntity> stateQuery = new TableQuery<infoEntity>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, x));
            foreach (infoEntity entity in infoTable.ExecuteQuery(stateQuery))
            {
                state = entity.description;
            }
            return state;
        }

        //returns the first 15 errors
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetErrors()
        {
            string errors = "";
            TableQuery<infoEntity> stateQuery = new TableQuery<infoEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "info"))
                .Take(15);
            foreach (infoEntity entity in errorTable.ExecuteQuery(stateQuery))
            {
                errors = errors + entity.description + "<br >";
            }
            return errors;
        }

        //returns the 10 most recent urls
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetURLs()
        {
            string urls = "";
            string numAcc = GetX("accepted");
            if (numAcc != "")
            {
                TableQuery<WebCrawlerEntity> urlQuery = new TableQuery<WebCrawlerEntity>()
                    .Where(TableQuery.GenerateFilterConditionForInt("num", QueryComparisons.GreaterThan, Convert.ToInt32(numAcc) - 10));

                foreach (WebCrawlerEntity entity in table.ExecuteQuery(urlQuery))
                {
                    urls += "," + entity.url;
                }
            }
            return urls;
        }

        //Returns the performance counters and their measurements
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPerformance()
        {
            PerformanceCounter availableRam = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            string cpu = cpuCounter.NextValue().ToString();
            Thread.Sleep(500);
            return availableRam.NextValue() + "MB," + cpuCounter.NextValue().ToString() + "%";
        }

        //Download wiki accesses the blob storage and writes a file to the local storage before calling build trie on it
        [WebMethod]
        public string DownloadWiki()
        {
            // CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            //ConfigurationManager.AppSettings["hmcd"]);
            trie = new Trie();
            var filename = System.IO.Path.GetTempFileName() + blob.Name;
            try
            {
                using (var fileStream = System.IO.File.OpenWrite(filename))
                {
                    blob.DownloadToStream(fileStream);
                    fileStream.Close();
                    //BuildTrie(filename);
                    filenameForBlob = filename;
                    return filename;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            //return "no filename found";
        }

        //Build Trie constructs the trie until it is out of words or too much memory is used
        [WebMethod]
        public string BuildTrie()
        {
            string line1;
            using (StreamReader reader = new StreamReader(filenameForBlob))
            {
                line1 = reader.ReadLine();
                Console.WriteLine(line1);
                PerformanceCounter availableRam = new PerformanceCounter("Memory", "Available MBytes");
                int count = 0;
                while (line1 != null && (Convert.ToInt32(availableRam.NextValue()) > 300))
                {
                    Debug.WriteLine(line1);
                    trie.AddTitle(line1);
                    availableRam = new PerformanceCounter("Memory", "Available MBytes");
                    line1 = reader.ReadLine();
                    count++;
                }
                return count + "titles loaded";
            }
        }

        //SearchTrie accepts a word and calls on the Trie to find the top ten results
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string SearchTrie(string word)
        {
            try
            {
                List<string> results = trie.SearchForWord(word);
                List<string> finalWords = new List<string>();
                foreach (string value in results)
                {
                    finalWords.Add(new JavaScriptSerializer().Serialize(value));
                }

                string resultString = string.Join(",", finalWords.ToArray());
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(resultString);
            }
            catch (Exception e)
            {
                return "Trie is not loaded or does not reach that word";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string SearchTrieTest()
        {
            return SearchTrie("a");
        }
    }
}
