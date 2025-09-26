namespace Antlr.Runtime
{
    /** <summary>AST rules have trees</summary> */
    public interface IAstRuleReturnScope<TAstLabel> : IAstRuleReturnScope
    {
        /** <summary>Has a value potentially if output=AST;</summary> */
        new TAstLabel Tree {
            get;
        }
    }
}
