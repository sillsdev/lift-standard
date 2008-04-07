using System;
using System.Xml;
using System.Xml.Xsl;
using System.IO;
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
            RelaxngValidatingReader reader = new RelaxngValidatingReader(
                documentReader,
                new XmlTextReader(typeof(LiftMultiText).Assembly.GetManifestResourceStream("LiftIO.lift.rng")));
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
                errors = string.Format("The dictionary file at {0} does not conform to the LIFT format used by this version of WeSay.  The RNG validator said: {1}.",
                                       pathToLiftFile, errors);
                throw new LiftFormatException(errors);
            }
        }

		public static string GetCorrectLiftVersionOfFile(string pathToOriginalLift)
		{
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.ValidationType = ValidationType.None;
			readerSettings.IgnoreComments = true;
			string version = null;
			using (XmlReader reader = XmlReader.Create(pathToOriginalLift, readerSettings))
			{
				if (reader.IsStartElement("lift"))
					version = reader.GetAttribute("version");
			}
			if (String.IsNullOrEmpty(version) || version == LiftIO.Validator.LiftVersion)
				return pathToOriginalLift;
			string[] resources = typeof(LiftMultiText).Assembly.GetManifestResourceNames();
			string pathToMigratedLift = pathToOriginalLift;
			while (version != LiftIO.Validator.LiftVersion)
			{
				string xslName = null;
				foreach (string name in resources)
				{
					if (name.EndsWith(".xsl") && name.StartsWith("LiftIO.LIFT-" + version + "-"))
					{
						xslName = name;
						break;
					}
				}
				if (xslName == null)
					break;
				string nextversion = xslName.Split(new char[] { '-' })[2];
				nextversion = nextversion.Remove(nextversion.LastIndexOf('.'));
				string nextfile = String.Format("{0}-{1}", pathToOriginalLift, nextversion);
				Stream xslstream = typeof(LiftMultiText).Assembly.GetManifestResourceStream(xslName);
				XslCompiledTransform xsl = new XslCompiledTransform();
				xsl.Load(new XmlTextReader(xslstream));
				xsl.Transform(pathToMigratedLift, nextfile);
				if (pathToMigratedLift != pathToOriginalLift)
					File.Delete(pathToMigratedLift);
				pathToMigratedLift = nextfile;
				version = nextversion;
			}
			return pathToMigratedLift;
		}
	}
}
