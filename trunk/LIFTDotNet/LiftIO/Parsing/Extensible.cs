using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LiftIO.Parsing
{
    /// <summary>
    /// See LIFT documentation for explanation of what this is.
    /// </summary>
    public class Extensible
    {
        private string _id;//actually not part of extensible (as of 8/1/2007)
        private DateTime _creationTime;
        private DateTime _modificationTime;

        //private List<Trait> _traits;
        //private List<Field> _fields;
        //private List<Annotation> _annotation;

 
        static public string LiftTimeFormatWithTimeZone = "yyyy-MM-ddTHH:mm:sszzzz";
        static public string LiftTimeFormatNoTimeZone = "yyyy-MM-ddTHH:mm:ssZ";
        static public string LiftDateOnlyFormat = "yyyy-MM-dd";
        private Guid _guid;

        public Extensible()
        {
            _creationTime = DateTime.UtcNow;
            _modificationTime = _creationTime;
            _guid = Guid.NewGuid();
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set 
            { 
                Debug.Assert(value == default(DateTime) || value.Kind == DateTimeKind.Utc);
                _creationTime = value; 
            }
        }
        public static DateTime ParseDateTimeCorrectly(string time)
        {
            var formats = new string[]
                                  {
                                      LiftTimeFormatNoTimeZone, LiftTimeFormatWithTimeZone,
                                      LiftDateOnlyFormat
                                  };
            try
            {
                DateTime result = DateTime.ParseExact(time,
                                                      formats,
                                                      new DateTimeFormatInfo(),
                                                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                Debug.Assert(result.Kind == DateTimeKind.Utc);
                return result;
            }
            catch (FormatException e)
            {
                var builder = new StringBuilder();
                builder.AppendFormat("One of the date fields contained a date/time format which could not be parsed ({0})." + Environment.NewLine, time);
                builder.Append("This program can parse the following formats: ");
                foreach (var format in formats)
                {
                    builder.Append(format + Environment.NewLine);
                }
                builder.Append("See: http://en.wikipedia.org/wiki/ISO_8601 for an explanation of these symbols.");
                throw new LiftFormatException(builder.ToString(), e);
            }
        }

        public DateTime ModificationTime
        {
            get { return _modificationTime; }
            set 
            {
                Debug.Assert(value==default(DateTime) || value.Kind == DateTimeKind.Utc);
                _modificationTime = value;
            }
        }

        public string Id
        {
            get
            {
                if (_id == null)
                {
                    return string.Empty;
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public Guid Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }

        public override string ToString()
        {
            string s = _id;
            if (Guid != Guid.Empty)
            {
                s += "/" + Guid;
            }
            s += ";";

            if (default(DateTime) != _creationTime)
            {
                s += _creationTime.ToString(LiftTimeFormatNoTimeZone);
            }
            s += ";";
            if (default(DateTime) != _modificationTime)
            {
                s += _modificationTime.ToString(LiftTimeFormatNoTimeZone);
            }
            s += ";";

            return s;
        }
    }
}