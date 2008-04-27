using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO.Merging
{
    public class MultiTextMerger
    {
        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor)
        {
            return MergeMultiTextPieces(ours, theirs, ancestor, null);
        }


        internal static XmlNode MergeMultiTextPieces(string ours, string theirs, string ancestor, XmlNode optionalNodeMaker)
        {
            if (string.IsNullOrEmpty(ours) && string.IsNullOrEmpty(theirs))
            {
                return null;
            }

            if (optionalNodeMaker == null)
                optionalNodeMaker = new XmlDocument();

            if (string.IsNullOrEmpty(ours))
            {
                return Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker);
            }

            if (string.IsNullOrEmpty(theirs))
            {
                return Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker);
            }


            return MergeMultiTextPieces(Utilities.GetDocumentNodeFromRawXml(ours, optionalNodeMaker),
                                         Utilities.GetDocumentNodeFromRawXml(theirs, optionalNodeMaker),
                                         Utilities.GetDocumentNodeFromRawXml(ancestor, optionalNodeMaker));
        }

        internal static XmlNode MergeMultiTextPieces(XmlNode ours, XmlNode theirs, XmlNode ancestor)
        {
            foreach (XmlNode theirForm in theirs.SelectNodes("./form"))
            {
                string lang = Utilities.GetStringAttribute(theirForm, "lang");
                XmlNode ourForm = ours.SelectSingleNode("./form[@lang='" + lang + "']");
                XmlNode ancestorForm = ancestor.SelectSingleNode("./form[@lang='" + lang + "']");

                if (ourForm == null)
                {
                    if(ancestorForm == null)
                    {
                        ours.AppendChild(theirForm);
                    }
                    else if(Utilities.AreXmlElementsEqual(ancestorForm,theirForm))
                    {
                        return ours; // we deleted it, they didn't touch it
                    }
                    else //we deleted it, but at the same time, they did change change it
                    {
                        //todo: should we add what they modified?
                        //needs a test first

                        //until then, this is a conflict  <-- todo

                        return ours;
                    }
                }
                else if (theirForm == null && ancestorForm !=null)
                {
                   if(Utilities.AreXmlElementsEqual(ourForm,ancestorForm))
                    {
                        ours.RemoveChild(ourForm);  //they deleted it, we didn't touch it
                    }
                   else
                   {
                       //they deleted it, but we modified it

                       //todo Log conflict

                       return ours;
                   }
                }
                else if (
                        ourForm.SelectSingleNode("text") == null
                    || ourForm.SelectSingleNode("text").InnerText.Trim() == string.Empty)
                {
                    ours.RemoveChild(ourForm);
                    ours.AppendChild(theirForm);//swap in theirs
                }
                else
                {
                    //log conflict
                }
            }
            return ours;
        }
    }
}
