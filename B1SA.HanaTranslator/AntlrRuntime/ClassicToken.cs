namespace Antlr.Runtime
{
    /** <summary>
     *  A Token object like we'd use in ANTLR 2.x; has an actual string created
     *  and associated with this object.  These objects are needed for imaginary
     *  tree nodes that have payload objects.  We need to create a Token object
     *  that has a string; the tree node will point at this token.  CommonToken
     *  has indexes into a char stream and hence cannot be used to introduce
     *  new strings.
     *  </summary>
     */
    [Serializable]
    public class ClassicToken : IToken
    {
        private string text;
        private int type;
        private int line;
        private int charPositionInLine;
        private int channel = TokenChannels.Default;

        /** <summary>What token number is this from 0..n-1 tokens</summary> */
        private int index;

        public ClassicToken(int type)
        {
            this.type = type;
        }

        public ClassicToken(IToken oldToken)
        {
            text = oldToken.Text;
            type = oldToken.Type;
            line = oldToken.Line;
            charPositionInLine = oldToken.CharPositionInLine;
            channel = oldToken.Channel;
        }

        public ClassicToken(int type, string text)
        {
            this.type = type;
            this.text = text;
        }

        public ClassicToken(int type, string text, int channel)
        {
            this.type = type;
            this.text = text;
            this.channel = channel;
        }

        #region IToken Members
        public string Text {
            get {
                return text;
            }
            set {
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
                return -1;
            }
            set {
            }
        }

        public int StopIndex {
            get {
                return -1;
            }
            set {
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
                return null;
            }
            set {
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
                txt = txt.Replace("\n", "\\\\n");
                txt = txt.Replace("\r", "\\\\r");
                txt = txt.Replace("\t", "\\\\t");
            }
            else {
                txt = "<no text>";
            }
            return "[@" + TokenIndex + ",'" + txt + "',<" + type + ">" + channelStr + "," + line + ":" + CharPositionInLine + "]";
        }
    }
}
