namespace Antlr.Runtime.Tree
{
    using Antlr.Runtime.Misc;

    /// <summary>Do a depth first walk of a tree, applying pre() and post() actions as we go.</summary>
    public class TreeVisitor
    {
        protected ITreeAdaptor adaptor;

        public TreeVisitor(ITreeAdaptor adaptor)
        {
            this.adaptor = adaptor;
        }
        public TreeVisitor()
            : this(new CommonTreeAdaptor())
        {
        }

        /// <summary>
        /// Visit every node in tree t and trigger an action for each node
        /// before/after having visited all of its children. Bottom up walk.
        /// Execute both actions even if t has no children. Ignore return
        /// results from transforming children since they will have altered
        /// the child list of this node (their parent). Return result of
        /// applying post action to this node.
        /// </summary>
        public object Visit(object t, ITreeVisitorAction action)
        {
            // System.out.println("visit "+((Tree)t).toStringTree());
            var isNil = adaptor.IsNil(t);
            if (action != null && !isNil) {
                t = action.Pre(t); // if rewritten, walk children of new t
            }
            for (var i = 0; i < adaptor.GetChildCount(t); i++) {
                var child = adaptor.GetChild(t, i);
                Visit(child, action);
            }
            if (action != null && !isNil)
                t = action.Post(t);
            return t;
        }

        public object Visit(object t, Func<object, object> preAction, Func<object, object> postAction)
        {
            return Visit(t, new TreeVisitorAction(preAction, postAction));
        }
    }
}
