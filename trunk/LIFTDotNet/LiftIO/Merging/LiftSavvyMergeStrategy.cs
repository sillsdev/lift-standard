using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Parsing;

namespace LiftIO.Merging
{
    /// <summary>
    /// This strategy uses the parser to parse the incoming node and then tries to merge it into our xml node
    /// </summary>
    public class LiftSavvyMergeStrategy : IMergeStrategy, LiftIO.Parsing.ILexiconMerger<XmlNode, XmlNode, XmlNode, XmlNode> 
    {
        private LiftParser<XmlNode, XmlNode, XmlNode, XmlNode> _parser;
        private XmlNode _ourEntry;

        public LiftSavvyMergeStrategy()
        {
            _parser = new LiftParser<XmlNode, XmlNode, XmlNode, XmlNode>(this);
        }

        public string MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode unusedCommonEntry)
        {
            _ourEntry = ourEntry;
            _parser.ReadEntry(theirEntry);
            return ourEntry.OuterXml;
        }


        #region ILexiconMerger<XmlNode,XmlNode,XmlNode,XmlNode> Members

        public XmlNode GetOrMakeEntry(Extensible info, int order)
        {
            return null;
        }

        public void EntryWasDeleted(Extensible info, DateTime dateDeleted)
        {
            
        }

        public XmlNode GetOrMakeSense(XmlNode entry, Extensible info)
        {
            return null;
        }

        public XmlNode GetOrMakeSubsense(XmlNode sense, Extensible info, string rawXml)
        {
            return null;
        }

        public XmlNode GetOrMakeExample(XmlNode sense, Extensible info)
        {
            return null;
        }

        public void MergeInLexemeForm(XmlNode entry, LiftMultiText contents)
        {
            
        }

        public void MergeInCitationForm(XmlNode entry, LiftMultiText contents)
        {
            
        }

        public XmlNode MergeInPronunciation(XmlNode entry, LiftMultiText contents, string rawXml)
        {
            return null;
        }

        public XmlNode MergeInVariant(XmlNode entry, LiftMultiText contents, string rawXml)
        {
            return null;
        }

        public void MergeInGloss(XmlNode sense, LiftMultiText multiText)
        {
            
        }

        public void MergeInDefinition(XmlNode sense, LiftMultiText liftMultiText)
        {
            
        }

        public void MergeInPicture(XmlNode sense, string href, LiftMultiText caption)
        {
            
        }

        public void MergeInExampleForm(XmlNode example, LiftMultiText multiText)
        {
            
        }

        public void MergeInTranslationForm(XmlNode example, string type, LiftMultiText multiText)
        {
            
        }

        public void MergeInSource(XmlNode example, string source)
        {
            
        }

        public void FinishEntry(XmlNode entry)
        {
            
        }

        public void MergeInField(XmlNode extensible, string typeAttribute, DateTime dateCreated, DateTime dateModified,
                                 LiftMultiText contents, List<Trait> traits)
        {
            
        }

        public void MergeInTrait(XmlNode extensible, Trait trait)
        {
            
        }

        public void MergeInRelation(XmlNode extensible, string relationTypeName, string targetId, string rawXml)
        {
            
        }

        public void MergeInNote(XmlNode extensible, string type, LiftMultiText contents)
        {
            
        }

        public void MergeInGrammaticalInfo(XmlNode senseOrReversal, string val, List<Trait> traits)
        {
            
        }

        public XmlNode GetOrMakeParentReversal(XmlNode parent, LiftMultiText contents, string type)
        {
            return null;
        }

        public XmlNode MergeInReversal(XmlNode sense, XmlNode parent, LiftMultiText contents, string type, string rawXml)
        {
            return null;
        }

        public XmlNode MergeInEtymology(XmlNode entry, string source, LiftMultiText form, LiftMultiText gloss,
                                        string rawXml)
        {
            return null;
        }

        public void ProcessRangeElement(string range, string id, string guid, string parent, LiftMultiText description,
                                        LiftMultiText label, LiftMultiText abbrev)
        {
            
        }

        public void ProcessFieldDefinition(string tag, LiftMultiText description)
        {
            
        }

        #endregion
    }
}