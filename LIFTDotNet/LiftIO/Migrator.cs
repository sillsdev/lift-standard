using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;

namespace LiftIO
{
    public class Migrator
    {
        public static bool IsMigrationNeeded(string pathToLift)
        {
            return (Validator.GetLiftVersion(pathToLift) != Validator.LiftVersion);
        }

        /// <summary>
        /// Creates a new file migrated to the current version
        /// </summary>
        /// <param name="pathToOriginalLift"></param>
        /// <returns>the path to the  migrated one, in the same directory</returns>
        public static string MigrateToLatestVersion(string pathToOriginalLift)
        {
            if (!IsMigrationNeeded(pathToOriginalLift))
            {
                throw new ArgumentException("This file is already the most current version. Use Validator.IsMigrationNeeded() first to determine if migration is needed.");
            }

            string sourceVersion = Validator.GetLiftVersion(pathToOriginalLift);

            string migrationSourcePath = pathToOriginalLift;
            while (sourceVersion != Validator.LiftVersion)
            {
                string xslName = GetNameOfXsltWhichConvertsFromVersion(sourceVersion);
                string targetVersion = xslName.Split(new char[] { '-' })[2];
                targetVersion = targetVersion.Remove(targetVersion.LastIndexOf('.'));
                string migrationTargetPath = String.Format("{0}-{1}", pathToOriginalLift, targetVersion);
                DoOneMigrationStep(xslName, migrationSourcePath, migrationTargetPath);
                if (migrationSourcePath != pathToOriginalLift)
                    File.Delete(migrationSourcePath);
                migrationSourcePath = migrationTargetPath;
                sourceVersion = targetVersion;
            }
            return migrationSourcePath;
        }



        private static void DoOneMigrationStep(string xslName, string migrationSourcePath, string migrationTargetPath)
        {
            Stream xslstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(xslName);
            XslCompiledTransform xsl = new XslCompiledTransform();
            xsl.Load(new XmlTextReader(xslstream));
            xsl.Transform(migrationSourcePath, migrationTargetPath);
        }

        private static string GetNameOfXsltWhichConvertsFromVersion(string sourceVersion)
        {
            string[] resources = typeof(LiftMultiText).Assembly.GetManifestResourceNames();
            string xslName = null;
            foreach (string name in resources)
            {
                if (name.EndsWith(".xsl") && name.StartsWith("LiftIO.LIFT-" + sourceVersion + "-"))
                {
                    xslName = name;
                    break;
                }
            }
            if (xslName == null)
                throw new LiftFormatException(string.Format("This program is not able to convert from the version of this lift file, {0}, to the version this program uses, {1}", sourceVersion, Validator.LiftVersion));
            return xslName;
        }
    }
}
