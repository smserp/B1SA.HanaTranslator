namespace Antlr.Runtime.Tree
{
    using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
    using Regex = System.Text.RegularExpressions.Regex;
    using RegexOptionsHelper = Misc.RegexOptionsHelper;

    /** <summary>
     *  A parser for a stream of tree nodes.  "tree grammars" result in a subclass
     *  of this.  All the error reporting and recovery is shared with Parser via
     *  the BaseRecognizer superclass.
     *  </summary>
    */
    public class TreeParser : BaseRecognizer
    {
        public const int DOWN = TokenTypes.Down;
        public const int UP = TokenTypes.Up;

        // precompiled regex used by inContext
        private static string dotdot = ".*[^.]\\.\\.[^.].*";
        private static string doubleEtc = ".*\\.\\.\\.\\s+\\.\\.\\..*";
        private static Regex dotdotPattern = new Regex(dotdot, RegexOptionsHelper.Compiled);
        private static Regex doubleEtcPattern = new Regex(doubleEtc, RegexOptionsHelper.Compiled);

        protected ITreeNodeStream input;

        public TreeParser(ITreeNodeStream input)
            : base() // highlight that we go to super to set state object
        {
            this.input = input;
        }

        public TreeParser(ITreeNodeStream input, RecognizerSharedState state)
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

        /** <summary>Set the input stream</summary> */
        public virtual void SetTreeNodeStream(ITreeNodeStream input)
        {
            this.input = input;
        }

        public virtual ITreeNodeStream GetTreeNodeStream()
        {
            return input;
        }

        public override string SourceName {
            get {
                return input.SourceName;
            }
        }

        protected override object GetCurrentInputSymbol(IIntStream input)
        {
            return ((ITreeNodeStream) input).LT(1);
        }

        protected override object GetMissingSymbol(IIntStream input,
                                          RecognitionException e,
                                          int expectedTokenType,
                                          BitSet follow)
        {
            var tokenText =
                "<missing " + TokenNames[expectedTokenType] + ">";
            var adaptor = ((ITreeNodeStream) e.Input).TreeAdaptor;
            return adaptor.Create(new CommonToken(expectedTokenType, tokenText));
        }

        /** <summary>
         *  Match '.' in tree parser has special meaning.  Skip node or
         *  entire tree if node has children.  If children, scan until
         *  corresponding UP node.
         *  </summary>
         */
        public override void MatchAny(IIntStream ignore)
        {
            state.errorRecovery = false;
            state.failed = false;
            // always consume the current node
            input.Consume();
            // if the next node is DOWN, then the current node is a subtree:
            // skip to corresponding UP. must count nesting level to get right UP
            var look = input.LA(1);
            if (look == DOWN) {
                input.Consume();
                var level = 1;
                while (level > 0) {
                    switch (input.LA(1)) {
                        case DOWN:
                            level++;
                            break;
                        case UP:
                            level--;
                            break;
                        case TokenTypes.EndOfFile:
                            return;
                        default:
                            break;
                    }
                    input.Consume();
                }
            }
        }

        /** <summary>
         *  We have DOWN/UP nodes in the stream that have no line info; override.
         *  plus we want to alter the exception type.  Don't try to recover
         *  from tree parser errors inline...
         *  </summary>
         */
        protected override object RecoverFromMismatchedToken(IIntStream input, int ttype, BitSet follow)
        {
            throw new MismatchedTreeNodeException(ttype, (ITreeNodeStream) input);
        }

        /** <summary>
         *  Prefix error message with the grammar name because message is
         *  always intended for the programmer because the parser built
         *  the input tree not the user.
         *  </summary>
         */
        public override string GetErrorHeader(RecognitionException e)
        {
            return GrammarFileName + ": node from " +
                   (e.ApproximateLineInfo ? "after " : "") + "line " + e.Line + ":" + e.CharPositionInLine;
        }

        /** <summary>
         *  Tree parsers parse nodes they usually have a token object as
         *  payload. Set the exception token and do the default behavior.
         *  </summary>
         */
        public override string GetErrorMessage(RecognitionException e, string[] tokenNames)
        {
            if (this is TreeParser) {
                var adaptor = ((ITreeNodeStream) e.Input).TreeAdaptor;
                e.Token = adaptor.GetToken(e.Node);
                if (e.Token == null) { // could be an UP/DOWN node
                    e.Token = new CommonToken(adaptor.GetType(e.Node),
                                              adaptor.GetText(e.Node));
                }
            }
            return base.GetErrorMessage(e, tokenNames);
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
