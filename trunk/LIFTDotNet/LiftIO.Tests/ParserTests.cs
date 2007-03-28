using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NMock2;
using NMock2.Matchers;
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

        /// <summary>
        /// only handles a single trait
        /// </summary>
        class LiftMultiTextTraitMatcher : NMock2.Matcher
        {
            private string _expectedLanguageOfFirstTrait;
            private string _expectedNameOfFirstTrait;
            private string _expectedValueOfFirstTrait;
            private int _expectedCount;

            public LiftMultiTextTraitMatcher(string expectedLanguageOfFirstTrait, string expectedNameOfFirstTrait, string expectedValueOfFirstTrait, int expectedNumberOfTraits)
            {
                _expectedLanguageOfFirstTrait = expectedLanguageOfFirstTrait;
                _expectedCount = expectedNumberOfTraits;
                _expectedValueOfFirstTrait = expectedValueOfFirstTrait;
                _expectedNameOfFirstTrait = expectedNameOfFirstTrait;
            }

            public override bool Matches(object o)
            {
                LiftMultiText m = o as LiftMultiText;
                if (m.Traits.Count != _expectedCount)
                {
                    return false;
                }
                LiftIO.Trait t = m.Traits[0];
                if (_expectedLanguageOfFirstTrait != null && _expectedLanguageOfFirstTrait != string.Empty)
                {
                    if (t.LanguageHint != _expectedLanguageOfFirstTrait)
                    {
                        return false;
                    }
                }
                return (t.Name == _expectedNameOfFirstTrait && t.Value == _expectedValueOfFirstTrait);
            }

            public override void DescribeTo(TextWriter writer)
            {
                writer.Write(string.Format("TraitMatcher(expectedLanguage={0}, expectedName={1}, expectedValue={2}, numberOfTraits={3})", _expectedLanguageOfFirstTrait, _expectedNameOfFirstTrait, _expectedValueOfFirstTrait,_expectedCount));
            }
        }

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
        public void MultipleFormsInOneLangAreCombined()
        {
            _doc.LoadXml("<foobar><form lang='x'><text>one</text></form><form lang='z'><text>zzzz</text></form><form lang='x'><text>two</text></form></foobar>");
            LiftMultiText t = _parser.ReadMultiText(_doc.FirstChild);
            Assert.AreEqual("one; two", t["x"]);
            Assert.AreEqual("zzzz", t["z"]);
        }



        [Test]
        public void FirstValueOfSimpleMultiText()
        {
            LiftMultiText t = new LiftMultiText();
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
            _doc.LoadXml(String.Format("<entry dateDeleted='{0}'/>", when));
            _parser.ReadEntry(_doc.FirstChild);
            _mocks.VerifyAllExpectationsHaveBeenMet();

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
            ExpectMergeInLexemeForm(Is.Anything);
            ParseEntryAndCheck(string.Format("<entry id=\"{0}\" />", g.ToString()), 
                 string.Format("{0};;;",g.ToString()));
        }
        [Test]
        public void EntryWithNonGuidId()
        {
            ExpectMergeInLexemeForm(Is.Anything);
            ParseEntryAndCheck(string.Format("<entry id=\"{0}\" />", "-foo-"),
                 string.Format("{0};;;", "-foo-"));
        }

        private void ParseEntryAndCheck(string content, string expectedIdString)
        {
            ExpectGetOrMakeEntry(expectedIdString);
            ExpectFinishEntry();
            
            _doc.LoadXml(content);
            _parser.ReadEntry(_doc.FirstChild);
            _mocks.VerifyAllExpectationsHaveBeenMet();
            
        }


        private void ParseEntryAndCheck(string content)
        {
            ExpectFinishEntry();
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
            ExpectMergeInLexemeForm(Is.Anything);
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
        private void ExpectMergeInGrammi(Matcher traitListMatcher)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInGrammaticalInfo")
                .With(Is.Anything, Is.Anything, traitListMatcher);
        }

        private void ExpectGetOrMakeExample()
        {
            Expect.Exactly(1).On(_merger)
                .Method("GetOrMakeExample")
                .Will(Return.Value(new Dummy()));
        }
        

        private void ExpectMergeInLexemeForm(Matcher matcher)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInLexemeForm")
                .With(Is.Anything,matcher);
        }
        private void ExpectFinishEntry()
        {
            Expect.Exactly(1).On(_merger)
                .Method("FinishEntry");
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

        private void ExpectMergeInTrait(Matcher traitMatcher)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInTrait")
                .With(Is.Anything, traitMatcher);
       }

        private void ExpectEntryWasDeleted()
        {
            Expect.Exactly(1).On(_merger)
                .Method("EntryWasDeleted");
            //todo expect more!
       }

        private void ExpectMergeInNote(string value)
        {
            Expect.Exactly(1).On(_merger)
                .Method("MergeInNote")
                .With(Is.Anything, Is.Anything/*todo type*/, Has.ToString(Is.EqualTo(value)));
        }
        
        
        
        
        
        
        [Test]
        public void EntryWithoutId()
        {
            ExpectMergeInLexemeForm(Is.Anything);
            ParseEntryAndCheck("<entry/>", ";;;");
        }

        [Test]
        public void EntryWithReadableIdPlusGuid()
        {
            ExpectMergeInLexemeForm(Is.Anything);
            Guid g = Guid.NewGuid();
//            string s = String.Format("<lift xmlns:flex='http://fieldworks.sil.org'><entry  id='-foo' flex:guid='{0}'/></lift>", g);
//
//            _doc.LoadXml(s);
//            _parser.ReadFile(_doc);
//

           // string s = String.Format("<entry xmlns:flex='http://fieldworks.sil.org' id='-foo' flex:guid='{0}'/>", g);
             string s = String.Format("<entry id='-foo' guid='{0}'/>", g);
          ParseEntryAndCheck(s, string.Format("-foo/{0};;;",g.ToString()));
        }

        [Test]
        public void FormMissingLangGeneratesNonFatalError()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ParseEntryAndCheck("<entry><lex><form/></lex></entry>");
            Assert.AreEqual(1, _parsingErrors.Count);
        }


        [Test]
        public void EmptyFormOk()
        {
           using (_mocks.Ordered)
            {
                ExpectGetOrMakeEntry(";;;");
                ExpectMergeInLexemeForm(Is.Anything);
            }
            ParseEntryAndCheck("<entry><lex><form lang='x'/></lex></entry>");
        }
        
        [Test]
        public void EntryWithLexemeForm()
        {
            ExpectGetOrMakeEntry();
            ExpectMultiTextMergeIn("LexemeForm", Has.Property("Count", Is.EqualTo(2)));
            ParseEntryAndCheck("<entry><lex><form lang='x'><text>hello</text></form><form lang='y'><text>bye</text></form></lex></entry>");
 //           ParseEntryAndCheck("<entry><lex><form lang='x'><text>hello</text></form><form lang='y'>bye</form></lex></entry>", "GetOrMakeEntry(;;;)MergeInLexemeForm(m,x=hello|y=bye|)");
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
//        private void ExpectMultiTextMergeIn(string MultiTextPropertyName, Matcher matcher)
//        {
//             Expect.Exactly(1).On(_merger)
//                            .Method("MergeIn" + MultiTextPropertyName)
//                            .With(Is.Anything, Has.Property("Traits",  matcher));
//       }

        private void ExpectMultiTextMergeIn(string MultiTextPropertyName, Matcher multiTextMatcher)
        {
            Expect.Exactly(1).On(_merger)
                           .Method("MergeIn" + MultiTextPropertyName)
                           .With(Is.Anything, multiTextMatcher);
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
            ExpectMergeInLexemeForm(Is.Anything);
            ParseEntryAndCheck(
                string.Format("<entry id='foo' dateCreated='{0}'></entry>", created));
        }

        [Test]
        public void DateWithoutTimeOk()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
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
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
           ParseEntryAndCheck(string.Format("<entry><sense></sense></entry>"));
        }

        [Test]
        public void SenseWithGloss()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss","x=hello|");
            ExpectMergeDefinition();
            
            ParseEntryAndCheck(string.Format("<entry><sense><gloss lang='x'><text>hello</text></gloss></sense></entry>"));
        }

        [Test]
        public void GlossWithTrait()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss", new LiftMultiTextTraitMatcher("x","flag","1", 1));
            ExpectMergeDefinition();

            ParseEntryAndCheck(string.Format("<entry><sense><gloss lang='x'><text>hello</text><trait name='flag' value='1'><annotation><form lang='x'><text>blah blah</text></form></annotation></trait></gloss></sense></entry>"));
        }

        [Test]
        public void LexicalUnitWithTrait()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(new LiftMultiTextTraitMatcher("x", "flag", "1", 1));
            ParseEntryAndCheck(string.Format("<entry><lexical-unit><form lang='x'><text>blah blah</text><trait name='flag' value='1'/></form></lexical-unit></entry>"));
        }

        /// <summary>
        /// e.g., a 
        /// </summary>
        [Test]
        public void SenseWithTraitThatHasSubTrait()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss", new LiftMultiTextTraitMatcher("x", "flag", "1", 1));
            ExpectMergeDefinition();

            ParseEntryAndCheck(string.Format("<entry><sense><gloss lang='x'><text>hello</text><trait name='flag' value='1'><annotation><form lang='x'><text>blah blah</text></form></annotation></trait></gloss></sense></entry>"));
        }

        [Test]
        public void GrammiWithTwoTraits()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss", Is.Anything);
            ExpectMergeDefinition();
            ExpectMergeInGrammi(Has.Property("Count", Is.EqualTo(2)));

            ParseEntryAndCheck(string.Format("<entry><sense><grammatical-info value='x'><trait name='one' value='1'/><trait name='two' value='2'/></grammatical-info></sense></entry>"));
        }

        [Test]
        public void GlossWithTwoLanguages()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss", "x=hello|y=bye|");
            ExpectMergeDefinition();

            ParseEntryAndCheck(string.Format("<entry><sense><gloss lang='x'><text>hello</text></gloss><gloss lang='y'><text>bye</text></gloss></sense></entry>"));
        }

        [Test]
        public void GlossWithTwoFormsInSameLanguageAreCombined()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMultiTextMergeIn("Gloss", "x=hello; bye|");
            ExpectMergeDefinition();

            ParseEntryAndCheck(string.Format("<entry><sense><gloss lang='x'><text>hello</text></gloss><gloss lang='x'><text>bye</text></gloss></sense></entry>"));
        }
        [Test]
        public void SenseWithDefintition()
        {
            ExpectEmptyEntry();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMultiTextMergeIn("Definition", "x=hello|");

            ParseEntryAndCheck(string.Format("<entry><sense><def><form lang='x'><text>hello</text></form></def></sense></entry>"));
        }

        [Test]
        public void SenseWithNote()
        {
            ExpectEmptyEntry();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectMergeInNote("x=hello|");

            ParseEntryAndCheck(string.Format("<entry><sense><note><form lang='x'><text>hello</text></form></note></sense></entry>"));
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
            ParseEntryAndCheck("<entry><field tag='color'><form lang='en'><text>red</text></form><form lang='es'><text>roco</text></form></field></entry>");

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
        public void SimpleTraitsAreRead()
        {
            ExpectEmptyEntry();
            ExpectMergeInTrait(new NMock2.Matchers.AndMatcher(
                    Has.Property("Name", Is.EqualTo("color")), Has.Property("Value", Is.EqualTo("red"))));
            ExpectMergeInTrait(new NMock2.Matchers.AndMatcher(
                    Has.Property("Name", Is.EqualTo("shape")), Has.Property("Value", Is.EqualTo("square"))));
            ParseEntryAndCheck(string.Format("<entry><trait name='color' value='red'/><trait name='shape' value='square'/></entry>"));
        }

        [Test]
        public void SenseWithGrammi()
        {
            ExpectEmptyEntry();
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectMergeInGrammi(Is.Anything);
            ParseEntryAndCheck("<entry><sense><grammatical-info value='blue'/></sense></entry>");
        }

        [Test]
        public void SenseWithExample()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectGetOrMakeExample();
            ExpectMultiTextMergeIn("ExampleForm", "x=hello|");
            ExpectMultiTextMergeIn("TranslationForm", "");

            ParseEntryAndCheck(
                string.Format("<entry><sense><example><form lang='x'><text>hello</text></form></example></sense></entry>"));
        }

        [Test]
        public void ExampleWithTranslation()
        {
            ExpectGetOrMakeEntry();
            ExpectMergeInLexemeForm(Is.Anything);
            ExpectGetOrMakeSense();
            ExpectMergeGloss();
            ExpectMergeDefinition();
            ExpectGetOrMakeExample();
            ExpectMultiTextMergeIn("ExampleForm", "");
            ExpectMultiTextMergeIn("TranslationForm", "x=hello|");

            ParseEntryAndCheck("<entry><sense><example><translation><form lang='x'><text>hello</text></form></translation></example></sense></entry>");
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