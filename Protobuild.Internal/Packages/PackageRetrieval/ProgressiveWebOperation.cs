using System;
using System.Net;

namespace Protobuild
{
    public class ProgressiveWebOperation : IProgressiveWebOperation
    {
        public byte[] Get(string uri)
        {
            try 
            {
                using (var client = new WebClient())
                {
                    Console.WriteLine("HTTP GET " + uri);
                    var done = false;
                    byte[] result = null;
                    Exception ex = null;
                    var downloadProgressRenderer = new DownloadProgressRenderer();
                    client.DownloadDataCompleted += (sender, e) => {
                        if (e.Error != null)
                        {
                            ex = e.Error;
                        }

                        downloadProgressRenderer.Update(100, e.Result.Length / 1024);
                        result = e.Result;
                        done = true;
                    };
                    client.DownloadProgressChanged += (sender, e) => {
                        if (!done)
                        {
                            downloadProgressRenderer.Update(e.ProgressPercentage, e.BytesReceived / 1024);
                        }
                    };
                    client.DownloadDataAsync(new Uri(uri));
                    while (!done)
                    {
                        System.Threading.Thread.Sleep(0);
                    }

                    Console.WriteLine();

                    if (ex != null)
                    {
                        throw new InvalidOperationException("Download error", ex);
                    }

                    return result;
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Web exception when retrieving: " + uri);
                throw;
            }
        }
    }
}

