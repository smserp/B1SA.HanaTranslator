namespace Antlr.Runtime.Tree
{
    using Antlr.Runtime.Misc;

    public class TreeFilter : TreeParser
    {
        protected ITokenStream originalTokenStream;
        protected ITreeAdaptor originalAdaptor;

        public TreeFilter(ITreeNodeStream input)
            : this(input, new RecognizerSharedState())
        {
        }
        public TreeFilter(ITreeNodeStream input, RecognizerSharedState state)
            : base(input, state)
        {
            originalAdaptor = input.TreeAdaptor;
            originalTokenStream = input.TokenStream;
        }

        public virtual void ApplyOnce(object t, Action whichRule)
        {
            if (t == null)
                return;

            try {
                // share TreeParser object but not parsing-related state
                SetState(new RecognizerSharedState());
                SetTreeNodeStream(new CommonTreeNodeStream(originalAdaptor, t));
                ((CommonTreeNodeStream) input).TokenStream = originalTokenStream;
                BacktrackingLevel = 1;
                whichRule();
                BacktrackingLevel = 0;
            }
            catch (RecognitionException) {
            }
        }

        public virtual void Downup(object t)
        {
            var v = new TreeVisitor(new CommonTreeAdaptor());
            Func<object, object> pre = (o) => {
                ApplyOnce(o, Topdown);
                return o;
            };
            Func<object, object> post = (o) => {
                ApplyOnce(o, Bottomup);
                return o;
            };
            v.Visit(t, pre, post);
        }

        // methods the downup strategy uses to do the up and down rules.
        // to override, just define tree grammar rule topdown and turn on
        // filter=true.
        protected virtual void Topdown()
        {
        }
        protected virtual void Bottomup()
        {
        }
    }
}
