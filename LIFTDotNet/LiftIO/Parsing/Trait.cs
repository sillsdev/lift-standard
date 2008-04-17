using System.Collections.Generic;

namespace LiftIO.Parsing
{
    public class Trait
    {
        private string _forFormInWritingSystem;
        private string _name;
        private string _value;

        private List<Annotation> _annotations = new List<Annotation>();

        public Trait(string name, string value)
        {
            _name = name;
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

        public List<Annotation> Annotations
        {
            get
            {
                return _annotations;
            }
            set
            {
                _annotations = value;
            }
        }

        public override bool Equals(object obj)
        {
            Trait that = obj as Trait;
            if (that == null)
                return false;
            if (this._name != that._name)
                return false;
            if (this._value != that._value)
                return false;
            if((this._annotations==null) != (that._annotations==null))
                return false;
            if (this._annotations != null && this._annotations.Count != that._annotations.Count)
                return false;

            int matches = 0;
            foreach (Annotation annotation in _annotations)
            {
                foreach (Annotation thatAnnotation in that.Annotations)
                {
                    if(annotation == thatAnnotation)
                    {
                        matches++;
                        break;
                    }
                }
            }
            if(matches < _annotations.Count)
                return false;
            return true;
        }

    }
}