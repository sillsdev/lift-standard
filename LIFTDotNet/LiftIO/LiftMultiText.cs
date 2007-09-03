using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LiftIO
{
  

    public class LiftMultiText : Dictionary<string, string>
    {
        private List<Trait> _traits = new List<Trait>();

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            foreach (string key in Keys)
            {
                b.AppendFormat("{0}={1}|", key, this[key]);
            }
            return b.ToString();
        }

        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public KeyValuePair<string,string> FirstValue
        {
            get
            {
                Enumerator enumerator = GetEnumerator();
                enumerator.MoveNext();
                return enumerator.Current;
            }
        }

        public List<Trait> Traits
        {
            get
            {
                return _traits;
            }
            set
            {
                _traits = value;
            }
        }

        public void Prepend(string key, string prepend)
        {
            string existing;
            if (TryGetValue(key, out existing))
            {
                this[key] = prepend + existing;
                return;
            }
            Debug.Fail("Tried to prepend to empty alternative "); //don't need to stop in release versions
        }

        /// <summary>
        /// if we already have a form in the lang, add the new one after adding the delimiter e.g. "tube; hose"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="delimiter"></param>
        public void AddOrAppend(string key, string newValue, string delimiter)
        {
            string existing;
            if (TryGetValue(key, out existing))
            {
                this[key] = existing + delimiter + newValue;
                return;
            }
            this[key] = newValue;
        }
    }
}