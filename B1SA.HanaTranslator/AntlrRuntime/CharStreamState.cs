namespace Antlr.Runtime
{

    /** <summary>
     *  When walking ahead with cyclic DFA or for syntactic predicates,
     *  we need to record the state of the input stream (char index,
     *  line, etc...) so that we can rewind the state after scanning ahead.
     *  </summary>
     *
     *  <remarks>This is the complete state of a stream.</remarks>
     */
    [Serializable]
    public class CharStreamState
    {
        /** <summary>Index into the char stream of next lookahead char</summary> */
        public int p;

        /** <summary>What line number is the scanner at before processing buffer[p]?</summary> */
        public int line;

        /** <summary>What char position 0..n-1 in line is scanner before processing buffer[p]?</summary> */
        public int charPositionInLine;
    }
}
