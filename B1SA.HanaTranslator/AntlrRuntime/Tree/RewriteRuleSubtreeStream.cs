namespace Antlr.Runtime.Tree
{
    using IList = System.Collections.IList;

    [Serializable]
    public class RewriteRuleSubtreeStream : RewriteRuleElementStream
    {
        public RewriteRuleSubtreeStream(ITreeAdaptor adaptor, string elementDescription)
            : base(adaptor, elementDescription)
        {
        }

        /// <summary>Create a stream with one element</summary>
        public RewriteRuleSubtreeStream(ITreeAdaptor adaptor, string elementDescription, object oneElement)
            : base(adaptor, elementDescription, oneElement)
        {
        }

        /// <summary>Create a stream, but feed off an existing list</summary>
        public RewriteRuleSubtreeStream(ITreeAdaptor adaptor, string elementDescription, IList elements)
            : base(adaptor, elementDescription, elements)
        {
        }

        /// <summary>
        /// Treat next element as a single node even if it's a subtree.
        /// This is used instead of next() when the result has to be a
        /// tree root node. Also prevents us from duplicating recently-added
        /// children; e.g., ^(type ID)+ adds ID to type and then 2nd iteration
        /// must dup the type node, but ID has been added.
        /// </summary>
        /// <remarks>
        /// Referencing a rule result twice is ok; dup entire tree as
        /// we can't be adding trees as root; e.g., expr expr.
        ///
        /// Hideous code duplication here with super.next(). Can't think of
        /// a proper way to refactor. This needs to always call dup node
        /// and super.next() doesn't know which to call: dup node or dup tree.
        /// </remarks>
        public virtual object NextNode()
        {
            //System.Console.WriteLine("nextNode: elements={0}, singleElement={1}", elements, ((ITree)singleElement).ToStringTree());
            var n = Count;
            if (dirty || (cursor >= n && n == 1)) {
                // if out of elements and size is 1, dup (at most a single node
                // since this is for making root nodes).
                var el = NextCore();
                return adaptor.DupNode(el);
            }
            // test size above then fetch
            var tree = NextCore();
            while (adaptor.IsNil(tree) && adaptor.GetChildCount(tree) == 1)
                tree = adaptor.GetChild(tree, 0);
            //System.Console.WriteLine("_next={0}", ((ITree)tree).ToStringTree());
            var el2 = adaptor.DupNode(tree); // dup just the root (want node here)
            return el2;
        }

        protected override object Dup(object el)
        {
            return adaptor.DupTree(el);
        }
    }
}
