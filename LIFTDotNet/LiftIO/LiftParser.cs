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
		public event EventHandler<MessageArgs> SetProgressMessage;

        private readonly ILexiconMerger<TBase, TEntry, TSense, TExample> _merger;
        private const string _wsAttributeLabel = "lang";

        private bool _cancelNow = false;
        private DateTime _defaultCreationModificationUTC;
		private int _count;		// number of entries read from file.
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
            _count = 0;
            const int kInterval = 50;
            int nextProgressPoint = _count + kInterval;
            ProgressTotalSteps = entryNodes.Count;
            foreach (XmlNode node in entryNodes)
            {
                ReadEntry(node);
                _count++;
                if (_count >= nextProgressPoint)
                {
                    ProgressStepsCompleted = _count;
                    nextProgressPoint = _count + kInterval;
                }
                if (_cancelNow)
                {
                    break;
                }
            }
        }

		public void ReadRangeElement(string range, XmlNode node)
		{
			string id = GetStringAttribute(node, "id");
			string guid = GetOptionalAttributeString(node, "guid");
			string parent = GetOptionalAttributeString(node, "parent");
			LiftMultiText description = LocateAndReadMultiText(node, "description");
			LiftMultiText label = LocateAndReadMultiText(node, "label");
			LiftMultiText abbrev = LocateAndReadMultiText(node, "abbrev");
			_merger.ProcessRangeElement(range, id, guid, parent, description, label, abbrev);
		}

		public void ReadFieldDefinition(XmlNode node)
		{
			string tag = GetStringAttribute(node, "tag");
			LiftMultiText description = ReadMultiText(node);
			_merger.ProcessFieldDefinition(tag, description);
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

			int homograph = 0;
			string order = GetOptionalAttributeString(node, "order");
			if (!String.IsNullOrEmpty(order))
			{
				if (!Int32.TryParse(order, out homograph))
					homograph = 0;
			}
			TEntry entry = _merger.GetOrMakeEntry(extensible, homograph);
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

			foreach (XmlNode n in node.SelectNodes("variant"))
			{
				ReadVariant(n, entry);
			}
			
			foreach (XmlNode n in node.SelectNodes("pronunciation"))
			{
				ReadPronunciation(n, entry);
			}

			foreach (XmlNode n in node.SelectNodes("etymology"))
			{
				ReadEtymology(n, entry);
			}

            ReadExtensibleElementDetails(entry, node);
            _merger.FinishEntry(entry);
            return entry;
        }

        private void ReadNotes(XmlNode node, TBase e)
        {
			// REVIEW (SRMc): Should we detect multiple occurrences of the same
			// type of note?  See ReadExtensibleElementDetails() for how field
			// elements are handled in this regard.
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
            string relationFieldName = GetStringAttribute(n, "type");

            _merger.MergeInRelation(parent, relationFieldName, targetId);
        }

		private void ReadPronunciation(XmlNode node, TEntry entry)
		{
			LiftMultiText contents = ReadMultiText(node);
			TBase pronunciation = _merger.MergeInPronunciation(entry, contents);
			if (pronunciation != null)
				ReadExtensibleElementDetails(pronunciation, node);
		}

		private void ReadVariant(XmlNode node, TEntry entry)
		{
			LiftMultiText contents = ReadMultiText(node);
			TBase variant = _merger.MergeInVariant(entry, contents);
			if (variant != null)
				ReadExtensibleElementDetails(variant, node);
		}

		private void ReadEtymology(XmlNode node, TEntry entry)
		{
			string source = GetOptionalAttributeString(node, "source");
			LiftMultiText form = LocateAndReadMultiText(node, null);
			LiftMultiText gloss = LocateAndReadOneElementPerFormData(node, "gloss");
			TBase etymology = _merger.MergeInEtymology(entry, source, form, gloss);
			if (etymology != null)
				ReadExtensibleElementDetails(etymology, node);
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

		/// <summary>
		/// Read the grammatical-info information for either a sense or a reversal.
		/// </summary>
		/// <param name="extensible"></param>
		/// <param name="node"></param>
		protected void ReadGrammi(TBase senseOrReversal, XmlNode senseNode)
        {
            XmlNode grammiNode = senseNode.SelectSingleNode("grammatical-info");
            if (grammiNode != null)
            {
                string val = GetStringAttribute(grammiNode, "value");
                _merger.MergeInGrammaticalInfo(senseOrReversal, val, GetTraitList(grammiNode));
            }
        }

        /// <summary>
        /// Used for elements with traits that are not top level objects (extensibles) or forms.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static List<Trait> GetTraitList(XmlNode node)
        {
            List<Trait> traits = new List<Trait>();
            foreach (XmlNode traitNode in node.SelectNodes("trait"))
            {
                traits.Add(GetTrait(traitNode));
            }
            return traits;
        }

        public TSense ReadSense(XmlNode node, TEntry entry)
        {
            TSense sense = _merger.GetOrMakeSense(entry, ReadExtensibleElementBasics(node));
			return FinishReadingSense(node, sense);
		}

		private TSense FinishReadingSense(XmlNode node, TSense sense)
		{
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
				foreach (XmlNode n in node.SelectNodes("illustration"))
                {
                    ReadPicture(n, sense);
                }
				foreach (XmlNode n in node.SelectNodes("reversal"))
				{
					ReadReversal(n, sense);
				}
				foreach (XmlNode n in node.SelectNodes("subsense"))
				{
					ReadSubsense(n, sense);
				}
				ReadExtensibleElementDetails(sense, node);
            }
            return sense;
        }

		private void ReadSubsense(XmlNode node, TSense sense)
		{
			TSense subsense = _merger.GetOrMakeSubsense(sense, ReadExtensibleElementBasics(node));
			FinishReadingSense(node, subsense);
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
				foreach (XmlNode n in node.SelectNodes("translation"))
				{
					LiftMultiText translation = ReadMultiText(n);
					string type = GetOptionalAttributeString(n, "type");
					_merger.MergeInTranslationForm(example, type, translation);
				}
                string source = GetOptionalAttributeString(node, "source");
                if (source != null)
                {
                    _merger.MergeInSource(example, source);
                }

				// REVIEW(SRMc): If you don't think the note element should be valid
				// inside an example, then remove the next line and the corresponding
				// chunk from the rng file.
                ReadNotes(node, example);

                ReadExtensibleElementDetails(example, node);
            }
            return example;
        }

		private void ReadReversal(XmlNode node, TSense sense)
		{
			string type = GetOptionalAttributeString(node, "type");
			XmlNodeList nodelist = node.SelectNodes("main");
			if (nodelist.Count > 1)
			{
				NotifyError(new LiftFormatException(String.Format("Only one <main> element is allowed inside a <reversal> element:\r\n", node.OuterXml)));
			}
			TBase parent = null;
			if (nodelist.Count == 1)
				parent = ReadParentReversal(type, nodelist[0]);
			LiftMultiText text = ReadMultiText(node);
			TBase reversal = _merger.MergeInReversal(sense, parent, text, type);
			if (reversal != null)
				ReadGrammi(reversal, node);
		}

		private TBase ReadParentReversal(string type, XmlNode node)
		{
			XmlNodeList nodelist = node.SelectNodes("main");
			if (nodelist.Count > 1)
			{
				NotifyError(new LiftFormatException(String.Format("Only one <main> element is allowed inside a <main> element:\r\n", node.OuterXml)));
			}
			TBase parent = null;
			if (nodelist.Count == 1)
				parent = ReadParentReversal(type, nodelist[0]);
			LiftMultiText text = ReadMultiText(node);
			TBase reversal = _merger.GetOrMakeParentReversal(parent, text, type);
			if (reversal != null)
				ReadGrammi(reversal, node);
			return reversal;
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
                string fieldType = GetStringAttribute(fieldNode, "type");
                string priorFieldWithSameTag = String.Format("preceding-sibling::field[@type='{0}']", fieldType);
                if(fieldNode.SelectSingleNode(priorFieldWithSameTag) != null)
                {
                    // a fatal error
                    throw new LiftFormatException(String.Format("Field with same type ({0}) as sibling not allowed. Context:{1}", fieldType, fieldNode.ParentNode.OuterXml));
                }
				//todo: read annotations           
				this._merger.MergeInField(target,
                                         fieldType,
                                         GetOptionalDate(fieldNode, "dateCreated", default(DateTime)),
                                         GetOptionalDate(fieldNode, "dateModified", default(DateTime)),
                                         ReadMultiText(fieldNode),
										 GetTraitList(fieldNode));
            }

            ReadTraits(node, target);
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
						// Add the separator if we need it.
						if (textNode.InnerText.Length > 0)
							text.AddOrAppend(lang, "", "; ");
						foreach (XmlNode xn in textNode.ChildNodes)
						{
							if (xn.Name == "span")
							{
								text.AddSpan(lang,
									GetOptionalAttributeString(xn, "lang"),
									GetOptionalAttributeString(xn, "class"),
									GetOptionalAttributeString(xn, "href"),
									xn.InnerText.Length);
							}
							text.AddOrAppend(lang, xn.InnerText, "");
						}
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

		/// <summary>
		/// Read a LIFT file, possibly an earlier version.
		/// </summary>
		/// <param name="sFilename"></param>
		public void ReadLIFTFile(string sFilename)
		{
			ProgressTotalSteps = 100;	// we scan the file sequentially, so we don't have a count.
			const int kInterval = 5;
			ProgressStepsCompleted = 0;
			XmlReaderSettings xrset = new XmlReaderSettings();
			xrset.ValidationType = ValidationType.None;
			xrset.IgnoreComments = true;
			string sOrigVersion = String.Empty;
			using (XmlReader xrdr = XmlReader.Create(sFilename, xrset))
			{
				if (xrdr.IsStartElement("lift"))
					sOrigVersion = xrdr.GetAttribute("version");
			}
			if (String.IsNullOrEmpty(sOrigVersion))
			{
				// we don't have a LIFT file -- what to do??
				string msg = String.Format("Cannot import {0} because it is not a LIFT file!", sFilename);
				throw new Exception(msg);
			}
			string sLiftFile;
			if (sOrigVersion != LiftIO.Validator.LiftVersion)
				sLiftFile = LiftIO.Validator.GetCorrectLiftVersionOfFile(sFilename);
			else
				sLiftFile = sFilename;
			string sVersion = null;
			string sProducer = null;
			using (XmlReader xrdr = XmlReader.Create(sLiftFile, xrset))
			{
				if (xrdr.IsStartElement("lift"))
				{
					sVersion = xrdr.GetAttribute("version");
					sProducer = xrdr.GetAttribute("producer");
					if (sVersion != LiftIO.Validator.LiftVersion)
					{
						// we don't have a matching version -- what to do??
						string msg = String.Format("Cannot import {0}.  It is LIFT version {1}, but we need LIFT version {2}.",
							sFilename, sOrigVersion, LiftIO.Validator.LiftVersion);
						throw new Exception(msg);
					}
				}
				ProgressMessage = "Reading LIFT file header";
				XmlDocument xd = new XmlDocument();
				xrdr.ReadStartElement("lift");
				if (xrdr.IsStartElement("header"))
				{
					xrdr.ReadStartElement("header");
					if (xrdr.IsStartElement("ranges"))
					{
						xrdr.ReadStartElement("ranges");
						while (xrdr.IsStartElement("range"))
						{
							string sId = xrdr.GetAttribute("id");
							string sHref = xrdr.GetAttribute("href");
							string sGuid = xrdr.GetAttribute("guid");
							ProgressMessage = String.Format("Reading LIFT range {0}", sId);
							xrdr.ReadStartElement();
							if (String.IsNullOrEmpty(sHref))
							{
								while (xrdr.IsStartElement("range-element"))
								{
									string sRangeElement = xrdr.ReadOuterXml();
									if (!String.IsNullOrEmpty(sRangeElement))
									{
										xd.LoadXml(sRangeElement);
										this.ReadRangeElement(sId, xd.FirstChild);
									}
								}
							}
							else
							{
								this.ReadExternalRange(sHref, sId, sGuid, xrset);
							}
							xrdr.ReadEndElement();	// </range>
						}
						xrdr.ReadEndElement();	// </ranges>
					}
					if (xrdr.IsStartElement("fields"))
					{
						xrdr.ReadStartElement("fields");
						while (xrdr.IsStartElement("field"))
						{
							string sField = xrdr.ReadOuterXml();
							if (!String.IsNullOrEmpty(sField))
							{
								xd.LoadXml(sField);
								this.ReadFieldDefinition(xd.FirstChild);
							}
						}
						xrdr.ReadEndElement();	// </fields>
					}
					xrdr.ReadEndElement();	// </header>
				}
				// Process all of the entry elements, reading them into memory one at a time.
				ProgressMessage = "Reading entries from LIFT file";
				if (!xrdr.IsStartElement("entry"))
					xrdr.ReadToFollowing("entry");	// not needed if no <header> element.
				_count = 0;
				while (xrdr.IsStartElement("entry"))
				{
					string sEntry = xrdr.ReadOuterXml();
					if (!String.IsNullOrEmpty(sEntry))
					{
						xd.LoadXml(sEntry);
						this.ReadEntry(xd.FirstChild);
					}
					++_count;
					if ((_count % kInterval) == 0)
						ProgressStepsCompleted = _count / kInterval;
				}
			}
		}

		/// <summary>
		/// Return the number of entries processed from the most recent file.
		/// </summary>
		public int EntryCount
		{
			get { return _count; }
		}

		/// <summary>
		/// Read a range from a separate file.
		/// </summary>
		/// <param name="sHref"></param>
		/// <param name="sIdRange"></param>
		/// <param name="sGuidRange"></param>
		/// <param name="xrset"></param>
		public void ReadExternalRange(string sHref, string sIdRange, string sGuidRange,
			XmlReaderSettings xrset)
		{
			string sFile = sHref;
			if (sHref.StartsWith("file://"))
				sFile = sHref.Substring(7);
			using (XmlReader xrdr = XmlReader.Create(sFile, xrset))
			{
				XmlDocument xd = new XmlDocument();
				xrdr.ReadStartElement("lift-ranges");
				while (xrdr.IsStartElement("range"))
				{
					string sId = xrdr.GetAttribute("id");
					string sGuid = xrdr.GetAttribute("guid");
					bool fStore = sId == sIdRange;
					xrdr.ReadStartElement();
					while (xrdr.IsStartElement("range-element"))
					{
						string sRangeElement = xrdr.ReadOuterXml();
						if (fStore)
						{
							xd.LoadXml(sRangeElement);
							this.ReadRangeElement(sId, xd.FirstChild);
						}
					}
					xrdr.ReadEndElement();
					if (fStore)
						return;		// we've seen the range we wanted from this file.
				}
			}
		}


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

		public class MessageArgs : EventArgs
		{
			private string _msg;

			public string Message
			{
				get { return this._msg; }
				set { _msg = value; }
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

		private string ProgressMessage
		{
			set
			{
				if (SetProgressMessage != null)
				{
					MessageArgs e = new MessageArgs();
					e.Message = value;
					SetProgressMessage.Invoke(this, e);
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
