namespace Antlr.Runtime.Tree
{
    using Exception = Exception;

    /** <summary>Ref to ID or expr but no tokens in ID stream or subtrees in expr stream</summary> */
    [Serializable]
    public class RewriteEmptyStreamException : RewriteCardinalityException
    {
        public RewriteEmptyStreamException()
        {
        }

        public RewriteEmptyStreamException(string elementDescription)
            : base(elementDescription)
        {
        }

        public RewriteEmptyStreamException(string elementDescription, Exception innerException)
            : base(elementDescription, innerException)
        {
        }

        public RewriteEmptyStreamException(string message, string elementDescription)
            : base(message, elementDescription)
        {
        }

        public RewriteEmptyStreamException(string message, string elementDescription, Exception innerException)
            : base(message, elementDescription, innerException)
        {
        }
    }
}
