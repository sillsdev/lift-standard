using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using LiftIO.Merging;
using LiftIO.Validation;
using NUnit.Framework;

namespace LiftIO.Tests.Merging
{
    [TestFixture]
    public class SynchronicMergerTests
    {
        private const string _baseLiftFileName = "base.lift";

        private string _directory;
        private SynchronicMerger _merger;

        [SetUp]
        public void Setup()
        {
            _merger = new SynchronicMerger();
            _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDOwn()
        {
//            DirectoryInfo di = new DirectoryInfo(_directory);          
            Directory.Delete(_directory, true);
        }

        private static string GetNextUpdateFileName()
        {
            // Linux filesystem only has resolution of 1 second so we add this to filename so will sort correctly
            return _baseLiftFileName + DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss'-'FFFFFFF UTC ") + SynchronicMerger.ExtensionOfIncrementalFiles;
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void GetPendingUpdateFiles_SimpleFileNameInsteadOfPath_Throws()
        {
            WriteFile(_baseLiftFileName, "", _directory);
            SynchronicMerger.GetPendingUpdateFiles(_baseLiftFileName);
        }

        [Test]
        public void OneFile_NoUpdates_LeftUntouched()
        {
            string content = WriteFile(_baseLiftFileName, "<entry id=\"\u0E0C\" guid=\"0ae89610-fc01-4bfd-a0d6-1125b7281dd1\"></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(false, _directory);
            ExpectFileCount(1, _directory);
            Assert.AreEqual(2, doc.SelectNodes("//entry").Count);
            Assert.IsTrue(GetBaseFileInfo().Length >= content.Length);

        }

        private FileInfo GetBaseFileInfo()
        {
            DirectoryInfo di = new DirectoryInfo(_directory);
            return di.GetFiles(_baseLiftFileName, SearchOption.TopDirectoryOnly)[0];
        }

        //private FileInfo[] GetFileInfos()
        //{
        //    DirectoryInfo di = new DirectoryInfo(_directory);
        //    return di.GetFiles("*"+SynchronicMerger.ExtensionOfIncrementalFiles, SearchOption.TopDirectoryOnly);
        //}

        [Test]
        public void NewEntries_Added()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='three3'></entry><entry id='four'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(4, doc.SelectNodes("//entry").Count);
        }

        [Test]
        public void EdittedEntry_Updated()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(2, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
        }

        [Test]
        public void EdittedEntry_BothOldAndNewHaveEscapedIllegalCharacter_Updated()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'>&#x1F;Foo</entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1'>&#x1F;Bar</entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry").Count);
            var entry= doc.SelectSingleNode("//entry[@id='one']");
            Assert.AreEqual("&#x1F;Bar", entry.InnerXml);
        }

        [Test]
        public void EdittedEntry_GuidSameIdDifferent_Updated()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='one1' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(2, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one1']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one1' and @greeting='hello']").Count);
        }

        [Test, Ignore("WS-236")]
        public void EdittedEntry_IdSameGuidDifferent_Updated()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd2' greeting='hello'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(3, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hi']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@greeting='hello']").Count);
        }

        [Test]
        [ExpectedException(typeof(BadUpdateFileException))]
        public void BaseHasEntryWithoutGuid_Throws()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd2' greeting='hello'></entry>", _directory);
            Merge(_directory);
        }


        [Test]
        public void ExistingBackup_Ok()
        {
            File.CreateText(Path.Combine(_directory, _baseLiftFileName + ".bak")).Dispose();
            WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, GetNextUpdateFileName());
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
        }

        [Test]
        public void WorksWithTempDirectoryOnADifferentVolumne()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                //Ignored on non-Windows
                return;
            }

            //testing approach: it's harder to get the temp locaiton changed, so we
            // instead put the destination project over on the non-default volume
            DriveInfo[] drives = DriveInfo.GetDrives();

            // get a drive I might be able to use
            string driveName = string.Empty;
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady &&
                    drive.DriveType != DriveType.CDRom &&
                    drive.Name != "C:\\")
                {
                    driveName = drive.Name;
                    break;
                }
            }
            if (driveName.Length == 0)
            {
                Console.WriteLine("Ignored when there is not an additional volume");
            }
            else
            {
                string directory;
                do
                {
                    directory = Path.Combine(driveName, Path.GetRandomFileName());
                } while (Directory.Exists(directory));

                Directory.CreateDirectory(directory);
                File.CreateText(Path.Combine(directory, _baseLiftFileName + ".bak")).Dispose();
                WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(directory, GetNextUpdateFileName());
                XmlDocument doc = MergeAndGetResult(true, directory);
                Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
                Directory.Delete(directory, true);
            }
        }


        [Test]
        public void ReadOnlyBaseFile_DoesNothing()
        {
            string baseFilePath = Path.Combine(this._directory, _baseLiftFileName);
            try
            {
                WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, GetNextUpdateFileName());
                File.SetAttributes(baseFilePath, FileAttributes.ReadOnly);

                Merge(_directory);
            }
            finally
            {
                File.SetAttributes(baseFilePath, FileAttributes.Normal);
            }

            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hi']").Count);
            ExpectFileCount(2, _directory);
        }

        [Test]
        public void ReadOnlyUpdate_DoesNothing()
        {
            string updateFilePath = Path.Combine(this._directory, GetNextUpdateFileName());
            try
            {
                WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, updateFilePath);
                File.SetAttributes(updateFilePath, FileAttributes.ReadOnly);

                Merge(_directory);
            }
            finally
            {
                File.SetAttributes(updateFilePath, FileAttributes.Normal);
            }

            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hi']").Count);
            ExpectFileCount(2, _directory);
        }

        [Test, ExpectedException(typeof(IOException))]
        public void LockedBaseFile_Throws()
        {
            string baseFilePath = Path.Combine(this._directory, _baseLiftFileName);
            WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, GetNextUpdateFileName());
            using(File.Open(baseFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Merge(_directory);
            }

            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hi']").Count);
            ExpectFileCount(2, _directory);
        }

        [Test, ExpectedException(typeof(IOException))]
        public void LockedUpdate_Throws()
        {
            string updateFilePath = Path.Combine(this._directory, GetNextUpdateFileName());

            WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, updateFilePath);
            using (File.Open(updateFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Merge(_directory);
            }

            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hi']").Count);
            ExpectFileCount(2, _directory);
        }


        [Test]
        public void ReadOnlyBackupFile_StillMakesBackup()
        {
            string backupFilePath = Path.Combine(this._directory, _baseLiftFileName + ".bak");
            File.CreateText(backupFilePath).Dispose();

            WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, GetNextUpdateFileName());
            File.SetAttributes(backupFilePath, FileAttributes.ReadOnly);
            Merge(_directory);
            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
            ExpectFileCount(3, _directory); //lift, readonly bak and new bak2
            File.SetAttributes(backupFilePath, FileAttributes.Normal);
        }

        [Test]
        public void LockedBackupFile_StillMakesBackup()
        {
            string backupFilePath = Path.Combine(this._directory, _baseLiftFileName + ".bak");
            File.CreateText(backupFilePath).Dispose();

            WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(_directory, GetNextUpdateFileName());
            using (File.Open(backupFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Merge(_directory);
            }
            XmlDocument doc = GetResult(_directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
            ExpectFileCount(3, _directory); //lift, locked bak and new unlocked bak2
        }

        static private void WriteBaseAndUpdateFilesSoMergedWillHaveHelloInsteadOfHi(string directory, string updateFile)
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", directory);
            WriteFile(updateFile, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>", directory);
        }

        /// <summary>
        /// This is a regression test... had <lift> and the lack of a closing 
        /// </lift> meant that the new item was never added
        /// </summary>
        [Test]
        public void AddingToEmptyLift() 
        {
            WriteEmptyLift();
            WriteFile(GetNextUpdateFileName(), "<entry id='one' greeting='hello'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(1, doc.SelectNodes("//lift[@preserveMe='foo']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
        }

        private void WriteEmptyLift()
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(_directory, _baseLiftFileName)))
            {
                string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift preserveMe='foo'/>";
                writer.Write(content);
                writer.Close();
            }
        }

        [Test]
        public void AddingToEmptyLift_HasIllegalUnicode_DoesNotCrash()
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(_directory, _baseLiftFileName)))
            {
                string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><lift/>";
                writer.Write(content);
                writer.Close();
            }
            WriteFile(GetNextUpdateFileName(), @"
                <entry id='one'>
                    <lexical-unit>
                          <form lang='bth'>
                            <text>&#x1F;</text>
                         </form>
                    </lexical-unit>
                </entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
        }

        [Test]
        public void EditOneAddOne()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='three'></entry><entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='three']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one' and @greeting='hello']").Count);
            Assert.AreEqual(3, doc.SelectNodes("//entry").Count);
        }

        [Test]
        public void ThreeFiles()
        {
            WriteFile(_baseLiftFileName, "<entry id='one' guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hi'></entry><entry id='two' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22' greeting='hi'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='three' guid='0ae89610-fc01-4bfd-a0d6-1125b7281d33'></entry><entry id='one'  guid='0ae89610-fc01-4bfd-a0d6-1125b7281dd1' greeting='hello'></entry>", _directory);
            WriteFile(GetNextUpdateFileName(), "<entry id='two'  guid='0ae89610-fc01-4bfd-a0d6-1125b7281d22' greeting='hello'></entry><entry id='four' ></entry>", _directory);
            XmlDocument doc = MergeAndGetResult(true, _directory);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='one']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='three']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='four']").Count);
            Assert.AreEqual(1, doc.SelectNodes("//entry[@id='two' and @greeting='hello']").Count);
            Assert.AreEqual(4, doc.SelectNodes("//entry").Count);
        }

        private XmlDocument MergeAndGetResult(bool isBackupFileExpected, string directory)
        {
            Merge(directory);
            ExpectFileCount(isBackupFileExpected?2:1, directory);

            return GetResult(directory);
        }

        private static XmlDocument GetResult(string directory) {
            XmlDocument doc = new XmlDocument();
            string outputPath = Path.Combine(directory,_baseLiftFileName);
            doc.Load(outputPath);
            Console.WriteLine(File.ReadAllText(outputPath));
            return doc;
        }

        private void Merge(string directory) {
            this._merger.MergeUpdatesIntoFile(Path.Combine(directory, _baseLiftFileName));
        }

        static private void ExpectFileCount(int count, string directory)
        {
            string[] files = Directory.GetFiles(directory);
            
            StringBuilder fileList = new StringBuilder();
            foreach (string s in files)
            {
                fileList.Append(s);
                fileList.Append('\n');
            }
            Assert.AreEqual(count, files.Length, fileList.ToString());
        }

        static private string WriteFile(string fileName, string xmlForEntries, string directory)
        {
            StreamWriter writer = File.CreateText(Path.Combine(directory, fileName));
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                             +"<lift version =\""
                             + Validator.LiftVersion
                             +"\" producer=\"WeSay.1Pt0Alpha\" xmlns:flex=\"http://fieldworks.sil.org\">"
                             +xmlForEntries
                             +"</lift>";
            writer.Write(content);
            writer.Close();
            writer.Dispose();

            //pause so they don't all have the same time
            Thread.Sleep(100);

            return content;
        }
    }
}