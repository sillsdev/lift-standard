using LiftIO.Migration;
using LiftIO.Tests.Migration;
using LiftIO.Validation;
using NUnit.Framework;

namespace LiftIO.Tests.Migration
{
    [TestFixture]
    public class V11To12MigratorTests : MigratorTestBase
    {
        [Test]
        public void HasEtymologyInSense_EtymologyMovedToEntry()
        {
            using (TempFile f = new TempFile("<lift version='0.11'><entry><sense><etymology/></sense></entry></lift>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                using (TempFile.TrackExisting(path))
                {
                    AssertXPathAtLeastOne("//entry/etymology", path);
                }
            }
        }

        [Test]
        public void Version11_ChangedToVersionLatest()
        {
            using (TempFile f = new TempFile("<lift version='0.11'></lift>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                Assert.AreEqual(Validator.LiftVersion, Validator.GetLiftVersion(path));
            }
        }


        [Test]
        public void PreservesProducer()
        {
            using (TempFile f = new TempFile("<lift version='0.11' producer='p'/>"))
            {
                string path = Migrator.MigrateToLatestVersion(f.Path);
                AssertXPathAtLeastOne("//lift[@producer='p']", path);
            }
        }
   
        
    }
}