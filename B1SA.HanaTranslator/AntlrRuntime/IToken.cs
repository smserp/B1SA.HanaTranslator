namespace Antlr.Runtime
{

    public interface IToken
    {
        /** <summary>Get the text of the token</summary> */
        string Text {
            get;
            set;
        }

        int Type {
            get;
            set;
        }

        /** <summary>The line number on which this token was matched; line=1..n</summary> */
        int Line {
            get;
            set;
        }

        /** <summary>The index of the first character relative to the beginning of the line 0..n-1</summary> */
        int CharPositionInLine {
            get;
            set;
        }

        int Channel {
            get;
            set;
        }

        int StartIndex {
            get;
            set;
        }

        int StopIndex {
            get;
            set;
        }

        /** <summary>
         *  An index from 0..n-1 of the token object in the input stream.
         *  This must be valid in order to use the ANTLRWorks debugger.
         *  </summary>
         */
        int TokenIndex {
            get;
            set;
        }

        /** <summary>
         *  From what character stream was this token created?  You don't have to
         *  implement but it's nice to know where a Token comes from if you have
         *  include files etc... on the input.
         *  </summary>
         */
        ICharStream InputStream {
            get;
            set;
        }
    }
}
