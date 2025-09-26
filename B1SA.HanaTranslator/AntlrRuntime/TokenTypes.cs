namespace Antlr.Runtime
{
    public static class TokenTypes
    {
        public const int EndOfFile = CharStreamConstants.EndOfFile;
        public const int Invalid = 0;
        public const int EndOfRule = 1;
        /** <summary>imaginary tree navigation type; traverse "get child" link</summary> */
        public const int Down = 2;
        /** <summary>imaginary tree navigation type; finish with a child list</summary> */
        public const int Up = 3;
        public const int Min = Up + 1;
    }
}
