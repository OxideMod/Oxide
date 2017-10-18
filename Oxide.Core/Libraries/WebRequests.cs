﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// Request methods for web requests
    /// </summary>
    public enum RequestMethod
    {
        DELETE,
        GET,
        PATCH,
        POST,
        PUT
    };

    /// <summary>
    /// The WebRequests library
    /// </summary>
    public class WebRequests : Library
    {
        /// <summary>
        /// Specifies the HTTP request timeout in seconds
        /// </summary>
        public static float Timeout = 30f;

        /// <summary>
        /// Represents a single WebRequest instance
        /// </summary>
        public class WebRequest
        {
            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<int, string> Callback { get; }

            /// <summary>
            /// Overrides the default request timeout
            /// </summary>
            public float Timeout { get; set; }

            /// <summary>
            /// Gets the web request method
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// Gets the destination URL
            /// </summary>
            public string Url { get; }

            /// <summary>
            /// Gets or sets the request body
            /// </summary>
            public string Body { get; set; }

            /// <summary>
            /// Gets the response code
            /// </summary>
            public int ResponseCode { get; protected set; }

            /// <summary>
            /// Gets the response text
            /// </summary>
            public string ResponseText { get; protected set; }

            /// <summary>
            /// Gets the plugin to which this web request belongs, if any
            /// </summary>
            public Plugin Owner { get; protected set; }

            /// <summary>
            /// Gets the web request headers
            /// </summary>
            public Dictionary<string, string> RequestHeaders { get; set; }

            private HttpWebRequest request;
            private WaitHandle waitHandle;
            private RegisteredWaitHandle registeredWaitHandle;
            private Event.Callback<Plugin, PluginManager> removedFromManager;

            /// <summary>
            /// Initializes a new instance of the WebRequest class
            /// </summary>
            /// <param name="url"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public WebRequest(string url, Action<int, string> callback, Plugin owner)
            {
                Url = url;
                Callback = callback;
                Owner = owner;
                removedFromManager = Owner?.OnRemovedFromManager.Add(owner_OnRemovedFromManager);
            }

            /// <summary>
            /// Used by the worker thread to start the request
            /// </summary>
            public void Start()
            {
                try
                {
                    // Create the request
                    request = (HttpWebRequest)System.Net.WebRequest.Create(Url);
                    request.Method = Method;
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Proxy = null;
                    request.KeepAlive = false;
                    request.Timeout = (int)Math.Round((Timeout.Equals(0f) ? WebRequests.Timeout : Timeout) * 1000f);
                    request.ServicePoint.MaxIdleTime = request.Timeout;
                    request.ServicePoint.Expect100Continue = ServicePointManager.Expect100Continue;
                    request.ServicePoint.ConnectionLimit = ServicePointManager.DefaultConnectionLimit;

                    // Optional request body for POST requests
                    var data = new byte[0];
                    if (Body != null)
                    {
                        data = Encoding.UTF8.GetBytes(Body);
                        request.ContentLength = data.Length;
                        request.ContentType = "application/x-www-form-urlencoded";
                    }

                    if (RequestHeaders != null) request.SetRawHeaders(RequestHeaders);

                    // Perform DNS lookup and connect (blocking)
                    if (data.Length > 0)
                    {
                        request.BeginGetRequestStream(result =>
                        {
                            if (request == null) return;
                            try
                            {
                                // Write request body
                                using (var stream = request.EndGetRequestStream(result)) stream.Write(data, 0, data.Length);
                            }
                            catch (Exception ex)
                            {
                                ResponseText = ex.Message.Trim('\r', '\n', ' ');
                                request?.Abort();
                                OnComplete();
                                return;
                            }
                            WaitForResponse();
                        }, null);
                    }
                    else
                    {
                        WaitForResponse();
                    }
                }
                catch (Exception ex)
                {
                    ResponseText = ex.Message.Trim('\r', '\n', ' ');
                    var message = $"Web request produced exception (Url: {Url})";
                    if (Owner) message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                    Interface.Oxide.LogException(message, ex);
                    request?.Abort();
                    OnComplete();
                }
            }

            private void WaitForResponse()
            {
                var result = request.BeginGetResponse(res => {
                    try
                    {
                        using (var response = (HttpWebResponse)request.EndGetResponse(res))
                        {
                            using (var stream = response.GetResponseStream())
                                using (var reader = new StreamReader(stream))
                                    ResponseText = reader.ReadToEnd();
                            ResponseCode = (int)response.StatusCode;
                        }
                    }
                    catch (WebException ex)
                    {
                        ResponseText = ex.Message.Trim('\r', '\n', ' ');
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            try
                            {
                                using (var stream = response.GetResponseStream())
                                    using (var reader = new StreamReader(stream))
                                        ResponseText = reader.ReadToEnd();
                            }
                            catch (Exception)
                            {
                                // Ignored
                            }
                            ResponseCode = (int)response.StatusCode;
                        }
                    }
                    catch (Exception ex)
                    {
                        ResponseText = ex.Message.Trim('\r', '\n', ' ');
                        var message = $"Web request produced exception (Url: {Url})";
                        if (Owner) message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        Interface.Oxide.LogException(message, ex);
                    }
                    if (request == null) return;
                    request.Abort();
                    OnComplete();
                }, null);
                waitHandle = result.AsyncWaitHandle;
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitHandle, OnTimeout, null, request.Timeout, true);
            }

            private void OnTimeout(object state, bool timedOut)
            {
                if (timedOut) request?.Abort();
                if (Owner == null) return;
                Event.Remove(ref removedFromManager);
                Owner = null;
            }

            private void OnComplete()
            {
                Event.Remove(ref removedFromManager);
                registeredWaitHandle?.Unregister(waitHandle);
                Interface.Oxide.NextTick(() =>
                {
                    if (request == null) return;
                    request = null;
                    Owner?.TrackStart();
                    try
                    {
                        Callback(ResponseCode, ResponseText);
                    }
                    catch (Exception ex)
                    {
                        var message = "Web request callback raised an exception";
                        if (Owner && Owner != null) message += $" in '{Owner.Name} v{Owner.Version}' plugin";
                        Interface.Oxide.LogException(message, ex);
                    }
                    Owner?.TrackEnd();
                    Owner = null;
                });
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                if (request == null) return;
                var outstandingRequest = request;
                request = null;
                outstandingRequest.Abort();
            }
        }

        private readonly Queue<WebRequest> queue = new Queue<WebRequest>();
        private readonly object syncroot = new object();
        private readonly Thread workerthread;
        private readonly AutoResetEvent workevent = new AutoResetEvent(false);
        private bool shutdown;
        private readonly int maxWorkerThreads;
        private readonly int maxCompletionPortThreads;

        /// <summary>
        /// Initializes a new instance of the WebRequests library
        /// </summary>
        public WebRequests()
        {
            // Initialize SSL
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.DefaultConnectionLimit = 200;

            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            maxCompletionPortThreads = (int)(maxCompletionPortThreads * 0.6);
            maxWorkerThreads = (int)(maxWorkerThreads * 0.75);

            // Start worker thread
            workerthread = new Thread(Worker);
            workerthread.Start();
        }

        /// <summary>
        /// Shuts down the worker thread
        /// </summary>
        public override void Shutdown()
        {
            if (shutdown) return;
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
            try
            {
                while (!shutdown)
                {
                    int workerThreads, completionPortThreads;
                    ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                    if (workerThreads <= maxWorkerThreads || completionPortThreads <= maxCompletionPortThreads)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    WebRequest request = null;
                    lock (syncroot)
                        if (queue.Count > 0) request = queue.Dequeue();
                    if (request != null)
                        request.Start();
                    else
                        workevent.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("WebRequests worker: ", ex);
            }
        }

        /// <summary>
        /// Enqueues a get request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("EnqueueGet")]
        [Obsolete("EnqueueGet is deprecated, use Enqueue instead")]
        public void EnqueueGet(string url, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            Enqueue(url, null, callback, owner, RequestMethod.GET, headers, timeout);
        }

        /// <summary>
        /// Enqueues a post request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("EnqueuePost")]
        [Obsolete("EnqueuePost is deprecated, use Enqueue instead")]
        public void EnqueuePost(string url, string body, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            Enqueue(url, body, callback, owner, RequestMethod.POST, headers, timeout);
        }

        /// <summary>
        /// Enqueues a put request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("EnqueuePut")]
        [Obsolete("EnqueuePut is deprecated, use Enqueue instead")]
        public void EnqueuePut(string url, string body, Action<int, string> callback, Plugin owner, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            Enqueue(url, body, callback, owner, RequestMethod.PUT, headers, timeout);
        }

        /// <summary>
        /// Enqueues a DELETE, GET, PATCH, POST, or PUT web request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="timeout"></param>
        [LibraryFunction("Enqueue")]
        public void Enqueue(string url, string body, Action<int, string> callback, Plugin owner, RequestMethod method = RequestMethod.GET, Dictionary<string, string> headers = null, float timeout = 0f)
        {
            var request = new WebRequest(url, callback, owner) { Method = method.ToString(), RequestHeaders = headers, Timeout = timeout, Body = body };
            lock (syncroot) queue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength() => queue.Count;
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
            foreach (var header in RestrictedHeaders) HeaderProperties[header] = type.GetProperty(header.Replace("-", ""));
        }

        /// <summary>
        /// Sets raw HTTP request headers
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="headers">Dictionary of headers to set</param>
        public static void SetRawHeaders(this WebRequest request, Dictionary<string, string> headers)
        {
            foreach (var keyValPair in headers) request.SetRawHeader(keyValPair.Key, keyValPair.Value);
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
                    property.SetValue(request, bool.Parse(value), null);
                else if (property.PropertyType == typeof(long))
                    property.SetValue(request, long.Parse(value), null);
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
