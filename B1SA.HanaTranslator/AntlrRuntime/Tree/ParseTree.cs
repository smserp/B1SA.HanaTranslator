namespace Antlr.Runtime.Tree
{
    using System.Collections.Generic;

    using StringBuilder = System.Text.StringBuilder;

    /// <summary>
    /// A record of the rules used to match a token sequence. The tokens
    /// end up as the leaves of this tree and rule nodes are the interior nodes.
    /// This really adds no functionality, it is just an alias for CommonTree
    /// that is more meaningful (specific) and holds a String to display for a node.
    /// </summary>
    [Serializable]
    public class ParseTree : BaseTree
    {
        public object payload;
        public List<IToken> hiddenTokens;

        public ParseTree(object label)
        {
            this.payload = label;
        }

        #region Properties
        public override string Text {
            get {
                return ToString();
            }
            set {
            }
        }
        public override int TokenStartIndex {
            get {
                return 0;
            }
            set {
            }
        }
        public override int TokenStopIndex {
            get {
                return 0;
            }
            set {
            }
        }
        public override int Type {
            get {
                return 0;
            }
            set {
            }
        }
        #endregion

        public override ITree DupNode()
        {
            return null;
        }

        public override string ToString()
        {
            if (payload is IToken) {
                var t = (IToken) payload;
                if (t.Type == TokenTypes.EndOfFile) {
                    return "<EOF>";
                }
                return t.Text;
            }
            return payload.ToString();
        }

        /// <summary>
        /// Emit a token and all hidden nodes before. EOF node holds all
        /// hidden tokens after last real token.
        /// </summary>
        public virtual string ToStringWithHiddenTokens()
        {
            var buf = new StringBuilder();
            if (hiddenTokens != null) {
                for (var i = 0; i < hiddenTokens.Count; i++) {
                    var hidden = (IToken) hiddenTokens[i];
                    buf.Append(hidden.Text);
                }
            }
            var nodeText = this.ToString();
            if (!nodeText.Equals("<EOF>"))
                buf.Append(nodeText);
            return buf.ToString();
        }

        /// <summary>
        /// Print out the leaves of this tree, which means printing original
        /// input back out.
        /// </summary>
        public virtual string ToInputString()
        {
            var buf = new StringBuilder();
            ToStringLeaves(buf);
            return buf.ToString();
        }

        protected virtual void ToStringLeaves(StringBuilder buf)
        {
            if (payload is IToken) { // leaf node token?
                buf.Append(this.ToStringWithHiddenTokens());
                return;
            }
            for (var i = 0; Children != null && i < Children.Count; i++) {
                var t = (ParseTree) Children[i];
                t.ToStringLeaves(buf);
            }
        }
    }
}
