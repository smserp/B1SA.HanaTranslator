namespace Antlr.Runtime
{
    using Regex = System.Text.RegularExpressions.Regex;

    [Serializable]
    public class CommonToken : IToken
    {
        private int type;
        private int line;
        private int charPositionInLine = -1; // set to invalid position
        private int channel = TokenChannels.Default;
        [NonSerialized]
        private ICharStream input;

        /** <summary>
         *  We need to be able to change the text once in a while.  If
         *  this is non-null, then getText should return this.  Note that
         *  start/stop are not affected by changing this.
         *  </summary>
          */
        private string text;

        /** <summary>What token number is this from 0..n-1 tokens; &lt; 0 implies invalid index</summary> */
        private int index = -1;

        /** <summary>The char position into the input buffer where this token starts</summary> */
        private int start;

        /** <summary>The char position into the input buffer where this token stops</summary> */
        private int stop;

        public CommonToken()
        {
        }

        public CommonToken(int type)
        {
            this.type = type;
        }

        public CommonToken(ICharStream input, int type, int channel, int start, int stop)
        {
            this.input = input;
            this.type = type;
            this.channel = channel;
            this.start = start;
            this.stop = stop;
        }

        public CommonToken(int type, string text)
        {
            this.type = type;
            this.channel = TokenChannels.Default;
            this.text = text;
        }

        public CommonToken(IToken oldToken)
        {
            text = oldToken.Text;
            type = oldToken.Type;
            line = oldToken.Line;
            index = oldToken.TokenIndex;
            charPositionInLine = oldToken.CharPositionInLine;
            channel = oldToken.Channel;
            input = oldToken.InputStream;
            if (oldToken is CommonToken) {
                start = ((CommonToken) oldToken).start;
                stop = ((CommonToken) oldToken).stop;
            }
        }

        #region IToken Members
        public string Text {
            get {
                if (text != null)
                    return text;
                if (input == null)
                    return null;

                if (start <= stop && stop < input.Count)
                    return input.Substring(start, stop - start + 1);

                return "<EOF>";
            }

            set {
                /* Override the text for this token.  getText() will return this text
                 *  rather than pulling from the buffer.  Note that this does not mean
                 *  that start/stop indexes are not valid.  It means that that input
                 *  was converted to a new string in the token object.
                 */
                text = value;
            }
        }

        public int Type {
            get {
                return type;
            }
            set {
                type = value;
            }
        }

        public int Line {
            get {
                return line;
            }
            set {
                line = value;
            }
        }

        public int CharPositionInLine {
            get {
                return charPositionInLine;
            }
            set {
                charPositionInLine = value;
            }
        }

        public int Channel {
            get {
                return channel;
            }
            set {
                channel = value;
            }
        }

        public int StartIndex {
            get {
                return start;
            }
            set {
                start = value;
            }
        }

        public int StopIndex {
            get {
                return stop;
            }
            set {
                stop = value;
            }
        }

        public int TokenIndex {
            get {
                return index;
            }
            set {
                index = value;
            }
        }

        public ICharStream InputStream {
            get {
                return input;
            }
            set {
                input = value;
            }
        }

        #endregion

        public override string ToString()
        {
            var channelStr = "";
            if (channel > 0) {
                channelStr = ",channel=" + channel;
            }
            var txt = Text;
            if (txt != null) {
                txt = Regex.Replace(txt, "\n", "\\\\n");
                txt = Regex.Replace(txt, "\r", "\\\\r");
                txt = Regex.Replace(txt, "\t", "\\\\t");
            }
            else {
                txt = "<no text>";
            }
            return "[@" + TokenIndex + "," + start + ":" + stop + "='" + txt + "',<" + type + ">" + channelStr + "," + line + ":" + CharPositionInLine + "]";
        }

        [System.Runtime.Serialization.OnSerializing]
        internal void OnSerializing(System.Runtime.Serialization.StreamingContext context)
        {
            if (text == null)
                text = Text;
        }
    }
}
