using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO.Merging.XmlMerge
{
    public class MergeStrategies
    {
        public Dictionary<string, ElementStrategy> _elementStrategies = new Dictionary<string, ElementStrategy>();

        public MergeStrategies()
        {
            ElementStrategy s = new ElementStrategy();
            s._mergePartnerFinder = new FindTextDumb();
            this.SetStrategy("_"+XmlNodeType.Text, s);

            ElementStrategy def = new ElementStrategy();
            def._mergePartnerFinder = new FindByEqualityOfTree();
            this.SetStrategy("_defaultElement", def);
        }

        public void SetStrategy(string key, ElementStrategy strategy)
        {
            _elementStrategies[key] = strategy;
        }

        public ElementStrategy GetElementStrategy(XmlNode element)
        {
            string name;
            switch (element.NodeType)
            {
                case XmlNodeType.Element:
                    name = element.Name;
                    break;
                default:
                    name = "_"+element.NodeType;
                    break;
            }

            ElementStrategy strategy;
            if (!_elementStrategies.TryGetValue(name, out strategy))
            {
                return _elementStrategies["_defaultElement"];
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
