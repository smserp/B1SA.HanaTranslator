namespace Antlr.Runtime.Tree
{

    /** <summary>
     *  A TreeAdaptor that works with any Tree implementation.  It provides
     *  really just factory methods; all the work is done by BaseTreeAdaptor.
     *  If you would like to have different tokens created than ClassicToken
     *  objects, you need to override this and then set the parser tree adaptor to
     *  use your subclass.
     *  </summary>
     *
     *  <remarks>
     *  To get your parser to build nodes of a different type, override
     *  create(Token), errorNode(), and to be safe, YourTreeClass.dupNode().
     *  dupNode is called to duplicate nodes during rewrite operations.
     *  </remarks>
     */
    public class CommonTreeAdaptor : BaseTreeAdaptor
    {
        public override object Create(IToken payload)
        {
            return new CommonTree(payload);
        }

        /** <summary>
         *  Tell me how to create a token for use with imaginary token nodes.
         *  For example, there is probably no input symbol associated with imaginary
         *  token DECL, but you need to create it as a payload or whatever for
         *  the DECL node as in ^(DECL type ID).
         *  </summary>
         *
         *  <remarks>
         *  If you care what the token payload objects' type is, you should
         *  override this method and any other createToken variant.
         *  </remarks>
         */
        public override IToken CreateToken(int tokenType, string text)
        {
            return new CommonToken(tokenType, text);
        }

        /** <summary>
         *  Tell me how to create a token for use with imaginary token nodes.
         *  For example, there is probably no input symbol associated with imaginary
         *  token DECL, but you need to create it as a payload or whatever for
         *  the DECL node as in ^(DECL type ID).
         *  </summary>
         *
         *  <remarks>
         *  This is a variant of createToken where the new token is derived from
         *  an actual real input token.  Typically this is for converting '{'
         *  tokens to BLOCK etc...  You'll see
         *
         *    r : lc='{' ID+ '}' -> ^(BLOCK[$lc] ID+) ;
         *
         *  If you care what the token payload objects' type is, you should
         *  override this method and any other createToken variant.
         *  </remarks>
         */
        public override IToken CreateToken(IToken fromToken)
        {
            return new CommonToken(fromToken);
        }

        /** <summary>
         *  What is the Token associated with this node?  If
         *  you are not using CommonTree, then you must
         *  override this in your own adaptor.
         *  </summary>
         */
        public override IToken GetToken(object t)
        {
            if (t is CommonTree) {
                return ((CommonTree) t).Token;
            }
            return null; // no idea what to do
        }
    }
}
