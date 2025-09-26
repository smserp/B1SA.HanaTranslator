namespace Antlr.Runtime
{
    public class TemplateParserRuleReturnScope<TTemplate, TToken> : ParserRuleReturnScope<TToken>, ITemplateRuleReturnScope<TTemplate>, ITemplateRuleReturnScope
    {
        private TTemplate _template;

        public TTemplate Template {
            get {
                return _template;
            }

            set {
                _template = value;
            }
        }

        object ITemplateRuleReturnScope.Template {
            get {
                return Template;
            }
        }
    }
}
