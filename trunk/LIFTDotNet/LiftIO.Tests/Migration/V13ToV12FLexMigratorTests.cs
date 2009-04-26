using LiftIO.Migration;
using LiftIO.Tests.Migration;
using LiftIO.Validation;
using NUnit.Framework;

namespace LiftIO.Tests.Migration
{
    [TestFixture]
    public class V13To12MigratorTests : MigratorTestBase
    {

        [Test]
        public void Version13_Changed12()
        {
            using (TempFile f = new TempFile("<lift version='0.13'></lift>"))
            {
                using (TempFolder x = new TempFolder("13-12LiftMigrator"))
                {
                    TempFile toFile = x.GetPathForNewTempFile(false);

                    Migrator.ReverseMigrateFrom13ToFLEx12(f.Path, toFile.Path);
                    Assert.AreEqual("0.12", Validator.GetLiftVersion(toFile.Path));
                }
            }
        }


        [Test]
        public void SenseLiteralDefinition_WasOnSense_MovedToEntry()
        {
            using (TempFile fromFile = new TempFile("<lift version='0.13' producer='tester'>" +
                "<entry>" +
                    "<trait name='MorphType' value='stem'></trait>"+
                    "<sense>" +
                        "<trait name='semantic-domain-ddp4' value='2.5'></trait>"+
                        "<field type='scientific-name'><form lang='en'><text>word of science!</text></form></field>"+
                    "</sense>" +
                "</entry>" +
                "</lift>"))
            {
                using (TempFolder x = new TempFolder("13-12LiftMigrator"))
                {
                    TempFile toFile = x.GetPathForNewTempFile(false);

                    Migrator.ReverseMigrateFrom13ToFLEx12(fromFile.Path, toFile.Path);

                    Migrator.ReverseMigrateFrom13ToFLEx12(fromFile.Path, toFile.Path);
                    AssertXPathAtLeastOne("//lift[@producer='tester']", toFile.Path);
                    AssertXPathAtLeastOne("//entry/trait[@name='MorphType']", toFile.Path);
                    AssertXPathAtLeastOne("//entry/sense/trait[@name='semantic_domain']", toFile.Path);
                }
            }
        }  
        
    }
}