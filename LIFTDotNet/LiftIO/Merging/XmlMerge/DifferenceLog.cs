using System.Xml;

namespace LiftIO.Merging.XmlMerge
{
   
    public interface IMergeLogger
    {
        void RegisterConflict(IConflict conflict);
    }
    public class ConsolMergeLogger : IMergeLogger
    {
        public void RegisterConflict(IConflict conflict)
        {
        }
    }

    public class DifferenceReport
    {
        public string _result;
    }
    
    public interface IDifferenceReportMaker
    {
        DifferenceReport GetReport();
    }
    
    public class DefaultDifferenceReportMaker : IDifferenceReportMaker
    {
 
        public DifferenceReport GetReport()
        {
            return new DifferenceReport();
        }

    }
}
