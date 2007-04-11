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
      //  private string _pathToBaseLiftFile;
        public const  string ExtensionOfIncrementalFiles = ".lift.update";

        public void MergeUpdatesIntoFile(string pathToBaseLiftFile)
        {
           // _pathToBaseLiftFile = pathToBaseLiftFile;

            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(pathToBaseLiftFile));
            FileInfo[] files = di.GetFiles("*"+ExtensionOfIncrementalFiles, SearchOption.TopDirectoryOnly);
            if (files.Length < 1)
            {
                return;
            }
            Array.Sort<FileInfo>(files, new FileInfoComparer());
            int count = files.Length;

            string pathToMergeInTo = pathToBaseLiftFile;// files[0].FullName;

            for (int i = 0; i < count; i++)
            {
                if(files[i].IsReadOnly)
                {
                    //todo: "Cannot merge safely because at least one file is locked: {0}
                    return;
                }
            }

            for (int i = 0; i < count; i++)
            {
                string outputPath = Path.GetTempFileName();
                MergeInNewFile(pathToMergeInTo, files[i].FullName, outputPath);
                pathToMergeInTo = outputPath;

                //TODO Delete temp files this creates
            }

            //string pathToBaseLiftFile = Path.Combine(directory, BaseLiftFileName);
            Debug.Assert(File.Exists(pathToMergeInTo));
            if (File.Exists(pathToBaseLiftFile))
            {
                string bakPath = pathToBaseLiftFile+".bak";
                if (File.Exists(bakPath))
                {
                    File.Delete(bakPath);
                }
                File.Replace(pathToMergeInTo, pathToBaseLiftFile, bakPath);
            }
            else
            {
                //todo: this is not going to work across volumes
                File.Move(pathToMergeInTo, pathToBaseLiftFile);
            }

            //delete all the non-base paths
            foreach (FileInfo file in files)
            {
                if (file.FullName != pathToBaseLiftFile)
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
                    Utilities.WriteShallowNode(olderReader, writer);
                    break;
            }
        }

        private static void ProcessElement(XmlReader olderReader, XmlWriter writer, XmlDocument newerDoc)
        {
            if ( olderReader.Name == "lift" && olderReader.IsEmptyElement) //i.e., <lift/>
            {
                writer.WriteStartElement("lift");
                writer.WriteAttributes(olderReader, true);
                foreach(XmlNode n in newerDoc.SelectNodes("//entry")) 
                {
                   writer.WriteNode(n.CreateNavigator(), true /*REVIEW*/);//REVIEW CreateNavigator
                }
                //write out the closing lift element
                writer.WriteEndElement();
                olderReader.Read();
            }
            else if (olderReader.Name == "lift" &&
                olderReader.NodeType == XmlNodeType.EndElement)
            {
                foreach (XmlNode n in newerDoc.SelectNodes("//entry")) //REVIEW CreateNavigator
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
                    string oldId = olderReader.GetAttribute("guid");
                    if (String.IsNullOrEmpty(oldId))
                    {
                        throw new ApplicationException("All entries must have guid attributes in order for merging to work. " + olderReader.Value);
                    }
                    XmlNode match= newerDoc.SelectSingleNode("//entry[@guid='" + oldId + "']");
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
                    Utilities.WriteShallowNode(olderReader, writer);
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

       

    }
}
