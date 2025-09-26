namespace Antlr.Runtime
{
    /// <summary>
    /// The most common stream of tokens is one where every token is buffered up
    /// and tokens are prefiltered for a certain channel (the parser will only
    /// see these tokens and cannot change the filter channel number during the
    /// parse).
    /// </summary>
    /// <remarks>TODO: how to access the full token stream? How to track all tokens matched per rule?</remarks>
    [Serializable]
    public class CommonTokenStream : BufferedTokenStream
    {
        /// <summary>Skip tokens on any channel but this one; this is how we skip whitespace...</summary>
        private int _channel;

        public CommonTokenStream()
        {
        }

        public CommonTokenStream(ITokenSource tokenSource)
            : this(tokenSource, TokenChannels.Default)
        {
        }

        public CommonTokenStream(ITokenSource tokenSource, int channel)
            : base(tokenSource)
        {
            this._channel = channel;
        }

        public int Channel {
            get {
                return _channel;
            }
        }

        /// <summary>Reset this token stream by setting its token source.</summary>
        public override ITokenSource TokenSource {
            get {
                return base.TokenSource;
            }
            set {
                base.TokenSource = value;
                _channel = TokenChannels.Default;
            }
        }

        /// <summary>Always leave p on an on-channel token.</summary>
        public override void Consume()
        {
            if (_p == -1)
                Setup();
            _p++;
            _p = SkipOffTokenChannels(_p);
        }

        protected override IToken LB(int k)
        {
            if (k == 0 || (_p - k) < 0)
                return null;

            var i = _p;
            var n = 1;
            // find k good tokens looking backwards
            while (n <= k) {
                // skip off-channel tokens
                i = SkipOffTokenChannelsReverse(i - 1);
                n++;
            }
            if (i < 0)
                return null;
            return _tokens[i];
        }

        public override IToken LT(int k)
        {
            if (_p == -1)
                Setup();
            if (k == 0)
                return null;
            if (k < 0)
                return LB(-k);
            var i = _p;
            var n = 1; // we know tokens[p] is a good one
            // find k good tokens
            while (n < k) {
                // skip off-channel tokens
                i = SkipOffTokenChannels(i + 1);
                n++;
            }

            if (i > Range)
                Range = i;

            return _tokens[i];
        }

        /// <summary>
        /// Given a starting index, return the index of the first on-channel
        /// token.
        /// </summary>
        protected virtual int SkipOffTokenChannels(int i)
        {
            Sync(i);
            while (_tokens[i].Channel != _channel) {
                // also stops at EOF (it's on channel)
                i++;
                Sync(i);
            }
            return i;
        }

        protected virtual int SkipOffTokenChannelsReverse(int i)
        {
            while (i >= 0 && ((IToken) _tokens[i]).Channel != _channel) {
                i--;
            }

            return i;
        }

        public override void Reset()
        {
            base.Reset();
            _p = SkipOffTokenChannels(0);
        }

        protected override void Setup()
        {
            _p = 0;
            _p = SkipOffTokenChannels(_p);
        }
    }
}
