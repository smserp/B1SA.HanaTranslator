namespace Antlr.Runtime
{
    using Exception = Exception;

    /** <summary>The recognizer did not match anything for a (..)+ loop.</summary> */
    [Serializable]
    public class EarlyExitException : RecognitionException
    {
        private readonly int _decisionNumber;

        public EarlyExitException()
        {
        }

        public EarlyExitException(string message)
            : base(message)
        {
        }

        public EarlyExitException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public EarlyExitException(int decisionNumber, IIntStream input)
            : base(input)
        {
            this._decisionNumber = decisionNumber;
        }

        public EarlyExitException(string message, int decisionNumber, IIntStream input)
            : base(message, input)
        {
            this._decisionNumber = decisionNumber;
        }

        public EarlyExitException(string message, int decisionNumber, IIntStream input, Exception innerException)
            : base(message, input, innerException)
        {
            this._decisionNumber = decisionNumber;
        }

        public int DecisionNumber {
            get {
                return _decisionNumber;
            }
        }
    }
}
