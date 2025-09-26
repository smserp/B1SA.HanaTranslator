namespace Antlr.Runtime
{
    using Exception = Exception;

    /** <summary>
     *  A semantic predicate failed during validation.  Validation of predicates
     *  occurs when normally parsing the alternative just like matching a token.
     *  Disambiguating predicate evaluation occurs when we hoist a predicate into
     *  a prediction decision.
     *  </summary>
     */
    [Serializable]
    public class FailedPredicateException : RecognitionException
    {
        private readonly string _ruleName;
        private readonly string _predicateText;

        public FailedPredicateException()
        {
        }

        public FailedPredicateException(string message)
            : base(message)
        {
        }

        public FailedPredicateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FailedPredicateException(IIntStream input, string ruleName, string predicateText)
            : base(input)
        {
            this._ruleName = ruleName;
            this._predicateText = predicateText;
        }

        public FailedPredicateException(string message, IIntStream input, string ruleName, string predicateText)
            : base(message, input)
        {
            this._ruleName = ruleName;
            this._predicateText = predicateText;
        }

        public FailedPredicateException(string message, IIntStream input, string ruleName, string predicateText, Exception innerException)
            : base(message, input, innerException)
        {
            this._ruleName = ruleName;
            this._predicateText = predicateText;
        }

        public string RuleName {
            get {
                return _ruleName;
            }
        }

        public string PredicateText {
            get {
                return _predicateText;
            }
        }

        public override string ToString()
        {
            return "FailedPredicateException(" + RuleName + ",{" + PredicateText + "}?)";
        }
    }
}
