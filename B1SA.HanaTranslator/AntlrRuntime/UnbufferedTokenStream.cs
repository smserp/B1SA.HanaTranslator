namespace Antlr.Runtime
{
    using Antlr.Runtime.Misc;

    /// <summary>
    /// A token stream that pulls tokens from the code source on-demand and
    /// without tracking a complete buffer of the tokens. This stream buffers
    /// the minimum number of tokens possible. It's the same as
    /// OnDemandTokenStream except that OnDemandTokenStream buffers all tokens.
    ///
    /// You can't use this stream if you pass whitespace or other off-channel
    /// tokens to the parser. The stream can't ignore off-channel tokens.
    /// 
    /// You can only look backwards 1 token: LT(-1).
    ///
    /// Use this when you need to read from a socket or other infinite stream.
    /// </summary>
    /// <seealso cref="BufferedTokenStream"/>
    /// <seealso cref="CommonTokenStream"/>
    public class UnbufferedTokenStream : LookaheadStream<IToken>, ITokenStream, ITokenStreamInformation
    {
        protected ITokenSource tokenSource;
        protected int tokenIndex; // simple counter to set token index in tokens

        /// <summary>Skip tokens on any channel but this one; this is how we skip whitespace...</summary>
        protected int channel = TokenChannels.Default;

        private readonly ListStack<IToken> _realTokens = [null];

        public UnbufferedTokenStream(ITokenSource tokenSource)
        {
            this.tokenSource = tokenSource;
        }

        public ITokenSource TokenSource {
            get {
                return this.tokenSource;
            }
        }

        public string SourceName {
            get {
                return TokenSource.SourceName;
            }
        }

        #region ITokenStreamInformation Members

        public IToken LastToken {
            get {
                return LB(1);
            }
        }

        public IToken LastRealToken {
            get {
                return _realTokens.Peek();
            }
        }

        public int MaxLookBehind {
            get {
                return 1;
            }
        }

        public override int Mark()
        {
            _realTokens.Push(_realTokens.Peek());
            return base.Mark();
        }

        public override void Release(int marker)
        {
            base.Release(marker);
            _realTokens.Pop();
        }

        public override void Clear()
        {
            _realTokens.Clear();
            _realTokens.Push(null);
        }

        public override void Consume()
        {
            base.Consume();
            if (PreviousElement != null && PreviousElement.Line > 0)
                _realTokens[_realTokens.Count - 1] = PreviousElement;
        }

        #endregion

        public override IToken NextElement()
        {
            var t = this.tokenSource.NextToken();
            t.TokenIndex = this.tokenIndex++;
            return t;
        }

        public override bool IsEndOfFile(IToken o)
        {
            return o.Type == CharStreamConstants.EndOfFile;
        }

        public IToken Get(int i)
        {
            throw new NotSupportedException("Absolute token indexes are meaningless in an unbuffered stream");
        }

        public int LA(int i)
        {
            return LT(i).Type;
        }

        public string ToString(int start, int stop)
        {
            return "n/a";
        }

        public string ToString(IToken start, IToken stop)
        {
            return "n/a";
        }
    }
}
