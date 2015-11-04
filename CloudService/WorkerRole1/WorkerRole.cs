using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using WebCrawlerLibrary;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        public static CloudQueue queue;
        public static CloudTable table;
        public static CloudTable infoTable;
        public static CloudTable errorTable;
        public static CloudQueue startStopQueue;
        private string state;
        private static Crawler crawl;
        public static int urlsCrawled;
        public static int urlsAccepted;
        private static HashSet<string> acceptedURLs;
        private int numErrors;
        private int queueSize;
        private int numTitles;
        private string lastTitle;
        private List<string> lastTen;

        public override void Run()
        {
            //put visited urls in a hash set
            startingCode();
            
            while (true)
            {
                Trace.TraceInformation("Working");

                //Sleep 50ms
                Thread.Sleep(50);
                
                //Check and handle admin messages
                CloudQueueMessage startStopMessage = startStopQueue.GetMessage();
                if (startStopMessage != null)
                {
                    state = startStopMessage.AsString;
                    Update("info", "state", state);
                    startStopQueue.DeleteMessage(startStopMessage);
                    if (state.Equals("started"))
                    {
                        crawl = new Crawler();
                        crawl.StartLoader();
                    }
                }

                if (state.Equals("clearing")){
                    //clear queue
                    queue.Clear();
                    startingCode();
                }
                else if (state.Equals("started")) //keepGoing
                {
                    //get message from url queue
                    CloudQueueMessage message = queue.GetMessage();
                    //if message isn't null
                    if (message != null)
                    {
                        urlsCrawled++;
                        Update("info", "total", urlsCrawled.ToString());
                        queue.FetchAttributes();
                        queueSize = (int)queue.ApproximateMessageCount;
                        Update("info", "queue", queueSize.ToString());
                        string url = message.AsString;
                        if (!acceptedURLs.Contains(url))
                        {
                            try
                            {
                                List<WebCrawlerEntity> entities = crawl.startCrawler(url); //Store dates
                                if (entities != null)
                                {
                                    numTitles++;
                                    Update("info", "numTitles", numTitles.ToString());
                                    string temp = "";
                                    foreach (WebCrawlerEntity w in entities)
                                    {
                                        temp += " " + w.PartitionKey;
                                        urlsAccepted++;
                                        w.num = urlsAccepted;
                                        Update("info", "accepted", urlsAccepted.ToString());
                                        TableOperation insertOperation = TableOperation.InsertOrReplace(w);
                                        table.ExecuteAsync(insertOperation);
                                        acceptedURLs.Add(url);
                                    }
                                    TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
                                    Update("info", "lastTitle", myTI.ToTitleCase(temp));
                                }

                            }
                            catch (Exception e)
                            { //put errors in error table
                                numErrors++;
                                infoEntity newError = new infoEntity(numErrors.ToString(), "url: " + url + " Error: " + e.Message);
                                TableOperation insertErrorOperation = TableOperation.InsertOrReplace(newError);
                                errorTable.Execute(insertErrorOperation);
                            }          
                        }
                        queue.DeleteMessage(message);
                    }
                }
            }
        }

        public void startingCode() {
            Trace.TraceInformation("WorkerRole1 is running");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();

            infoTable = tableClient.GetTableReference("infotable");
            infoTable.CreateIfNotExists();

            errorTable = tableClient.GetTableReference("errortable");
            errorTable.CreateIfNotExists();

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("urlstoqueue");
            queue.CreateIfNotExists();

            startStopQueue = queueClient.GetQueueReference("startstop");
            startStopQueue.CreateIfNotExists();

            state = "idle";
            infoEntity newState = new infoEntity("state", state);
            TableOperation insertInfoOperation = TableOperation.InsertOrReplace(newState);
            infoTable.Execute(insertInfoOperation);
            acceptedURLs = new HashSet<string>();
            urlsCrawled = 0;
            urlsAccepted = 0;
            numErrors = 0;
            queueSize = 0;
            numTitles = 0;
            lastTitle = "No titles crawled yet";
            lastTen = new List<string>();
            infoEntity newTotalNum = new infoEntity("total", urlsCrawled.ToString());
            TableOperation insertTotalOperation = TableOperation.InsertOrReplace(newTotalNum);
            infoTable.Execute(insertTotalOperation);

            infoEntity newNumAccepted = new infoEntity("accepted", urlsAccepted.ToString());
            TableOperation insertAcceptedOperation = TableOperation.InsertOrReplace(newNumAccepted);
            infoTable.Execute(insertAcceptedOperation);

            infoEntity newQueueSize = new infoEntity("queue", queueSize.ToString());
            TableOperation insertNewQSize = TableOperation.InsertOrReplace(newQueueSize);
            infoTable.Execute(insertNewQSize);

            infoEntity num = new infoEntity("numTitles", numTitles.ToString());
            TableOperation insertNum = TableOperation.InsertOrReplace(num);
            infoTable.Execute(insertNum);

            infoEntity last = new infoEntity("lastTitle", lastTitle);
            TableOperation insertLast = TableOperation.InsertOrReplace(last);
            infoTable.Execute(insertLast);
        }

        public void Update(string pk, string rk, string newValue)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<infoEntity>(pk, rk);
            TableResult retrievedResult = infoTable.Execute(retrieveOperation);
            infoEntity entity = (infoEntity)retrievedResult.Result;
            entity.description = newValue;
            TableOperation update = TableOperation.Replace(entity);
            infoTable.Execute(update);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                //CloudQueueMessage message = new CloudQueueMessage(Guid.NewGuid().ToString());
                //queue.AddMessage(message);
                await Task.Delay(1000);
            }
        }
    }
}