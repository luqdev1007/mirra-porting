using System;
using System.IO;
using Luqmus.MirraPorting.Report;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Report
{
    public class ReportWriterTests
    {
        private string _temp;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(Path.GetTempPath(), "MirraPortingTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temp))
            {
                Directory.Delete(_temp, true);
            }
        }

        [Test]
        public void PrepareDirectory_CreatesTheFolder()
        {
            ReportWriter.PrepareDirectory(_temp);

            Assert.IsTrue(Directory.Exists(_temp));
        }

        [Test]
        public void PrepareDirectory_RemovesTemporaryFilesFromAnInterruptedRun()
        {
            Directory.CreateDirectory(_temp);
            File.WriteAllText(Path.Combine(_temp, "report.json.tmp"), "half a report");
            File.WriteAllText(Path.Combine(_temp, "report.md"), "kept");

            ReportWriter.PrepareDirectory(_temp);

            Assert.IsFalse(File.Exists(Path.Combine(_temp, "report.json.tmp")));
            Assert.IsTrue(File.Exists(Path.Combine(_temp, "report.md")));
        }

        [Test]
        public void WriteText_WritesANewFile()
        {
            ReportWriter.PrepareDirectory(_temp);
            string path = Path.Combine(_temp, "report.json");

            ReportWriter.WriteText(path, "{ }");

            Assert.AreEqual("{ }", File.ReadAllText(path));
            CollectionAssert.IsEmpty(Directory.GetFiles(_temp, "*.tmp"));
        }

        [Test]
        public void WriteText_ReplacesAnExistingFile()
        {
            ReportWriter.PrepareDirectory(_temp);
            string path = Path.Combine(_temp, "report.json");
            ReportWriter.WriteText(path, "old");

            ReportWriter.WriteText(path, "new");

            Assert.AreEqual("new", File.ReadAllText(path));
            CollectionAssert.IsEmpty(Directory.GetFiles(_temp, "*.tmp"));
        }

        [Test]
        public void WriteText_WritesUtf8WithoutAByteOrderMark()
        {
            ReportWriter.PrepareDirectory(_temp);
            string path = Path.Combine(_temp, "report.md");

            ReportWriter.WriteText(path, "Отчёт");

            byte[] bytes = File.ReadAllBytes(path);
            Assert.AreNotEqual(0xEF, bytes[0], "The file starts with a byte order mark");
            Assert.AreEqual("Отчёт", File.ReadAllText(path));
        }
    }
}
