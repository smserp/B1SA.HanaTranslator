namespace Antlr.Runtime
{

    /** <summary>A source of characters for an ANTLR lexer</summary> */
    public interface ICharStream : IIntStream
    {

        /** <summary>
         *  For infinite streams, you don't need this; primarily I'm providing
         *  a useful interface for action code.  Just make sure actions don't
         *  use this on streams that don't support it.
         *  </summary>
         */
        string Substring(int start, int length);

        /** <summary>
         *  Get the ith character of lookahead.  This is the same usually as
         *  LA(i).  This will be used for labels in the generated
         *  lexer code.  I'd prefer to return a char here type-wise, but it's
         *  probably better to be 32-bit clean and be consistent with LA.
         *  </summary>
         */
        int LT(int i);

        /** <summary>ANTLR tracks the line information automatically</summary> */
        /** <summary>Because this stream can rewind, we need to be able to reset the line</summary> */
        int Line {
            get;
            set;
        }

        /** <summary>The index of the character relative to the beginning of the line 0..n-1</summary> */
        int CharPositionInLine {
            get;
            set;
        }
    }
}
