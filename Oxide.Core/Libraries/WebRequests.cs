using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The webrequests library
    /// </summary>
    public class WebRequests : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Represents a single webrequest instance
        /// </summary>
        public abstract class WebrequestInstance
        {
            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<int, string> Callback { get; private set; }

            /// <summary>
            /// Gets if this webrequest has finished
            /// </summary>
            public bool Finished { get; private set; }

            /// <summary>
            /// Gets the destination URL
            /// </summary>
            public string URL { get; private set; }

            /// <summary>
            /// Gets the response code
            /// </summary>
            public int ResponseCode { get; protected set; }

            /// <summary>
            /// Gets the response text
            /// </summary>
            public string ResponseText { get; protected set; }

            /// <summary>
            /// Gets the plugin to which this webrequest belongs, if any
            /// </summary>
            public Plugin Owner { get; private set; }

            /// <summary>
            /// Gets the HTTP Request headers
            /// </summary>
            public Dictionary<string, string> RequestHeaders { get; set; }

            /// <summary>
            /// Initializes a new instance of the WebrequestInstance class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public WebrequestInstance(string url, Action<int, string> callback, Plugin owner)
            {
                URL = url;
                Callback = callback;
                Owner = owner;
                if (owner != null) owner.OnRemovedFromManager += owner_OnRemovedFromManager;
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                Finish();
            }

            /// <summary>
            /// Finishes this webrequest
            /// </summary>
            protected void Finish()
            {
                Finished = true;
            }

            /// <summary>
            /// Processes this web request
            /// </summary>
            public abstract void Process();
        }

        /// <summary>
        /// Represents a get request
        /// </summary>
        public class GetWebrequest : WebrequestInstance
        {
            /// <summary>
            /// Initializes a new instance of the GetWebrequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public GetWebrequest(string url, Action<int, string> callback, Plugin owner)
                : base(url, callback, owner)
            {

            }

            /// <summary>
            /// Processes this web request
            /// </summary>
            public override void Process()
            {
                HttpWebRequest request = null;
                try
                {
                    // Create the request
                    request = (HttpWebRequest)WebRequest.Create(URL);
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Proxy = null;
                    request.KeepAlive = false;

                    request.Timeout = 5000;
                    request.ServicePoint.MaxIdleTime = 5000;
                    if (RequestHeaders != null)
                        request.SetRawHeaders(RequestHeaders);
                    // Get the response
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        // Read the output
                        using (var responseStream = response.GetResponseStream())
                            using (var reader = new StreamReader(responseStream))
                                ResponseText = reader.ReadToEnd();
                        ResponseCode = (int) response.StatusCode;
                    }
                }
                catch (WebException webex)
                {
                    var response = webex.Response as HttpWebResponse;
                    ResponseText = webex.Message;
                    ResponseCode = response != null ? (int)response.StatusCode : 0;
                }
                catch (Exception ex)
                {
                    ResponseText = ex.Message;
                    ResponseCode = 0;
                    Interface.GetMod().RootLogger.WriteException(String.Format("Web request produced exception (Url: {0})", URL), ex);
                }
                finally
                {
                    if (request != null) request.Abort();
                }

                // Done
                Finish();
            }
        }

        /// <summary>
        /// Represents a post request
        /// </summary>
        public class PostWebrequest : WebrequestInstance
        {
            /// <summary>
            /// Gets the post data associated with this request
            /// </summary>
            public string PostData { get; private set;}

            /// <summary>
            /// Initializes a new instance of the GetWebrequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="postdata"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public PostWebrequest(string url, string postdata, Action<int, string> callback, Plugin owner)
                : base(url, callback, owner)
            {
                PostData = postdata;
            }

            /// <summary>
            /// Processes this web request
            /// </summary>
            public override void Process()
            {
                HttpWebRequest request = null;
                try
                {
                    // Create the request
                    request = (HttpWebRequest)WebRequest.Create(URL);
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Proxy = null;
                    request.KeepAlive = false;

                    request.Timeout = 5000;
                    request.ServicePoint.MaxIdleTime = 5000;

                    // Setup post data
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    if (RequestHeaders != null)
                        request.SetRawHeaders(RequestHeaders);
                    var data = Encoding.UTF8.GetBytes(PostData);
                    request.ContentLength = data.Length;
                    using (var requeststream = request.GetRequestStream())
                        requeststream.Write(data, 0, data.Length);

                    // Get the response
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        // Read the output
                        using (var responseStream = response.GetResponseStream())
                            using (var reader = new StreamReader(responseStream))
                                ResponseText = reader.ReadToEnd();
                        ResponseCode = (int) response.StatusCode;
                    }
                }
                catch (WebException webex)
                {
                    var response = webex.Response as HttpWebResponse;
                    ResponseText = webex.Message;
                    ResponseCode = response != null ? (int)response.StatusCode : 0;
                }
                catch (Exception ex)
                {
                    ResponseText = ex.Message;
                    ResponseCode = 0;
                    Interface.GetMod().RootLogger.WriteException(String.Format("Web request produced exception (Url: {0})", URL), ex);
                }
                finally
                {
                    if (request != null) request.Abort();
                }
                // Done
                Finish();
            }
        }

        private readonly Queue<WebrequestInstance> waitingqueue, completequeue;
        private readonly object syncroot;
        private readonly Thread workerthread;
        private bool shutdown;
        private readonly AutoResetEvent workevent;

        /// <summary>
        /// Initializes a new instance of the WebRequests library
        /// </summary>
        public WebRequests()
        {
            // Initialize
            waitingqueue = new Queue<WebrequestInstance>();
            completequeue = new Queue<WebrequestInstance>();
            syncroot = new object();
            workevent = new AutoResetEvent(false);

            // Initialize SSL
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.DefaultConnectionLimit = 200;

            // Start worker thread
            workerthread = new Thread(Worker);
            workerthread.Start();
        }

        /// <summary>
        /// Shuts down the worker thread
        /// </summary>
        public void Shutdown()
        {
            shutdown = true;
            workevent.Set();
            Thread.Sleep(250);
            workerthread.Abort();
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            while (!shutdown)
            {
                workevent.WaitOne();
                WebrequestInstance request = null;
                lock (syncroot) if (waitingqueue.Count > 0) request = waitingqueue.Dequeue();
                if (request != null)
                {
                    try
                    {
                        request.Process();
                    }
                    catch (Exception ex)
                    {
                        Interface.GetMod().RootLogger.WriteException("Web request produced exception", ex);
                    }

                    lock (syncroot) completequeue.Enqueue(request);
                }
                lock (syncroot) if (waitingqueue.Count > 0) workevent.Set();
            }
        }

        /// <summary>
        /// Enqueues a get request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        [LibraryFunction("EnqueueGet")]
        public void EnqueueGet(string url, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null)
        {
            var request = new GetWebrequest(url, callback, owner) { RequestHeaders = headers };
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a post request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postdata"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        [LibraryFunction("EnqueuePost")]
        public void EnqueuePost(string url, string postdata, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null)
        {
            var request = new PostWebrequest(url, postdata, callback, owner) { RequestHeaders = headers};
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength()
        {
            lock (syncroot) return waitingqueue.Count;
        }

        /// <summary>
        /// Updates all webrequests
        /// </summary>
        public void Update()
        {
            lock (syncroot)
            {
                while (completequeue.Count > 0)
                {
                    var webrequest = completequeue.Dequeue();
                    if (webrequest.Finished)
                    {
                        webrequest.Callback(webrequest.ResponseCode, webrequest.ResponseText);
                    }
                    else
                    {
                        // When can this happen? Did the webrequest fail?
                        // Should we fire a separate error callback or something?
                        webrequest.Callback(0, null);
                    }
                }
            }
        }
    }

    // HttpWebRequest extensions to add raw header support
    public static class HttpWebRequestExtensions
    {
        /// <summary>
        /// Headers that require modification via a property
        /// </summary>
        private static readonly string[] RestrictedHeaders = {
                "Accept",
                "Connection",
                "Content-Length",
                "Content-Type",
                "Date",
                "Expect",
                "Host",
                "If-Modified-Since",
                "Keep-Alive",
                "Proxy-Connection",
                "Range",
                "Referer",
                "Transfer-Encoding",
                "User-Agent"
            };

        /// <summary>
        /// Dictionary of all of the header properties
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialize the HeaderProperties dictionary
        /// </summary>
        static HttpWebRequestExtensions()
        {
            var type = typeof(HttpWebRequest);
            foreach (var header in RestrictedHeaders)
            {
                HeaderProperties[header] = type.GetProperty(header.Replace("-", ""));
            }
        }

        /// <summary>
        /// Sets raw HTTP request headers
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="headers">Dictionary of headers to set</param>
        public static void SetRawHeaders(this WebRequest request, Dictionary<string, string> headers)
        {
            foreach (var keyValPair in headers)
                request.SetRawHeader(keyValPair.Key, keyValPair.Value);
        }

        /// <summary>
        /// Sets a raw HTTP request header
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="name">Name of the header</param>
        /// <param name="value">Value of the header</param>
        public static void SetRawHeader(this WebRequest request, string name, string value)
        {
            if (HeaderProperties.ContainsKey(name))
            {
                var property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                    property.SetValue(request, DateTime.Parse(value), null);
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(request, Boolean.Parse(value), null);
                else if (property.PropertyType == typeof(long))
                    property.SetValue(request, Int64.Parse(value), null);
                else
                    property.SetValue(request, value, null);
            }
            else
            {
                request.Headers[name] = value;
            }
        }
    }
}
