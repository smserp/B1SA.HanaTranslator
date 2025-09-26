using System.Text;
using Antlr.Runtime;

namespace B1SA.HanaTranslator
{
    /// <summary>
    /// Class to translate from T-SQL to HANA SQL
    /// </summary>
    public class Translator
    {
        public Config Configuration { get; private set; } = new Config();

        /// <summary>
        /// Uses default configuration
        /// </summary>
        public Translator()
        {
            Configuration = new Config();
        }

        public Translator(Config configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Main API function for 3rd party usage: Translates T-SQL into HANA SQL. Execution is costly, so plan for some caching...
        /// </summary>
        /// <param name="inputQuery">T-SQL</param>
        /// <param name="resultSummary">textual summary of the translation</param>
        /// <param name="numOfStatement">number of translated statements</param>
        /// <param name="numOfErrors">number of errors, if none translation was successful!</param>
        /// <returns>HANA SQL, if translation was possible</returns>
        public string Translate(string inputQuery, out string resultSummary, out int numOfStatement, out int numOfErrors)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb)) {
                var sbSummary = new StringBuilder();
                using (var writerSummary = new StringWriter(sbSummary)) {
                    StatusReporter.Initialize(writerSummary);
                    numOfStatement = TranslateInternal(writer, inputQuery, writerSummary);
                }
                resultSummary = sbSummary.ToString();
            }

            // calculating amount of errors
            var ns = ScanNotes(null, StatusReporter.OutputQueries);
            numOfErrors = ns.GetMsgCount(Note.STRINGIFIER) + ns.GetMsgCount(Note.ERR_MODIFIER);

            return sb.ToString();
        }

        /// <summary>
        /// Main and only function for conversion 
        /// </summary>
        /// <param name="writer">Output writer for translations, can be null</param>
        /// <param name="input"></param>
        /// <param name="infoWriter"></param>
        /// <returns>Returns number of processed input statements</returns>
        private int TranslateInternal(TextWriter writer, string input, TextWriter infoWriter)
        {
            var tokenizer = new Tokenizer(Configuration, input);
            input = tokenizer.TokenizeInputStatements();

            var numOfStatements = 1;

            if (tokenizer.OnlyTokenInput() == false) {
                var statements = ParseStatements(input);

                TranslateStatements(statements, infoWriter, out var translatedStatement);

                if (writer != null) {
                    PrintOutput(translatedStatement, writer, tokenizer);
                }
                PrintSummary(infoWriter);
            }
            else {
                if (writer != null) {
                    writer.Write(input);
                }
                PrintSummary(infoWriter);
            }

            return numOfStatements;
        }

        private static IList<Statement> ParseStatements(string text)
        {
            // The parser internally works in Unicode.
            var buffer = Encoding.Unicode.GetBytes(text);
            using (Stream stream = new MemoryStream(buffer)) {
                ANTLRInputStream input = new ANTLRLowerCaseInputStream(stream, Encoding.Unicode);

                Lexer lexer = new TransactSqlLexer(input);

                var tokens = new CommentedTokenStream(lexer);
                SetupCommentHandling(tokens);

                var parser = new TransactSqlParser(tokens);
                IList<Statement> statements;

                using (StringWriter errorWriter = new()) {
                    parser.TraceDestination = errorWriter;

                    statements = parser.sql();
                    PrintParseErrors(errorWriter.ToString());
                }

                return statements;
            }
        }

        private static void PrintParseErrors(string errors)
        {
            using (var errorReader = new StringReader(errors)) {
                while (true) {
                    var line = errorReader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    line = line.Trim();
                    if (line != String.Empty) {
                        Console.WriteLine(Resources.MSG_STATEMENT_NOT_SUPPORTED, line);
                    }
                }
            }
        }

        /// <summary>
        /// Translate input statements. Returns output statement.
        /// </summary>
        /// <param name="statements"></param>
        /// <param name="infoWriter"></param>
        /// <param name="translatedStatement"></param>
        private void TranslateStatements(IList<Statement> statements, TextWriter infoWriter, out Statement translatedStatement)
        {
            var md = new Modifier(Configuration);
            var rootStatement = new BlockStatement(statements);

            StatusReporter.SetStage(Resources.MSG_SCAN_INPUT_STATEMENTS, string.Empty);
            StatusReporter.SetInputQueries(rootStatement);
            StatusReporter.Message("       " + Resources.MSG_INPUT_STATEMENTS_FOUND + StatusReporter.InputQueriesCount);

            StatusReporter.SetStage(Resources.MSG_CONVERSION_STAGE, Resources.MSG_CONVERSION_STEP);

            md.Scan(rootStatement);
            Statement translated = md.Statement;

            StatusReporter.SetOutputQueries(translated, StatusReporter.GetCount());

            FixStatements(translated, infoWriter);
            translatedStatement = translated;

            StatusReporter.Message("\n\n" + Resources.MSG_DIFFERENT_STATEMENTS_COUNT);
            StatusReporter.Finish();
        }

        /// <summary>
        /// Call case fixing for given statements
        /// </summary>
        /// <param name="statement">Alreaedy translated statement that needs fixing</param>
        /// <param name="infoWriter">Stream for info/summary, may be null.</param>
        private void FixStatements(Statement statement, TextWriter infoWriter)
        {
            if (statement == null) { return; }

            StatusReporter.SetStage(Resources.MSG_CASEFIXING_STEP);

            if (statement is BlockStatement bs) {
                foreach (var stmt in bs.Statements) {
                    FixStatement(stmt, infoWriter);
                }
            }
            else {
                FixStatement(statement, infoWriter);
            }
        }

        /// <summary>
        /// Apply case fixing for given statement. Return new value for display warning
        /// </summary>
        /// <param name="statement">Statement for case fixing</param>
        /// <param name="infoWriter">Writter for info/summary stream</param>
        private void FixStatement(Statement statement, TextWriter infoWriter)
        {
            if (statement is not SqlStartStatement) {
                var fixer = new IdentifierFixer(Configuration);

                // scan & fix
                fixer.Scan(statement);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="translated"></param>
        /// <param name="writer"></param>
        /// <param name="tokenizer"></param>
        private void PrintOutput(Statement translated, TextWriter writer, Tokenizer tokenizer)
        {
            if (translated == null) {
                return;
            }

            string output;
            if (Configuration.FormatOutput) {
                var formatter = new Formatter(Configuration);
                formatter.Add(translated);
                output = formatter.Statement;
            }
            else {
                var stringifier = new Stringifier(Configuration);
                stringifier.Add(translated);
                output = stringifier.Statement;
            }

            writer.Write(tokenizer.DetokenizeInputStatements(output));
        }

        private void PrintSummary(TextWriter writer)
        {
            if (writer == null) {
                return;
            }

            var ns = ScanNotes(writer, StatusReporter.OutputQueries);

            var msgs = new Dictionary<string, string> {
                [Note.CASEFIXER] = Resources.MSG_CORRECTED_IDENTIFIERS,
                [Note.ERR_CASEFIXER] = Resources.MSG_COLUMNS_NOT_FOUND,
                [Note.STRINGIFIER] = Resources.MSG_SUMINFO_UNSUPPORTED_FEATURES,
                [Note.ERR_MODIFIER] = Resources.MSG_ERRORS_LIMITATIONS
            };

            writer.WriteLine("\n------------------------------------");
            ns.DisplaySummaryInfo(writer, msgs);
            //writer.WriteLine(Resources.MSG_NUM_OF_OK_QUERIES, numQueries - nokQueries);
            //writer.WriteLine(Resources.MSG_NUM_OF_NOK_QUERIES, nokQueries);
            writer.WriteLine(Resources.MSG_NUM_OF_QUERIES, StatusReporter.InputQueriesCount);
        }

        private NotesScanner ScanNotes(TextWriter writer, Statement statement)
        {
            var ns = new NotesScanner(writer);

            ns.SetFilter(Configuration.TranslationCommentsFilter.ToArray());
            ns.WriteNotesToWriter(false);
            ns.ClearSummaryInfo();

            ns.Scan(StatusReporter.OutputQueries);
            ns.SetFilter(Configuration.ResultSummaryFilter.ToArray());

            return ns;
        }

        internal class ANTLRLowerCaseInputStream : ANTLRInputStream
        {
            public ANTLRLowerCaseInputStream(Stream input, Encoding encoding) : base(input, encoding) { }

            public override int LA(int i)
            {
                var la = base.LA(i);
                return la <= 0 ? la : Convert.ToInt32(Char.ToLowerInvariant(Convert.ToChar(la)));
            }
        }

        // This is called by parser when consuming a Default channel token that has some preceding
        // Comments channel tokens. We will append the tokens to the most recently created GrammarNode.
        // Unfortunately, token may be consumed multiple times (because of predicates) and only the
        // last one is the "real" consumption.
        // So we have to remember where we have put each comment, and if we see the same comment again,
        // remove it from the previous node and put it to current one.
        private static Dictionary<int, GrammarNode> commentedNodes = [];

        private static void SetupCommentHandling(CommentedTokenStream tokens)
        {
            commentedNodes.Clear();
            tokens.PrecedingComments += HandlePrecedingComments;
        }

        private static void HandlePrecedingComments(object o, CommentedTokenStream.PrecedingCommentsEventArgs e)
        {
            var lastWasNewLine = false;
            foreach (var comment in e.Comments) {
                if (IsNewLineComment(comment)) {
                    // New line comments are not added to the tree, they are used just to mark
                    // that a comment starts on a new line.
                    lastWasNewLine = true;
                }
                else {
                    if (commentedNodes.TryGetValue(comment.TokenIndex, out var commentedNode)) {
                        // The comment was already used, re-attach it.
                        commentedNode.RemoveComment(comment);
                    }

                    commentedNodes[comment.TokenIndex] = GrammarNode.LastGrammarNode;
                    GrammarNode.LastGrammarNode.AppendComment(lastWasNewLine, comment);

                    lastWasNewLine = false;
                }
            }
        }

        private static bool IsNewLineComment(IToken token)
        {
            return token.Text == "\n" || token.Text == "\r";
        }
    }

}
