using System;
using System.Collections.Generic;
using System.Text;

namespace LiftIO.Merging.XmlMerge
{
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
    }

}
