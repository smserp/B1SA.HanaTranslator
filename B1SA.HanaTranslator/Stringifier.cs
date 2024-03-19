namespace B1SA.HanaTranslator
{
    public class Stringifier : Assembler
    {
        private static Stringifier stringifier = null;
        protected Config config;
        public string Statement;

        public static object GetProcessed(Config configuration, GrammarNode node)
        {
            if (stringifier == null) {
                stringifier = new Stringifier(configuration);
            }

            stringifier.Statement = "";
            node.Assembly(stringifier);
            return stringifier.Statement;
        }

        public override void Clear()
        {
            Statement = string.Empty;
        }

        public Stringifier(Config configuration)
        {
            config = configuration;
            Statement = string.Empty;
        }

        //space
        public override void AddSpace()
        {
            Statement += " ";
        }

        // basic types
        public override void Add(string right)
        {
            Statement += right;
        }
        public override void Add(decimal right)
        {
            Statement += right;
        }
        public override void Add(int right)
        {
            Statement += right;
        }
        public override void AddToken(string right)
        {
            Statement += right;
        }

        public override void Add(Statement stmt)
        {
            stmt.Assembly(this);
            if (stmt.Terminate) {
                if (!stmt.Hide) {
                    Statement += ";" + Environment.NewLine;
                }
                // setting configuration the latest possible (easier fix, not easier to maintain...)
                stmt.Configuration = config;
                Statement += stmt.ReturnNotes();
            }
            HandleComments(stmt);
        }

        public override void Add(GrammarNode node)
        {
            node.Assembly(this);
        }

        public override void HandleComments(GrammarNode node)
        {
            foreach (Comment comment in node.Comments) {
                bool isMultiline = false;
                if (comment.NewLine && !Statement.EndsWith(Environment.NewLine)) {
                    Statement += Environment.NewLine;
                }
                if (comment.Type == CommentType.SingleLine) {
                    Statement += "--";
                }
                else {
                    Statement += "/*";
                    isMultiline = true;
                }
                Add(comment.Text);
                if (isMultiline) {
                    Statement += "*/" + Environment.NewLine;
                }
                else {
                    Statement += Environment.NewLine;
                }
            }
        }
    }
}
