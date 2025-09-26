namespace Antlr.Runtime
{
    using Exception = Exception;

    [Serializable]
    public class MismatchedSetException : RecognitionException
    {
        private readonly BitSet _expecting;

        public MismatchedSetException()
        {
        }

        public MismatchedSetException(string message)
            : base(message)
        {
        }

        public MismatchedSetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MismatchedSetException(BitSet expecting, IIntStream input)
            : base(input)
        {
            this._expecting = expecting;
        }

        public MismatchedSetException(string message, BitSet expecting, IIntStream input)
            : base(message, input)
        {
            this._expecting = expecting;
        }

        public MismatchedSetException(string message, BitSet expecting, IIntStream input, Exception innerException)
            : base(message, input, innerException)
        {
            this._expecting = expecting;
        }

        public BitSet Expecting {
            get {
                return _expecting;
            }
        }

        public override string ToString()
        {
            return "MismatchedSetException(" + UnexpectedType + "!=" + Expecting + ")";
        }
    }
}
