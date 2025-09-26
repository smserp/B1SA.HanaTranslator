namespace Antlr.Runtime
{
    using System.Collections.Generic;
    using Exception = Exception;

    /// <summary>An extra token while parsing a TokenStream</summary>
    [Serializable]
    public class UnwantedTokenException : MismatchedTokenException
    {
        public UnwantedTokenException()
        {
        }

        public UnwantedTokenException(string message)
            : base(message)
        {
        }

        public UnwantedTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public UnwantedTokenException(int expecting, IIntStream input)
            : base(expecting, input)
        {
        }

        public UnwantedTokenException(int expecting, IIntStream input, IList<string> tokenNames)
            : base(expecting, input, tokenNames)
        {
        }

        public UnwantedTokenException(string message, int expecting, IIntStream input, IList<string> tokenNames)
            : base(message, expecting, input, tokenNames)
        {
        }

        public UnwantedTokenException(string message, int expecting, IIntStream input, IList<string> tokenNames, Exception innerException)
            : base(message, expecting, input, tokenNames, innerException)
        {
        }

        public virtual IToken UnexpectedToken {
            get {
                return Token;
            }
        }

        public override string ToString()
        {
            //int unexpectedType = getUnexpectedType();
            //string unexpected = ( tokenNames != null && unexpectedType >= 0 && unexpectedType < tokenNames.Length ) ? tokenNames[unexpectedType] : unexpectedType.ToString();
            var expected = (TokenNames != null && Expecting >= 0 && Expecting < TokenNames.Count) ? TokenNames[Expecting] : Expecting.ToString();

            var exp = ", expected " + expected;
            if (Expecting == TokenTypes.Invalid) {
                exp = "";
            }
            if (Token == null) {
                return "UnwantedTokenException(found=" + null + exp + ")";
            }
            return "UnwantedTokenException(found=" + Token.Text + exp + ")";
        }
    }
}
