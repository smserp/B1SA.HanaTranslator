using System.Globalization;

namespace B1SA.HanaTranslator
{
    public class Formatter : Assembler
    {
        private const int MAX_LINE_LENGTH = 120;
        private const string INDENTATION = "    ";
        protected Config config;

        private string statement;
        public string Statement {
            set {
                return;
            }
            get {
                return statement;
            }
        }

        private int indentationLevel;
        private int lastBreakable;

        private void Append(string toAppend)
        {
            if (statement.Length - statement.LastIndexOf(Environment.NewLine) + toAppend.Length > MAX_LINE_LENGTH) {
                if (lastBreakable > 0) {
                    var tmp = Environment.NewLine;
                    for (var i = 0; i < indentationLevel; i++) {
                        tmp += INDENTATION;
                    }
                    statement = statement.Insert(lastBreakable, tmp);
                    lastBreakable = 0;
                }
                else {
                    NewLine();
                }
            }
            statement += toAppend;
        }

        private string GetIndentationString()
        {
            var ret = string.Empty;
            for (var i = 0; i < indentationLevel; i++) {
                ret += INDENTATION;
            }
            return ret;
        }

        override public void NewLine()
        {
            if (statement.Length != 0 && !statement.TrimEnd(' ').EndsWith(Environment.NewLine)) {
                statement += Environment.NewLine;
                statement += GetIndentationString();
            }
        }

        override public void IncreaseIndentation()
        {
            indentationLevel++;
        }
        override public void DecreaseIndentation()
        {
            indentationLevel--;
        }

        override public void Breakable()
        {
            lastBreakable = statement.Length;
        }


        override public void Clear()
        {
            statement = string.Empty;
        }

        public Formatter(Config configuration)
        {
            config = configuration;

            statement = string.Empty;
            indentationLevel = 0;
            lastBreakable = 0;
        }

        //space
        override public void AddSpace()
        {
            Append(" ");
        }
        override public void AddToken(string right)
        {
            Append(right);
        }

        // basic types
        override public void Add(string right)
        {
            Append(right);
        }
        override public void Add(decimal right)
        {
            Append(right.ToString(CultureInfo.InvariantCulture));
        }
        override public void Add(int right)
        {
            Append(right.ToString());
        }

        private void AddNotes(string notes)
        {
            if (notes.Length != 0) {
                notes = Environment.NewLine + notes;
                if (notes.EndsWith(Environment.NewLine)) {
                    notes = notes.TrimEnd();
                }
                var newStr = Environment.NewLine + GetIndentationString();
                notes = notes.Replace(Environment.NewLine, newStr);
                statement += notes;
            }
        }

        override public void Add(Statement stmt)
        {
            var notes = string.Empty;
            stmt.Assembly(this);
            if (stmt.Terminate) {
                if (!stmt.Hide)
                    AddToken(";");
                // setting configuration the latest possible (easier fix, not easier to maintain...)
                stmt.Configuration = config;
                notes = stmt.ReturnNotes();
                AddNotes(notes);
            }

            if (notes.Length > 0 && stmt.Comments.Count > 0) {
                if (!stmt.Comments[0].NewLine) {
                    NewLine();
                }
            }

            HandleComments(stmt);
        }

        override public void Add(GrammarNode node)
        {
            node.Assembly(this);
            HandleComments(node);
        }

        override public void HandleComments(GrammarNode node)
        {
            foreach (var comment in node.Comments) {
                var isMultiline = false;
                if (comment.NewLine) {
                    NewLine();
                }
                if (comment.Type == CommentType.SingleLine) {
                    Append("--");
                }
                else {
                    Append("/*");
                    isMultiline = true;
                }
                Append(comment.Text);
                if (isMultiline) {
                    Append("*/");
                }
            }
        }
    }
}
