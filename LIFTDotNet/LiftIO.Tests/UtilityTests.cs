using System;
using System.IO;
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

//       even an empty only is getting cannonicalized [Test]
//        public void EmptyLiftUnchanged()
//        {
//            string input = Path.GetTempFileName();
//            Utilities.CreateEmptyLiftFile(input,"test",true);
//            string output = Utilities.ProcessLiftForLaterMerging(input);
//            Assert.AreEqual(File.ReadAllText(input), File.ReadAllText(output));
//        }

        [Test]
        public void ExistingGuidsUnchanged()
        {
            using (TempFile f =  TempFile.CreateWithXmlHeader("<entry guid='123abc'/>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(f.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@guid='123abc']");
            }
        }

        [Test]
        public void MissingGuidsAdded()
        {
            using (TempFile file =  TempFile.CreateWithXmlHeader("<entry id='one'/><entry id='two'/>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(file.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@id='one' and @guid]");
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@id='two' and @guid]");
            }
        }


        [Test]
        public void MissingHumanReadableIdsAdded_NoGuid()
        {
            using (TempFile f =  TempFile.CreateWithXmlHeader("<entry><lexical-unit><form lang='v'><text>kindness</text></form></lexical-unit></entry>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(f.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@id and @guid]");
            }
        }

        [Test]
        public void MissingHumanReadableIdsAdded_AlreadyHadGuid()
        {
            using (TempFile f = TempFile.CreateWithXmlHeader("<entry guid='6b4269b9-f5d4-4e48-ad91-17109d9882e4'><lexical-unit ><form lang='v'><text>kindness</text></form></lexical-unit></entry>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(f.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@id and @guid]");
            }
        }

        [Test]
        public void NoIdAddedIf_NoLexemeFormToUse()
        {
            using (TempFile f = TempFile.CreateWithXmlHeader("<entry></entry>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(f.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry[@guid and not(@id)]");
            }
        }

        [Test]
        public void InnerContentsUntouched()
        {
            using (TempFile f = TempFile.CreateWithXmlHeader("<entry id='one'><sense id='foo'><example/></sense></entry>"))
            {
                string output = Utilities.ProcessLiftForLaterMerging(f.Path);
                XmlTestHelper.AssertXPathNotNull(output, "//entry/sense[@id='foo']/example");
            }
        }

      
    }

}