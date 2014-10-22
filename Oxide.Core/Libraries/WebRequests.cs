using System;
using System.Threading;
using System.Web;
using System.Collections.Generic;

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
        public class WebrequestInstance
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
                Finish(0, null);
            }

            /// <summary>
            /// Finishes this webrequest
            /// </summary>
            public void Finish(int code, string response)
            {
                ResponseCode = code;
                ResponseText = response;
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
                
            }
        }

        private Queue<WebrequestInstance> requestqueue;

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
            requestqueue = new Queue<WebrequestInstance>();
            syncroot = new object();
            workevent = new AutoResetEvent(false);

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
                lock (syncroot) if (requestqueue.Count > 0) request = requestqueue.Dequeue();
                if (request != null)
                {
                    request.Process();
                    // Note: We can't fire the callback here because we have to do that on the main thread
                    // Todo: Shove the request onto another queue and let some update method consume that
                    if (request.Finished)
                    {
                        //request.Callback(request.ResponseCode, request.ResponseText);
                    }
                    else
                    {
                        // When can this happen? Did the webrequest fail?
                        // Should we fire a seperate error callback or something?
                        //request.Callback(0, null);
                    }
                }
                lock (syncroot) if (requestqueue.Count > 0) workevent.WaitOne();
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
            lock (requestqueue) requestqueue.Enqueue(request);
            workevent.Set();
        }

        /// <summary>
        /// Returns the current queue length
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetQueueLength")]
        public int GetQueueLength()
        {
            lock (requestqueue) return requestqueue.Count;
        }
    }
}
