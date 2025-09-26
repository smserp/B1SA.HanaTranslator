namespace Antlr.Runtime
{
    using Attribute = Attribute;
    using AttributeTargets = AttributeTargets;
    using AttributeUsageAttribute = AttributeUsageAttribute;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class GrammarRuleAttribute : Attribute
    {
        private readonly string _name;

        public GrammarRuleAttribute(string name)
        {
            this._name = name;
        }

        public string Name {
            get {
                return _name;
            }
        }
    }
}
