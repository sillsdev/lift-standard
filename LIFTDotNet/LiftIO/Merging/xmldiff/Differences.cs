/* From http://xmlunit.sourceforge.net/ Moved here because the original library is for testing, and 
 * it is tied to nunit, which we don't want to ship in production
 */

namespace LiftIO.Merging.XmlDiff
{
    public class Differences {
        private Differences() { }
        
        public static bool isMajorDifference(DifferenceType differenceType) {
            switch (differenceType) {
                case DifferenceType.ATTR_SEQUENCE_ID:
                    return false;
                case DifferenceType.HAS_XML_DECLARATION_PREFIX_ID:
                    return false;
                default:
                    return true;
            }
        }
        
    }
}
