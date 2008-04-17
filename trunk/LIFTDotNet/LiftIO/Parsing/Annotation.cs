using System;

namespace LiftIO.Parsing
{
    public class Annotation
    {
        private string _name;
        private string _value;
        private DateTime _when;
        private string _who;
        private string _forFormInWritingSystem;

        public Annotation(string name, string value, DateTime when, string who)
        {
            _name = name;
            _who = who;
            _when = when;
            _value = value;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        /// <summary>
        /// This is an index into the forms... !!!!!!!this probably not going to really work
        /// </summary>
        public string LanguageHint
        {
            get
            {
                return _forFormInWritingSystem;
            }
            set
            {
                _forFormInWritingSystem = value;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                this._value = value;
            }
        }

        public DateTime When
        {
            get { return _when; }
            set { _when = value; }
        }

        public string Who
        {
            get { return _who; }
            set { _who = value; }
        }


        public override bool Equals(object obj)
        {
            Annotation that = obj as Annotation;
            if (that == null)
                return false;
            if (this._name != that._name)
                return false;
            if (this.When != that.When)
                return false;
            if (this._value != that._value)
                return false;
            if (this.Who != that.Who)
                return false;
            return true;
        }
    }
}