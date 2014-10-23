using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            /// Initialises a new instance of the WebrequestInstance class
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
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
            /// Initialises a new instance of the GetWebrequest class
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
            public GetWebrequest(string url, Action<int, string> callback, Plugin owner)
                : base(url, callback, owner)
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
                    Interface.GetMod().RootLogger.WriteException("Web request produced exception (Url: {0})", ex);
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
            /// Initialises a new instance of the GetWebrequest class
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
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
                try
                {
                    // Create the request
                    WebRequest request = WebRequest.Create(URL);
                    request.Credentials = CredentialCache.DefaultCredentials;
                    request.Timeout = 5 * 1000;

                    // Setup post data
                    request.Method = "POST";
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
                    Interface.GetMod().RootLogger.WriteException("Web request produced exception (Url: {0})", ex);
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
            ServicePointManager.ServerCertificateValidationCallback = (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain,
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
                    request.Process();
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
        [LibraryFunction("EnqueueGet")]
        public void EnqueueGet(string url, Action<int, string> callback, Plugin owner)
        {
            GetWebrequest request = new GetWebrequest(url, callback, owner);
            lock (syncroot) waitingqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Enqueues a post request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        [LibraryFunction("EnqueuePost")]
        public void EnqueuePost(string url, string postdata, Action<int, string> callback, Plugin owner)
        {
            PostWebrequest request = new PostWebrequest(url, postdata, callback, owner);
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
                        // Should we fire a seperate error callback or something?
                        webrequest.Callback(0, null);
                    }
                }
            }
        }
    }
}
