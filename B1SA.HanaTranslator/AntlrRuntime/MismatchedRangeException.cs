namespace Antlr.Runtime
{
    using Exception = Exception;

    [Serializable]
    public class MismatchedRangeException : RecognitionException
    {
        private readonly int _a;
        private readonly int _b;

        public MismatchedRangeException()
        {
        }

        public MismatchedRangeException(string message)
            : base(message)
        {
        }

        public MismatchedRangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MismatchedRangeException(int a, int b, IIntStream input)
            : base(input)
        {
            this._a = a;
            this._b = b;
        }

        public MismatchedRangeException(string message, int a, int b, IIntStream input)
            : base(message, input)
        {
            this._a = a;
            this._b = b;
        }

        public MismatchedRangeException(string message, int a, int b, IIntStream input, Exception innerException)
            : base(message, input, innerException)
        {
            this._a = a;
            this._b = b;
        }

        public int A {
            get {
                return _a;
            }
        }

        public int B {
            get {
                return _b;
            }
        }

        public override string ToString()
        {
            return "MismatchedRangeException(" + UnexpectedType + " not in [" + A + "," + B + "])";
        }
    }
}
