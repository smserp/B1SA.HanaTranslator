namespace Antlr.Runtime.Tree
{
    using IList = System.Collections.IList;
    using NotSupportedException = NotSupportedException;

    /** <summary>
     *  Queues up nodes matched on left side of -> in a tree parser. This is
     *  the analog of RewriteRuleTokenStream for normal parsers.
     *  </summary>
     */
    [Serializable]
    public class RewriteRuleNodeStream : RewriteRuleElementStream
    {

        public RewriteRuleNodeStream(ITreeAdaptor adaptor, string elementDescription)
            : base(adaptor, elementDescription)
        {
        }

        /** <summary>Create a stream with one element</summary> */
        public RewriteRuleNodeStream(ITreeAdaptor adaptor, string elementDescription, object oneElement)
            : base(adaptor, elementDescription, oneElement)
        {
        }

        /** <summary>Create a stream, but feed off an existing list</summary> */
        public RewriteRuleNodeStream(ITreeAdaptor adaptor, string elementDescription, IList elements)
            : base(adaptor, elementDescription, elements)
        {
        }

        public virtual object NextNode()
        {
            return NextCore();
        }

        protected override object ToTree(object el)
        {
            return adaptor.DupNode(el);
        }

        protected override object Dup(object el)
        {
            // we dup every node, so don't have to worry about calling dup; short-
            // circuited next() so it doesn't call.
            throw new NotSupportedException("dup can't be called for a node stream.");
        }
    }
}
