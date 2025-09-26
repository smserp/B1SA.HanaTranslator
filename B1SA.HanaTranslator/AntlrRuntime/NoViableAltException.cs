namespace Antlr.Runtime
{
    using Exception = Exception;

    [Serializable]
    public class NoViableAltException : RecognitionException
    {
        private readonly string _grammarDecisionDescription;
        private readonly int _decisionNumber;
        private readonly int _stateNumber;

        public NoViableAltException()
        {
        }

        public NoViableAltException(string grammarDecisionDescription)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
        }

        public NoViableAltException(string message, string grammarDecisionDescription)
            : base(message)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
        }

        public NoViableAltException(string message, string grammarDecisionDescription, Exception innerException)
            : base(message, innerException)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
        }

        public NoViableAltException(string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input)
            : this(grammarDecisionDescription, decisionNumber, stateNumber, input, 1)
        {
        }

        public NoViableAltException(string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input, int k)
            : base(input, k)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
            this._decisionNumber = decisionNumber;
            this._stateNumber = stateNumber;
        }

        public NoViableAltException(string message, string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input)
            : this(message, grammarDecisionDescription, decisionNumber, stateNumber, input, 1)
        {
        }

        public NoViableAltException(string message, string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input, int k)
            : base(message, input, k)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
            this._decisionNumber = decisionNumber;
            this._stateNumber = stateNumber;
        }

        public NoViableAltException(string message, string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input, Exception innerException)
            : this(message, grammarDecisionDescription, decisionNumber, stateNumber, input, 1, innerException)
        {
        }

        public NoViableAltException(string message, string grammarDecisionDescription, int decisionNumber, int stateNumber, IIntStream input, int k, Exception innerException)
            : base(message, input, k, innerException)
        {
            this._grammarDecisionDescription = grammarDecisionDescription;
            this._decisionNumber = decisionNumber;
            this._stateNumber = stateNumber;
        }

        public int DecisionNumber {
            get {
                return _decisionNumber;
            }
        }

        public string GrammarDecisionDescription {
            get {
                return _grammarDecisionDescription;
            }
        }

        public int StateNumber {
            get {
                return _stateNumber;
            }
        }

        public override string ToString()
        {
            if (Input is ICharStream) {
                return "NoViableAltException('" + (char) UnexpectedType + "'@[" + GrammarDecisionDescription + "])";
            }
            else {
                return "NoViableAltException(" + UnexpectedType + "@[" + GrammarDecisionDescription + "])";
            }
        }
    }
}
