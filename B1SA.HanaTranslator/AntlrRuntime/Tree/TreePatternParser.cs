namespace Antlr.Runtime.Tree
{
    using InvalidOperationException = InvalidOperationException;

    public class TreePatternParser
    {
        protected TreePatternLexer tokenizer;
        protected int ttype;
        protected TreeWizard wizard;
        protected ITreeAdaptor adaptor;

        public TreePatternParser(TreePatternLexer tokenizer, TreeWizard wizard, ITreeAdaptor adaptor)
        {
            this.tokenizer = tokenizer;
            this.wizard = wizard;
            this.adaptor = adaptor;
            ttype = tokenizer.NextToken(); // kickstart
        }

        public virtual object Pattern()
        {
            if (ttype == TreePatternLexer.Begin) {
                return ParseTree();
            }
            else if (ttype == TreePatternLexer.Id) {
                var node = ParseNode();
                if (ttype == CharStreamConstants.EndOfFile) {
                    return node;
                }
                return null; // extra junk on end
            }
            return null;
        }

        public virtual object ParseTree()
        {
            if (ttype != TreePatternLexer.Begin)
                throw new InvalidOperationException("No beginning.");

            ttype = tokenizer.NextToken();
            var root = ParseNode();
            if (root == null) {
                return null;
            }
            while (ttype == TreePatternLexer.Begin ||
                    ttype == TreePatternLexer.Id ||
                    ttype == TreePatternLexer.Percent ||
                    ttype == TreePatternLexer.Dot) {
                if (ttype == TreePatternLexer.Begin) {
                    var subtree = ParseTree();
                    adaptor.AddChild(root, subtree);
                }
                else {
                    var child = ParseNode();
                    if (child == null) {
                        return null;
                    }
                    adaptor.AddChild(root, child);
                }
            }

            if (ttype != TreePatternLexer.End)
                throw new InvalidOperationException("No end.");

            ttype = tokenizer.NextToken();
            return root;
        }

        public virtual object ParseNode()
        {
            // "%label:" prefix
            string label = null;
            if (ttype == TreePatternLexer.Percent) {
                ttype = tokenizer.NextToken();
                if (ttype != TreePatternLexer.Id) {
                    return null;
                }
                label = tokenizer.sval.ToString();
                ttype = tokenizer.NextToken();
                if (ttype != TreePatternLexer.Colon) {
                    return null;
                }
                ttype = tokenizer.NextToken(); // move to ID following colon
            }

            // Wildcard?
            if (ttype == TreePatternLexer.Dot) {
                ttype = tokenizer.NextToken();
                IToken wildcardPayload = new CommonToken(0, ".");
                TreeWizard.TreePattern node =
                    new TreeWizard.WildcardTreePattern(wildcardPayload);
                if (label != null) {
                    node.label = label;
                }
                return node;
            }

            // "ID" or "ID[arg]"
            if (ttype != TreePatternLexer.Id) {
                return null;
            }
            var tokenName = tokenizer.sval.ToString();
            ttype = tokenizer.NextToken();
            if (tokenName.Equals("nil")) {
                return adaptor.Nil();
            }
            var text = tokenName;
            // check for arg
            string arg = null;
            if (ttype == TreePatternLexer.Arg) {
                arg = tokenizer.sval.ToString();
                text = arg;
                ttype = tokenizer.NextToken();
            }

            // create node
            var treeNodeType = wizard.GetTokenType(tokenName);
            if (treeNodeType == TokenTypes.Invalid) {
                return null;
            }
            object node2;
            node2 = adaptor.Create(treeNodeType, text);
            if (label != null && node2.GetType() == typeof(TreeWizard.TreePattern)) {
                ((TreeWizard.TreePattern) node2).label = label;
            }
            if (arg != null && node2.GetType() == typeof(TreeWizard.TreePattern)) {
                ((TreeWizard.TreePattern) node2).hasTextArg = true;
            }
            return node2;
        }
    }
}
