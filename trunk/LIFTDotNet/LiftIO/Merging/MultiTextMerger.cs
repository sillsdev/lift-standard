using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using LiftIO.Tests.Merging;

namespace LiftIO.Merging
{
    public class MultiTextMerger
    {
        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor)
        {
            XmlMerger m = new XmlMerger();
            m._finders.Add("form", new FindByKeyAttribute("lang"));
            XmlDocument doc = new XmlDocument();
            return Utilities.GetDocumentNodeFromRawXml(m.Merge(ours, theirs, ancestor), doc);

            //return MergeMultiTextPieces(ours, theirs, ancestor, null);
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
            XmlMerger m = new XmlMerger();
            m._finders.Add("form", new FindByKeyAttribute("lang"));
            return m.Merge(ours, theirs, ancestor);
        }

    }
}
