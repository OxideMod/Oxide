using Microsoft.VisualStudio.TestTools.UnitTesting;

using Oxide.Core;

namespace Oxide.Tests
{
    [TestClass]
    public class CommandLineTests
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context) => Interface.Initialize();

        [TestMethod]
        public void TestCommandLine()
        {
            var cmdline = new CommandLine(new[] { "-batchmode", "+server.hostname", "My", "Server", "Name", "+server.port", "28015", "+server.identity", "facepunchdev", "+server.seed", "6738" });

            Assert.AreEqual(true, cmdline.HasVariable("server.hostname"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.port"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.identity"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("server.seed"), "Failed cmdline.HasVariable");
            Assert.AreEqual(false, cmdline.HasVariable("server.random"), "Failed cmdline.HasVariable");
            Assert.AreEqual(true, cmdline.HasVariable("batchmode"), "Failed cmdline.HasVariable");

            Assert.AreEqual("My Server Name", cmdline.GetVariable("server.hostname"), "Failed cmdline.GetVariable");
            Assert.AreEqual("28015", cmdline.GetVariable("server.port"), "Failed cmdline.GetVariable");
            Assert.AreEqual("facepunchdev", cmdline.GetVariable("server.identity"), "Failed cmdline.GetVariable");
            Assert.AreEqual("6738", cmdline.GetVariable("server.seed"), "Failed cmdline.GetVariable");
            Assert.AreEqual(null, cmdline.GetVariable("server.random"), "Failed cmdline.GetVariable");
            Assert.AreEqual(string.Empty, cmdline.GetVariable("batchmode"), "Failed cmdline.GetVariable");
        }
    }
}
