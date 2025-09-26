namespace Antlr.Runtime
{
    using Exception = Exception;
    using ITreeNodeStream = Tree.ITreeNodeStream;

    [Serializable]
    public class MismatchedTreeNodeException : RecognitionException
    {
        private readonly int _expecting;

        public MismatchedTreeNodeException()
        {
        }

        public MismatchedTreeNodeException(string message)
            : base(message)
        {
        }

        public MismatchedTreeNodeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MismatchedTreeNodeException(int expecting, ITreeNodeStream input)
            : base(input)
        {
            this._expecting = expecting;
        }

        public MismatchedTreeNodeException(string message, int expecting, ITreeNodeStream input)
            : base(message, input)
        {
            this._expecting = expecting;
        }

        public MismatchedTreeNodeException(string message, int expecting, ITreeNodeStream input, Exception innerException)
            : base(message, input, innerException)
        {
            this._expecting = expecting;
        }

        public int Expecting {
            get {
                return _expecting;
            }
        }

        public override string ToString()
        {
            return "MismatchedTreeNodeException(" + UnexpectedType + "!=" + Expecting + ")";
        }
    }
}
