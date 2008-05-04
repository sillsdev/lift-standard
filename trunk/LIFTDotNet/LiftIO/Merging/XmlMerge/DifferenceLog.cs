using System;
using System.Collections.Generic;
using System.Text;

namespace LiftIO.Merging.XmlMerge
{
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
