namespace Antlr.Runtime.Tree
{
    public class TemplateTreeRuleReturnScope<TTemplate, TTree> : TreeRuleReturnScope<TTree>, ITemplateRuleReturnScope<TTemplate>, ITemplateRuleReturnScope
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
