using System.Text.RegularExpressions;

namespace B1SA.HanaTranslator
{
    using TokenMatch = Tuple<string, string, TokenHandler>;

    internal class Tokenizer
    {
        // true when input statement is only token
        protected bool onlyTokenInput = false;
        private readonly Config config;
        private readonly string inputQuery;
        private Dictionary<string, TokenHandler> tokenPatterns = [];

        protected List<TokenMatch> tokenValueMatch = [];

        private VariableTokenizer varTokenHandler = new VariableTokenizer();
        private IdentifierTokenizer idTokenHandler = new IdentifierTokenizer();

        public Tokenizer(Config configuration, string inputQuery)
        {
            config = configuration;

            this.inputQuery = inputQuery;
        }

        public bool OnlyTokenInput()
        {
            return onlyTokenInput;
        }

        private TokenHandler GetTokenHandlerByName(string tokenName)
        {
            if (string.Equals(tokenName, VariableTokenizer.TokenName(), StringComparison.OrdinalIgnoreCase)) {
                return varTokenHandler;
            }

            if (string.Equals(tokenName, IdentifierTokenizer.TokenName(), StringComparison.OrdinalIgnoreCase)) {
                return idTokenHandler;
            }

            return null;
        }

        private void LoadTokens()
        {
            var ith = GetTokenHandlerByName("IdToken");
            foreach (var token in config.IdTokens) {
                // Ignore comments and blank lines.
                if (!token.StartsWith("//") && token.Any(c => !char.IsWhiteSpace(c))) {
                    if (ith != null) {
                        tokenPatterns.Add(token, ith);
                    }
                    else {
                        Console.WriteLine(Resources.MSG_INVALID_TOKEN_OPTIONS, token);
                    }
                }
            }

            var vth = GetTokenHandlerByName("VarToken");
            foreach (var token in config.VarTokens) {
                // Ignore comments and blank lines.
                if (!token.StartsWith("//") && token.Any(c => !char.IsWhiteSpace(c))) {
                    if (vth != null) {
                        tokenPatterns.Add(token, vth);
                    }
                    else {
                        Console.WriteLine(Resources.MSG_INVALID_TOKEN_OPTIONS, token);
                    }
                }
            }
        }

        public string TokenizeInputStatements()
        {
            LoadTokens();

            //tokenize
            if (tokenPatterns.Count == 0) {
                return this.inputQuery;
            }

            var outQuery = this.inputQuery;
            var tokenNum = 1;

            foreach (var pair in tokenPatterns) {
                while (true) {
                    var match = Regex.Match(outQuery, pair.Key);

                    if (match.Success) {
                        if (QueryIsTokenOnly(match.Value, outQuery)) {
                            onlyTokenInput = true;
                            return this.inputQuery;
                        }

                        var tokenID = pair.Value.GetTokenString(tokenNum);
                        tokenValueMatch.Add(new Tuple<string, string, TokenHandler>(tokenID, match.Value, pair.Value));

                        //Replace method replaces all occurence of the string match.value in the outputQuery
                        outQuery = outQuery.Replace(match.Value, tokenID);
                        tokenNum++;
                    }
                    else {
                        break;
                    }
                }
            }


            return outQuery;
        }

        public string DetokenizeInputStatements(string translatedStatement)
        {
            var outputQuery = translatedStatement;

            foreach (var tuple in tokenValueMatch) {
                // Translate tokens strings
                var translatedToken = tuple.Item3.TranslateToken(tuple.Item1);

                outputQuery = outputQuery.Replace(translatedToken, tuple.Item2);

                //If tokens were used inside string constants,use original value to revert
                outputQuery = outputQuery.Replace(tuple.Item1, tuple.Item2);
            }

            return outputQuery;
        }

        /// <summary>
        /// Input query is consider as token only when is equals to:
        /// customToken
        /// customToken;
        /// SELECT customToken
        /// SELECT customToken;
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        protected virtual bool QueryIsTokenOnly(string pattern, string query)
        {
            var trimmQuery = query.Trim().TrimEnd(';');

            if (pattern.Length == trimmQuery.Length) {
                // whole input query is: customToken
                // whole input query is: customToken;
                return true;
            }

            var arr = query.Split(' ');

            if (arr.Length == 2 && (arr[0].Trim().ToUpper() == "SELECT")) {
                trimmQuery = arr[1].Trim().TrimEnd(';');
                if (trimmQuery.Length == pattern.Length) {
                    //whole input query is: SELECT customToken
                    //whole input query is: SELECT customToken;
                    return true;
                }
            }

            return false;
        }
    }
}
