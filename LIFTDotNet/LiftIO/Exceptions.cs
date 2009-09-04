using System;


namespace LiftIO
{
    public class LiftFormatException : ApplicationException
    {
        private string _filePath;
        public LiftFormatException(string message) :base(message)
        {
        }

        public LiftFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
    }
    public class BadUpdateFileException :ApplicationException
    {
        private readonly string _pathToOldFile;
        private readonly string _pathToNewFile;

        public BadUpdateFileException(string pathToOldFile, string pathToNewFile, Exception innerException)
            : base("Error merging lift", innerException)
        {
            this._pathToOldFile = pathToOldFile;
            this._pathToNewFile = pathToNewFile;
        }

        public string PathToOldFile
        {
            get { return _pathToOldFile; }
        }

        public string PathToNewFile
        {
            get { return _pathToNewFile; }
        }
    }}
