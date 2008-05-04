using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO.Merging.XmlMerge
{
    public class MergeStrategies
    {
        public Dictionary<string, ElementStrategy> _elementStrategies = new Dictionary<string, ElementStrategy>();

        public ElementStrategy GetElementStrategy(XmlNode element)
        {
            ElementStrategy strategy;
            if (!_elementStrategies.TryGetValue(element.Name, out strategy))
            {
                strategy = new ElementStrategy();
                strategy._mergePartnerFinder = new FindByEqualityOfTree();
            }
            return strategy;
        }

        public IFindNodeToMerge GetMergePartnerFinder(XmlNode element)
        {
            return GetElementStrategy(element)._mergePartnerFinder;
        }

//        private IDifferenceReportMaker GetDifferenceReportMaker(XmlNode element)
//        {
//            ElementStrategy strategy;
//            if (!this._mergeStrategies._elementStrategies.TryGetValue(element.Name, out strategy))
//            {
//                return new DefaultDifferenceReportMaker();
//            }
//            return strategy._differenceReportMaker;
//        }

    }

    public class ElementStrategy
    {
        public IFindNodeToMerge _mergePartnerFinder;
        public IDifferenceReportMaker _differenceReportMaker;

        public static ElementStrategy CreateForKeyedElement(string keyAttributeName)
        {
            ElementStrategy strategy = new ElementStrategy();
            strategy._mergePartnerFinder = new FindByKeyAttribute(keyAttributeName);
            return strategy;
        }

        public string GetHumanDescription(XmlNode element)
        {
            return "not implemented";
        }
    }

}
