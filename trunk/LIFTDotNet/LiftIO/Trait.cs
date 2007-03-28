using System.Collections.Generic;

namespace LiftIO
{
    public class Trait
    {
        private string _forFormInWritingSystem;
        private string _name;
        private string _value;

        private List<Trait> _subTraits= new List<Trait>();

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

        public List<Trait> Traits
        {
            get
            {
                return _subTraits;
            }
            set
            {
                _subTraits = value;
            }
        }
    }
}