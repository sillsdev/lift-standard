using System.Xml;
using LiftIO.Merging.XmlDiff;

namespace LiftIO.Merging.XmlMerge
{
    public interface IFindNodeToMerge
    {
        XmlNode GetNodeToMerge(XmlNode elementToMatch, XmlNode parentToSearchIn);
    }

    public class FindByKeyAttribute : IFindNodeToMerge
    {
        private string _keyAttribute;

        public FindByKeyAttribute(string keyAttribute)
        {
            _keyAttribute = keyAttribute;
        }


        public XmlNode GetNodeToMerge(XmlNode elementToMatch, XmlNode parentToSearchIn)
        {
            string key = Utilities.GetOptionalAttributeString(elementToMatch, _keyAttribute);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            string xpath = string.Format("{0}[@{1}='{2}']", elementToMatch.Name, _keyAttribute, key);

            return parentToSearchIn.SelectSingleNode(xpath);
        }

    }

    public class FindByEqualityOfTree : IFindNodeToMerge
    {
        public XmlNode GetNodeToMerge(XmlNode elementToMatch, XmlNode parentToSearchIn)
        {
            //match any exact xml matches, including all the children

            foreach (XmlNode node in parentToSearchIn.ChildNodes)
            {
                XmlDiff.XmlDiff d = new XmlDiff.XmlDiff(elementToMatch.OuterXml, node.OuterXml);
                DiffResult result = d.Compare();
                if (result == null || result.Equal)
                {
                    return node;
                }
            }
            return null;
        }
    }
}