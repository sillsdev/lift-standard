using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Merging.XmlMerge;
using LiftIO.Tests.Merging;

namespace LiftIO.Merging
{
    public class MultiTextMerger
    {
        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor)
        {
            XmlMerger m = GetMerger();
            XmlDocument doc = new XmlDocument();
            return Utilities.GetDocumentNodeFromRawXml(m.Merge(ours, theirs, ancestor), doc);

            //return MergeMultiTextPieces(ours, theirs, ancestor, null);
        }

        private static XmlMerger GetMerger()
        {
            XmlMerger m = new XmlMerger(new ConsolMergeLogger());
            ElementStrategy formStrategy = new ElementStrategy();
            formStrategy._mergePartnerFinder = new FindByKeyAttribute("lang");
            m._mergeStrategies._elementStrategies.Add("form", formStrategy);
            return m;
        }


//        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor, XmlNode optionalNodeMaker)
//        {
////            if (string.IsNullOrEmpty(ours) && string.IsNullOrEmpty(theirs))
////            {
////                return null;
////            }
////
////            if (optionalNodeMaker == null)
////                optionalNodeMaker = new XmlDocument();
////
////            if (string.IsNullOrEmpty(ours))
////            {
////                return Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker);
////            }
////
////            if (string.IsNullOrEmpty(theirs))
////            {
////                return Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker);
////            }
////
////
////            return MergeMultiTextPieces(Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker),
////                                         Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker),
////                                         Utilities.GetDocumentNodeFromRawXml(ancestor, optionalNodeMaker));
//        }

        internal static XmlNode MergeMultiTextPieces(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            XmlMerger m = GetMerger();
            return m.Merge(ours, theirs, ancestor);
        }

    }
}
