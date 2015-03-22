using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Oxide.Core.Libraries;
using Newtonsoft.Json;

namespace Oxide.Core
{
    public static class RemoteLogger
    {
        private static int projectId = 3;
        private static string host = "logg.re";
        private static string publicKey = "5bd22fdca1ad47eeb8bf81b82f1d05f8";
        private static string secretKey = "90925e2f297944db853a6c872d2b6c60";
        private static string url = "https://" + host + "/api/" + projectId + "/store/";

        private static string[][] sentryAuth =
        {
            new string[] { "sentry_version", "5" },
            new string[] { "sentry_client", "MiniRaven/1.0" },
            new string[] { "sentry_key", publicKey },
            new string[] { "sentry_secret", secretKey }
        };

        private static Dictionary<string, string> BuildHeaders()
        {
            var auth_string = string.Join(", ", sentryAuth.Select(x => string.Join("=", x)).ToArray());
            auth_string += ", sentry_timestamp=" + (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return new Dictionary<string, string> { { "X-Sentry-Auth", "Sentry " + auth_string } };
        }

        private static Dictionary<string, string> tags = new Dictionary<string, string>()
        {
            { "arch", IntPtr.Size == 4 ? "x86" :  "x64" }
        };

        public class Report
        {
            public string event_id;
            public string message;
            public string level;
            public string platform = "csharp";
            public string culprit;
            public string release = OxideMod.Version.ToString();
            // ext tags: rust protocols, SystemInfo.operatingSystem
            // ext fields: server_name
            public Dictionary<string, string> tags = RemoteLogger.tags;
            public Dictionary<string, string> modules;
            public Dictionary<string, string> extra;

            public Report(string level, Assembly assembly, string culprit, string message, string exception=null)
            {
                this.level = level;
                this.message = message.Length > 1000 ? message.Substring(0, 1000) : message;
                this.event_id = this.message.GetHashCode().ToString();
                this.culprit = culprit;
                var extension_type = assembly.GetTypes().FirstOrDefault(t => t.BaseType == typeof(Extensions.Extension));
                var extension = Interface.Oxide.GetAllExtensions().FirstOrDefault(e => e.GetType() == extension_type);
                var assembly_version = extension != null ? extension.Version.ToString() : string.Empty;
                this.modules = new Dictionary<string, string> { { assembly.GetName().Name, assembly_version } };
                if (exception != null)
                {
                    var exception_lines = exception.Split('\n').Take(31).Select(line => line.Trim(' ', '\r', '\n').Replace('\t', ' '));
                    extra = new Dictionary<string, string> { { "exception", string.Join("\\n", exception_lines.ToArray()) } };
                }                
            }
        }

        private static WebRequests webrequests;

        static RemoteLogger()
        {
            webrequests = Interface.Oxide.GetLibrary<WebRequests>("WebRequests");
        }

        public static void SetTag(string name, string value)
        {
            tags[name] = value;
        }

        public static void Debug(string message)
        {
            SendReport("debug", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Info(string message)
        {
            SendReport("info", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Warning(string message)
        {
            SendReport("warning", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Error(string message)
        {
            SendReport("error", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Exception(string message, Exception exception)
        {
            SendReport("exception", Assembly.GetCallingAssembly(), GetCurrentMethod(), message, exception.ToString());
        }

        private static void SendReport(string level, Assembly assembly, string culprit, string message, string exception = null)
        {
            var body = JsonConvert.SerializeObject(new Report(level, assembly, culprit, message, exception.ToString()));
            webrequests.EnqueuePost(url, body, OnReportSent, null, BuildHeaders());
        }

        private static void OnReportSent(int code, string response)
        {
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var calling_method = (new StackTrace()).GetFrame(2).GetMethod();
            return calling_method.DeclaringType.FullName + "." + calling_method.Name;
        }
    }
}