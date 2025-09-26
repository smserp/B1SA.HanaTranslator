namespace Antlr.Runtime
{
    public interface ITemplateRuleReturnScope<TTemplate> : ITemplateRuleReturnScope
    {
        new TTemplate Template {
            get;
        }
    }
}
