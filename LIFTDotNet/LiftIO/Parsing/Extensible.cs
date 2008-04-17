using System;
using System.Diagnostics;
using System.Globalization;

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
            DateTime result = DateTime.ParseExact(time,
                                                  new string[] { LiftTimeFormatNoTimeZone, LiftTimeFormatWithTimeZone, LiftDateOnlyFormat },
                                                  new DateTimeFormatInfo(),
                                                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            Debug.Assert(result.Kind == DateTimeKind.Utc);
            return result;
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