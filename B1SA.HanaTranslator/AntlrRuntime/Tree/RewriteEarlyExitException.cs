namespace Antlr.Runtime.Tree
{
    using Exception = Exception;

    /// <summary>No elements within a (...)+ in a rewrite rule</summary>
    [Serializable]
    public class RewriteEarlyExitException : RewriteCardinalityException
    {
        public RewriteEarlyExitException()
        {
        }

        public RewriteEarlyExitException(string elementDescription)
            : base(elementDescription)
        {
        }

        public RewriteEarlyExitException(string elementDescription, Exception innerException)
            : base(elementDescription, innerException)
        {
        }

        public RewriteEarlyExitException(string message, string elementDescription)
            : base(message, elementDescription)
        {
        }

        public RewriteEarlyExitException(string message, string elementDescription, Exception innerException)
            : base(message, elementDescription, innerException)
        {
        }
    }
}
