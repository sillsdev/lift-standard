using System;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class MigratorTests
    {
        [Test]
        public void IsMigrationNeeded_V10_ReturnsTrue()
        {
            using (TempFile f = new TempFile("<lift version='0.10'></lift>"))
            {
                Assert.IsTrue(Migrator.IsMigrationNeeded(f.Path));
            }
        }

        [Test]
        public void IsMigrationNeeded_Latest_ReturnsFalse()
        {
            using (TempFile f = new TempFile(string.Format("<lift version='{0}'></lift>", Validator.LiftVersion)))
            {
                Assert.IsFalse(Migrator.IsMigrationNeeded(f.Path));
            }
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void MigrateToLatestVersion_HasCurrentVersion_Throws()
        {
            using (TempFile f = new TempFile(string.Format("<lift version='{0}'></lift>", Validator.LiftVersion)))
            {
                Migrator.MigrateToLatestVersion(f.Path);
            }
        }

        [Test]
        public void MigrateToLatestVersion_IsOldVersion_ReturnsDifferentPath()
        {
            using (TempFile f = new TempFile("<lift version='0.10'></lift>"))
            {
                Assert.AreNotEqual(f.Path, Migrator.MigrateToLatestVersion(f.Path));
            }
        }

        /// <summary>
        /// this is important because if we change the behavior to use, say, temp,
        /// that could be a different volumne, which can make some File operations
        /// fail (like rename).  
        /// </summary>
        [Test]
        public void MigrateToLatestVersion_ResultingFileInSameDirectory()
        {
            using (TempFile f = new TempFile("<lift version='0.10'></lift>"))
            {
                Assert.AreEqual(Path.GetDirectoryName(f.Path), Path.GetDirectoryName(Migrator.MigrateToLatestVersion(f.Path)));
            }
        }

        [Test, ExpectedException(typeof(LiftFormatException))]
        public void MigrateToLatestVersion_VersionWithoutMigrationXsl_Throws()
        {
            using (TempFile f = new TempFile("<lift version='0.5'></lift>"))
            {
                Migrator.MigrateToLatestVersion(f.Path);
            }
        }

        [Test]
        public void MigrateToLatestVersion_V10_ConvertedToLatest()
        {
            using (TempFile f = new TempFile("<lift version='0.10'></lift>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                Assert.AreEqual(Validator.LiftVersion, Validator.GetLiftVersion(path));
            }
        }

        [Test]
        public void MigrateToLatestVersion_V10_TraitNameChangedToType()
        {
            using (TempFile f = new TempFile("<lift version='0.10'><entry><relation name='foo'/></entry></lift>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                using (TempFile.TrackExisting(path))
                {
                    AssertXPathNotNull("//relation/@type", path);
                }
            }
        }

        [Test]
        public void MigrateToLatestVersion_V11_EtymologyMoved()
        {
            using (TempFile f = new TempFile("<lift version='0.11'><entry><sense><etymology/></sense></entry></lift>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                using (TempFile.TrackExisting(path))
                {
                    AssertXPathNotNull("//entry/etymology", path);
                }
            }
        }


        private void AssertXPathNotNull(string xpath, string filePath)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filePath);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Console.WriteLine(File.ReadAllText(filePath));
            }
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