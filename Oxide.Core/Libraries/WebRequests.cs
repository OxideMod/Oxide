using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;

using Oxide.Core.Plugins;
using Oxide.Core.Logging;

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
        /// Represents a single Webrequest instance
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
            /// Gets the HTTP Request headers
            /// </summary>
            public Dictionary<string, string> RequestHeaders { get; private set; }

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
            /// Initialises a Webrequest instance
            /// </summary>
            /// <param name="url">URL to request</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public WebrequestInstance(string url, Action<int, string> callback, Plugin owner)
            {
                URL = url;
                Callback = callback;
                Owner = owner;
                if (owner != null) owner.OnRemovedFromManager += owner_OnRemovedFromManager;
            }

            /// <summary>
            /// Initialises a Webrequest instance with custom HTTP headers
            /// </summary>
            /// <param name="url">URL to request</param>
            /// <param name="headers">HTTP request headesr</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public WebrequestInstance(string url, Dictionary<string, string> headers, Action<int, string> callback, Plugin owner)
                : this(url, callback, owner)
            {
                RequestHeaders = headers;
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender">Plugin being unloaded</param>
            /// <param name="manager">PluginManager object</param>
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
        /// Represents a GET request
        /// </summary>
        public class GetWebrequest : WebrequestInstance
        {
            /// <summary>
            /// Initialises a GetWebrequest instance
            /// </summary>
            /// <param name="url">URL to GET</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public GetWebrequest(string url, Action<int, string> callback, Plugin owner)
                : base(url, callback, owner)
            {
            }

            /// <summary>
            /// Initialises a GetWebrequest instance with custom HTTP request headers
            /// </summary>
            /// <param name="url">URL to GET</param>
            /// <param name="headers">HTTP request headers</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public GetWebrequest(string url, Dictionary<string, string> headers, Action<int, string> callback, Plugin owner)
                : base(url, headers, callback, owner)
            {
            }

            /// <summary>
            /// Processes this web request
            /// </summary>
            public override void Process()
            {
                try
                {
                    // Create the request
                    WebRequest request = WebRequest.Create(URL);
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Timeout = 5 * 1000;
                    if (RequestHeaders != null)
                        request.SetRawHeaders(RequestHeaders);

                    // Get the response
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    if (response == null)
                    {
                        Interface.GetMod().RootLogger.Write(LogType.Warning, "Web request produced no response! (Url: {0})", URL);
                        return;
                    }

                    // Read the output
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        ResponseText = reader.ReadToEnd();
                    ResponseCode = (int)response.StatusCode;

                    // Clean up
                    response.Close();
                }
                catch (WebException webex)
                {
                    HttpWebResponse response = webex.Response as HttpWebResponse;
                    ResponseText = webex.Message;
                    ResponseCode = (int)response.StatusCode;
                }
                catch (Exception ex)
                {
                    ResponseText = ex.Message;
                    ResponseCode = 0;
                    Interface.GetMod().RootLogger.WriteException(String.Format("Web request produced exception (Url: {0})", URL), ex);
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
            public string PostData { get; private set; }

            /// <summary>
            /// Initialises a PostWebrequest instance
            /// </summary>
            /// <param name="url">URL</param>
            /// <param name="postdata">Post data</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public PostWebrequest(string url, string postdata, Action<int, string> callback, Plugin owner)
                : base(url, callback, owner)
            {
                PostData = postdata;
            }

            /// <summary>
            /// Initialises a PostWebrequest instance with custom HTTP request headers
            /// </summary>
            /// <param name="url">URL</param>
            /// <param name="postdata">Post data</param>
            /// <param name="headers">HTTP request headers</param>
            /// <param name="callback">Callback function</param>
            /// <param name="owner">Calling plugin</param>
            public PostWebrequest(string url, string postdata, Dictionary<string, string> headers, Action<int, string> callback, Plugin owner)
                : base(url, headers, callback, owner)
            {
                PostData = postdata;
            }

            /// <summary>
            /// Processes this web request
            /// </summary>
            public override void Process()
            {
                try
                {
                    // Create the request
                    WebRequest request = WebRequest.Create(URL);
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Timeout = 5 * 1000;
                    if (RequestHeaders != null)
                        request.SetRawHeaders(RequestHeaders);

                    // Setup post data
                    request.Method = "POST";
                    if (RequestHeaders != null && !RequestHeaders.ContainsKey("Content-Type"))
                        request.ContentType = "application/x-www-form-urlencoded";
                    byte[] data = Encoding.UTF8.GetBytes(PostData);
                    request.ContentLength = data.Length;
                    using (Stream requeststream = request.GetRequestStream())
                        requeststream.Write(data, 0, data.Length);

                    // Get the response
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    if (response == null)
                    {
                        Interface.GetMod().RootLogger.Write(LogType.Warning, "Web request produced no response! (Url: {0})", URL);
                        return;
                    }

                    // Read the output
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        ResponseText = reader.ReadToEnd();
                    ResponseCode = (int)response.StatusCode;

                    // Clean up
                    response.Close();
                }
                catch (WebException webex)
                {
                    HttpWebResponse response = webex.Response as HttpWebResponse;
                    ResponseText = webex.Message;
                    ResponseCode = (int)response.StatusCode;
                }
                catch (Exception ex)
                {
                    ResponseText = ex.Message;
                    ResponseCode = 0;
                    Interface.GetMod().RootLogger.WriteException(String.Format("Web request produced exception (Url: {0})", URL), ex);
                }

                // Done
                Finish();
            }
        }

        private Queue<WebrequestInstance> waitingqueue, completequeue;


        private object syncroot;

        private Thread workerthread;
        private bool shutdown;
        private AutoResetEvent workevent;

        /// <summary>
        /// Initialises a new instance of the WebRequests library
        /// </summary>
        public WebRequests()
        {
            // Initialise
            waitingqueue = new Queue<WebrequestInstance>();
            completequeue = new Queue<WebrequestInstance>();
            syncroot = new object();
            workevent = new AutoResetEvent(false);

            // Initialise SSL
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ServerCertificateValidationCallback =
                (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                    System.Security.Cryptography.X509Certificates.X509Chain chain,
                    System.Net.Security.SslPolicyErrors sslPolicyErrors) => { return true; };
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
        /// Enqueues a GET request
        /// </summary>
        /// <param name="url">URL to GET</param>
        /// <param name="callback">Callback function</param>
        /// <param name="owner">Calling plugin</param>
        [LibraryFunction("EnqueueGet")]
        public void EnqueueGet(string url, Action<int, string> callback, Plugin owner)
        {
            GetWebrequest request = new GetWebrequest(url, callback, owner);
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a GET request with custom HTTP request headers
        /// </summary>
        /// <param name="url">URL to GET</param>
        /// <param name="headers">HTTP headers</param>
        /// <param name="callback">Callback function</param>
        /// <param name="owner">Calling plugin</param>
        [LibraryFunction("EnqueueGetWithHeaders")]
        public void EnqueueGetWithHeaders(string url, Dictionary<string, string> headers, Action<int, string> callback, Plugin owner)
        {
            GetWebrequest request = new GetWebrequest(url, headers, callback, owner);
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a POST request
        /// </summary>
        /// <param name="url">URL to POST to</param>
        /// <param name="postdata">POST data</param>
        /// <param name="callback">Callback function</param>
        /// <param name="owner">Calling plugin</param>
        [LibraryFunction("EnqueuePost")]
        public void EnqueuePost(string url, string postdata, Action<int, string> callback, Plugin owner)
        {
            PostWebrequest request = new PostWebrequest(url, postdata, callback, owner);
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a POST request with custom HTTP request headers
        /// </summary>
        /// <param name="url">URL to POST to</param>
        /// <param name="postdata">POST data</param>
        /// <param name="headers">HTTP headers</param>
        /// <param name="callback">Callback function</param>
        /// <param name="owner">Calling plugin</param>
        [LibraryFunction("EnqueuePostWithHeaders")]
        public void EnqueuePostWithHeaders(string url, string postdata, Dictionary<string, string> headers, Action<int, string> callback, Plugin owner)
        {
            PostWebrequest request = new PostWebrequest(url, postdata, headers, callback, owner);
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
                WebrequestInstance webrequest;
                while (completequeue.Count > 0)
                {
                    webrequest = completequeue.Dequeue();
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
        private static string[] RestrictedHeaders = new string[]
            {
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
        private static Dictionary<string, PropertyInfo> HeaderProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialise the HeaderProperties dictionary
        /// </summary>
        static HttpWebRequestExtensions()
        {
            Type type = typeof(HttpWebRequest);
            foreach (string header in RestrictedHeaders)
            {
                string propertyName = header.Replace("-", "");
                PropertyInfo headerProperty = type.GetProperty(propertyName);
                HeaderProperties[header] = headerProperty;
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
                PropertyInfo property = HeaderProperties[name];
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
