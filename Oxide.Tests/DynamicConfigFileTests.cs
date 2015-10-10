﻿using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Tests
{
    [TestClass]
    public class DynamicConfigFileTests
    {
        [TestMethod]
        public void WhenYouGetNotExistingValueTypeSetting_ThenDefaultInstanceIsCreated()
        {
            //Given
            var configFile = new DynamicConfigFile(string.Empty);

            //When
            var value = configFile.Get<int>("NotExistingSettingKey");

            //Then
            Assert.AreEqual(value, default(int));
        }

        [TestMethod]
        public void DynamicConfigLoadSaveTest()
        {
            const string inputfile = "{ \"x\": 10, \"y\": \"hello\", \"z\": [ 10, \"yo\" ], \"w\": { \"a\": 20, \"b\": [ 500, 600 ] } }";
            string filename = Path.Combine(Interface.Oxide.ConfigDirectory, Path.GetRandomFileName());
            File.WriteAllText(filename, inputfile);

            var cfg = new DynamicConfigFile(filename);
            cfg.Load();

            TestConfigFile(cfg);

            cfg.Save();
            cfg.Load();

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