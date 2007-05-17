using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            FileInfo[] files = GetPendingUpdateFiles(pathToBaseLiftFile);
            if (files.Length < 1)
            {
                return;
            }
            Array.Sort(files, new FileInfoLastWriteTimeComparer());
            int count = files.Length;

            string pathToMergeInTo = pathToBaseLiftFile;// files[0].FullName;

            FileAttributes fa =  File.GetAttributes(pathToBaseLiftFile);
            if((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if(files[i].IsReadOnly)
                {
                    //todo: "Cannot merge safely because at least one file is read only: {0}
                    return;
                }
            }

            List<string> filesToDelete = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string outputPath = Path.GetTempFileName();
                try
                {
                    MergeInNewFile(pathToMergeInTo, files[i].FullName, outputPath);
                }
                catch(IOException)
                {
                    // todo: "Cannot most likely one of the files is locked
                    return;
                }
                pathToMergeInTo = outputPath;
                filesToDelete.Add(outputPath);
            }

            //string pathToBaseLiftFile = Path.Combine(directory, BaseLiftFileName);
            Debug.Assert(File.Exists(pathToMergeInTo));

            // File.Move works across volumes but the destination cannot exist.
            if (File.Exists(pathToBaseLiftFile))
            {
                string backupOfBackup;
                do
                {
                    backupOfBackup = pathToBaseLiftFile + Path.GetRandomFileName();
                }
                while(File.Exists(backupOfBackup));


                string bakPath = pathToBaseLiftFile+".bak";
                if (File.Exists(bakPath))
                {
                    // move the backup out of the way, if something fails here we have nothing to do
                    if ((File.GetAttributes(bakPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        bakPath = GetNextAvailableBakPath(bakPath);
                    }
                    else
                    {
                        try
                        {
                            File.Move(bakPath, backupOfBackup);
                        }
                        catch (IOException)
                        {
                            // back file is Locked. Create the next available backup Path
                            bakPath = GetNextAvailableBakPath(bakPath);
                        }
                    }
                }

                try
                {
                    File.Move(pathToBaseLiftFile, bakPath);

                    try
                    {
                        File.Move(pathToMergeInTo, pathToBaseLiftFile);
                    }
                    catch
                    {
                        // roll back to prior state
                        File.Move(bakPath, pathToBaseLiftFile);
                        throw;
                    }
                }
                catch 
                {
                    // roll back to prior state
                    if (File.Exists(backupOfBackup))
                    {
                        File.Move(backupOfBackup, bakPath);
                    }
                    throw; 
                }

                //everything was successful so can get rid of backupOfBackup
                if (File.Exists(backupOfBackup))
                {
                    File.Delete(backupOfBackup);
                }
            }
            else
            {
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

            //delete all our temporary files
            foreach (string s in filesToDelete)
            {
                File.Delete(s);
            }
        }

        public static FileInfo[] GetPendingUpdateFiles(string pathToBaseLiftFile)
        {
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(pathToBaseLiftFile));
            return di.GetFiles("*"+ExtensionOfIncrementalFiles, SearchOption.TopDirectoryOnly);
        }

        static private void TestWriting(XmlWriter w)
        {
         //  w.WriteStartDocument();
            w.WriteStartElement("start");
            w.WriteElementString("one", "hello");
            w.WriteElementString("two", "bye");
            w.WriteEndElement();
        //   w.WriteEndDocument();
        }
       static public void TestWritingFile()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
         // nb:  don't use XmlTextWriter.Create, that's broken. Ignores the indent setting
            using (XmlWriter writer = XmlWriter.Create("C:\\test.xml", settings))
            {
                TestWriting(writer);
            }
        }

        private static string GetNextAvailableBakPath(string bakPath) {
            int i = 0;
            string newBakPath;
            do
            {
                i++;
                newBakPath = bakPath + i;
            }
            while (File.Exists(newBakPath));
            bakPath = newBakPath;
            return bakPath;
        }

        static private void MergeInNewFile(string olderFilePath, string newerFilePath, string outputPath)
        {
            XmlDocument newerDoc = new XmlDocument();
            newerDoc.Load(newerFilePath);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.NewLineOnAttributes = true;//ugly, but great for merging with revision control systems
            settings.Indent = true;
            settings.IndentChars = "\t";

         // nb:  don't use XmlTextWriter.Create, that's broken. Ignores the indent setting
            using (XmlWriter writer = XmlWriter.Create(outputPath /*Console.Out*/, settings)) 
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


            //empty lift file, write new elements

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

            //hit the end, write out any remaing new elements

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


        internal class FileInfoLastWriteTimeComparer : IComparer<FileInfo>
        {
            public int Compare(FileInfo x, FileInfo y)
            {
                return DateTime.Compare(x.LastWriteTime, y.LastWriteTime);
            }
        }

       

    }
}
