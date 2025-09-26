namespace Antlr.Runtime.Tree
{
    using System.Collections.Generic;

    /** Return a node stream from a doubly-linked tree whose nodes
     *  know what child index they are.  No remove() is supported.
     *
     *  Emit navigation nodes (DOWN, UP, and EOF) to let show tree structure.
     */
    public class TreeIterator : IEnumerator<object>
    {
        protected ITreeAdaptor adaptor;
        protected object root;
        protected object tree;
        protected bool firstTime = true;
        private bool reachedEof;

        // navigation nodes to return during walk and at end
        public object up;
        public object down;
        public object eof;

        /** If we emit UP/DOWN nodes, we need to spit out multiple nodes per
         *  next() call.
         */
        protected Queue<object> nodes;

        public TreeIterator(CommonTree tree)
            : this(new CommonTreeAdaptor(), tree)
        {
        }

        public TreeIterator(ITreeAdaptor adaptor, object tree)
        {
            this.adaptor = adaptor;
            this.tree = tree;
            this.root = tree;
            nodes = new Queue<object>();
            down = adaptor.Create(TokenTypes.Down, "DOWN");
            up = adaptor.Create(TokenTypes.Up, "UP");
            eof = adaptor.Create(TokenTypes.EndOfFile, "EOF");
        }

        #region IEnumerator<object> Members

        public object Current {
            get;
            private set;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        public bool MoveNext()
        {
            if (firstTime) {
                // initial condition
                firstTime = false;
                if (adaptor.GetChildCount(tree) == 0) {
                    // single node tree (special)
                    nodes.Enqueue(eof);
                }
                Current = tree;
            }
            else {
                // if any queued up, use those first
                if (nodes != null && nodes.Count > 0) {
                    Current = nodes.Dequeue();
                }
                else {
                    // no nodes left?
                    if (tree == null) {
                        Current = eof;
                    }
                    else {
                        // next node will be child 0 if any children
                        if (adaptor.GetChildCount(tree) > 0) {
                            tree = adaptor.GetChild(tree, 0);
                            nodes.Enqueue(tree); // real node is next after DOWN
                            Current = down;
                        }
                        else {
                            // if no children, look for next sibling of tree or ancestor
                            var parent = adaptor.GetParent(tree);
                            // while we're out of siblings, keep popping back up towards root
                            while (parent != null &&
                                    adaptor.GetChildIndex(tree) + 1 >= adaptor.GetChildCount(parent)) {
                                nodes.Enqueue(up); // we're moving back up
                                tree = parent;
                                parent = adaptor.GetParent(tree);
                            }

                            // no nodes left?
                            if (parent == null) {
                                tree = null; // back at root? nothing left then
                                nodes.Enqueue(eof); // add to queue, might have UP nodes in there
                                Current = nodes.Dequeue();
                            }
                            else {
                                // must have found a node with an unvisited sibling
                                // move to it and return it
                                var nextSiblingIndex = adaptor.GetChildIndex(tree) + 1;
                                tree = adaptor.GetChild(parent, nextSiblingIndex);
                                nodes.Enqueue(tree); // add to queue, might have UP nodes in there
                                Current = nodes.Dequeue();
                            }
                        }
                    }
                }
            }

            var result = Current != eof || !reachedEof;
            reachedEof = Current == eof;
            return result;
        }

        public void Reset()
        {
            firstTime = true;
            tree = root;
            nodes.Clear();
        }

        #endregion
    }
}
