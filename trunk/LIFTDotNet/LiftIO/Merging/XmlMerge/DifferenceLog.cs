using System.Collections.Generic;
using System.Xml;

namespace LiftIO.Merging.XmlMerge
{
   
    public interface IMergeLogger
    {
        void RegisterConflict(IConflict conflict);
    }
    public class MergeLogger : IMergeLogger
    {
        private readonly IList<IConflict> _conflicts;

        public MergeLogger(IList<IConflict> conflicts)
        {
            _conflicts = conflicts;
        }

        public void RegisterConflict(IConflict conflict)
        {
            _conflicts.Add(conflict);
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
