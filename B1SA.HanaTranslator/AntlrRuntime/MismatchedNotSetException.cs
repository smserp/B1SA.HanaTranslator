namespace Antlr.Runtime
{
    using Exception = Exception;

    [Serializable]
    public class MismatchedNotSetException : MismatchedSetException
    {
        public MismatchedNotSetException()
        {
        }

        public MismatchedNotSetException(string message)
            : base(message)
        {
        }

        public MismatchedNotSetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MismatchedNotSetException(BitSet expecting, IIntStream input)
            : base(expecting, input)
        {
        }

        public MismatchedNotSetException(string message, BitSet expecting, IIntStream input)
            : base(message, expecting, input)
        {
        }

        public MismatchedNotSetException(string message, BitSet expecting, IIntStream input, Exception innerException)
            : base(message, expecting, input, innerException)
        {
        }

        public override string ToString()
        {
            return "MismatchedNotSetException(" + UnexpectedType + "!=" + Expecting + ")";
        }
    }
}
