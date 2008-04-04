using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace LiftIO
{
    public class LiftParser<TBase, TEntry, TSense, TExample> 
        where TBase : class
        where TEntry : TBase
        where TSense : TBase
        where TExample : TBase 
    {
        // Parsing Errors should throw an exception
        public event EventHandler<ErrorArgs> ParsingWarning;
        public event EventHandler<StepsArgs> SetTotalNumberSteps;
        public event EventHandler<ProgressEventArgs> SetStepsCompleted;

        private readonly ILexiconMerger<TBase, TEntry, TSense, TExample> _merger;
        private const string _wsAttributeLabel = "lang";

        private bool _cancelNow = false;
        private DateTime _defaultCreationModificationUTC;
//        private string _defaultLangId="??";


        public LiftParser(ILexiconMerger<TBase, TEntry, TSense, TExample> merger)
        {
            _merger = merger;
        }
    
        /// <summary>
        /// 
        /// </summary>
        public virtual void ReadFile(XmlDocument doc, DateTime defaultCreationModificationUTC)
        {
            _defaultCreationModificationUTC = defaultCreationModificationUTC;
            XmlNodeList entryNodes = doc.SelectNodes("/lift/entry");
            int count = 0;
            const int kInterval = 50;
            int nextProgressPoint = count + kInterval;
            ProgressTotalSteps = entryNodes.Count;
            foreach (XmlNode node in entryNodes)
            {
                ReadEntry(node);
                count++;
                if (count >= nextProgressPoint)
                {
                    ProgressStepsCompleted = count;
                    nextProgressPoint = count + kInterval;
                }
                if (_cancelNow)
                {
                    break;
                }
            }
        }

        public TEntry ReadEntry(XmlNode node)
        {
            Extensible extensible = ReadExtensibleElementBasics(node);
            DateTime dateDeleted = GetOptionalDate(node, "dateDeleted", default(DateTime));
            if(dateDeleted != default(DateTime))
            {
                _merger.EntryWasDeleted(extensible, dateDeleted);
                return default(TEntry);
            }

            TEntry entry = _merger.GetOrMakeEntry(extensible);
            if (entry == null)// pruned
            {
                return entry;
            }


            LiftMultiText lexemeForm = LocateAndReadMultiText(node, "lexical-unit");
            if (!lexemeForm.IsEmpty)
            {
                _merger.MergeInLexemeForm(entry, lexemeForm);
            }
            LiftMultiText citationForm = LocateAndReadMultiText(node, "citation");
            if (!citationForm.IsEmpty)
            {
                _merger.MergeInCitationForm(entry, citationForm);
            }

            ReadNotes(node, entry);

            foreach (XmlNode n in node.SelectNodes("sense"))
            {
                ReadSense(n, entry);
            }

            foreach (XmlNode n in node.SelectNodes("relation"))
            {
                ReadRelation(n, entry);
            }
            
            ReadExtensibleElementDetails(entry, node);
            _merger.FinishEntry(entry);
            return entry;
        }

        private void ReadNotes(XmlNode node, TBase e)
        {
            foreach (XmlNode noteNode in node.SelectNodes("note"))
            {
                string noteType = GetOptionalAttributeString(noteNode, "type");
                LiftMultiText noteText = ReadMultiText(noteNode);
                _merger.MergeInNote(e, noteType, noteText);
            }
        }

        private void ReadRelation(XmlNode n, TBase parent)
        {
            string targetId = GetStringAttribute(n, "ref");
            string relationFieldName = GetStringAttribute(n, "name");

            _merger.MergeInRelation(parent, relationFieldName, targetId);
        }

        private void ReadPicture(XmlNode n, TSense parent)
        {
            string href = GetStringAttribute(n, "href");
            LiftMultiText caption = LocateAndReadMultiText(n, "label");
            if(caption.IsEmpty)
            {
                caption = null;
            }
            _merger.MergeInPicture(parent, href, caption);
        }

        protected void ReadGrammi(TSense sense, XmlNode senseNode)
        {
            XmlNode grammiNode = senseNode.SelectSingleNode("grammatical-info");
            if (grammiNode != null)
            {
                string val = GetStringAttribute(grammiNode, "value");
                _merger.MergeInGrammaticalInfo(sense, val, GetTraitList(grammiNode));
            }
        }

        /// <summary>
        /// Used for elements with traits that are not top level objects (extensibles) or forms.
        /// In Mar 2007, this appied only to grammatical-info elements
        /// </summary>
        /// <param name="grammi"></param>
        /// <returns></returns>
        private static List<Trait> GetTraitList(XmlNode grammi)
        {
            List<Trait> traits = new List<Trait>();
            foreach (XmlNode traitNode in grammi.SelectNodes("trait"))
            {
                traits.Add(GetTrait(traitNode));
            }
            return traits;
        }

        public TSense ReadSense(XmlNode node, TEntry entry)
        {
            TSense sense = _merger.GetOrMakeSense(entry, ReadExtensibleElementBasics(node));
            if (sense != null)//not been pruned
            {
                ReadGrammi(sense, node);
                LiftMultiText gloss = LocateAndReadOneElementPerFormData(node, "gloss");
                if (!gloss.IsEmpty)
                {
                    _merger.MergeInGloss(sense, gloss);
                }



                LiftMultiText def = LocateAndReadMultiText(node, "definition");
                if (!def.IsEmpty)
                {
                    _merger.MergeInDefinition(sense, def);
                }

                ReadNotes(node, sense);

                foreach (XmlNode n in node.SelectNodes("example"))
                {
                    ReadExample(n, sense);
                }
                foreach (XmlNode n in node.SelectNodes("relation"))
                {
                    ReadRelation(n, sense);
                }

                foreach (XmlNode n in node.SelectNodes("picture"))
                {
                    ReadPicture(n, sense);
                } 
                ReadExtensibleElementDetails(sense, node);
            }
            return sense;
        }

        private TExample ReadExample(XmlNode node, TSense sense)
        {
            TExample example = _merger.GetOrMakeExample(sense, ReadExtensibleElementBasics(node));
            if (example != null)//not been pruned
            {
                LiftMultiText exampleSentence = LocateAndReadMultiText(node, null);
                if (!exampleSentence.IsEmpty)
                {
                    _merger.MergeInExampleForm(example, exampleSentence);
                }
                //NB: only one translation supported in LIFT at the moment
                LiftMultiText translation = LocateAndReadMultiText(node, "translation");
                if (!translation.IsEmpty)
                {
                    _merger.MergeInTranslationForm(example, translation);
                }

                string source = GetOptionalAttributeString(node, "source");
                if (source != null)
                {
                    _merger.MergeInSource(example, source);
                }

                ReadNotes(node, example);

                ReadExtensibleElementDetails(example, node);
            }
            return example;
        }

        /// <summary>
        /// read enough for finding a potential match to merge with
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Extensible ReadExtensibleElementBasics(XmlNode node)
        {
            Extensible extensible = new Extensible();
            extensible.Id = GetOptionalAttributeString(node, "id");//actually not part of extensible (as of 8/1/2007)

           //todo: figure out how to actually look it up:
            //      string flexPrefix = node.OwnerDocument.GetPrefixOfNamespace("http://fieldworks.sil.org");
//            string flexPrefix = "flex";
//            if (flexPrefix != null && flexPrefix != string.Empty)
            {
                string guidString = GetOptionalAttributeString(node, /*flexPrefix + ":guid"*/"guid");
                if (guidString != null)
                {
                    try
                    {
                        extensible.Guid = new Guid(guidString);
                    }
                    catch (Exception)
                    {
                        NotifyError(new LiftFormatException(String.Format("{0} is not a valid GUID", guidString)));
                    }
                }
            }
            extensible.CreationTime = GetOptionalDate(node, "dateCreated", _defaultCreationModificationUTC);
            extensible.ModificationTime = GetOptionalDate(node, "dateModified", _defaultCreationModificationUTC);

            return extensible;
        }
        
        /// <summary>
        /// Once we have the thing we're creating/merging with, we can read in any details,
        /// i.e. traits, fields, and annotations
        /// </summary>
        /// <param name="target"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected void ReadExtensibleElementDetails(TBase target, XmlNode node)
        {
            foreach (XmlNode fieldNode in node.SelectNodes("field"))
            {
                string fieldType = GetStringAttribute(fieldNode, "tag");
                string priorFieldWithSameTag = String.Format("preceding-sibling::field[@tag='{0}']", fieldType);
                if(fieldNode.SelectSingleNode(priorFieldWithSameTag) != null)
                {
                    // a fatal error
                    throw new LiftFormatException(String.Format("Field with same tag ({0}) as sibling not allowed. Context:{1}", fieldType, fieldNode.ParentNode.OuterXml));
                }
                this._merger.MergeInField(target,
                                         fieldType,
                                         GetOptionalDate(fieldNode, "dateCreated", default(DateTime)),
                                         GetOptionalDate(fieldNode, "dateModified", default(DateTime)),
                                         ReadMultiText(fieldNode));

                //todo (maybe) ReadTraits(node, target);
            }

            ReadTraits(node, target);
            //todo: read fields
            //todo: read annotations           
        }

        private void ReadTraits(XmlNode node, TBase target)
        {
            foreach (XmlNode traitNode in node.SelectNodes("trait"))
            {
                _merger.MergeInTrait(target,GetTrait(traitNode));
            }
        }



        protected static string GetStringAttribute(XmlNode form, string attr)
        {
            try
            {
                return form.Attributes[attr].Value;
            }
            catch(NullReferenceException)
            {
                throw new LiftFormatException(string.Format("Expected a {0} attribute on {1}.", attr, form.OuterXml));
            }
        }

        protected static string GetOptionalAttributeString(XmlNode xmlNode, string name)
        {
            XmlAttribute attr = xmlNode.Attributes[name];
            if (attr == null)
                return null;
            return attr.Value;
        }


        /// <summary>
        /// careful, can't return null, so give MinValue
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="name"></param>
        /// <param name="defaultDateTime">the time to use if this attribute isn't found</param>
        /// <returns></returns>
        protected DateTime GetOptionalDate(XmlNode xmlNode, string name, DateTime defaultDateTime)
        {
            XmlAttribute attr = xmlNode.Attributes[name];
            if (attr == null)
                return defaultDateTime;

            /* if the incoming data lacks a time, we'll have a kind of 'unspecified', else utc */

            try
            {
                return Extensible.ParseDateTimeCorrectly(attr.Value);
            }
            catch (FormatException e)
            {
                NotifyError(e); // not a fatal error
                return defaultDateTime;
            }
        }

        protected LiftMultiText LocateAndReadMultiText(XmlNode node, string query)
        {
            XmlNode element;
            if (query == null)
            {
                element = node;
            }
            else
            {
                element = node.SelectSingleNode(query);
                XmlNodeList nodes = node.SelectNodes(query);
                if (nodes.Count > 1)
                {
                    throw new LiftFormatException(String.Format("Duplicated element of type {0} unexpected. Context:{1}", query, nodes.Item(0).ParentNode.OuterXml));
                }
            }

            if (element != null)
            {
                return ReadMultiText(element);
            }
            return new LiftMultiText();
        }

        protected List<LiftMultiText> LocateAndReadOneOrMoreMultiText(XmlNode node, string query)
        {
            List<LiftMultiText> results = new List<LiftMultiText>();      
            foreach (XmlNode n in node.SelectNodes(query))
            {
                results.Add(ReadMultiText(n));
            }
            return results;
        }

        protected LiftMultiText LocateAndReadOneElementPerFormData(XmlNode node, string query)
        {
            Debug.Assert(query != null);
            LiftMultiText text = new LiftMultiText();
            ReadFormNodes(node.SelectNodes(query), text);
            return text;
        }

        public  LiftMultiText ReadMultiText(XmlNode node)
        {
            LiftMultiText text = new LiftMultiText();
            ReadFormNodes(node.SelectNodes("form"), text);
            return text;
        }

        private void ReadFormNodes(XmlNodeList nodesWithForms, LiftMultiText text)
        {
            foreach (XmlNode formNode in nodesWithForms)
            {
                try
                {
                    string lang = GetStringAttribute(formNode, _wsAttributeLabel);
                    XmlNode textNode= formNode.SelectSingleNode("text");
                    if (textNode != null)
                    {
                        text.AddOrAppend(lang, textNode.InnerText.Trim(), "; ");
                    }

                    foreach (XmlNode traitNode in formNode.SelectNodes("trait"))
                    {
                        Trait trait = GetTrait(traitNode);
                        trait.LanguageHint = lang;
                        text.Traits.Add(trait);
                    }
                }
                catch (Exception e)
                {
                    // not a fatal error
                    NotifyError(e);
                }
            }
        }

        private static Trait GetTrait(XmlNode traitNode)
        {
            return new Trait(GetStringAttribute(traitNode, "name"),
                             GetStringAttribute(traitNode, "value"));
        }

        //private static bool NodeContentIsJustAString(XmlNode node)
        //{
        //    return node.InnerText != null
        //                        && (node.ChildNodes.Count == 1)
        //                        && (node.ChildNodes[0].NodeType == XmlNodeType.Text)
        //                        && node.InnerText.Trim() != string.Empty;
        //}

//        public LexExampleSentence ReadExample(XmlNode xmlNode)
//        {
//            LexExampleSentence example = new LexExampleSentence();
//            LocateAndReadMultiText(xmlNode, "source", example.Sentence);
//            //NB: will only read in one translation
//            LocateAndReadMultiText(xmlNode, "trans", example.Translation);
//            return example;
//        }
//



        #region Progress

        public class StepsArgs : EventArgs
        {
            private int _steps;

            public int Steps
            {
                get { return this._steps; }
                set { this._steps = value; }
            }
        }

        public class ErrorArgs : EventArgs
        {
            private Exception _exception;

            public Exception Exception
            {
                get { return this._exception; }
                set { this._exception = value; }
            }
        }

        private int ProgressStepsCompleted
        {
            set
            {
                if (SetStepsCompleted != null)
                {
                    ProgressEventArgs e = new ProgressEventArgs(value);
                    SetStepsCompleted.Invoke(this, e);
                    _cancelNow = e.Cancel;
                }
            }
        }

        private int ProgressTotalSteps
        {
            set
            {
                if (SetTotalNumberSteps != null)
                {
                    StepsArgs e = new StepsArgs();
                    e.Steps = value;
                    SetTotalNumberSteps.Invoke(this, e);
                }
            }
        }

       private void NotifyError(Exception error)
        {
            if (ParsingWarning != null)
            {
                ErrorArgs e = new ErrorArgs();
                e.Exception = error;
                ParsingWarning.Invoke(this, e);
            }          
        }

        public class ProgressEventArgs : EventArgs
        {
            private readonly int _progress;
            private bool _cancel = false;
            public ProgressEventArgs(int progress)
            {
                _progress = progress;
            }

            public int Progress
            {
                get { return _progress; }
            }

            public bool Cancel
            {
                get { return _cancel; }
                set { _cancel = value; }
            }
        }

#endregion

    }

}
