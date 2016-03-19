using System;
using System.Net;

namespace Protobuild
{
    internal class ProgressiveWebOperation : IProgressiveWebOperation
    {
        public byte[] Get(string uri)
        {
            using (var client = new RetryableWebClient())
            {
                var done = false;
                byte[] result = null;
                Exception ex = null;
                var downloadProgressRenderer = new DownloadProgressRenderer();
                client.DownloadDataCompleted += (sender, e) => {
                    if (e.Error != null)
                    {
                        ex = e.Error;
                    }

                    try
                    {
                        if (e.Result != null)
                        {
                            downloadProgressRenderer.Update(100, e.Result.Length / 1024);
                        }
                        result = e.Result;
                    }
                    catch (System.Reflection.TargetInvocationException)
                    {
                        // This is sometimes thrown when an error occurs.  It is
                        // thrown when reporting that the result is invalid.
                    }

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

                downloadProgressRenderer.FinalizeRendering();

                if (ex != null)
                {
                    throw new InvalidOperationException("Download error", ex);
                }

                return result;
            }
        }
    }
}

