using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace Protobuild
{
    internal class RetryableWebClient : IDisposable
    {
        private readonly List<WebClient> _clientsToDispose = new List<WebClient>();

        public event UploadDataCompletedEventHandler UploadDataCompleted;
        public event UploadProgressChangedEventHandler UploadProgressChanged;

        public event DownloadDataCompletedEventHandler DownloadDataCompleted;
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        private const int MaxRequests = 10;

        public byte[] UploadValues(string url, NameValueCollection uploadParameters)
        {
            var client = new WebClient();
            SetupClient(client);

            return PerformRetryableRequest(ProtocolOf(url) + " POST " + url, new Uri(url), u => client.UploadValues(u, uploadParameters));
        }

        public void DownloadFile(string url, string filename)
        {
            var client = new WebClient();
            SetupClient(client);

            PerformRetryableRequest(ProtocolOf(url) + " GET " + url, new Uri(url), u => client.DownloadFile(u, filename));
        }

        public void UploadDataAsync(Uri uri, string method, byte[] bytes)
        {
            var client = new AccurateWebClient(bytes.Length);
            SetupClient(client);

            PerformRetryableRequest(ProtocolOf(uri) + " " + method + " " + uri, uri, u => client.UploadDataAsync(u, method, bytes));
        }

        public string DownloadString(Uri uri)
        {
            var client = new WebClient();
            SetupClient(client);

            return PerformRetryableRequest(ProtocolOf(uri) + " GET " + uri, uri, u => client.DownloadString(u));
        }

        public void DownloadDataAsync(Uri uri)
        {
            var client = new WebClient();
            SetupClient(client);

            PerformRetryableRequest(ProtocolOf(uri) + " GET " + uri, uri, u => client.DownloadDataAsync(u));
        }

        public string DownloadString(string uri)
        {
            var client = new WebClient();
            SetupClient(client);

            return PerformRetryableRequest(ProtocolOf(uri) + " GET " + uri, new Uri(uri), u => client.DownloadString(u));
        }

        public void Dispose()
        {
            foreach (var client in _clientsToDispose)
            {
                client.Dispose();
            }
        }

        private string ProtocolOf(Uri uri)
        {
            if (uri.Scheme == "https")
            {
                return "HTTPS";
            }
            else
            {
                return "HTTP";
            }
        }

        private string ProtocolOf(string uri)
        {
            if (new Uri(uri).Scheme == "https")
            {
                return "HTTPS";
            }
            else
            {
                return "HTTP";
            }
        }

        private T PerformRetryableRequest<T>(string message, Uri baseUri, Func<Uri, T> func)
        {
            var exceptions = new List<Exception>();
            var backoff = 100;

            for (var i = 0; i < MaxRequests; i++)
            {
                try
                {
                    Console.WriteLine("(" + (i + 1) + "/" + MaxRequests + ") " + message);
                    return func(baseUri);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception during web request: ");
                    Console.Error.WriteLine(ex);
                    exceptions.Add(ex);

                    var webException = ex as WebException;
                    if (webException != null)
                    {
                        switch (webException.Status)
                        {
                            case WebExceptionStatus.NameResolutionFailure:
                                // This is a permanent failure.
                                i = MaxRequests;
                                backoff = 0;
                                break;
                        }
                    }

                    Console.WriteLine("Backing off web requests for " + backoff + "ms...");
                    System.Threading.Thread.Sleep(backoff);
                    backoff *= 2;
                    if (backoff > 20000)
                    {
                        backoff = 20000;
                    }
                }
            }

            throw new AggregateException(exceptions);
        }

        private void PerformRetryableRequest(string message, Uri baseUri, Action<Uri> func)
        {
            var exceptions = new List<Exception>();
            var backoff = 100;

            for (var i = 0; i < MaxRequests; i++)
            {
                try
                {
                    Console.WriteLine("(" + (i + 1) + "/" + MaxRequests + ") " + message);
                    func(baseUri);
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception during web request: ");
                    Console.Error.WriteLine(ex);
                    exceptions.Add(ex);

                    var webException = ex as WebException;
                    if (webException != null)
                    {
                        switch (webException.Status)
                        {
                            case WebExceptionStatus.NameResolutionFailure:
                                // This is a permanent failure.
                                i = MaxRequests;
                                backoff = 0;
                                break;
                        }
                    }

                    Console.WriteLine("Backing off web requests for " + backoff + "ms...");
                    System.Threading.Thread.Sleep(backoff);
                    backoff *= 2;
                    if (backoff > 20000)
                    {
                        backoff = 20000;
                    }
                }
            }

            throw new AggregateException(exceptions);
        }

        private void SetupClient(WebClient client)
        {
            client.UploadDataCompleted += (sender, args) =>
            {
                if (UploadDataCompleted != null)
                {
                    UploadDataCompleted(sender, args);
                }
            };
            client.UploadProgressChanged += (sender, args) =>
            {
                if (UploadProgressChanged != null)
                {
                    UploadProgressChanged(sender, args);
                }
            };
            client.DownloadDataCompleted += (sender, args) =>
            {
                if (DownloadDataCompleted != null)
                {
                    DownloadDataCompleted(sender, args);
                }
            };
            client.DownloadProgressChanged += (sender, args) =>
            {
                if (DownloadProgressChanged != null)
                {
                    DownloadProgressChanged(sender, args);
                }
            };
        }

        private class AccurateWebClient : WebClient
        {
            private readonly int m_ContentLength;

            public AccurateWebClient(int length)
            {
                m_ContentLength = length;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var req = base.GetWebRequest(address) as HttpWebRequest;
                req.AllowWriteStreamBuffering = false;
                req.ContentLength = m_ContentLength;
                return req;
            }
        }
    }
}