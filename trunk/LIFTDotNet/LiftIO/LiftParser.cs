using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Xml;
using Commons.Xml.Relaxng;

namespace LiftIO
{
    public class LiftParser<TBase, TEntry, TSense, TExample> 
        where TBase : class
        where TEntry : TBase
        where TSense : TBase
        where TExample : TBase 
    {
        public event EventHandler<ErrorArgs> ParsingError;
        public event EventHandler<StepsArgs> SetTotalNumberSteps;
        public event EventHandler<ProgressEventArgs> SetStepsCompleted;

        private ILexiconMerger<TBase, TEntry, TSense, TExample> _merger;
        protected string _wsAttributeLabel = "lang";

        private bool _cancelNow = false;
        private string _defaultLangId="??";


        public LiftParser(ILexiconMerger<TBase, TEntry, TSense, TExample> merger)
        {
            _merger = merger;
        }
    
        /// <summary>
        /// 
        /// </summary>
        public virtual void ReadFile(XmlDocument doc)
        {
            XmlNodeList entryNodes = doc.SelectNodes("./lift/entry");
            int count = 0;
            const int kInterval = 50;
            int nextProgressPoint = count + kInterval;
            this.ProgressTotalSteps = entryNodes.Count;
            foreach (XmlNode node in entryNodes)
            {
                this.ReadEntry(node);
                count++;
                if (count >= nextProgressPoint)
                {
                    this.ProgressStepsCompleted = count;
                    nextProgressPoint = count + kInterval;
                    if (_cancelNow)
                        break;
                }
            }
        }

        public TEntry ReadEntry(XmlNode node)
        {
            Extensible extensible = ReadExtensibleElementBasics(node);
            DateTime dateDeleted = GetOptionalDate(node, "dateDeleted");
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


            SimpleMultiText lexemeForm = LocateAndReadMultiText(node, "lexical-unit");
            if (lexemeForm == null || lexemeForm.Count == 0)
            {
                lexemeForm = LocateAndReadMultiText(node, "lex");
            }

            //review: assuming it's better to notify that it's missing than not
            //  call at all.
            //if (lexemeForm.Count > 0)
            {
                _merger.MergeInLexemeForm(entry, lexemeForm);
            }
            foreach (XmlNode n in node.SelectNodes("sense"))
            {
                ReadSense(n, entry);
            }
            
            ReadExtensibleElementDetails(entry, node);
            _merger.FinishEntry(entry);
            return entry;
        }

        protected void ReadGrammi(TSense sense, XmlNode senseNode)
        {
            XmlNode grammi = senseNode.SelectSingleNode("grammatical-info");
            if (grammi != null)
            {
                string val = GetStringAttribute(grammi, "value");
                _merger.MergeInGrammaticalInfo(sense, val);
            }
        }

        public TSense ReadSense(XmlNode node, TEntry entry)
        {
            TSense sense = _merger.GetOrMakeSense(entry, ReadExtensibleElementBasics(node));
            if (sense != null)//not been pruned
            {
                ReadGrammi(sense, node);
                SimpleMultiText gloss = LocateAndReadOneElementPerFormData(node, "gloss");
              //no: do it anyways: remember, we may be merging, not just importing  if (!gloss.IsEmpty)
                {
                    _merger.MergeInGloss(sense, gloss);
                }
                
                SimpleMultiText def = ProcessMultiText(node, "def");
                //no: do it anyways: remember, we may be merging, not just importing if (!def.IsEmpty)
                {
                    _merger.MergeInDefinition(sense, def);
                }

                foreach (XmlNode n in node.SelectNodes("example"))
                {
                    ReadExample(n, sense);
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
                
                _merger.MergeInExampleForm(example, ProcessMultiText(node, null));
                //NB: only one translation supported in LIFT at the moment
                _merger.MergeInTranslationForm(example, ProcessMultiText(node, "translation"));

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
                        NotifyError(new ApplicationException(String.Format("{0} is not a valid GUID", guidString)));
                    }
                }
            }
            extensible.CreationTime = GetOptionalDate(node, "dateCreated");
            extensible.ModificationTime = GetOptionalDate(node, "dateModified");

            return extensible;
        }
        
        /// <summary>
        /// Once we have the thing we're creating/merging with, we can read in any details,
        /// i.e. traits, fields, and annotations
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected void ReadExtensibleElementDetails(TBase target, XmlNode node)
        {
            foreach (XmlNode fieldNode in node.SelectNodes("field"))
            {
                _merger.MergeInField(target,
                    GetStringAttribute(fieldNode, "tag"),
                    GetOptionalDate(fieldNode, "dateCreated"),
                    GetOptionalDate(fieldNode, "dateModified"),
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
                _merger.MergeInTrait(target,
                                     GetStringAttribute(traitNode, "name"),
                                     GetStringAttribute(traitNode, "value"),
                                     GetOptionalAttributeString(traitNode, "id")
                                     );
            }
        }

        protected SimpleMultiText ProcessMultiText(XmlNode node, string fieldName)
        {
            return LocateAndReadMultiText(node, fieldName);
        }




        protected static string GetStringAttribute(XmlNode form, string attr)
        {
            try
            {
                return form.Attributes[attr].Value;
            }
            catch(NullReferenceException)
            {
                throw new ApplicationException(string.Format("Expected a {0} attribute on {1}.", attr, form.OuterXml));
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
        /// <returns></returns>
        protected DateTime GetOptionalDate(XmlNode xmlNode, string name)
        {
            XmlAttribute attr = xmlNode.Attributes[name];
            if (attr == null)
                return default(DateTime);

            /* if the incoming data lacks a time, we'll have a kind of 'unspecified', else utc */


            try
            {
                DateTime result = DateTime.ParseExact(attr.Value, 
                                                      new string[] {Extensible.LiftTimeFormatNoTimeZone, Extensible.LiftTimeFormatWithTimeZone, Extensible.LiftDateOnlyFormat },
                                                      CultureInfo.InvariantCulture, 
                                                      DateTimeStyles.AdjustToUniversal & DateTimeStyles.AssumeUniversal);
                if(result.Kind != DateTimeKind.Utc)
                {
                    result = result.ToUniversalTime();
                }
                return result;
            }
            catch (FormatException e)
            {
                NotifyError(e);
                return default(DateTime);
            }
        }

        protected SimpleMultiText LocateAndReadMultiText(XmlNode node, string query)
        {
            XmlNode element=null;
            if (query == null)
            {
                element = node;
            }
            else
            {
                element = node.SelectSingleNode(query);
            }

            if (element != null)
            {
                return ReadMultiText(element);
            }
            return new SimpleMultiText();
        }

        protected SimpleMultiText LocateAndReadOneElementPerFormData(XmlNode node, string query)
        {
            Debug.Assert(query != null);
            SimpleMultiText text = new SimpleMultiText();
            ReadFormNodes(node.SelectNodes(query), text);
            return text;
        }

        public  SimpleMultiText ReadMultiText(XmlNode node)
        {
            SimpleMultiText text = new SimpleMultiText();
            ReadFormNodes(node.SelectNodes("form"), text);
            return text;
        }

        private void ReadFormNodes(XmlNodeList nodesWithForms, SimpleMultiText text)
        {
            foreach (XmlNode form in nodesWithForms)
            {
                try
                {
                    string lang = GetStringAttribute(form, _wsAttributeLabel);
                    text.AddOrAppend(lang, form.InnerText, "; ");
                }
                catch (Exception e)
                {
                    NotifyError(e);
                }
            }
        }

        private static bool NodeContentIsJustAString(XmlNode node)
        {
            return node.InnerText != null
                                && (node.ChildNodes.Count == 1)
                                && (node.ChildNodes[0].NodeType == XmlNodeType.Text)
                                && node.InnerText.Trim() != string.Empty;
        }

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
            if (ParsingError != null)
            {
                ErrorArgs e = new ErrorArgs();
                e.Exception = error;
                ParsingError.Invoke(this, e);
            }          
        }

        public class ProgressEventArgs : EventArgs
        {
            private int _progress;
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
