namespace Antlr.Runtime
{
    using System.Collections.Generic;
    using Exception = Exception;

    /** <summary>
     *  We were expecting a token but it's not found.  The current token
     *  is actually what we wanted next.  Used for tree node errors too.
     *  </summary>
     */
    [Serializable]
    public class MissingTokenException : MismatchedTokenException
    {
        private readonly object _inserted;

        public MissingTokenException()
        {
        }

        public MissingTokenException(string message)
            : base(message)
        {
        }

        public MissingTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MissingTokenException(int expecting, IIntStream input, object inserted)
            : this(expecting, input, inserted, null)
        {
        }

        public MissingTokenException(int expecting, IIntStream input, object inserted, IList<string> tokenNames)
            : base(expecting, input, tokenNames)
        {
            this._inserted = inserted;
        }

        public MissingTokenException(string message, int expecting, IIntStream input, object inserted, IList<string> tokenNames)
            : base(message, expecting, input, tokenNames)
        {
            this._inserted = inserted;
        }

        public MissingTokenException(string message, int expecting, IIntStream input, object inserted, IList<string> tokenNames, Exception innerException)
            : base(message, expecting, input, tokenNames, innerException)
        {
            this._inserted = inserted;
        }

        public virtual int MissingType {
            get {
                return Expecting;
            }
        }

        public override string ToString()
        {
            if (_inserted != null && Token != null) {
                return "MissingTokenException(inserted " + _inserted + " at " + Token.Text + ")";
            }
            if (Token != null) {
                return "MissingTokenException(at " + Token.Text + ")";
            }
            return "MissingTokenException";
        }
    }
}
