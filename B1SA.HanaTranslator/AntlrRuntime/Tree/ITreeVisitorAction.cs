namespace Antlr.Runtime.Tree
{
    using Antlr.Runtime.Misc;

    /// <summary>
    /// How to execute code for node t when a visitor visits node t. Execute
    /// Pre() before visiting children and execute Post() after visiting children.
    /// </summary>
    public interface ITreeVisitorAction
    {
        /// <summary>
        /// Execute an action before visiting children of t. Return t or
        /// a rewritten t. It is up to the visitor to decide what to do
        /// with the return value. Children of returned value will be
        /// visited if using TreeVisitor.visit().
        /// </summary>
        object Pre(object t);

        /// <summary>
        /// Execute an action after visiting children of t. Return t or
        /// a rewritten t. It is up to the visitor to decide what to do
        /// with the return value.
        /// </summary>
        object Post(object t);
    }

    public class TreeVisitorAction
        : ITreeVisitorAction
    {
        private readonly Func<object, object> _preAction;
        private readonly Func<object, object> _postAction;

        public TreeVisitorAction(Func<object, object> preAction, Func<object, object> postAction)
        {
            _preAction = preAction;
            _postAction = postAction;
        }

        public object Pre(object t)
        {
            if (_preAction != null)
                return _preAction(t);

            return t;
        }

        public object Post(object t)
        {
            if (_postAction != null)
                return _postAction(t);

            return t;
        }
    }
}
