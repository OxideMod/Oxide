using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;

using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Core
{
    public static class RemoteLogger
    {
        private const int projectId = 3;
        private const string host = "logg.re";
        private const string publicKey = "5bd22fdca1ad47eeb8bf81b82f1d05f8";
        private const string secretKey = "90925e2f297944db853a6c872d2b6c60";
        private static readonly string Url = "https://" + host + "/api/" + projectId + "/store/";

        private static readonly string[][] sentryAuth =
        {
            new[] { "sentry_version", "5" },
            new[] { "sentry_client", "MiniRaven/1.0" },
            new[] { "sentry_key", publicKey },
            new[] { "sentry_secret", secretKey }
        };

        private static Dictionary<string, string> BuildHeaders()
        {
            var authString = string.Join(", ", sentryAuth.Select(x => string.Join("=", x)).ToArray());
            authString += ", sentry_timestamp=" + (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return new Dictionary<string, string> { { "X-Sentry-Auth", "Sentry " + authString } };
        }

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>
        {
            { "arch", IntPtr.Size == 4 ? "x86" : "x64" },
            { "game", Utility.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLower() }
        };

        private class QueuedReport
        {
            public readonly Dictionary<string, string> Headers;
            public readonly string Body;

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
            public Dictionary<string, string> tags = Tags;
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
                this.modules = new Dictionary<string, string>();
                foreach (var extension in Interface.Oxide.GetAllExtensions())
                    modules[extension.GetType().Assembly.GetName().Name] = extension.Version.ToString();
                if (exception != null)
                {
                    extra = new Dictionary<string, string>();
                    var exceptionLines = exception.Split('\n').Take(31).ToArray();
                    for (var i = 0; i < exceptionLines.Length; i++)
                    {
                        var line = exceptionLines[i].Trim(' ', '\r', '\n').Replace('\t', ' ');
                        if (line.Length > 0) extra["line_" + i.ToString("00")] = line;
                    }
                }
            }

            public void DetectModules(Assembly assembly)
            {
                var assemblyName = assembly.GetName().Name;
                var extensionType = assembly.GetTypes().FirstOrDefault(t => t.BaseType == typeof(Extension));
                if (extensionType == null)
                {
                    var pluginType = assembly.GetTypes().FirstOrDefault(t => IsTypeDerivedFrom(t, typeof(Plugin)));
                    if (pluginType != null)
                    {
                        var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginType.Name);
                        if (plugin != null) modules["Plugins." + plugin.Name] = plugin.Version.ToString();
                    }
                }
            }

            public void DetectModules(string[] stackTrace)
            {
                foreach (var line in stackTrace)
                {
                    if (!line.StartsWith("Oxide.Plugins.") || !line.Contains("+")) continue;
                    var pluginName = line.Split('+')[0];
                    var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
                    if (plugin != null) modules["Plugins." + plugin.Name] = plugin.Version.ToString();
                    break;
                }
            }

            private static bool IsTypeDerivedFrom(Type type, Type baseType)
            {
                while (type != null && type != baseType) if ((type = type.BaseType) == baseType) return true;
                return false;
            }
        }

        private static readonly Timer Timers = Interface.Oxide.GetLibrary<Timer>();
        private static readonly WebRequests Webrequests = Interface.Oxide.GetLibrary<WebRequests>();
        private static readonly List<QueuedReport> QueuedReports = new List<QueuedReport>();
        private static bool submittingReports;

        public static void SetTag(string name, string value) => Tags[name] = value;

        public static string GetTag(string name)
        {
            string value;
            return Tags.TryGetValue(name, out value) ? value : "unknown";
        }

        public static void Debug(string message) => EnqueueReport("debug", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);

        public static void Info(string message) => EnqueueReport("info", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);

        public static void Warning(string message) => EnqueueReport("warning", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);

        public static void Error(string message) => EnqueueReport("error", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);

        public static void Exception(string message, Exception exception)
        {
            EnqueueReport("exception", Assembly.GetCallingAssembly(), GetCurrentMethod(), message, exception.ToString());
        }

        public static void Exception(string message, string raw_stack_trace)
        {
            var stack_trace = raw_stack_trace.Split('\r', '\n');
            var culprit = stack_trace[0].Split('(')[0].Trim();
            EnqueueReport("exception", stack_trace, culprit, message, raw_stack_trace);
        }

        private static void EnqueueReport(string level, Assembly assembly, string culprit, string message, string exception = null)
        {
            var report = new Report(level, culprit, message, exception);
            report.DetectModules(assembly);
            EnqueueReport(report);
        }

        private static void EnqueueReport(string level, string[] stack_trace, string culprit, string message, string exception = null)
        {
            var report = new Report(level, culprit, message, exception);
            report.DetectModules(stack_trace);
            EnqueueReport(report);
        }

        private static void EnqueueReport(Report report)
        {
            QueuedReports.Add(new QueuedReport(report));
            if (!submittingReports) SubmitNextReport();
        }

        private static void SubmitNextReport()
        {
            if (QueuedReports.Count < 1) return;
            var queuedReport = QueuedReports[0];
            submittingReports = true;
            Webrequests.EnqueuePost(Url, queuedReport.Body, (code, response) =>
            {
                if (code == 200)
                {
                    QueuedReports.RemoveAt(0);
                    submittingReports = false;
                    SubmitNextReport();
                }
                else
                {
                    Timers.Once(5f, SubmitNextReport);
                }
            }, null, queuedReport.Headers);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var callingMethod = (new StackTrace()).GetFrame(2).GetMethod();
            return callingMethod.DeclaringType?.FullName + "." + callingMethod.Name;
        }
    }
}
