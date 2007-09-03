using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;

namespace LiftIO
{
    public class Utilities
    {

        /// <summary>
        /// Add guids
        /// </summary>
        /// <param name="inputPath"></param>
        /// <returns>path to a processed version</returns>
        static public string ProcessLiftForLaterMerging(string inputPath)
        {
            string outputOfPassOne = Path.GetTempFileName();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;//ugly, but great for merging with revision control systems

            // nb:  don't use XmlTextWriter.Create, that's broken. Ignores the indent setting
            using (XmlWriter writer = XmlWriter.Create(outputOfPassOne /*Console.Out*/, settings))
            {
                using (XmlReader reader = XmlTextReader.Create(inputPath))
                {
                    //bool elementWasReplaced = false;
                    while (!reader.EOF)
                    {
                        ProcessNode(reader, writer);
                    }
                }
            }

            XslCompiledTransform transform = new XslCompiledTransform();
            using ( Stream canonicalizeXsltStream= Assembly.GetExecutingAssembly().GetManifestResourceStream("LiftIO.canonicalizeLift.xsl"))
            {
                using (XmlReader xsltReader = XmlReader.Create(canonicalizeXsltStream))
                {
                    transform.Load(xsltReader);
                    xsltReader.Close();
                }
                canonicalizeXsltStream.Close();
            }

            string outputOfPassTwo = Path.GetTempFileName();
            using (Stream output = File.Create(outputOfPassTwo))
            {
                transform.Transform(outputOfPassOne, new XsltArgumentList(), output);
            }
            transform.TemporaryFiles.Delete();
            File.Delete(outputOfPassOne);

            return outputOfPassTwo;
        }



        private static void ProcessNode(XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.EndElement:
                case XmlNodeType.Element:
                    ProcessElement(reader, writer);
                    break;
                default:
                    Utilities.WriteShallowNode(reader, writer);
                    break;
            }
        }


        private static void ProcessElement(XmlReader reader, XmlWriter writer)
        {
                if (reader.Name == "entry")
                {
                    string guid = reader.GetAttribute("guid");
                    if (String.IsNullOrEmpty(guid))
                    {
                        guid = Guid.NewGuid().ToString();
                        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        writer.WriteAttributes(reader, true);
                        writer.WriteAttributeString("guid", guid);
                        string s = reader.ReadInnerXml();//this seems to be enough to get the reader to the next element
                        writer.WriteRaw(s);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteNode(reader, true);
                    }
                }
                else
                {
                    WriteShallowNode(reader, writer);
                }
        }

        static public void CreateEmptyLiftFile(string path, string producerString, bool doOverwriteIfExists)
        {
            if (File.Exists(path))
            {
                if (doOverwriteIfExists)
                {
                    File.Delete(path);
                }
                else
                {
                    return;
                }
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;//ugly, but great for merging with revision control systems
            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("lift");
                writer.WriteAttributeString("version", Validator.LiftVersion);
                writer.WriteAttributeString("producer", producerString);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }


        //came from a blog somewhere
        static internal void WriteShallowNode(XmlReader reader, XmlWriter writer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"),
                                        reader.Value);
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
            }
            reader.Read();
        }
    }
}
