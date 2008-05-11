using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Merging.XmlMerge;

namespace LiftIO.Merging
{
    public class EntryMerger : IMergeStrategy
    {
        private XmlMerger _entryMerger;

        public EntryMerger()
        {
            _entryMerger = new XmlMerger();
            AddKeyedElementType("entry", "id");
            AddKeyedElementType("sense", "id");
            AddKeyedElementType("form", "lang");
            AddKeyedElementType("field", "type");

            AddSingletonElementType("grammatical-info");
            AddSingletonElementType("lexical-unit" );
            AddSingletonElementType("citation" );
            AddSingletonElementType("definition");
            AddSingletonElementType("label");
            AddSingletonElementType("usage");
            AddSingletonElementType("header");
            AddSingletonElementType("description"); // in header
            AddSingletonElementType("ranges"); // in header
            AddSingletonElementType("fields"); // in header

            //enhance: don't currently have a way of limitting etymology/form to a single instance but not multitext/form

            AddSingletonElementType("main"); //reversal/main

        }

        private ElementStrategy AddKeyedElementType(string name, string attribute)
        {
            ElementStrategy strategy = new ElementStrategy();
            strategy._mergePartnerFinder = new FindByKeyAttribute(attribute);
            _entryMerger._mergeStrategies.SetStrategy(name, strategy);
            return strategy;
        }

        private ElementStrategy AddSingletonElementType(string name)
        {
            ElementStrategy strategy = new ElementStrategy();
            strategy._mergePartnerFinder = new FindFirstElementWithSameName();
            _entryMerger._mergeStrategies.SetStrategy(name, strategy);
            return strategy;
        }

        public MergeResult MakeMergedEntry(XmlNode ourEntry, XmlNode theirEntry, XmlNode commonEntry)
        {
            return _entryMerger.Merge(ourEntry, theirEntry, commonEntry);
        }
    }

 /*   public class FindMatchingExampleTranslation : IFindNodeToMerge
    {
        public XmlNode GetNodeToMerge(XmlNode nodeToMatch, XmlNode parentToSearchIn)
        {
           //todo: this may choke with multiples of the same type!
            
            //enhance... if we could rely on creation date + type, that'd help, but if
            // it was automatically done, multiple could come in with the same datetime

            string type = Utilities.GetOptionalAttributeString(nodeToMatch, "type");
            string xpath;
            if (string.IsNullOrEmpty(type))
            {
                xpath = String.Format("translation[not(@type)]");
            }
            else
            {
                xpath = string.Format("translation[@type='{0}']", type);
            }
            XmlNode n= parentToSearchIn.SelectSingleNode(xpath);
            if (n != null)
            {
                return n;
            }
            else
            {
                //enhance: can we find one with a matching multitext? Maybe one guy just changed the type.
                return null;
            }
        }

    }*/
}
