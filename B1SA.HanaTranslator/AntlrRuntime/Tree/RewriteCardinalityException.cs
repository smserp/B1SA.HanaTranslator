namespace Antlr.Runtime.Tree
{
    using Exception = Exception;

    /** <summary>
     *  Base class for all exceptions thrown during AST rewrite construction.
     *  This signifies a case where the cardinality of two or more elements
     *  in a subrule are different: (ID INT)+ where |ID|!=|INT|
     *  </summary>
     */
    [Serializable]
    public class RewriteCardinalityException : Exception
    {
        private readonly string _elementDescription;

        public RewriteCardinalityException()
        {
        }

        public RewriteCardinalityException(string elementDescription)
            : this(elementDescription, elementDescription)
        {
            this._elementDescription = elementDescription;
        }

        public RewriteCardinalityException(string elementDescription, Exception innerException)
            : this(elementDescription, elementDescription, innerException)
        {
        }

        public RewriteCardinalityException(string message, string elementDescription)
            : base(message)
        {
            _elementDescription = elementDescription;
        }

        public RewriteCardinalityException(string message, string elementDescription, Exception innerException)
            : base(message, innerException)
        {
            _elementDescription = elementDescription;
        }
    }
}
