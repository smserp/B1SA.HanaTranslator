namespace Antlr.Runtime.Misc
{
    using System.Collections.Generic;
    using InvalidOperationException = InvalidOperationException;

    public class ListStack<T> : List<T>
    {
        public T Peek()
        {
            return Peek(0);
        }

        public T Peek(int depth)
        {
            T item;
            if (!TryPeek(depth, out item))
                throw new InvalidOperationException();

            return item;
        }

        public bool TryPeek(out T item)
        {
            return TryPeek(0, out item);
        }

        public bool TryPeek(int depth, out T item)
        {
            if (depth >= Count) {
                item = default(T);
                return false;
            }

            item = this[Count - depth - 1];
            return true;
        }

        public T Pop()
        {
            T result;
            if (!TryPop(out result))
                throw new InvalidOperationException();

            return result;
        }

        public bool TryPop(out T item)
        {
            if (Count == 0) {
                item = default(T);
                return false;
            }

            item = this[Count - 1];
            RemoveAt(Count - 1);
            return true;
        }

        public void Push(T item)
        {
            Add(item);
        }
    }
}
