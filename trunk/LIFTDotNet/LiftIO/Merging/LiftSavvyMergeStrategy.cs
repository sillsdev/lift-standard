using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private XmlNode _ancestorEntry;

        public LiftSavvyMergeStrategy()
        {
            _parser = new LiftParser<XmlNode, XmlNode, XmlNode, XmlNode>(this);
        }

        public string MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode ancestorEntry)
        {
            _ourEntry = ourEntry;
            _parser.ReadEntry(theirEntry);
            _ancestorEntry = ancestorEntry;
            return ourEntry.OuterXml;
        }



        #region ILexiconMerger<XmlNode,XmlNode,XmlNode,XmlNode> Members

        public XmlNode GetOrMakeEntry(Extensible info, int order)
        {
            //Merge fields?
            //Merge traits & annotations?
            return _ourEntry;
        }

        public void EntryWasDeleted(Extensible info, DateTime dateDeleted)
        {
            
        }

        public XmlNode GetOrMakeSense(XmlNode entry, Extensible info, string rawXml)
        {
            //todo: match sense that doesn't have an id
            XmlNode sense =entry.SelectSingleNode("sense[@id='" + info.Id + "']");
            if (sense == null)
            {
                sense = Utilities.GetDocumentNodeFromRawXml(rawXml, _ourEntry);
                _ourEntry.AppendChild(sense);
            }
            return sense;
        }

        public XmlNode GetOrMakeSubsense(XmlNode sense, Extensible info, string rawXml)
        {
            return null;
        }

        public XmlNode GetOrMakeExample(XmlNode sense, Extensible info)
        {
            return null;
        }

        public void MergeInLexemeForm(XmlNode entry, LiftMultiText theirs)
        {
            Debug.Assert(entry == _ourEntry);

            MergeMultiTextElement("lexical-unit", theirs);
        }

        private void MergeMultiTextElement(string xpathToSingularElement, LiftMultiText theirs)
        {
            XmlNode ourNode = _ourEntry.SelectSingleNode(xpathToSingularElement);
            XmlNode ancestorNode = _ancestorEntry.SelectSingleNode(xpathToSingularElement);

            if (ourNode != null && Utilities.AreXmlElementsEqual(ourNode.OuterXml, theirs.OriginalRawXml))
            {
                return;
            }

            if(ourNode==null && ancestorNode ==null) // just take theirs
            {
                _ourEntry.AppendChild(Utilities.GetDocumentNodeFromRawXml(theirs.OriginalRawXml, _ourEntry));
                return;
            }

            if(ourNode==null && theirs != null)
            {
                bool theyDidSomethingNew = Utilities.AreXmlElementsEqual(theirs.OriginalRawXml, ancestorNode.OuterXml);
                if(theyDidSomethingNew)
                {
                    //todo conflict
                    return;
                }
                else
                {
                    //we just deleted it, and they didn't touch it
                    return;
                }
            }

            //actually have to merge
            XmlNode merged = MultiTextMerger.MergeMultiTextPieces(ourNode, 
                                                 Utilities.GetDocumentNodeFromRawXml(theirs.OriginalRawXml, _ourEntry),
                                                 ancestorNode);
            _ourEntry.ReplaceChild(merged,ourNode);
            _ourEntry.AppendChild(merged);
        }

        


//        private LiftMultiText GetOrCreateMultiText(XmlNode node, string xpath)
//        {
//            XmlNode mt = node.SelectSingleNode(xpath);
//            if(mt==null)
//            {
//                return new LiftMultiText();
//            }
//            else
//            {
//                return _parser.ReadMultiText(mt);
//            }
//        }

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