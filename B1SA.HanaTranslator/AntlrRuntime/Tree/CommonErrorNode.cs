namespace Antlr.Runtime.Tree
{

    /** <summary>A node representing erroneous token range in token stream</summary> */
    [Serializable]
    public class CommonErrorNode : CommonTree
    {
        public IIntStream input;
        public IToken start;
        public IToken stop;
        public RecognitionException trappedException;

        public CommonErrorNode(ITokenStream input, IToken start, IToken stop,
                               RecognitionException e)
        {
            //System.out.println("start: "+start+", stop: "+stop);
            if (stop == null ||
                 (stop.TokenIndex < start.TokenIndex &&
                  stop.Type != TokenTypes.EndOfFile)) {
                // sometimes resync does not consume a token (when LT(1) is
                // in follow set.  So, stop will be 1 to left to start. adjust.
                // Also handle case where start is the first token and no token
                // is consumed during recovery; LT(-1) will return null.
                stop = start;
            }
            this.input = input;
            this.start = start;
            this.stop = stop;
            this.trappedException = e;
        }

        #region Properties
        public override bool IsNil {
            get {
                return false;
            }
        }
        public override string Text {
            get {
                string badText = null;
                if (start is IToken) {
                    var i = ((IToken) start).TokenIndex;
                    var j = ((IToken) stop).TokenIndex;
                    if (((IToken) stop).Type == TokenTypes.EndOfFile) {
                        j = ((ITokenStream) input).Count;
                    }
                    badText = ((ITokenStream) input).ToString(i, j);
                }
                else if (start is ITree) {
                    badText = ((ITreeNodeStream) input).ToString(start, stop);
                }
                else {
                    // people should subclass if they alter the tree type so this
                    // next one is for sure correct.
                    badText = "<unknown>";
                }
                return badText;
            }
            set {
            }
        }
        public override int Type {
            get {
                return TokenTypes.Invalid;
            }
            set {
            }
        }
        #endregion

        public override string ToString()
        {
            if (trappedException is MissingTokenException) {
                return "<missing type: " +
                       ((MissingTokenException) trappedException).MissingType +
                       ">";
            }
            else if (trappedException is UnwantedTokenException) {
                return "<extraneous: " +
                       ((UnwantedTokenException) trappedException).UnexpectedToken +
                       ", resync=" + Text + ">";
            }
            else if (trappedException is MismatchedTokenException) {
                return "<mismatched token: " + trappedException.Token + ", resync=" + Text + ">";
            }
            else if (trappedException is NoViableAltException) {
                return "<unexpected: " + trappedException.Token +
                       ", resync=" + Text + ">";
            }
            return "<error: " + Text + ">";
        }
    }
}
