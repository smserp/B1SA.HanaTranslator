namespace Antlr.Runtime
{
    using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

    /** <summary>
     *  A lexer is recognizer that draws input symbols from a character stream.
     *  lexer grammars result in a subclass of this object. A Lexer object
     *  uses simplified match() and error recovery mechanisms in the interest
     *  of speed.
     *  </summary>
     */
    public abstract class Lexer : BaseRecognizer, ITokenSource
    {
        /** <summary>Where is the lexer drawing characters from?</summary> */
        protected ICharStream input;

        public Lexer()
        {
        }

        public Lexer(ICharStream input)
        {
            this.input = input;
        }

        public Lexer(ICharStream input, RecognizerSharedState state)
            : base(state)
        {
            this.input = input;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the text matched so far for the current token or any text override.
        /// </summary>
        /// <remarks>
        /// <para>Setting this value replaces any previously set value, and overrides the original text.</para>
        /// </remarks>
        public string Text {
            get {
                if (state.text != null) {
                    return state.text;
                }
                return input.Substring(state.tokenStartCharIndex, CharIndex - state.tokenStartCharIndex);
            }
            set {
                state.text = value;
            }
        }
        public int Line {
            get {
                return input.Line;
            }
            set {
                input.Line = value;
            }
        }
        public int CharPositionInLine {
            get {
                return input.CharPositionInLine;
            }
            set {
                input.CharPositionInLine = value;
            }
        }
        #endregion

        public override void Reset()
        {
            base.Reset(); // reset all recognizer state variables
            // wack Lexer state variables
            if (input != null) {
                input.Seek(0); // rewind the input
            }
            if (state == null) {
                return; // no shared state work to do
            }
            state.token = null;
            state.type = TokenTypes.Invalid;
            state.channel = TokenChannels.Default;
            state.tokenStartCharIndex = -1;
            state.tokenStartCharPositionInLine = -1;
            state.tokenStartLine = -1;
            state.text = null;
        }

        /** <summary>Return a token from this source; i.e., match a token on the char stream.</summary> */
        public virtual IToken NextToken()
        {
            for (; ; )
            {
                state.token = null;
                state.channel = TokenChannels.Default;
                state.tokenStartCharIndex = input.Index;
                state.tokenStartCharPositionInLine = input.CharPositionInLine;
                state.tokenStartLine = input.Line;
                state.text = null;
                if (input.LA(1) == CharStreamConstants.EndOfFile) {
                    return GetEndOfFileToken();
                }
                try {
                    ParseNextToken();
                    if (state.token == null) {
                        Emit();
                    }
                    else if (state.token == Tokens.Skip) {
                        continue;
                    }
                    return state.token;
                }
                catch (MismatchedRangeException mre) {
                    ReportError(mre);
                    // MatchRange() routine has already called recover()
                }
                catch (MismatchedTokenException mte) {
                    ReportError(mte);
                    // Match() routine has already called recover()
                }
                catch (RecognitionException re) {
                    ReportError(re);
                    Recover(re); // throw out current char and try again
                }
            }
        }

        /** Returns the EOF token (default), if you need
         *  to return a custom token instead override this method.
         */
        public virtual IToken GetEndOfFileToken()
        {
            IToken eof = new CommonToken((ICharStream) input, CharStreamConstants.EndOfFile, TokenChannels.Default, input.Index, input.Index);
            eof.Line = Line;
            eof.CharPositionInLine = CharPositionInLine;
            return eof;
        }

        /** <summary>
         *  Instruct the lexer to skip creating a token for current lexer rule
         *  and look for another token.  nextToken() knows to keep looking when
         *  a lexer rule finishes with token set to SKIP_TOKEN.  Recall that
         *  if token==null at end of any token rule, it creates one for you
         *  and emits it.
         *  </summary>
         */
        public virtual void Skip()
        {
            state.token = Tokens.Skip;
        }

        /** <summary>This is the lexer entry point that sets instance var 'token'</summary> */
        public abstract void mTokens();

        public virtual ICharStream CharStream {
            get {
                return input;
            }
            /* Set the char stream and reset the lexer */
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

        /** <summary>
         *  Currently does not support multiple emits per nextToken invocation
         *  for efficiency reasons.  Subclass and override this method and
         *  nextToken (to push tokens into a list and pull from that list rather
         *  than a single variable as this implementation does).
         *  </summary>
         */
        public virtual void Emit(IToken token)
        {
            state.token = token;
        }

        /** <summary>
         *  The standard method called to automatically emit a token at the
         *  outermost lexical rule.  The token object should point into the
         *  char buffer start..stop.  If there is a text override in 'text',
         *  use that to set the token's text.  Override this method to emit
         *  custom Token objects.
         *  </summary>
         *
         *  <remarks>
         *  If you are building trees, then you should also override
         *  Parser or TreeParser.getMissingSymbol().
         *  </remarks>
         */
        public virtual IToken Emit()
        {
            IToken t = new CommonToken(input, state.type, state.channel, state.tokenStartCharIndex, CharIndex - 1);
            t.Line = state.tokenStartLine;
            t.Text = state.text;
            t.CharPositionInLine = state.tokenStartCharPositionInLine;
            Emit(t);
            return t;
        }

        public virtual void Match(string s)
        {
            var i = 0;
            while (i < s.Length) {
                if (input.LA(1) != s[i]) {
                    if (state.backtracking > 0) {
                        state.failed = true;
                        return;
                    }
                    var mte = new MismatchedTokenException(s[i], input, TokenNames);
                    Recover(mte);
                    throw mte;
                }
                i++;
                input.Consume();
                state.failed = false;
            }
        }

        public virtual void MatchAny()
        {
            input.Consume();
        }

        public virtual void Match(int c)
        {
            if (input.LA(1) != c) {
                if (state.backtracking > 0) {
                    state.failed = true;
                    return;
                }
                var mte = new MismatchedTokenException(c, input, TokenNames);
                Recover(mte);  // don't really recover; just consume in lexer
                throw mte;
            }
            input.Consume();
            state.failed = false;
        }

        public virtual void MatchRange(int a, int b)
        {
            if (input.LA(1) < a || input.LA(1) > b) {
                if (state.backtracking > 0) {
                    state.failed = true;
                    return;
                }
                var mre = new MismatchedRangeException(a, b, input);
                Recover(mre);
                throw mre;
            }
            input.Consume();
            state.failed = false;
        }

        /** <summary>What is the index of the current character of lookahead?</summary> */
        public virtual int CharIndex {
            get {
                return input.Index;
            }
        }

        public override void ReportError(RecognitionException e)
        {
            /* TODO: not thought about recovery in lexer yet.
             *
             * // if we've already reported an error and have not matched a token
             * // yet successfully, don't report any errors.
             * if ( errorRecovery ) {
             *     //System.err.print("[SPURIOUS] ");
             *     return;
             * }
             * errorRecovery = true;
             */

            DisplayRecognitionError(this.TokenNames, e);
        }

        public override string GetErrorMessage(RecognitionException e, string[] tokenNames)
        {
            string msg = null;
            if (e is MismatchedTokenException) {
                var mte = (MismatchedTokenException) e;
                msg = "mismatched character " + GetCharErrorDisplay(e.Character) + " expecting " + GetCharErrorDisplay(mte.Expecting);
            }
            else if (e is NoViableAltException) {
                var nvae = (NoViableAltException) e;
                // for development, can add "decision=<<"+nvae.grammarDecisionDescription+">>"
                // and "(decision="+nvae.decisionNumber+") and
                // "state "+nvae.stateNumber
                msg = "no viable alternative at character " + GetCharErrorDisplay(e.Character);
            }
            else if (e is EarlyExitException) {
                var eee = (EarlyExitException) e;
                // for development, can add "(decision="+eee.decisionNumber+")"
                msg = "required (...)+ loop did not match anything at character " + GetCharErrorDisplay(e.Character);
            }
            else if (e is MismatchedNotSetException) {
                var mse = (MismatchedNotSetException) e;
                msg = "mismatched character " + GetCharErrorDisplay(e.Character) + " expecting set " + mse.Expecting;
            }
            else if (e is MismatchedSetException) {
                var mse = (MismatchedSetException) e;
                msg = "mismatched character " + GetCharErrorDisplay(e.Character) + " expecting set " + mse.Expecting;
            }
            else if (e is MismatchedRangeException) {
                var mre = (MismatchedRangeException) e;
                msg = "mismatched character " + GetCharErrorDisplay(e.Character) + " expecting set " +
                      GetCharErrorDisplay(mre.A) + ".." + GetCharErrorDisplay(mre.B);
            }
            else {
                msg = base.GetErrorMessage(e, tokenNames);
            }
            return msg;
        }

        public virtual string GetCharErrorDisplay(int c)
        {
            var s = ((char) c).ToString();
            switch (c) {
                case TokenTypes.EndOfFile:
                    s = "<EOF>";
                    break;
                case '\n':
                    s = "\\n";
                    break;
                case '\t':
                    s = "\\t";
                    break;
                case '\r':
                    s = "\\r";
                    break;
            }
            return "'" + s + "'";
        }

        /** <summary>
         *  Lexers can normally match any char in it's vocabulary after matching
         *  a token, so do the easy thing and just kill a character and hope
         *  it all works out.  You can instead use the rule invocation stack
         *  to do sophisticated error recovery if you are in a fragment rule.
         *  </summary>
         */
        public virtual void Recover(RecognitionException re)
        {
            //System.out.println("consuming char "+(char)input.LA(1)+" during recovery");
            //re.printStackTrace();
            input.Consume();
        }

        [Conditional("ANTLR_TRACE")]
        public virtual void TraceIn(string ruleName, int ruleIndex)
        {
            var inputSymbol = ((char) input.LT(1)) + " line=" + Line + ":" + CharPositionInLine;
            base.TraceIn(ruleName, ruleIndex, inputSymbol);
        }

        [Conditional("ANTLR_TRACE")]
        public virtual void TraceOut(string ruleName, int ruleIndex)
        {
            var inputSymbol = ((char) input.LT(1)) + " line=" + Line + ":" + CharPositionInLine;
            base.TraceOut(ruleName, ruleIndex, inputSymbol);
        }

        protected virtual void ParseNextToken()
        {
            mTokens();
        }
    }
}
