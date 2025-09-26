namespace Antlr.Runtime
{
    public static class TokenChannels
    {
        /** <summary>
         *  All tokens go to the parser (unless skip() is called in that rule)
         *  on a particular "channel".  The parser tunes to a particular channel
         *  so that whitespace etc... can go to the parser on a "hidden" channel.
         *  </summary>
         */
        public const int Default = 0;

        /** <summary>
         *  Anything on different channel than DEFAULT_CHANNEL is not parsed
         *  by parser.
         *  </summary>
         */
        public const int Hidden = 99;
    }
}
