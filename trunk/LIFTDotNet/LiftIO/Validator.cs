using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Commons.Xml.Relaxng;

namespace LiftIO
{
    public class Validator
    {
        static public string GetAnyValidationErrors(string path)
        {
            using (XmlTextReader documentReader = new XmlTextReader(path))
            {
                return GetAnyValidationErrors(documentReader);
            }
        }

        static public string GetAnyValidationErrors(XmlTextReader documentReader)
        {
            string[] s = typeof(SimpleMultiText).Assembly.GetManifestResourceNames();

            RelaxngValidatingReader reader = new RelaxngValidatingReader(
                documentReader,
                new XmlTextReader(typeof(SimpleMultiText).Assembly.GetManifestResourceStream("LiftIO.lift.rng")));
            reader.ReportDetails = true;
            string lastGuy="lift";
            try
            {
                while (!reader.EOF)
                {
                    // Debug.WriteLine(reader.v
                    reader.Read();
                    lastGuy = reader.Name;
                }
            }
            catch (Exception e)
            {
                if (reader.Name == "version" && lastGuy == "lift")
                {
                    return String.Format(
                        "This file claims to be version {0} of LIFT, but this version of the program uses version {1}",
                        reader.Value, LiftIO.Validator.LiftVersion);
                }
                string m = string.Format("{0}\r\nError near: {1} {2} '{3}'",e.Message, lastGuy, reader.Name, reader.Value);
                return m;
            }
            return null;
        }

        public static string LiftVersion
        {
            get
            {
                return "0.9.1";
            }
        }
    }
}
