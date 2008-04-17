using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LiftVersionControlMerger=LiftIO.Merging.LiftVersionControlMerger;

namespace LiftMerge
{
    /// <summary>
    /// ;ext.lift = external C:\WeSay\lib\LIFT\LIFTDotNet\output\debug\liftmerge  $base $local $other $output
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
           string basePath = args[0];
            string localPath = args[1];
            string otherPath = args[2];
            string outputPath = args[3];

//            foreach (string s in args)
//            {
//                Console.WriteLine(s);
//            }

           Console.WriteLine("LiftMerge...");
           using (StreamWriter output = File.CreateText(outputPath))
            {
                LiftVersionControlMerger merger =
                    new LiftVersionControlMerger(File.ReadAllText(localPath), File.ReadAllText(otherPath), File.ReadAllText(basePath));
               
                //output.WriteLine("<!-- Processed by LiftMerge {0}, {1}, {2} -->", basePath, localPath, otherPath);
                output.Write(merger.GetMergedLift());
            }
        }
    }
}
