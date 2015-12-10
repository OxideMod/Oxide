using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NLua;

using Oxide.Core.Configuration;
using Oxide.Ext.Lua;

struct TestStruct {}

namespace Oxide.Tests
{
    [TestClass]
    public class LuaTests
    {
        [TestMethod]
        public void TestLuaTableConfig()
        {
            Lua lua = new Lua();

            const string inputfile = "{ \"x\": 10, \"y\": \"hello\", \"z\": [ 10, \"yo\" ], \"w\": { \"a\": 20, \"b\": [ 500, 600 ] } }";
            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, inputfile);

            var cfg = ConfigFile.Load<DynamicConfigFile>(filename);

            TestConfigFile(cfg); // This should always pass so long as the CoreTests pass

            LuaTable tbl = Utility.TableFromConfig(cfg, lua);

            Assert.AreEqual(10.0, tbl["x"], "Failed tbl.x");
            Assert.AreEqual("hello", tbl["y"], "Failed tbl.y");

            Assert.IsInstanceOfType(tbl["z"], typeof(LuaTable), "Failed tbl.z");
            LuaTable ztbl = tbl["z"] as LuaTable;
            if (ztbl != null)
            {
                Assert.IsNull(ztbl[0], "Failed tbl.z[0]");
                Assert.AreEqual(10.0, ztbl[1], "Failed tbl.z[1]");
                Assert.AreEqual("yo", ztbl[2], "Failed tbl.z[2]");
                Assert.IsNull(ztbl[3], "Failed tbl.z[3]");
            }

            Assert.IsInstanceOfType(tbl["w"], typeof(LuaTable), "Failed tbl.w");
            LuaTable wtbl = tbl["w"] as LuaTable;
            if (wtbl != null)
            {
                Assert.AreEqual(20.0, wtbl["a"], "Failed tbl.w.a");

                Assert.IsInstanceOfType(wtbl["b"], typeof(LuaTable), "Failed tbl.w.b");
                LuaTable wbtbl = wtbl["b"] as LuaTable;
                if (wbtbl != null)
                {
                    Assert.IsNull(wbtbl[0], "Failed tbl.w.b[0]");
                    Assert.AreEqual(500.0, wbtbl[1], "Failed tbl.w.b[1]");
                    Assert.AreEqual(600.0, wbtbl[2], "Failed tbl.w.b[2]");
                    Assert.IsNull(wbtbl[3], "Failed tbl.w.b[3]");
                }
            }

            cfg = new DynamicConfigFile(Path.GetTempFileName());
            Utility.SetConfigFromTable(cfg, tbl);

            TestConfigFile(cfg);
        }

        private void TestConfigFile(DynamicConfigFile cfg)
        {
            Assert.AreEqual(10, cfg["x"], "Failed cfg.x");
            Assert.AreEqual("hello", cfg["y"], "Failed cfg.y");

            var list = cfg["z"] as List<object>;
            Assert.IsNotNull(list, "Failed cfg.z");
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
            Assert.IsNotNull(dict, "Failed cfg.w");
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
                    Assert.IsNotNull(list, "Failed cfg.w.b");
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

        [TestMethod]
        public void TestEmptyTableInConfig()
        {
            Lua lua = new Lua();

            lua.LoadString("TeleportData = { AdminTP = {}, Test = 3, ABC=4 }", "test").Call();

            LuaTable tdata = lua["TeleportData"] as LuaTable;
            DynamicConfigFile cfgfile = new DynamicConfigFile(Path.GetTempFileName());
            Utility.SetConfigFromTable(cfgfile, tdata);

            Assert.AreEqual(3, cfgfile["Test"], "Failed TeleportData.Test");
            Assert.AreEqual(4, cfgfile["ABC"], "Failed TeleportData.ABC");

            //Assert.IsInstanceOfType(cfgfile["AdminTP"], typeof(List<string, object>), "Failed TeleportData.AdminTP");

            string tmp = Path.GetTempFileName();
            cfgfile.Save();

            string text = File.ReadAllText(tmp);
            File.Delete(tmp);
        }

        [TestMethod]
        public void TestGetNamespace()
        {
            Assert.AreEqual("System", Utility.GetNamespace(typeof(UInt16)));
            Assert.AreEqual("System.Collections.Generic", Utility.GetNamespace(typeof(List<string>)));
            Assert.AreEqual("", Utility.GetNamespace(typeof(TestStruct)));
        }
    }
}
