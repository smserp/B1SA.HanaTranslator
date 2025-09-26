namespace Antlr.Runtime.Tree
{
    using System.Collections.Generic;
    using Antlr.Runtime.Misc;

    using StringBuilder = System.Text.StringBuilder;

    [Serializable]
    public class CommonTreeNodeStream : LookaheadStream<object>, ITreeNodeStream, IPositionTrackingStream
    {
        public const int DEFAULT_INITIAL_BUFFER_SIZE = 100;
        public const int INITIAL_CALL_STACK_SIZE = 10;

        /** <summary>Pull nodes from which tree?</summary> */
        private readonly object _root;

        /** <summary>If this tree (root) was created from a token stream, track it.</summary> */
        protected ITokenStream tokens;

        /** <summary>What tree adaptor was used to build these trees</summary> */
        [NonSerialized]
        private ITreeAdaptor _adaptor;

        /** The tree iterator we are using */
        private readonly TreeIterator _it;

        /** <summary>Stack of indexes used for push/pop calls</summary> */
        private Stack<int> _calls;

        /** <summary>Tree (nil A B C) trees like flat A B C streams</summary> */
        private bool _hasNilRoot = false;

        /** <summary>Tracks tree depth.  Level=0 means we're at root node level.</summary> */
        private int _level = 0;

        /**
         * Tracks the last node before the start of {@link #data} which contains
         * position information to provide information for error reporting. This is
         * tracked in addition to {@link #prevElement} which may or may not contain
         * position information.
         *
         * @see #hasPositionInformation
         * @see RecognitionException#extractInformationFromTreeNodeStream
         */
        private object _previousLocationElement;

        public CommonTreeNodeStream(object tree)
            : this(new CommonTreeAdaptor(), tree)
        {
        }

        public CommonTreeNodeStream(ITreeAdaptor adaptor, object tree)
        {
            this._root = tree;
            this._adaptor = adaptor;
            _it = new TreeIterator(adaptor, _root);
        }

        #region Properties

        public virtual string SourceName {
            get {
                if (TokenStream == null)
                    return null;

                return TokenStream.SourceName;
            }
        }

        public virtual ITokenStream TokenStream {
            get {
                return tokens;
            }

            set {
                tokens = value;
            }
        }

        public virtual ITreeAdaptor TreeAdaptor {
            get {
                return _adaptor;
            }

            set {
                _adaptor = value;
            }
        }

        public virtual object TreeSource {
            get {
                return _root;
            }
        }

        public virtual bool UniqueNavigationNodes {
            get {
                return false;
            }

            set {
            }
        }

        #endregion

        public override void Reset()
        {
            base.Reset();
            _it.Reset();
            _hasNilRoot = false;
            _level = 0;
            _previousLocationElement = null;
            if (_calls != null)
                _calls.Clear();
        }

        public override object NextElement()
        {
            _it.MoveNext();
            var t = _it.Current;
            //System.out.println("pulled "+adaptor.getType(t));
            if (t == _it.up) {
                _level--;
                if (_level == 0 && _hasNilRoot) {
                    _it.MoveNext();
                    return _it.Current; // don't give last UP; get EOF
                }
            }
            else if (t == _it.down) {
                _level++;
            }

            if (_level == 0 && TreeAdaptor.IsNil(t)) {
                // if nil root, scarf nil, DOWN
                _hasNilRoot = true;
                _it.MoveNext();
                t = _it.Current; // t is now DOWN, so get first real node next
                _level++;
                _it.MoveNext();
                t = _it.Current;
            }

            return t;
        }

        public override object Dequeue()
        {
            var result = base.Dequeue();
            if (_p == 0 && HasPositionInformation(PreviousElement))
                _previousLocationElement = PreviousElement;

            return result;
        }

        public override bool IsEndOfFile(object o)
        {
            return TreeAdaptor.GetType(o) == CharStreamConstants.EndOfFile;
        }

        public virtual int LA(int i)
        {
            return TreeAdaptor.GetType(LT(i));
        }

        /** Make stream jump to a new location, saving old location.
         *  Switch back with pop().
         */
        public virtual void Push(int index)
        {
            if (_calls == null)
                _calls = new Stack<int>();

            _calls.Push(_p); // save current index
            Seek(index);
        }

        /** Seek back to previous index saved during last push() call.
         *  Return top of stack (return index).
         */
        public virtual int Pop()
        {
            var ret = _calls.Pop();
            Seek(ret);
            return ret;
        }

        /**
         * Returns an element containing position information. If {@code allowApproximateLocation} is {@code false}, then
         * this method will return the {@code LT(1)} element if it contains position information, and otherwise return {@code null}.
         * If {@code allowApproximateLocation} is {@code true}, then this method will return the last known element containing position information.
         *
         * @see #hasPositionInformation
         */
        public object GetKnownPositionElement(bool allowApproximateLocation)
        {
            var node = _data[_p];
            if (HasPositionInformation(node))
                return node;

            if (!allowApproximateLocation)
                return null;

            for (var index = _p - 1; index >= 0; index--) {
                node = _data[index];
                if (HasPositionInformation(node))
                    return node;
            }

            return _previousLocationElement;
        }

        public bool HasPositionInformation(object node)
        {
            var token = TreeAdaptor.GetToken(node);
            if (token == null)
                return false;

            if (token.Line <= 0)
                return false;

            return true;
        }

        #region Tree rewrite interface

        public virtual void ReplaceChildren(object parent, int startChildIndex, int stopChildIndex, object t)
        {
            if (parent != null) {
                TreeAdaptor.ReplaceChildren(parent, startChildIndex, stopChildIndex, t);
            }
        }

        #endregion

        public virtual string ToString(object start, object stop)
        {
            // we'll have to walk from start to stop in tree; we're not keeping
            // a complete node stream buffer
            return "n/a";
        }

        /** <summary>For debugging; destructive: moves tree iterator to end.</summary> */
        public virtual string ToTokenTypeString()
        {
            Reset();
            var buf = new StringBuilder();
            var o = LT(1);
            var type = TreeAdaptor.GetType(o);
            while (type != TokenTypes.EndOfFile) {
                buf.Append(" ");
                buf.Append(type);
                Consume();
                o = LT(1);
                type = TreeAdaptor.GetType(o);
            }
            return buf.ToString();
        }
    }
}
