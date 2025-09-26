namespace Antlr.Runtime
{
    /** <summary>AST rules have trees</summary> */
    public interface IAstRuleReturnScope : IRuleReturnScope
    {
        /** <summary>Has a value potentially if output=AST;</summary> */
        object Tree {
            get;
        }
    }
}
