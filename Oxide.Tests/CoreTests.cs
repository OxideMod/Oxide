using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Tests
{
    [TestClass]
    public class CoreTests
    {
        [TestMethod]
        public void TestCommandLine()
        {
            CommandLine cmdline = new CommandLine(new string[] { "-batchmode", "+server.hostname", "My", "Server", "Name", "+server.port", "28015", "+server.identity", "facepunchdev", "+server.seed", "6738" });

            Assert.AreEqual(true, cmdline.HasVariable("batchmode"), "Failed cmdline.HasFlag");
            Assert.AreEqual(false, cmdline.HasVariable("random"), "Failed cmdline.HasFlag");
            Assert.AreEqual(false, cmdline.HasVariable("server.hostname"), "Failed cmdline.HasFlag");

            Assert.AreEqual(true, cmdline.HasVariable("server.hostname"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.port"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.identity"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.seed"), "Failed cmdline.HasVariable");
            Assert.AreEqual(false, cmdline.HasVariable("server.random"), "Failed cmdline.HasVariable");
            Assert.AreEqual(false, cmdline.HasVariable("batchmode"), "Failed cmdline.HasVariable");

            Assert.AreEqual("My Server Name", cmdline.GetVariable("server.hostname"), "Failed cmdline.GetVariable");
            Assert.AreEqual("28015", cmdline.GetVariable("server.port"), "Failed cmdline.GetVariable");
            Assert.AreEqual("facepunchdev", cmdline.GetVariable("server.identity"), "Failed cmdline.GetVariable");
            Assert.AreEqual("6738", cmdline.GetVariable("server.seed"), "Failed cmdline.GetVariable");
            Assert.AreEqual(null, cmdline.GetVariable("server.random"), "Failed cmdline.GetVariable");
            Assert.AreEqual(null, cmdline.GetVariable("batchmode"), "Failed cmdline.GetVariable");
        }

        [TestMethod]
        public void TestDynamicConfig()
        {
            const string inputfile = "{ \"x\": 10, \"y\": \"hello\", \"z\": [ 10, \"yo\" ], \"w\": { \"a\": 20, \"b\": [ 500, 600 ] } }";
            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, inputfile);

            var cfg = ConfigFile.Load<DynamicConfigFile>(filename);

            TestConfigFile(cfg);

            cfg.Save();
            cfg = ConfigFile.Load<DynamicConfigFile>(filename);

            TestConfigFile(cfg);

            File.Delete(filename);
        }

        private void TestConfigFile(DynamicConfigFile cfg)
        {
            Assert.AreEqual(10, cfg["x"], "Failed cfg.x");
            Assert.AreEqual("hello", cfg["y"], "Failed cfg.y");

            var list = cfg["z"] as List<object>;
            Assert.AreNotEqual(null, list, "Failed cfg.z");
            if (list != null)
            {
                Assert.AreEqual(2, list.Count, "Failed cfg.z.Count");
                if (list.Count == 2)
                {
                    Assert.AreEqual(10, list[0], "Failed cfg.z[0]");
                    Assert.AreEqual("yo", list[1], "Failed cfg.z[1]");
                }
            }

            var dict = cfg["w"] as Dictionary<string, object>;
            Assert.AreNotEqual(null, dict, "Failed cfg.w");
            if (dict != null)
            {
                Assert.AreEqual(2, dict.Count, "Failed cfg.w.Count");
                if (dict.Count == 2)
                {
                    object tmp;
                    Assert.AreEqual(true, dict.TryGetValue("a", out tmp), "Failed cfg.w.a");
                    Assert.AreEqual(20, tmp, "Failed cfg.w.a");
                    Assert.AreEqual(true, dict.TryGetValue("b", out tmp), "Failed cfg.w.b");

                    list = tmp as List<object>;
                    Assert.AreNotEqual(null, list, "Failed cfg.w.b");
                    if (list != null)
                    {
                        Assert.AreEqual(2, list.Count, "Failed cfg.w.b.Count");
                        if (list.Count == 2)
                        {
                            Assert.AreEqual(500, list[0], "Failed cfg.w.b[0]");
                            Assert.AreEqual(600, list[1], "Failed cfg.w.b[1]");
                        }
                    }
                }
            }
        }
    }
}
