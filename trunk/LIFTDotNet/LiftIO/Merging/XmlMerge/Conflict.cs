using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiftIO.Merging.XmlMerge
{
    public interface IConflict
    {
        string GetFullHumanReadableDescription();
        string ConflictTypeHumanName
        {
            get;
        }
    }


    public abstract class AttributeConflict : IConflict
    {
        protected readonly string _attributeName;
        protected readonly string _ourValue;
        protected readonly string _theirValue;
        protected readonly string _ancestorValue;
        protected readonly MergeStrategies _mergeStrategies;

        public AttributeConflict(string attributeName, string ourValue, string theirValue, string ancestorValue, MergeStrategies mergeStrategies)
        {
            _attributeName = attributeName;
            _ourValue = ourValue;
            _theirValue = theirValue;
            _ancestorValue = ancestorValue;
            _mergeStrategies = mergeStrategies;
        }

        public string AttributeDescription
        {
            get
            {
                return string.Format("{0}", _attributeName);
            }
        }

        public string WhatHappened
        {
            get
            {
                string ancestor = string.IsNullOrEmpty(_ancestorValue) ? "<didn't exist>" : _ancestorValue;
                string ours = string.IsNullOrEmpty(_ourValue) ? "<removed>" : _ourValue;
                string theirs = string.IsNullOrEmpty(_theirValue) ? "<removed>" : _theirValue;
                return string.Format("When we last synchronized, the value was {0}. Since then, we changed it to {1}, while they changed it to {2}.",
                    ancestor, ours, theirs);
            }
        }
        public virtual string GetFullHumanReadableDescription()
        {
            return string.Format("{0} ({1}): {2}", ConflictTypeHumanName, AttributeDescription, WhatHappened);
        }
        public virtual string GetXmlOfConflict()
        {
            return string.Format("<conflict type='{0}'/>", this.GetType().Name);
        }
        public abstract string ConflictTypeHumanName
        {
            get;
        }
    }

    public class RemovedVsEditedAttributeConflict : AttributeConflict
    {
        public RemovedVsEditedAttributeConflict(string attributeName, string ourValue, string theirValue, string ancestorValue, MergeStrategies mergeStrategies)
            : base(attributeName, ourValue, theirValue, ancestorValue, mergeStrategies)
        {
        }
        public override string ConflictTypeHumanName
        {
            get { return string.Format("Removed Vs Edited Attribute Conflict"); }
        }
    }

    internal class BothEdittedAttributeConflict : AttributeConflict
    {
        public BothEdittedAttributeConflict(string attributeName, string ourValue, string theirValue, string ancestorValue, MergeStrategies mergeStrategies)
            : base(attributeName, ourValue, theirValue, ancestorValue, mergeStrategies)
        {
        }

        public override string ConflictTypeHumanName
        {
            get { return string.Format("Both Edited Attribute Conflict"); }
        }
    }

    internal class BothEdittedTextConflict : AttributeConflict
    {
        public BothEdittedTextConflict(XmlNode ours, XmlNode theirs, XmlNode ancestor, MergeStrategies mergeStrategies)
            : base("text", ours.InnerText, theirs.InnerText,
                        ancestor == null ? string.Empty : ancestor.InnerText,
            mergeStrategies)
        {
        }

        public override string ConflictTypeHumanName
        {
            get { return string.Format("Both Edited Text Field Conflict"); }
        }
    }

    public class RemovedVsEdittedTextConflict : AttributeConflict
    {
        public RemovedVsEdittedTextConflict(XmlNode ours, XmlNode theirs, XmlNode ancestor, MergeStrategies mergeStrategies)
            : base("text", ours == null ? string.Empty : ours.InnerText,
                        theirs == null ? string.Empty : theirs.InnerText,
                        ancestor.InnerText,
                        mergeStrategies)
        {
        }

        public override string ConflictTypeHumanName
        {
            get { return string.Format("Both Edited Text Field Conflict"); }
        }
    }

    public abstract class ElementConflict : IConflict
    {
        protected readonly string _elementName;
        protected readonly XmlNode _ourElement;
        protected readonly XmlNode _theirElement;
        protected readonly XmlNode _ancestorElement;
        protected readonly MergeStrategies _mergeStrategies;

        public ElementConflict(string elementName, XmlNode ourElement, XmlNode theirElement, XmlNode ancestorElement,
            MergeStrategies mergeStrategies)
        {
            _elementName = elementName;
            _ourElement = ourElement;
            _theirElement = theirElement;
            _ancestorElement = ancestorElement;
            _mergeStrategies = mergeStrategies;
        }



        public virtual string GetFullHumanReadableDescription()
        {
            //enhance: this is a bit of a hack to pick some element that isn't null
            XmlNode element = _ourElement == null ? _ancestorElement : _ourElement;
            if(element == null)
            {
                element = _theirElement;
            }

            return string.Format("{0} ({1}): {2}", ConflictTypeHumanName, _mergeStrategies.GetElementStrategy(element).GetHumanDescription(element), WhatHappened);
        }




        public abstract string ConflictTypeHumanName
        {
            get;
        }
        public abstract string WhatHappened
        {
            get;
        }
    }

    internal class RemovedVsEditedElementConflict : ElementConflict
    {
        public RemovedVsEditedElementConflict(string elementName, XmlNode ourElement, XmlNode theirElement, XmlNode ancestorElement, MergeStrategies mergeStrategies)
            : base(elementName, ourElement, theirElement, ancestorElement, mergeStrategies)
        {
        }

        public override string ConflictTypeHumanName
        {
            get { return "Removed Vs Edited Element Conflict"; }
        }

        public override string WhatHappened
        {
            get
            {
                if (_theirElement == null)
                {
                    return "Since we last synchronized, they deleted this element, while you or the program you were using edited it.";
                }
                else 
                {
                    return "Since we last synchronized, you deleted this element, while they or the program they were using edited it.";
                }
            }
        }
    }

}
