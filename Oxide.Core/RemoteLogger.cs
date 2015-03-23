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

        private static Dictionary<string, string> tags = new Dictionary<string, string>
        {
            { "arch", IntPtr.Size == 4 ? "x86" : "x64" }
        };

        private class QueuedReport
        {
            public Dictionary<string, string> Headers;
            public string Body;

            public QueuedReport(Report report)
            {
                Headers = BuildHeaders();
                Body = JsonConvert.SerializeObject(report);
            }
        }
        
        public class Report
        {
            public string event_id;
            public string message;
            public string level;
            public string platform = "csharp";
            public string culprit;
            public string release = OxideMod.Version.ToString();
            public Dictionary<string, string> tags = RemoteLogger.tags;
            public Dictionary<string, string> modules;
            public Dictionary<string, string> extra;

            private Dictionary<string, string> headers;

            public Report(string level, string culprit, string message, string exception=null)
            {
                this.headers = BuildHeaders();
                this.level = level;
                this.message = message.Length > 1000 ? message.Substring(0, 1000) : message;
                this.event_id = this.message.GetHashCode().ToString();
                this.culprit = culprit;
                if (exception != null)
                {
                    var exception_lines = exception.Split('\n').Take(31).Select(line => line.Trim(' ', '\r', '\n').Replace('\t', ' '));
                    extra = new Dictionary<string, string> { { "exception", string.Join("\\n", exception_lines.ToArray()) } };
                }                
            }

            public void DetectModules(Assembly assembly)
            {
                var assembly_name = assembly.GetName().Name;
                string assembly_version = string.Empty;
                var extension_type = assembly.GetTypes().FirstOrDefault(t => t.BaseType == typeof(Extensions.Extension));
                if (extension_type != null)
                {
                    var extension = Interface.Oxide.GetAllExtensions().FirstOrDefault(e => e.GetType() == extension_type);
                    if (extension != null) assembly_version = extension.Version.ToString();
                }
                else
                {
                    var plugin_type = assembly.GetTypes().FirstOrDefault(t => IsTypeDerivedFrom(t, typeof(Plugins.Plugin)));
                    if (plugin_type != null)
                    {
                        var plugin = Interface.Oxide.RootPluginManager.GetPlugin(plugin_type.Name);
                        if (plugin != null)
                        {
                            assembly_name = "Plugins." + plugin.Name;
                            assembly_version = plugin.Version.ToString();
                        }
                    }
                }
                this.modules = new Dictionary<string, string> { { assembly_name, assembly_version } };
            }

            private bool IsTypeDerivedFrom(Type type, Type base_type)
            {
                while (type != null && type != base_type)
                    if ((type = type.BaseType) == base_type) return true;
                return false;
            }
        }

        private static Timer timers = Interface.Oxide.GetLibrary<Timer>("Timer");
        private static WebRequests webrequests = Interface.Oxide.GetLibrary<WebRequests>("WebRequests");
        private static List<QueuedReport> queuedReports = new List<QueuedReport>();
        private static bool submittingReports;
        
        public static void SetTag(string name, string value)
        {
            tags[name] = value;
        }

        public static void Debug(string message)
        {
            EnqueueReport("debug", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Info(string message)
        {
            EnqueueReport("info", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Warning(string message)
        {
            EnqueueReport("warning", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Error(string message)
        {
            EnqueueReport("error", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Exception(string message, Exception exception)
        {
            EnqueueReport("exception", Assembly.GetCallingAssembly(), GetCurrentMethod(), message, exception.ToString());
        }

        private static void EnqueueReport(string level, Assembly assembly, string culprit, string message, string exception = null)
        {
            var report = new Report(level, culprit, message, exception);
            report.DetectModules(assembly);
            queuedReports.Add(new QueuedReport(report));
            SubmitNextReport();
        }

        private static void SubmitNextReport()
        {
            if (submittingReports || queuedReports.Count < 1) return;
            var queued_report = queuedReports[0];
            submittingReports = true;
            Action<int, string> on_request_complete = (code, response) =>
            {
                if (code == 200)
                {
                    queuedReports.RemoveAt(0);
                    submittingReports = false;
                    SubmitNextReport();
                }
                else
                {
                    timers.Once(5f, () =>
                    {
                        submittingReports = false;
                        SubmitNextReport();
                    });
                }
            };
            webrequests.EnqueuePost(url, queued_report.Body, on_request_complete, null, queued_report.Headers);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var calling_method = (new StackTrace()).GetFrame(2).GetMethod();
            return calling_method.DeclaringType.FullName + "." + calling_method.Name;
        }
    }
}