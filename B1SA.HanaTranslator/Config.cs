namespace B1SA.HanaTranslator
{
    public class Config
    {
        /// <summary>
        /// Format the output HANA SQL string
        /// </summary>
        public bool FormatOutput { get; set; } = false;

        /// <summary>
        /// Define which information to show in the result summary (see <see cref="Note"/>)
        /// </summary>
        public List<string> ResultSummaryFilter { get; set; } = new() {
            Note.CASEFIXER,
            Note.ERR_CASEFIXER,
            Note.STRINGIFIER,
            Note.ERR_MODIFIER
        };

        /// <summary>
        /// Enable commenting of translations
        /// </summary>
        public bool TranslationComments { get; set; } = true;

        /// <summary>
        /// Define which translation comments to show (see <see cref="Note"/>)
        /// </summary>
        public List<string> TranslationCommentsFilter { get; set; } = new() {
            Note.CASEFIXER,
            Note.ERR_CASEFIXER,
            Note.STRINGIFIER,
            Note.MODIFIER,
            Note.ERR_MODIFIER
        };

        /// <summary>
        /// Definition of identifier tokens.
        /// Do something like Add(@"{[0-9A-Za-z_,\.\-:]+}") to identify C# format/interpolation variables.
        /// The example is also the default, clear it if not needed!
        /// </summary>
        public List<string> IdTokens { get; set; } = new() {
            // here we identify C# string format/interpolation variables like {0:##} or {variable}
            @"{[0-9A-Za-z_,\.\-:]+}",

            // here we identify variables like $["VAR_1"]
            //@"\$\[[\$]*[0-9A-Z_a-z\.\""]+\]",

            // here we identify variables like [%42]
            //@"\[\%[0-9]+\]",
        };

        /// <summary>
        /// Definition of variable tokens.
        /// Do something like Add(@"\[TABLE[0-9]+\]") to identify IDs like [TABLE123].
        /// </summary>
        public List<string> VarTokens { get; set; } = new() {
            // here we scan for identifiers like [table]
            //@"\[[0-9A-Za-z_@]+\]"

            // here we scan for identifiers like [TABLE123]
            //@"\[TABLE[0-9]+\]",
        };
    }
}
