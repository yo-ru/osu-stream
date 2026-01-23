using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.IO;

#if iOS
using Foundation;
using System.Runtime.InteropServices;
#endif

namespace osum.Libraries.NetLib
{
#if iOS
    public class NRDelegate : NSUrlConnectionDataDelegate
    {
        public byte[] result;
        public int written = 0;
        public bool finished;
        public Exception error;

        DataNetRequest nr;

        public NRDelegate(DataNetRequest nr) : base()
        {
            this.nr = nr;
        }

        public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
        {
            long length = response.ExpectedContentLength;
            if (length != -1)
                result = new byte[length];
        }

        public override void ReceivedData(NSUrlConnection connection, NSData data)
        {
            if (result == null)
            {
                result = new byte[data.Length];
                Marshal.Copy(data.Bytes, result, 0, (int)data.Length);
            }
            else if (written + (int)data.Length > result.Length)
            {
                byte[] nb = new byte[result.Length + (int)data.Length];
                result.CopyTo(nb, 0);
                Marshal.Copy(data.Bytes, nb, result.Length, (int)data.Length);
                result = nb;
            }
            else
                Marshal.Copy(data.Bytes, result, written, (int)data.Length);

            written += (int)data.Length;

            if (nr.AbortRequested)
            {
                connection.Cancel();
                return;
            }

            nr.TriggerUpdate();
        }

        public override void FinishedLoading(NSUrlConnection connection)
        {
            finished = true;
            nr.TriggerUpdate();
            nr.data = result;
            nr.error = error;
            nr.processFinishedRequest();
        }

        public override void FailedWithError(NSUrlConnection connection, NSError err)
        {
            if (err != null)
                error = new Exception(err.ToString());

            nr.error = error;
            finished = true;
            nr.processFinishedRequest();
        }
    }
#endif

    public class DataNetRequest : NetRequest
    {
        private readonly string method;
        private readonly string postData;

        public DataNetRequest(string _url, string method = "GET", string postData = null)
            : base(_url)
        {
            this.method = method;
            this.postData = postData;
        }

        public event RequestStartHandler onStart;
        public event RequestUpdateHandler onUpdate;
        public event RequestCompleteHandler onFinish;

        public byte[] data;
        public Exception error;

#if iOS
        NRDelegate del;

        public void TriggerUpdate()
        {
            if (del?.result == null) return;
            if (onUpdate != null)
                onUpdate(this, del.written, del.result.Length);
        }
#endif

        public override void Perform()
        {
            try
            {
                onStart?.Invoke();

#if iOS
                del = new NRDelegate(this);

                NSMutableUrlRequest req =
                    new NSMutableUrlRequest(
                        new NSUrl(UrlEncode(m_url)),
                        NSUrlRequestCachePolicy.ReloadIgnoringCacheData,
                        30);

                req.HttpMethod = method;

                if (method == "POST")
                {
                    NSMutableDictionary headers =
                        (NSMutableDictionary)req.Headers.MutableCopy();
                    headers.SetValueForKey(
                        new NSString("application/x-www-form-urlencoded"),
                        new NSString("content-type"));
                    req.Headers = headers;
                    req.Body = NSData.FromString(postData);
                }

                new NSUrlConnection(req, del, true);

#else
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip |
                        DecompressionMethods.Deflate
                };

                using (var hc = new HttpClient(handler))
                using (var request = new HttpRequestMessage(
                    postData != null ? HttpMethod.Post : HttpMethod.Get,
                    m_url))
                {
                    if (postData != null)
                    {
                        request.Content = new StringContent(
                            postData,
                            Encoding.UTF8,
                            "application/x-www-form-urlencoded");
                    }

                    var response = hc.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead
                    ).GetAwaiter().GetResult();

                    response.EnsureSuccessStatusCode();

                    using (var stream = response.Content
                        .ReadAsStreamAsync()
                        .GetAwaiter().GetResult())
                    using (var ms = new MemoryStream())
                    {
                        var buffer = new byte[80 * 1024];
                        int read;
                        long total = response.Content.Headers.ContentLength ?? -1;
                        int downloaded = 0;

                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                            downloaded += read;

                            if (onUpdate != null && total > 0)
                            {
                                int d = downloaded;
                                int t = (int)total;
                                GameBase.Scheduler.Add(() =>
                                    onUpdate?.Invoke(this, d, t));
                            }

                            if (AbortRequested)
                                break;
                        }

                        data = ms.ToArray();
                    }
                }

                processFinishedRequest();
#endif
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                error = e;
                processFinishedRequest();
            }
        }

        private const string badChars = " \"%'\\";

        public static string UrlEncode(string s)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in s)
            {
                ushort u = c;
                if (u < 32 || badChars.IndexOf(c) >= 0)
                {
                    result.Append('%');
                    result.Append(u.ToString("X2"));
                }
                else result.Append(c);
            }
            return result.ToString();
        }

        public virtual void processFinishedRequest()
        {
            NetManager.ReportCompleted(this);
            if (AbortRequested) return;

            GameBase.Scheduler.Add(() =>
                onFinish?.Invoke(data, error));
        }

        public override bool Valid() => true;

        public override void OnException(Exception e)
        {
            error = e;
            processFinishedRequest();
        }

        public delegate void RequestCompleteHandler(byte[] data, Exception e);
    }
}
