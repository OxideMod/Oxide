using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Core.JavaScript.Libraries
{
    public class JavaScriptWebRequests : Library
    {
        public override bool IsGlobal => false;

        /// <summary>
        /// Enqueues a get request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        [LibraryFunction("EnqueueGetHook")]
        public void EnqueueGet(string url, string callback, Plugin owner, Dictionary<string, string> headers = null)
        {
            Interface.Oxide.GetLibrary<WebRequests>("WebRequests").Enqueue(url, null, (a, b) =>
            {
                owner.CallHook(callback, a, b);
            }, owner, RequestMethod.GET, headers);
        }

        /// <summary>
        /// Enqueues a post request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postdata"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <param name="headers"></param>
        [LibraryFunction("EnqueuePostHook")]
        public void EnqueuePost(string url, string postdata, string callback, Plugin owner, Dictionary<string, string> headers = null)
        {
            Interface.Oxide.GetLibrary<WebRequests>("WebRequests").Enqueue(url, postdata, (a, b) =>
            {
                owner.CallHook(callback, a, b);
            }, owner, RequestMethod.POST, headers);
        }
    }
}
