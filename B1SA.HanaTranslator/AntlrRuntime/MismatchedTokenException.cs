namespace Antlr.Runtime
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Exception = Exception;

    /** <summary>A mismatched char or Token or tree node</summary> */
    [Serializable]
    public class MismatchedTokenException : RecognitionException
    {
        private readonly int _expecting = TokenTypes.Invalid;
        private readonly ReadOnlyCollection<string> _tokenNames;

        public MismatchedTokenException()
        {
        }

        public MismatchedTokenException(string message)
            : base(message)
        {
        }

        public MismatchedTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MismatchedTokenException(int expecting, IIntStream input)
            : this(expecting, input, null)
        {
        }

        public MismatchedTokenException(int expecting, IIntStream input, IList<string> tokenNames)
            : base(input)
        {
            this._expecting = expecting;

            if (tokenNames != null)
                this._tokenNames = new ReadOnlyCollection<string>(new List<string>(tokenNames));
        }

        public MismatchedTokenException(string message, int expecting, IIntStream input, IList<string> tokenNames)
            : base(message, input)
        {
            this._expecting = expecting;

            if (tokenNames != null)
                this._tokenNames = new ReadOnlyCollection<string>(new List<string>(tokenNames));
        }

        public MismatchedTokenException(string message, int expecting, IIntStream input, IList<string> tokenNames, Exception innerException)
            : base(message, input, innerException)
        {
            this._expecting = expecting;

            if (tokenNames != null)
                this._tokenNames = new ReadOnlyCollection<string>(new List<string>(tokenNames));
        }

        public int Expecting {
            get {
                return _expecting;
            }
        }

        public ReadOnlyCollection<string> TokenNames {
            get {
                return _tokenNames;
            }
        }

        public override string ToString()
        {
            var unexpectedType = UnexpectedType;
            var unexpected = (TokenNames != null && unexpectedType >= 0 && unexpectedType < TokenNames.Count) ? TokenNames[unexpectedType] : unexpectedType.ToString();
            var expected = (TokenNames != null && Expecting >= 0 && Expecting < TokenNames.Count) ? TokenNames[Expecting] : Expecting.ToString();
            return "MismatchedTokenException(" + unexpected + "!=" + expected + ")";
        }
    }
}
