using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace LiftIO.Tests
{
    public class XmlTestHelper
    {
        public static void AssertXPathMatchesExactlyOne(string xml, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            AssertXPathMatchesExactlyOneInner(doc, xpath);
        }
        public static void AssertXPathMatchesExactlyOne(XmlNode node, string xpath)
        {
             XmlDocument doc = new XmlDocument();
            doc.LoadXml(node.OuterXml);
            AssertXPathMatchesExactlyOneInner(doc, xpath);
       }

        private static void AssertXPathMatchesExactlyOneInner(XmlDocument doc, string xpath)
        {
            XmlNodeList nodes = doc.SelectNodes(xpath);
            if (nodes == null || nodes.Count != 1)
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
                doc.WriteContentTo(writer);
                writer.Flush();
                if (nodes != null && nodes.Count > 1)
                {
                    Assert.Fail("Too Many matches for XPath: {0}", xpath);
                }
                else
                {
                    Assert.Fail("No Match: XPath failed: {0}", xpath);
                }
            }
        }

        public static void AssertXPathNotNull(string documentPath, string xpath)
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

    public class TempFile : IDisposable
    {
        private string _path;

        public TempFile()
        {
            _path = System.IO.Path.GetTempFileName();
        }


        public TempFile(string contents)
            : this()
        {
            File.WriteAllText(_path, contents);
        }

        public static TempFile CreateWithXmlHeader(string xmlForEntries)
        {
                string content =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift producer=\"test\" >" +
                    xmlForEntries + "</lift>";
            return new TempFile(content);

        }

        public string Path
        {
            get { return _path; }
        }
        public void Dispose()
        {
            File.Delete(_path);
        }

        private TempFile(string existingPath, bool dummy)
        {
            _path = existingPath;
        }

        public static TempFile TrackExisting(string path)
        {
            return new TempFile(path, false);
        }
    }
}

