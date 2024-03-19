namespace B1SA.HanaTranslator
{
    class TokenHandler
    {
        static public string TokenName()
        {
            return "Undefined";
        }

        protected TokenHandler()
        {
            // empty
        }

        public virtual string GetTokenString(int tokenID)
        {
            // No token string for abstract Token handler
            return string.Empty;
        }

        public virtual string TranslateToken(string tokenString)
        {
            // No conversion
            return tokenString;
        }
    }
}
