namespace Antlr.Runtime.Tree
{
    using IList = System.Collections.IList;
    using NotSupportedException = NotSupportedException;

    [Serializable]
    public class RewriteRuleTokenStream : RewriteRuleElementStream
    {

        public RewriteRuleTokenStream(ITreeAdaptor adaptor, string elementDescription)
            : base(adaptor, elementDescription)
        {
        }

        /** <summary>Create a stream with one element</summary> */
        public RewriteRuleTokenStream(ITreeAdaptor adaptor, string elementDescription, object oneElement)
            : base(adaptor, elementDescription, oneElement)
        {
        }

        /** <summary>Create a stream, but feed off an existing list</summary> */
        public RewriteRuleTokenStream(ITreeAdaptor adaptor, string elementDescription, IList elements)
            : base(adaptor, elementDescription, elements)
        {
        }

        /** <summary>Get next token from stream and make a node for it</summary> */
        public virtual object NextNode()
        {
            var t = (IToken) NextCore();
            return adaptor.Create(t);
        }

        public virtual IToken NextToken()
        {
            return (IToken) NextCore();
        }

        /** <summary>
         *  Don't convert to a tree unless they explicitly call nextTree.
         *  This way we can do hetero tree nodes in rewrite.
         *  </summary>
         */
        protected override object ToTree(object el)
        {
            return el;
        }

        protected override object Dup(object el)
        {
            throw new NotSupportedException("dup can't be called for a token stream.");
        }
    }
}
