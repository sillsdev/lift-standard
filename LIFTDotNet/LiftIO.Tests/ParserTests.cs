using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NMock2;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class ParserTests
    {
        private ILexiconMerger<DummyBase, Dummy, Dummy, Dummy> _merger;
        private LiftParser<DummyBase, Dummy, Dummy, Dummy> _parser;
        private XmlDocument _doc;
        public StringBuilder _results;
        private Mockery _mocks;
        private List<LiftParser<DummyBase, Dummy, Dummy, Dummy>.ErrorArgs> _parsingErrors;


        [SetUp]
        public void Setup()
        {
            //_parsingErrors = new List<Exception>();
            _doc = new XmlDocument();
            //_doc.DocumentElement.SetAttribute("xmlns:flex", "http://fieldworks.sil.org");
            
            _mocks = new Mockery();
            _merger = _mocks.NewMock<ILexiconMerger<DummyBase, Dummy, Dummy, Dummy>>();
            _parser = new LiftParser<DummyBase, Dummy, Dummy, Dummy>(_merger);
            _parsingErrors = new List<LiftParser<DummyBase, Dummy, Dummy, Dummy>.ErrorArgs>();
            _parser.ParsingError += new EventHandler<LiftParser<DummyBase, Dummy, Dummy, Dummy>.ErrorArgs>(OnParsingError);
        }

        void OnParsingError(object sender, LiftParser<DummyBase, Dummy, Dummy, Dummy>.ErrorArgs e)
        {
            _parsingErrors.Add(e);
        }

        [TearDown]
        public void TearDown()
        {


        }

        [Test]
        public void GoodLiftValidates()
        {
            string contents = "<lift version='0.9'></lift>";
            Validate(contents, true);
        }
        [Test]
        public void BadLiftDoesNotValidate()
        {
            string contents = "<lift version='0.9'><header></header><header></header></lift>";
            Validate(contents, false);
        }

        private static void Validate(string contents, bool shouldPass)
        {
            string f = Path.GetTempFileName();
            File.WriteAllText(f, contents);
            try
            {
                Assert.AreEqual(shouldPass, LiftParser<DummyBase, Dummy, Dummy, Dummy>.CheckValidity(f));
            }
            finally
            {
                File.Delete(f);
            }
        }


        [Test]
        public void MultipleFormsInOneLangAreCombined()
        {
            _doc.LoadXml("<gloss><form lang='x'>one</form><form lang='z'>zzzz</form><form lang='x'>two</form></gloss>");
            SimpleMultiText t = _parser.ReadMultiText(_doc.FirstChild);
            Assert.AreEqual("one; two", t["x"]);
            Assert.AreEqual("zzzz", t["z"]);
        }

        [Test]
        public void FirstValueOfSimpleMultiText()
        {
            SimpleMultiText t = new SimpleMultiText();
            t.Add("x", "1");
            t.Add("y", "2");
            Assert.AreEqual("x", t.FirstValue.Key);
            Assert.AreEqual("1", t.FirstValue.Value);
        }

        [Test]
        public void EmptyLiftOk()
        {
            SimpleCheckGetOrMakeEntry("<lift/>", 0);
        }

        [Test]
        public void EntryMissingIdNonFatal()
        {
            SimpleCheckGetOrMakeEntry("<lift><entry/></lift>", 1);
        }

        [Test]
        public void EmptyEntriesOk()
        {
            SimpleCheckGetOrMakeEntry("<lift><entry/><entry/></lift>", 2);
        }
        [Test]
        public void NotifyOfDeletedEntry()
        {
            DateTime now = DateTime.UtcNow;
            string when = now.ToString(Extensible.LiftTimeFormatNoTimeZone);
            ExpectEntryWasDeleted();            //todo expect more!
            ParseEntryAndCheck(String.Format("<entry dateDeleted='{0}'/>", when));
        }
        private void SimpleCheckGetOrMakeEntry(string content, int times)
        {
            _doc.LoadXml(content);
            using (_mocks.Ordered)
            {
                Expect.Exactly(times).On(_merger)
                    .Method("GetOrMakeEntry")
                    .WithAnyArguments()
                    .Will(Return.Value(null));
            }
            _parser.ReadFile(_doc);
            _mocks.VerifyAllExpectationsHaveBeenMet();
        }

        [Test]
        public void EntryWithGuidId()
        {
            Guid g = Guid.NewGuid();
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck(string.Format("<entry id=\"{0}\" />", g.ToString()), 
                 string.Format("{0};;;",g.ToString()));
        }
        [Test]
        public void EntryWithNonGuidId()
        {
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck(string.Format("<entry id=\"{0}\" />", "-foo-"),
                 string.Format("{0};;;", "-foo-"));
        }

        private void ParseEntryAndCheck(string content, string expectedIdString)
        {
            ExpectGetOrMakeEntry(expectedIdString);
            
            _doc.LoadXml(content);
            _parser.ReadEntry(_doc.FirstChild);
            _mocks.VerifyAllExpectationsHaveBeenMet();
            
        }


        private void ParseEntryAndCheck(string content)
        {
            _doc.LoadXml(content);
            _parser.ReadEntry(_doc.FirstChild);
            _mocks.VerifyAllExpectationsHaveBeenMet();
        }

        private void ExpectGetOrMakeEntry(string expectedIdString)
        {
            Expect.Exactly(1).On(_merger)
                .Method("GetOrMakeEntry")
                //.With(Is.Anything)
                .With(Has.ToString(Is.EqualTo(expectedIdString)))
                .Will(Return.Value(new Dummy()));
        }

        private void ExpectEmptyEntry()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
        }


        private void ExpectGetOrMakeEntry()
        {
            Expect.Exactly(1).On(_merger)
                .Method("GetOrMakeEntry")
                .Will(Return.Value(new Dummy()));
        }

        private void ExpectGetOrMakeSense()
        {
            Expect.Exactly(1).On(_merger)
                .Method("GetOrMakeSense")
                .Will(Return.Value(new Dummy()));
        }
        private void ExpectMergeInGrammi()
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInGrammaticalInfo");
        }

        private void ExpectGetOrMakeExample()
        {
            Expect.Exactly(1).On(_merger)
                .Method("GetOrMakeExample")
                .Will(Return.Value(new Dummy()));
        }
        
        private void ExpectMergeInLexemeForm(string exactMultiTextToString)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInLexemeForm")
                .With(Is.Anything, Is.EqualTo(exactMultiTextToString));
        }
        private void ExpectMergeInLexemeForm()
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInLexemeForm");
        }
        private void ExpectMergeGloss()
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInGloss");
        }
        private void ExpectMergeDefinition()
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInDefinition");
        }


        private void ExpectMergeInField(Matcher tagMatcher, Matcher dateCreatedMatcher, Matcher dateModifiedMatcher, Matcher multiTextMatcher)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInField").With(Is.Anything, tagMatcher, 
                dateCreatedMatcher, dateModifiedMatcher, multiTextMatcher);
            //  .Method("MergeInField").With(matchers);
        }


//        private void ExpectMergeInField(params object[] matchers)
//        {
//            Expect.Exactly(1).On(_merger)
//                .Method("MergeInField").With(Is.Anything, Is.Anything, Is.Anything, Is.Anything, Is.Anything);
//              //  .Method("MergeInField").With(matchers);
//        }

        private void ExpectMergeInTrait(string nameAttribute, string valueAttribute, string optionalId)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInTrait")
                .With(Is.Anything, Is.EqualTo(nameAttribute), Is.EqualTo(valueAttribute), Is.EqualTo(optionalId));
       }

        private void ExpectEntryWasDeleted()
        {
            Expect.Exactly(1).On(_merger)
                .Method("EntryWasDeleted");
            //todo expect more!
       }
        
        private void ExpectMergeInNote()
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInNote");
        }
        
        
        
        
        
        
        [Test]
        public void EntryWithoutId()
        {
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck("<entry/>", ";;;");
        }

        [Test]
        public void EntryWithReadableIdPlusGuid()
        {
            ExpectMergeInLexemeForm();
            Guid g = Guid.NewGuid();
//            string s = String.Format("<lift xmlns:flex='http://fieldworks.sil.org'><entry  id='-foo' flex:guid='{0}'/></lift>", g);
//
//            _doc.LoadXml(s);
//            _parser.ReadFile(_doc);
//

            string s = String.Format("<entry xmlns:flex='http://fieldworks.sil.org' id='-foo' flex:guid='{0}'/>", g);
           ParseEntryAndCheck(s, string.Format("-foo/{0};;;",g.ToString()));
        }

        [Test]
        public void FormMissingLangGeneratesNonFatalError()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck("<entry><lex><form/></lex></entry>");
            Assert.AreEqual(1, _parsingErrors.Count);
        }


        [Test]
        public void EmptyFormOk()
        {
           using (_mocks.Ordered)
            {
                ExpectGetOrMakeEntry(";;;");
                ExpectMergeInLexemeForm();
            }
            ParseEntryAndCheck("<entry><lex><form lang='x'/></lex></entry>");
        }
        
        [Test]
        public void EntryWithLexemeForm()
        {
            ExpectGetOrMakeEntry();
            ExpectMultiTextMergeIn("LexemeForm", Has.Property("Count", Is.EqualTo(2)));
            ParseEntryAndCheck("<entry><lex><form lang='x'>hello</form><form lang='y'>bye</form></lex></entry>");
 //           ParseEntryAndCheck("<entry><lex><form lang='x'>hello</form><form lang='y'>bye</form></lex></entry>", "GetOrMakeEntry(;;;)MergeInLexemeForm(m,x=hello|y=bye|)");
        }

         private void ExpectEmptyMultiTextMergeIn(string MultiTextPropertyName)
        {
            Expect.Exactly(1).On(_merger)
                            .Method("MergeIn" + MultiTextPropertyName)
                            .With(Is.Anything, Has.Property("Count",Is.EqualTo(0)));

        }
        
        private void ExpectMultiTextMergeIn(string MultiTextPropertyName, string value)
        {
             Expect.Exactly(1).On(_merger)
                            .Method("MergeIn" + MultiTextPropertyName)
                            .With(Is.Anything, Has.ToString(Is.EqualTo(value)));
       }

        private void ExpectMultiTextMergeIn(string MultiTextPropertyName, Matcher multiTextMatcher)
        {
            Expect.Exactly(1).On(_merger)
                           .Method("MergeIn" + MultiTextPropertyName)
                           .With(Is.Anything, multiTextMatcher);
        }

        [Test]
        public void EntryWithLexemeForm_NoFormTag()
        {
            ExpectGetOrMakeEntry();
            ExpectMultiTextMergeIn("LexemeForm", "??=hello|");
            ParseEntryAndCheck("<entry><lex>hello</lex></entry>");
            //            ParseEntryAndCheck("<entry><lex>hello</lex></entry>","GetOrMakeEntry(;;;)MergeInLexemeForm(m,??=hello)");
        }

        [Test]
        public void NonLiftDateError()
        {
            TryDateFormat("last tuesday");
            TryDateFormat("2005-01-01T01:11:11");
            TryDateFormat("1/2/2003");
            Assert.AreEqual(3, _parsingErrors.Count);
        }

        private void TryDateFormat(string created)
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck(
                string.Format("<entry id='foo' dateCreated='{0}'></entry>", created));
        }

        [Test]
        public void DateWithoutTimeOk()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ParseEntryAndCheck("<entry id='foo' dateCreated='2005-01-01'></entry>");
            Assert.AreEqual(0, _parsingErrors.Count);
        }

        [Test]
        public void EntryWithDates()
        {
            string createdIn = "2003-08-07T08:42:42+07:00";
            string modIn = "2005-01-01T01:11:11+07:00";
            string createdOut = "2003-08-07T01:42:42Z"; // has to be UTC (in - 7 hours)
            string modOut = "2004-12-31T18:11:11Z"; // has to be UTC (in - 7 hours)
            ExpectGetOrMakeEntry(String.Format("foo;{0};{1};", createdOut, modOut));

            //getting {foo;2003-08-07T01:42:42Z;2004-12-31T    18    :11:11Z;}  

//
//            Expect.Exactly(1).On(_merger)
//    .Method("GetOrMakeEntry")
//                //.With(Is.Anything)
//    .With(Has.ToString(Is.EqualTo("foo;2003-08-07T01:42:42Z;2004-12-31T06:11:11Z")))
//    .Will(Return.Value(new Dummy()));
//

            ExpectEmptyMultiTextMergeIn("LexemeForm");
            ParseEntryAndCheck(
                string.Format("<entry id='foo' dateCreated='{0}' dateModified='{1}'></entry>", createdIn, modIn));

        }




        [Test]
        public void EntryWithSense()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
           ParseEntryAndCheck(string.Format("<entry><sense></sense></entry>"));
        }

        [Test]
        public void SenseWithGloss()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss","x=hello|");
            ExpectMergeDefinition();
            ParseEntryAndCheck(string.Format("<entry><sense><gloss><form lang='x'>hello</form></gloss></sense></entry>"));
        }
        [Test]
        public void SenseWithDefintition()
        {
            ExpectEmptyEntry();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMultiTextMergeIn("Definition", "x=hello|");

            ParseEntryAndCheck(string.Format("<entry><sense><def><form lang='x'>hello</form></def></sense></entry>"));
        }

        [Test]
        public void ReadsExpectedFieldEntityAsMultiText()
        {
            ExpectEmptyEntry();
            ExpectMergeInField(
                Is.EqualTo("color"),
                Is.EqualTo(default(DateTime)),
                Is.EqualTo(default(DateTime)),
                Has.Property("Count", Is.EqualTo(2))
                );
            ParseEntryAndCheck("<entry><field tag='color'><form lang='en'>red</form><form lang='es'>roco</form></field></entry>");

            ExpectEmptyEntry();
            DateTime creat = new DateTime(2000,1,1).ToUniversalTime();
            string createdTime = creat.ToString(Extensible.LiftTimeFormatNoTimeZone);
            DateTime mod = new DateTime(2000, 1, 2).ToUniversalTime();
            string modifiedTime = mod.ToString(Extensible.LiftTimeFormatNoTimeZone);
            ExpectMergeInField(
                Is.EqualTo("color"),
               Is.EqualTo(creat),
               Is.EqualTo(mod),
               Is.Anything
                );
            ParseEntryAndCheck(String.Format("<entry><field tag='color' dateCreated='{0}'  dateModified='{1}' ></field></entry>",
                createdTime,
                modifiedTime));
        }

        [Test]
        public void Trait()
        {
            ExpectEmptyEntry();
            ExpectMergeInTrait("color","red",null);
            ParseEntryAndCheck(string.Format("<entry><trait name='color' value='red'/></entry>"));

            ExpectEmptyEntry();
            ExpectMergeInTrait("color", "red", "myid");
            ParseEntryAndCheck(string.Format("<entry><trait name='color' value='red' id='myid'/></entry>"));
        }

        [Test, Ignore("Not implemented")]
        public void SenseWithSemanticDomains()
        {
            ParseEntryAndCheck(string.Format("<entry><sense></sense></entry>"),
                "");
        }

        [Test]
        public void SenseWithGrammi()
        {
            ExpectEmptyEntry();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectMergeInGrammi();
            ParseEntryAndCheck("<entry><sense><grammatical-info value='blue'/></sense></entry>");
        }

        [Test]
        public void SenseWithExample()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectGetOrMakeExample();
            ExpectMultiTextMergeIn("ExampleForm", "x=hello|");
            ExpectMultiTextMergeIn("TranslationForm", "");

            ParseEntryAndCheck(
                string.Format("<entry><sense><example><form lang='x'>hello</form></example></sense></entry>"));
        }

        [Test]
        public void ExampleWithTranslation()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectGetOrMakeExample();
            ExpectMultiTextMergeIn("ExampleForm", "");
            ExpectMultiTextMergeIn("TranslationForm", "x=hello|");

            ParseEntryAndCheck("<entry><sense><example><translation><form lang='x'>hello</form></translation></example></sense></entry>");
            //    "GetOrMakeEntry(;;;)GetOrMakeSense(m,)GetOrMakeExample(m,)MergeInTranslationForm(m,x=hello|)");
        }

        /*
         * 
        /// <summary>
        /// when I wrote the flex exporter, lift did not yet implement semantic domain
        /// </summary>
        [Test, Ignore("Not yet implemented in WeSay")]
        public void SemanticDomainTraitIsBroughtInCorrectly()
        {
            _doc.LoadXml("<trait range=\"semantic-domain\" value=\"6.5.1.1\"/>");
            //TODO   _importer.ReadTrait(_doc.SelectSingleNode("wrap"));
        }

        /// <summary>
        /// when I wrote the flex exporter, lift did not yet implement part of speech
        /// </summary>
        [Test, Ignore("Not yet implemented in WeSay")]
        public void GrammiWithTextLabel()
        {
            _doc.LoadXml("<sense><grammi type=\"conc\"/></sense>");
            //TODO   _importer.ReadSense(_doc.SelectSingleNode("sense"));
        }

        /// <summary>
        /// when I wrote the flex exporter, lift did not yet implement part of speech
        /// </summary>
        [Test, Ignore("Not yet implemented in WeSay")]
        public void GrammiWithEmptyLabel()
        {
            _doc.LoadXml("<sense><grammi type=\"\"/></sense>");
            //TODO   _importer.ReadSense(_doc.SelectSingleNode("sense"));
        }


         * */

        private void ParseAndCheck(string content, string expectedResults)
        {
            _doc.LoadXml(content);
            _parser.ReadFile(_doc);
            Assert.AreEqual(expectedResults, _results.ToString());
        }

//        private void ParseEntryAndCheck(string content, string expectedResults)
//        {
//            _doc.LoadXml(content);
//            _parser.ReadEntry(_doc.FirstChild);
//            Assert.AreEqual(expectedResults, _results.ToString());
//        }
    }

    public class DummyBase
    {
    }

    public class Dummy : DummyBase
    {
        public override string ToString()
        {
            return "m";
        }
    }
/*
    class TestLiftMerger : ILexiconMerger<Dummy, Dummy, Dummy>
    {
        public StringBuilder _results;

        public TestLiftMerger(StringBuilder results)
        {
            _results = results;
        }

        public Dummy GetOrMakeEntry(IdentifyingInfo idInfo)
        {
            _results.AppendFormat("GetOrMakeEntry({0})",idInfo);
            return new Dummy();
        }

        public void MergeInLexemeForm(Dummy entry, SimpleMultiText forms)
        {
            _results.AppendFormat("MergeInLexemeForm({0},{1})", entry, GetStingFromMultiText(forms));
       } 

        private static string GetStingFromMultiText(SimpleMultiText forms)
        {
            string s="";
            foreach (string key in forms.Keys)
            {
                s += string.Format("{0}={1}|", key, forms[key]);
            }
            return s;
        }

        public Dummy GetOrMakeSense(Dummy entry, IdentifyingInfo idInfo)
        {
            _results.AppendFormat("GetOrMakeSense({0},{1})", entry, idInfo);
            return new Dummy();
        }

        public Dummy GetOrMakeExample(Dummy sense, IdentifyingInfo idInfo)
        {
            _results.AppendFormat("GetOrMakeExample({0},{1})", sense, idInfo);
            return new Dummy();
        }


        public void MergeInGloss(Dummy sense, SimpleMultiText forms)
        {
            _results.AppendFormat("MergeInGloss({0},{1})", sense, GetStingFromMultiText(forms));
        }

        public void MergeInExampleForm(Dummy example, SimpleMultiText forms)
        {
            _results.AppendFormat("MergeInExampleForm({0},{1})", example, GetStingFromMultiText(forms));
        }

        public void MergeInTranslationForm(Dummy example, SimpleMultiText forms)
        {
            _results.AppendFormat("MergeInTranslationForm({0},{1})", example, GetStingFromMultiText(forms));
        }
    }*/

}