using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System;

namespace LiftIO
{
	/// <summary>
	/// This class represents the formatting information for a span of text.
	/// </summary>
	public class LiftSpan
	{
		int _index;			// index of first character covered by the span in the string
		int _length;		// length of span in the string
		string _lang;		// lang attribute value for the span, if any
		string _class;		// class attribute value for the span, if any
		string _linkurl;	// href attribute value for the span, if any

		public LiftSpan(int index, int length, string lang, string className, string href)
		{
			_index = index;
			_length = length;
			_lang = lang;
			_class = className;
			_linkurl = href;
		}

		public int Index
		{
			get { return _index; }
		}
		public int Length
		{
			get { return _length; }
		}
		public string Lang
		{
			get { return _lang; }
		}
		public string Class
		{
			get { return _class; }
		}
		public string LinkURL
		{
			get { return _linkurl; }
		}
	}

	/// <summary>
	/// This class represents a string with optional embedded formatting information.
	/// </summary>
	public class LiftString
	{
		string _text = null;
		List<LiftSpan> _spans = new List<LiftSpan>();

		public LiftString()
		{
		}
        public LiftString(string simpleContent)
        {
            Text = simpleContent;
        }

		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		/// <summary>
		/// Return the list of format specifications, if any.
		/// </summary>
		public List<LiftSpan> Spans
		{
			get { return _spans; }
		}
	}

	/// <summary>
	/// This class represents a multilingual string, possibly with embedded formatting
	/// information in each of the alternatives.
	/// </summary>
	public class LiftMultiText : Dictionary<string, LiftString>
    {
        private List<Trait> _traits = new List<Trait>();

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            foreach (string key in Keys)
            {
				b.AppendFormat("{0}={1}|", key, this[key].Text);
			}
            return b.ToString();
        }

        /// <summary>
        /// For WeSay, which doesn't yet understand structured strings 
        /// </summary>
	    public Dictionary<string, string> AsSimpleStrings
	    {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (KeyValuePair<string, LiftString> pair in this)
                {
                    if (pair.Value != null)
                    {
                        result.Add(pair.Key, pair.Value.Text);
                    }
                }

                return result;
            }
	    }

        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public KeyValuePair<string,LiftString> FirstValue
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
            LiftString existing;
            if (TryGetValue(key, out existing))
            {
                this[key].Text = prepend + existing.Text;
                return;
            }
            Debug.Fail("Tried to prepend to empty alternative "); //don't need to stop in release versions
        }

		// Add this method if you think we really need backward compatibility...
		//public void Add(string key, string text)
		//{
		//    LiftString str = new LiftString();
		//    str.Text = text;
		//    this.Add(key, str);
		//}

        /// <summary>
        /// if we already have a form in the lang, add the new one after adding the delimiter e.g. "tube; hose"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="delimiter"></param>
        public void AddOrAppend(string key, string newValue, string delimiter)
        {
            LiftString existing;
			if (TryGetValue(key, out existing))
			{
				if (String.IsNullOrEmpty(existing.Text))
					this[key].Text = newValue;
				else
					this[key].Text = existing.Text + delimiter + newValue;
			}
			else
			{
				LiftString alternative = new LiftString();
				alternative.Text = newValue;
				this[key] = alternative;
			}
        }

		/// <summary>
		/// Return the length of the text stored for the given language code, or zero if that
		/// alternative doesn't exist.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public int LengthOfAlternative(string key)
		{
			LiftString existing;
			if (TryGetValue(key, out existing))
				return existing.Text == null ? 0 : existing.Text.Length;
			else
				return 0;
		}

       public void Add(string key, string simpleContent)
        {
           this.Add(key, new LiftString(simpleContent));
        }

		public void AddSpan(string key, string lang, string style, string href, int length)
		{
			LiftString alternative;
			if (!TryGetValue(key, out alternative))
			{
				alternative = new LiftString();
				this[key] = alternative;
			}
			int start = alternative.Text.Length;
			if (lang == key)
				lang = null;
			alternative.Spans.Add(new LiftSpan(start, length, lang, style, href));
		}
	}
}