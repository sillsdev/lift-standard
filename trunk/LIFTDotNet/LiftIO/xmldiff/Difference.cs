/* From http://xmlunit.sourceforge.net/ Moved here because the original library is for testing, and 
 * it is tied to nunit, which we don't want to ship in production
 */

namespace LiftIO.Merging.XmlDiff
{
    using System.Xml;    
    
    public class Difference {
        private readonly DifferenceType _diffType;
        private readonly bool _hasMajorDifference;
        private XmlNodeType _controlNodeType;
        private XmlNodeType _testNodeType;
        
        public Difference(DifferenceType id) {
            _diffType = id;
            _hasMajorDifference = Differences.isMajorDifference(id);
        }
        
        public Difference(DifferenceType id, XmlNodeType controlNodeType, XmlNodeType testNodeType) 
        : this(id) {
            _controlNodeType = controlNodeType;
            _testNodeType = testNodeType;
        }
        
        public DifferenceType DiffType {
            get {
                return _diffType;
            }
        }
        
        public bool HasMajorDifference {
            get {
                return _hasMajorDifference;
            }
        }
        
        public XmlNodeType ControlNodeType {
            get {
                return _controlNodeType;
            }
        }
        
        public XmlNodeType TestNodeType {
            get {
                return _testNodeType;
            }
        }
        
        public override string ToString() {
            string asString = base.ToString() + " type: " + (int) _diffType 
                + ", control Node: " + _controlNodeType.ToString()
                + ", test Node: " + _testNodeType.ToString();            
            return asString;
        }
    }
}
