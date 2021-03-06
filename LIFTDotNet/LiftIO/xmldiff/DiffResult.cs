/* From http://xmlunit.sourceforge.net/ Moved here because the original library is for testing, and 
 * it is tied to nunit, which we don't want to ship in production
 */

namespace LiftIO.Merging.XmlDiff
{
	using System; 
	using System.Text; 
	
    public class DiffResult {
        private bool _areIdentical = true;
        private bool _areEqual = true;
        private Difference _difference;
    	private StringBuilder _stringBuilder;
    	
    	public DiffResult() {
    		_stringBuilder = new StringBuilder();
    	}
        
        public bool AreIdentical {
            get {
                return _areIdentical;
            }
        }
        
        public bool AreEqual {
            get {
                return _areEqual;
            }
        }
        
        public Difference Difference {
            get {
                return _difference;
            }
        }
     
        public string StringValue {
        	get {
	        	if (_stringBuilder.Length == 0) {
	        		if (AreIdentical) {
	        			_stringBuilder.Append("Identical");        			
	        		} else {
	        			_stringBuilder.Append("Equal");
	        		}
	        	}
	        	return _stringBuilder.ToString();
        	}
        }
        //was public, but jh didn't see why
        internal void DifferenceFound(XmlDiff inDiff, Difference difference) {
            _areIdentical = false;
            if (difference.HasMajorDifference) {
                _areEqual = false;
            }       
            _difference = difference;
        	if (_stringBuilder.Length == 0) {
        		_stringBuilder.Append(inDiff.OptionalDescription);
        	}
        	_stringBuilder.Append(Environment.NewLine).Append(difference);
        }        
    }
	
}
