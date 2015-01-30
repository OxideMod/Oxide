﻿using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Ext.JavaScript.Libraries
{
    public class JavaScriptWebRequests : Library
    {
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Enqueues a get request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        [LibraryFunction("EnqueueGetHook")]
        public void EnqueueGet(string url, string callback, Plugin owner)
        {
            Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (a,b) =>
            {
                owner.CallHook(callback, new object[] {a, b});
            }, owner);
        }

        /// <summary>
        /// Enqueues a post request
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postdata"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        [LibraryFunction("EnqueuePostHook")]
        public void EnqueuePost(string url, string postdata, string callback, Plugin owner)
        {
            Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueuePost(url, postdata, (a, b) =>
            {
                owner.CallHook(callback, new object[] { a, b });
            }, owner);
        }
    }
}
