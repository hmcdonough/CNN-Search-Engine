using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Configuration;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;

namespace WebCrawlerLibrary
{
    public class Crawler
    {
        private static HashSet<string> disallowedURLS;

        private static HashSet<string> hrefs;
        private static List<string> firstSitemaps;
        private static CloudQueue queue;
        public static CloudTable table;

        public Crawler()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("urlstoqueue");
            queue.CreateIfNotExists();

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();

            disallowedURLS = new HashSet<string>();
            hrefs = new HashSet<string>();
            firstSitemaps = new List<string>();
            firstSitemaps.Add("http://www.bleacherreport.com/sitemap/nba.xml");
        }

        //calls for the loader to be created.
        public void StartLoader()
        {
            LoadRobot("http://www.cnn.com", true);
            LoadRobot("http://www.bleacherreport.com", false);
            LoadCrawler();
        }

        //loads robot texts and fills disallows.
        public void LoadRobot(string site, bool user)
        {
            string robotsURL = site + "/robots.txt";


            HttpWebRequest request = WebRequest.Create(robotsURL) as HttpWebRequest;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            WebHeaderCollection header = response.Headers;
            var encoding = ASCIIEncoding.ASCII;
            string responseText;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            string splitWord = "";
            if (user)
            {
                splitWord = "User-agent:";
            }
            else
            {
                splitWord = "User-Agent:";
            }
            String[] user_agents = Regex.Split(responseText, splitWord);
            String middlePoint = "";
            foreach (String temp in user_agents)
            {
                if (temp.Trim().StartsWith("*")) //might be " *"
                {
                    middlePoint = temp.Trim().Substring(1);
                }
            }

            String[] disallows = Regex.Split(middlePoint, "Disallow:");
            foreach (String item in disallows)
            {
                if (item != "\n")
                {
                    if (user)
                    { //it is a cnn disallowed url
                        disallowedURLS.Add("c" + item.Trim());
                    }
                    else
                    { //it is a br disallowed url
                        disallowedURLS.Add("b" + item.Trim());
                    }
                    Debug.WriteLine(item.Trim());
                }
            }
            Debug.WriteLine(user_agents[0]);
            Debug.WriteLine("-------------");
            Debug.WriteLine(user_agents[1]);
            if (user)
            {
                String[] sitemaps = Regex.Split(user_agents[0], "Sitemap: ");
                foreach (String temp in sitemaps)
                {
                    if (temp != "\n" && temp != "")
                    {
                        firstSitemaps.Add(temp.Trim());
                        Debug.WriteLine(temp.Trim());
                    }
                }
            }

        }

        //calls for both sites' sitemaps to be loaded
        public void LoadCrawler()
        {
            foreach (string s in firstSitemaps)
            {
                Debug.WriteLine(s);
                DFSCrawl(s);
            }
            Crawl("http://bleacherreport.com/sitemap/nba.xml");
        }

        //Depth first searches the sitemap for urls
        public void DFSCrawl(string s)
        {
            try
            {
                XDocument doc = XDocument.Load(@s);
                XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName urlset = XName.Get("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName sitemap = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName lastmod = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var sitemapElement in doc.Descendants().Elements(sitemap))
                {
                    var locElement = sitemapElement.Element(loc);
                    var lastmodElement = sitemapElement.Element(lastmod);
                    Debug.WriteLine(locElement.Value.ToString());
                    string[] split = Regex.Split(locElement.Value.ToString(), ".xml");
                    string lastSeven = split[0].Substring(split[0].Length - 7);
                    if (lastSeven == "2015-05" || lastSeven == "2015-04" || ((lastmodElement == null || (lastmodElement != null && compareDates(lastmodElement.Value.ToString()) > 0)) && !split[0].Substring(split[0].Length - 7).StartsWith("20")))
                    {
                        DFSCrawl(locElement.Value.ToString());
                    }
                }
                foreach (var urlElement in doc.Descendants(urlset).Elements(url))
                {
                    var locElement = urlElement.Element(loc);
                    var lastmodElement = urlElement.Element(lastmod);
                    string[] split = Regex.Split(locElement.Value.ToString(), ".cnn.com/");
                    string lastSeven = split[1].Substring(0, 7);
                    if (lastSeven == "2015/05" || lastSeven == "2015/04" || ((lastmodElement == null || (lastmodElement != null && compareDates(lastmodElement.Value.ToString()) > 0)) && split[1].Substring(0, 2) != "20"))
                    {
                        Debug.WriteLine(locElement.Value.ToString());
                        if (locElement.Value.ToString().Contains("cnn.com"))
                        {
                            CloudQueueMessage message = new CloudQueueMessage(locElement.Value.ToString());
                            queue.AddMessage(message);
                        }
                    }
                }
            }
            catch (Exception e) { }
        }

        public static int compareDates(string s)
        {
            return DateTime.Compare(ToDateTime(s), new DateTime(2015, 4, 1));
        }

        private static DateTime ToDateTime(string value)
        {
            DateTime convertedDateTime;
            try
            {
                convertedDateTime = Convert.ToDateTime(value);
                return convertedDateTime;

            }
            catch (FormatException)
            {
                Debug.WriteLine("'{0}' is not in the proper format.", value);
            }
            convertedDateTime = new DateTime(2015, 4, 1);
            return convertedDateTime;
        }

        //starts the crawler, searching for url and returning entity if succesful.
        public List<WebCrawlerEntity> startCrawler(string url)
        {
            bool isCNN = true;
            if (url.StartsWith("//"))
            {
                url = url.Substring(2);
            }
            if (url.Contains("bleacherreport.com"))
            {
                isCNN = false;
            }
            WebClient x = new WebClient(); //if error then add url to error list
            if (x != null)
            {
                string source = x.DownloadString(url);
                int firstTitle = source.IndexOf("<title>") + 7;
                string title = source.Substring(firstTitle, source.IndexOf("</title>", firstTitle) - firstTitle);
                string lastmod = "";
                try
                {
                    int end = source.IndexOf("Z\" name=\"lastmod\">");
                    lastmod = source.Substring(end - 19, end);
                }
                catch (Exception e)
                {
                    lastmod = "no date listed";
                }
                if (title != null)
                {
                    List<WebCrawlerEntity> entities = new List<WebCrawlerEntity>();
                    title = title.Split(new string[] { " - CNN.com" }, StringSplitOptions.None)[0];
                    title = title.Split(new string[] { " | Bleacher Report" }, StringSplitOptions.None)[0];
                    string[] titleWords = title.Split(' ');
                    foreach (string s in titleWords)
                    {
                        string temp = new string(s.ToCharArray().Where(c => !char.IsPunctuation(c)).ToArray());
                        Debug.WriteLine(temp);
                        WebCrawlerEntity tempObject = new WebCrawlerEntity(url, temp.ToLower(), lastmod, 0, title);
                        entities.Add(tempObject);
                    }
                    getNewURLSFromHTML(source, isCNN);
                    return entities;
                }
            }
            return null;
        }

        //parses html for urls and adds them to hrefTags
        public void getNewURLSFromHTML(string s, bool isCNN)
        {
            List<string> hrefTags = new List<string>();
            string[] body = s.Split(new string[] { "<body" }, StringSplitOptions.None);
            string[] possibleLinks = body[1].Split(new string[] { "href=" }, StringSplitOptions.None);
            int count = 0;
            foreach (string a in possibleLinks)
            {
                if (count != 0)
                {
                    int firstQuote = a.IndexOf("\"") + 1;
                    if (a.StartsWith("\"") && a.Split().Count(r => r == "\"") >= 2)
                    {
                        string link = a.Substring(firstQuote, a.IndexOf("\"", firstQuote) - firstQuote);
                        if (!hrefs.Contains(link))
                        {
                            hrefs.Add(link);
                            hrefTags.Add(link);
                        }
                    }

                }
                count++;
            }
            processHrefs(hrefTags, isCNN);
        }

        //processes the hrefs to make sure they pass all parameters for the given site
        public void processHrefs(List<string> hrefTags, bool isCNN)
        {
            foreach (string s in hrefTags)
            {
                string temp = s;
                if (s.StartsWith("//"))
                {
                    temp = temp.Substring(1);
                }
                string fullURL = temp;
                if (isCNN)
                {
                    if (s.StartsWith("/") && !fullURL.Contains(".com") && !fullURL.Contains(".cnn."))
                    {
                        fullURL = "http://www.cnn.com" + temp;
                    }
                    if (fullURL.Contains("cnn.com"))
                    {
                        String[] checkDisallows = Regex.Split(fullURL, "cnn.com");
                        String[] checkDisallows2 = Regex.Split(checkDisallows[1], "/");
                        //string a = ("/" + checkDisallows2[1]);
                        if ((checkDisallows2 != null && !disallowedURLS.Contains("c/" + checkDisallows2[0])))
                        {
                            addToQueue(fullURL);
                        }
                    }
                }
                else
                {
                    if (s.StartsWith("/") && !fullURL.Contains(".com") && !fullURL.Contains(".bleacherreport."))
                    {
                        fullURL = "http://www.bleacherreport.com" + temp;
                    }
                    if (fullURL.Contains("bleacherreport.com"))
                    {
                        String[] checkDisallows = Regex.Split(fullURL, "bleacherreport.com");
                        String[] checkDisallows2 = Regex.Split(checkDisallows[1], "/");
                        //string a = ("/" + checkDisallows2[1]);
                        if (checkDisallows2 != null && !disallowedURLS.Contains("b/" + checkDisallows2[0]))
                        {
                            addToQueue(fullURL);
                        }
                    }
                }
            }
        }

        //asynchronously adds to queue
        public void addToQueue(string url)
        {
            CloudQueueMessage message = new CloudQueueMessage(url);
            queue.AddMessageAsync(message);
        }

        //crawls BR's website given the sitemap string
        public void Crawl(string s)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(s);
            string xmlContents = doc.InnerXml;
            string[] links = xmlContents.Split(new string[] { "<loc>" }, StringSplitOptions.None);
            for (int i = 1; i < links.Length; i++)
            {
                string[] firstLinks = links[i].Split(new string[] { "</loc>" }, StringSplitOptions.None);
                Debug.WriteLine(firstLinks[0]);
                CloudQueueMessage message = new CloudQueueMessage(firstLinks[0]);
                queue.AddMessage(message);
            }
        }
    }

}