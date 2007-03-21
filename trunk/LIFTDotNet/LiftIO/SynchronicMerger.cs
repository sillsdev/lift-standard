using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace LiftIO
{
    /// <summary>
    /// Class to merge two or more LIFT files that are created incrementally, such that
    /// 1) the data in previous ones is overwritten by data in newer ones
    /// 2) the *entire contents* of the new entry element replaces the previous contents
    /// I.e., merging is only on an entry level.
    /// </summary>
    public class SynchronicMerger
    {
        public const string BaseLiftFileName = "base.lift.xml";

        public void MergeDirectory(string directory)
        {
            
            DirectoryInfo di = new DirectoryInfo(directory);
            FileInfo[] files = di.GetFiles("*lift.xml", SearchOption.TopDirectoryOnly);
            if (files.Length < 2)
            {
                return;
            }
            Array.Sort<FileInfo>(files, new FileInfoComparer());
            int count = files.Length;
            
            string pathToMergeInTo = files[0].FullName;

            for (int i = 0; i < count; i++)
            {
                if(files[i].IsReadOnly)
                {
                    //todo: "Cannot merge safely because at least one file is locked: {0}
                    return;
                }
            }

            for (int i = 1 /*skip first one*/; i < count; i++)
            {
                string outputPath = Path.GetTempFileName();
                MergeInNewFile(pathToMergeInTo, files[i].FullName, outputPath);
                pathToMergeInTo = outputPath;

                //TODO Delete temp files this creates
            }

            string basePath = Path.Combine(directory, BaseLiftFileName);
            Debug.Assert(File.Exists(pathToMergeInTo));
            if (File.Exists(basePath))
            {
                string bakPath = basePath+".bak";
                if (File.Exists(bakPath))
                {
                    File.Delete(bakPath);
                }
                File.Replace(pathToMergeInTo, basePath, bakPath);
            }
            else
            {
                File.Move(pathToMergeInTo, basePath);
            }

            //delete all the non-base paths
            foreach (FileInfo file in files)
            {
                if (file.Name.ToLower() != BaseLiftFileName.ToLower())
                {
                    file.Delete();
                }
            }
        }

        private void MergeInNewFile(string olderFilePath, string newerFilePath, string outputPath)
        {
            XmlDocument newerDoc = new XmlDocument();
            newerDoc.Load(newerFilePath);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlTextWriter.Create(outputPath /*Console.Out*/, settings)) 
            {
                //For each entry in the new guy, read through the whole base file
                using (XmlReader olderReader = XmlTextReader.Create(olderFilePath))
                {
                    //bool elementWasReplaced = false;
                    while (!olderReader.EOF)
                    {
                        ProcessOlderNode(olderReader, newerDoc, writer);
                    }
                }
            }
        }


        private static void ProcessOlderNode(XmlReader olderReader, XmlDocument newerDoc, XmlWriter writer)
        {
            switch (olderReader.NodeType)
            {
                case XmlNodeType.EndElement:
                case XmlNodeType.Element:
                    ProcessElement(olderReader, writer, newerDoc);
                    break;
                default:
                    WriteShallowNode(olderReader, writer);
                    break;
            }
        }

        private static void ProcessElement(XmlReader olderReader, XmlWriter writer, XmlDocument newerDoc)
        {
            if ( olderReader.NodeType == XmlNodeType.EndElement && olderReader.Name == "lift")
            {
                foreach(XmlNode n in newerDoc.SelectNodes("//entry")) //REVIEW CreateNavigator
                {
                   writer.WriteNode(n.CreateNavigator(), true /*REVIEW*/);
                }
                //write out the closing lift element
                 writer.WriteNode(olderReader, true);
            }
            else
            {
                if (olderReader.Name == "entry")
                {
                    string oldId = olderReader.GetAttribute("id");
                    XmlNode match= newerDoc.SelectSingleNode("//entry[@id='" + oldId + "']");
                    if (match != null)
                    {
                        olderReader.Skip(); //skip the old one
                        writer.WriteNode(match.CreateNavigator(), true); //REVIEW CreateNavigator
                        match.ParentNode.RemoveChild(match);
                   }
                    else
                    {
                        writer.WriteNode(olderReader, true);
                    }
                }
                else
                {
                    WriteShallowNode(olderReader, writer);
                }
            }
        }


        internal class FileInfoComparer : IComparer<FileInfo>
        {
            public int Compare(FileInfo x, FileInfo y)
            {
                return DateTime.Compare(((FileInfo)x).LastWriteTime, ((FileInfo)y).LastWriteTime);
            }
        }

        static void WriteShallowNode(XmlReader reader, XmlWriter writer)
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
