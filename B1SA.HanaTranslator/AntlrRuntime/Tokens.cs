namespace Antlr.Runtime
{
    public static class Tokens
    {
        /** <summary>
         *  In an action, a lexer rule can set token to this SKIP_TOKEN and ANTLR
         *  will avoid creating a token for this symbol and try to fetch another.
         *  </summary>
         */
        public static readonly IToken Skip = new CommonToken(TokenTypes.Invalid);
    }
}
