using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Commons.Xml.Relaxng;

namespace LiftIO
{
    public class Validator
    {
        static public bool CheckValidity(string path)
        {
            using (XmlTextReader documentReader = new XmlTextReader(path))
            {
                return CheckValidity(documentReader);
            }
        }

        static public bool CheckValidity(XmlTextReader documentReader)
        {
            string[] s = typeof(SimpleMultiText).Assembly.GetManifestResourceNames();

            RelaxngValidatingReader reader = new RelaxngValidatingReader(
                documentReader,
                new XmlTextReader(typeof(SimpleMultiText).Assembly.GetManifestResourceStream("LiftIO.lift.rng")));

            try
            {
                while (!reader.EOF)
                {
                    // Debug.WriteLine(reader.v
                    reader.Read();
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }
}
