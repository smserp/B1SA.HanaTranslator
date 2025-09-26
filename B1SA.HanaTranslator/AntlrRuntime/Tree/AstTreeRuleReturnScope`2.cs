namespace Antlr.Runtime.Tree
{
    public class AstTreeRuleReturnScope<TOutputTree, TInputTree> : TreeRuleReturnScope<TInputTree>, IAstRuleReturnScope<TOutputTree>, IAstRuleReturnScope
    {
        private TOutputTree _tree;

        public TOutputTree Tree {
            get {
                return _tree;
            }

            set {
                _tree = value;
            }
        }

        object IAstRuleReturnScope.Tree {
            get {
                return Tree;
            }
        }
    }
}
