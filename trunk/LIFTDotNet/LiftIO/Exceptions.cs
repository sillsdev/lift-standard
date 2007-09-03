using System;
using System.Collections.Generic;
using System.Text;

namespace LiftIO
{
    public class LiftFormatException : ApplicationException
    {
        public LiftFormatException(string message) :base(message)
        {
        }
    }
}
