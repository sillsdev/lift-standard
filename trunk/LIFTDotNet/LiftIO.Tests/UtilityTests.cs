using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class UtilityTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }

        [Test]
        public void EmptyLiftUnchanged()
        {
            string input = Path.GetTempFileName();
            Utilities.CreateEmptyLiftFile(input,"test",true);
            string output = Utilities.ProcessLiftForLaterMerging(input);
            Assert.AreEqual(File.ReadAllText(input), File.ReadAllText(output));
        }

        [Test]
        public void ExistingGuidsUnchanged()
        {
            string input = WriteFile("<entry guid='123abc'/>");
            string output = Utilities.ProcessLiftForLaterMerging(input);
            AssertXPathNotNull(output, "//entry[@guid='123abc']");
        }

        [Test]
        public void MissingGuidsAdded()
        {
            string input = WriteFile("<entry id='one'/><entry id='two'/>");
            string output = Utilities.ProcessLiftForLaterMerging(input);
            AssertXPathNotNull(output, "//entry[@id='one' and @guid]");
            AssertXPathNotNull(output, "//entry[@id='two' and @guid]");
        }

        [Test]
        public void InnerContentsUntouched()
        {
            string input = WriteFile("<entry id='one'><sense id='foo'><example/></sense></entry>");
            string output = Utilities.ProcessLiftForLaterMerging(input);
            AssertXPathNotNull(output, "//entry/sense[@id='foo']/example");
        }

        private string WriteFile(string xmlForEntries)
        {
            string output = Path.GetTempFileName();
            using (StreamWriter writer = File.CreateText(output))
            {
                string content =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift producer=\"test\" >" +
                    xmlForEntries + "</lift>";
                writer.Write(content);
                writer.Close();
            }
            return output;
        }


        private void AssertXPathNotNull(string documentPath, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(documentPath);
            XmlNode node = doc.SelectSingleNode(xpath);
            if (node == null)
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
                doc.WriteContentTo(writer);
                writer.Flush();
            }
            Assert.IsNotNull(node);
        }
    }

}