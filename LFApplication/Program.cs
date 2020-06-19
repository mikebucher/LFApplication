using Azure.Storage.Queues;
using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace LFApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter Path to the input file:");
            Console.WriteLine("");
            string path = Console.ReadLine();

            try
            {
                string[] lines = File.ReadAllLines(path);

                string connectionString = ConfigurationManager.AppSettings["storageConnectionString"];
                QueueClient queueClient = new QueueClient(connectionString, "mikebucher");
                queueClient.CreateIfNotExists();

                if (queueClient.Exists())
                {
                    using (WebClient client = new WebClient())
                    {
                        //After looking into performance foreach is more performant than for when 'site' is accessed multiple times 
                        foreach (string site in lines)
                        {
                            ResultObject result = new ResultObject
                            {
                                Website = site,
                                ScanStarted = DateTime.Now,
                                Google = false
                            };

                            string htmlCode;

                            //It seems the limiting factor in terms of performance here is establishing a connection to the website
                            //This series of try catches exists to attempt to get a reply from http first as that is faster
                            //To increase performance, commenting out the try around connecting a second time would speed things up
                            //The trade off is that the result may not be as accurate as it could be
                            try
                            {
                                htmlCode = client.DownloadString("http://" + site);
                                if (htmlCode.Contains("www.google-analytics.com")) result.Google = true;
                            }
                            catch (WebException)
                            {
                                //try
                                //{
                                //    htmlCode = client.DownloadString("https://" + site);
                                //}
                                //catch (WebException)
                                //{
                                    result.Google = false;
                                //}
                            }

                            result.ScanCompleted = DateTime.Now;
                            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
                            
                            // Send a message to the queue
                            //queueClient.SendMessage();
                        }
                    }
                }
                Console.WriteLine("Execution finished");
                Console.ReadKey();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("");
                Console.WriteLine("File not found");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
        }
    }
}
