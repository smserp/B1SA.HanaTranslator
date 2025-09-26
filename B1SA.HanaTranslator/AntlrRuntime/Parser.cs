namespace Antlr.Runtime
{
    using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

    /** <summary>
     *  A parser for TokenStreams.  "parser grammars" result in a subclass
     *  of this.
     *  </summary>
     */
    public class Parser : BaseRecognizer
    {
        public ITokenStream input;

        public Parser(ITokenStream input)
            : base()
        {
            //super(); // highlight that we go to super to set state object
            TokenStream = input;
        }

        public Parser(ITokenStream input, RecognizerSharedState state)
            : base(state) // share the state object with another parser
        {
            this.input = input;
        }

        public override void Reset()
        {
            base.Reset(); // reset all recognizer state variables
            if (input != null) {
                input.Seek(0); // rewind the input
            }
        }

        protected override object GetCurrentInputSymbol(IIntStream input)
        {
            return ((ITokenStream) input).LT(1);
        }

        protected override object GetMissingSymbol(IIntStream input,
                                          RecognitionException e,
                                          int expectedTokenType,
                                          BitSet follow)
        {
            string tokenText = null;
            if (expectedTokenType == TokenTypes.EndOfFile)
                tokenText = "<missing EOF>";
            else
                tokenText = "<missing " + TokenNames[expectedTokenType] + ">";
            var t = new CommonToken(expectedTokenType, tokenText);
            var current = ((ITokenStream) input).LT(1);
            if (current.Type == TokenTypes.EndOfFile) {
                current = ((ITokenStream) input).LT(-1);
            }
            t.Line = current.Line;
            t.CharPositionInLine = current.CharPositionInLine;
            t.Channel = DefaultTokenChannel;
            t.InputStream = current.InputStream;
            return t;
        }

        /** <summary>Gets or sets the token stream; resets the parser upon a set.</summary> */
        public virtual ITokenStream TokenStream {
            get {
                return input;
            }
            set {
                input = null;
                Reset();
                input = value;
            }
        }

        public override string SourceName {
            get {
                return input.SourceName;
            }
        }

        [Conditional("ANTLR_TRACE")]
        public virtual void TraceIn(string ruleName, int ruleIndex)
        {
            base.TraceIn(ruleName, ruleIndex, input.LT(1));
        }

        [Conditional("ANTLR_TRACE")]
        public virtual void TraceOut(string ruleName, int ruleIndex)
        {
            base.TraceOut(ruleName, ruleIndex, input.LT(1));
        }
    }
}
