namespace Antlr.Runtime.Tree
{
    using System.Diagnostics;

    internal sealed class AntlrRuntime_BaseTreeDebugView
    {
        private readonly BaseTree _tree;

        public AntlrRuntime_BaseTreeDebugView(BaseTree tree)
        {
            _tree = tree;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ITree[] Children {
            get {
                if (_tree == null || _tree.Children == null)
                    return null;

                var children = new ITree[_tree.Children.Count];
                _tree.Children.CopyTo(children, 0);
                return children;
            }
        }
    }
}
