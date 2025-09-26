namespace Antlr.Runtime.Tree
{
    using StringBuilder = System.Text.StringBuilder;

    public class TreePatternLexer
    {
        public const int Begin = 1;
        public const int End = 2;
        public const int Id = 3;
        public const int Arg = 4;
        public const int Percent = 5;
        public const int Colon = 6;
        public const int Dot = 7;

        /// <summary>
        /// The tree pattern to lex like "(A B C)"
        /// </summary>
        protected string pattern;

        /// <summary>
        /// Index into input string
        /// </summary>
        protected int p = -1;

        /// <summary>
        /// Current char
        /// </summary>
        protected int c;

        /// <summary>
        /// How long is the pattern in char?
        /// </summary>
        protected int n;

        /// <summary>
        /// Set when token type is ID or ARG (name mimics Java's StreamTokenizer)
        /// </summary>
        public StringBuilder sval = new StringBuilder();

        public bool error = false;

        public TreePatternLexer(string pattern)
        {
            this.pattern = pattern;
            this.n = pattern.Length;
            Consume();
        }

        public virtual int NextToken()
        {
            sval.Length = 0; // reset, but reuse buffer
            while (c != CharStreamConstants.EndOfFile) {
                if (c == ' ' || c == '\n' || c == '\r' || c == '\t') {
                    Consume();
                    continue;
                }
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_') {
                    sval.Append((char) c);
                    Consume();
                    while ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                            (c >= '0' && c <= '9') || c == '_') {
                        sval.Append((char) c);
                        Consume();
                    }
                    return Id;
                }
                if (c == '(') {
                    Consume();
                    return Begin;
                }
                if (c == ')') {
                    Consume();
                    return End;
                }
                if (c == '%') {
                    Consume();
                    return Percent;
                }
                if (c == ':') {
                    Consume();
                    return Colon;
                }
                if (c == '.') {
                    Consume();
                    return Dot;
                }
                if (c == '[') {
                    // grab [x] as a string, returning x
                    Consume();
                    while (c != ']') {
                        if (c == '\\') {
                            Consume();
                            if (c != ']') {
                                sval.Append('\\');
                            }
                            sval.Append((char) c);
                        }
                        else {
                            sval.Append((char) c);
                        }
                        Consume();
                    }
                    Consume();
                    return Arg;
                }
                Consume();
                error = true;
                return CharStreamConstants.EndOfFile;
            }
            return CharStreamConstants.EndOfFile;
        }

        protected virtual void Consume()
        {
            p++;
            if (p >= n) {
                c = CharStreamConstants.EndOfFile;
            }
            else {
                c = pattern[p];
            }
        }
    }
}
