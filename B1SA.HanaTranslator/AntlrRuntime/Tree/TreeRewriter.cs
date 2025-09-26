namespace Antlr.Runtime.Tree
{
    using Antlr.Runtime.Misc;
    public class TreeRewriter : TreeParser
    {
        protected bool showTransformations;

        protected ITokenStream originalTokenStream;
        protected ITreeAdaptor originalAdaptor;

        private Func<IAstRuleReturnScope> topdown_func;
        private Func<IAstRuleReturnScope> bottomup_func;

        public TreeRewriter(ITreeNodeStream input)
            : this(input, new RecognizerSharedState())
        {
        }
        public TreeRewriter(ITreeNodeStream input, RecognizerSharedState state)
            : base(input, state)
        {
            originalAdaptor = input.TreeAdaptor;
            originalTokenStream = input.TokenStream;
            topdown_func = () => Topdown();
            bottomup_func = () => Bottomup();
        }

        public virtual object ApplyOnce(object t, Func<IAstRuleReturnScope> whichRule)
        {
            if (t == null)
                return null;

            try {
                // share TreeParser object but not parsing-related state
                SetState(new RecognizerSharedState());
                SetTreeNodeStream(new CommonTreeNodeStream(originalAdaptor, t));
                ((CommonTreeNodeStream) input).TokenStream = originalTokenStream;
                BacktrackingLevel = 1;
                var r = whichRule();
                BacktrackingLevel = 0;
                if (Failed)
                    return t;

                if (showTransformations && r != null && !t.Equals(r.Tree) && r.Tree != null)
                    ReportTransformation(t, r.Tree);

                if (r != null && r.Tree != null)
                    return r.Tree;
                else
                    return t;
            }
            catch (RecognitionException) {
            }

            return t;
        }

        public virtual object ApplyRepeatedly(object t, Func<IAstRuleReturnScope> whichRule)
        {
            var treeChanged = true;
            while (treeChanged) {
                var u = ApplyOnce(t, whichRule);
                treeChanged = !t.Equals(u);
                t = u;
            }
            return t;
        }

        public virtual object Downup(object t)
        {
            return Downup(t, false);
        }

        public virtual object Downup(object t, bool showTransformations)
        {
            this.showTransformations = showTransformations;
            var v = new TreeVisitor(new CommonTreeAdaptor());
            t = v.Visit(t, (o) => ApplyOnce(o, topdown_func), (o) => ApplyRepeatedly(o, bottomup_func));
            return t;
        }

        // methods the downup strategy uses to do the up and down rules.
        // to override, just define tree grammar rule topdown and turn on
        // filter=true.
        protected virtual IAstRuleReturnScope Topdown()
        {
            return null;
        }

        protected virtual IAstRuleReturnScope Bottomup()
        {
            return null;
        }

        /** Override this if you need transformation tracing to go somewhere
         *  other than stdout or if you're not using ITree-derived trees.
         */
        protected virtual void ReportTransformation(object oldTree, object newTree)
        {
            var old = oldTree as ITree;
            var @new = newTree as ITree;
            var oldMessage = old != null ? old.ToStringTree() : "??";
            var newMessage = @new != null ? @new.ToStringTree() : "??";
        }
    }
}
