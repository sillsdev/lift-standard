using System;
using System.Xml;
using Commons.Xml.Relaxng;
using LiftIO.Parsing;

namespace LiftIO.Validation
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
            RelaxngValidatingReader reader = new RelaxngValidatingReader(
                documentReader,
                new XmlTextReader(typeof(LiftMultiText).Assembly.GetManifestResourceStream("LiftIO.Validation.lift.rng")));
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
                if (reader.Name == "version" && (lastGuy == "lift" || lastGuy==""))
                {
                    return String.Format(
                        "This file claims to be version {0} of LIFT, but this version of the program uses version {1}",
                        reader.Value, LiftVersion);
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
                return "0.12";
            }
        }

        public static void CheckLiftWithPossibleThrow(string pathToLiftFile)
        {
            string errors = GetAnyValidationErrors(pathToLiftFile);
            if (!String.IsNullOrEmpty(errors))
            {
                errors = string.Format("The dictionary file at {0} does not conform to the current version of the LIFT format ({1}).  The RNG validator said: {2}.",
                                       pathToLiftFile, LiftVersion, errors);
                throw new LiftFormatException(errors);
            }
        }

        public static string GetLiftVersion(string pathToLift)
        {
            string liftVersionOfRequestedFile = String.Empty;

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.None;
            readerSettings.IgnoreComments = true;

            using (XmlReader reader = XmlReader.Create(pathToLift, readerSettings))
            {
                if (reader.IsStartElement("lift"))
                    liftVersionOfRequestedFile = reader.GetAttribute("version");
            }
            if (String.IsNullOrEmpty(liftVersionOfRequestedFile))
            {
                throw new LiftFormatException(String.Format("Cannot import {0} because this was not recognized as well-formed LIFT file (missing version).", pathToLift));
            }
            return liftVersionOfRequestedFile;
        }   
    }
}