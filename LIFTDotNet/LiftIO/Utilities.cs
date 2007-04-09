using System.IO;
using System.Xml;

namespace LiftIO
{
    public class Utilities
    {
        static public void CreateEmptyLiftFile(string path, string producerString, bool doOverwriteIfExists)
        {
            if (File.Exists(path))
            {
                if (doOverwriteIfExists)
                {
                    File.Delete(path);
                }
                else
                {
                    return;
                }
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlTextWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("lift");
                writer.WriteAttributeString("version", Validator.LiftVersion);
                writer.WriteAttributeString("producer", producerString);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
    }
}
