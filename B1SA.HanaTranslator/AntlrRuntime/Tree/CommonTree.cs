namespace Antlr.Runtime.Tree
{
    using ArgumentNullException = ArgumentNullException;

    /** <summary>
     *  A tree node that is wrapper for a Token object.  After 3.0 release
     *  while building tree rewrite stuff, it became clear that computing
     *  parent and child index is very difficult and cumbersome.  Better to
     *  spend the space in every tree node.  If you don't want these extra
     *  fields, it's easy to cut them out in your own BaseTree subclass.
     *  </summary>
     */
    [Serializable]
    public class CommonTree : BaseTree
    {
        /** <summary>A single token is the payload</summary> */
        private IToken _token;

        /** <summary>
         *  What token indexes bracket all tokens associated with this node
         *  and below?
         *  </summary>
         */
        protected int startIndex = -1;
        protected int stopIndex = -1;

        /** <summary>Who is the parent node of this node; if null, implies node is root</summary> */
        private CommonTree parent;

        /** <summary>What index is this node in the child list? Range: 0..n-1</summary> */
        private int childIndex = -1;

        public CommonTree()
        {
        }

        public CommonTree(CommonTree node)
            : base(node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            this.Token = node.Token;
            this.startIndex = node.startIndex;
            this.stopIndex = node.stopIndex;
        }

        public CommonTree(IToken t)
        {
            this.Token = t;
        }

        #region Properties

        public override int CharPositionInLine {
            get {
                if (Token == null || Token.CharPositionInLine == -1) {
                    if (ChildCount > 0)
                        return Children[0].CharPositionInLine;

                    return 0;
                }
                return Token.CharPositionInLine;
            }

            set {
                base.CharPositionInLine = value;
            }
        }

        public override int ChildIndex {
            get {
                return childIndex;
            }

            set {
                childIndex = value;
            }
        }

        public override bool IsNil {
            get {
                return Token == null;
            }
        }

        public override int Line {
            get {
                if (Token == null || Token.Line == 0) {
                    if (ChildCount > 0)
                        return Children[0].Line;

                    return 0;
                }

                return Token.Line;
            }

            set {
                base.Line = value;
            }
        }

        public override ITree Parent {
            get {
                return parent;
            }

            set {
                parent = (CommonTree) value;
            }
        }

        public override string Text {
            get {
                if (Token == null)
                    return null;

                return Token.Text;
            }

            set {
            }
        }

        public IToken Token {
            get {
                return _token;
            }

            set {
                _token = value;
            }
        }

        public override int TokenStartIndex {
            get {
                if (startIndex == -1 && Token != null)
                    return Token.TokenIndex;

                return startIndex;
            }

            set {
                startIndex = value;
            }
        }

        public override int TokenStopIndex {
            get {
                if (stopIndex == -1 && Token != null) {
                    return Token.TokenIndex;
                }
                return stopIndex;
            }

            set {
                stopIndex = value;
            }
        }

        public override int Type {
            get {
                if (Token == null)
                    return TokenTypes.Invalid;

                return Token.Type;
            }

            set {
            }
        }

        #endregion

        public override ITree DupNode()
        {
            return new CommonTree(this);
        }

        /** <summary>
         *  For every node in this subtree, make sure it's start/stop token's
         *  are set.  Walk depth first, visit bottom up.  Only updates nodes
         *  with at least one token index &lt; 0.
         *  </summary>
         */
        public virtual void SetUnknownTokenBoundaries()
        {
            if (Children == null) {
                if (startIndex < 0 || stopIndex < 0)
                    startIndex = stopIndex = Token.TokenIndex;

                return;
            }

            foreach (var childTree in Children) {
                var commonTree = childTree as CommonTree;
                if (commonTree == null)
                    continue;

                commonTree.SetUnknownTokenBoundaries();
            }

            if (startIndex >= 0 && stopIndex >= 0)
                return; // already set

            if (Children.Count > 0) {
                var firstChild = Children[0];
                var lastChild = Children[Children.Count - 1];
                startIndex = firstChild.TokenStartIndex;
                stopIndex = lastChild.TokenStopIndex;
            }
        }

        public override string ToString()
        {
            if (IsNil)
                return "nil";

            if (Type == TokenTypes.Invalid)
                return "<errornode>";

            if (Token == null)
                return string.Empty;

            return Token.Text;
        }
    }
}
