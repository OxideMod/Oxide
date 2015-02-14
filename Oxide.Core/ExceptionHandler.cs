using System;
using System.Collections.Generic;

namespace Oxide.Core
{
    public class ExceptionHandler
    {
        private static readonly Dictionary<Type, Func<Exception, string>> Handlers = new Dictionary<Type, Func<Exception, string>>();

        public static void RegisterType(Type ex, Func<Exception, string> handler)
        {
            Handlers[ex] = handler;
        }

        public static string FormatException(Exception ex)
        {
            Func<Exception, string> func;
            return Handlers.TryGetValue(ex.GetType(), out func) ? func(ex) : null;
        }
    }
}