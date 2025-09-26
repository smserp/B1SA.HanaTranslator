namespace Antlr.Runtime.Misc
{
    using System.Text.RegularExpressions;

    internal static class RegexOptionsHelper
    {
        public static readonly RegexOptions Compiled;

        static RegexOptionsHelper()
        {
            Compiled = RegexOptions.Compiled;
        }
    }
}
