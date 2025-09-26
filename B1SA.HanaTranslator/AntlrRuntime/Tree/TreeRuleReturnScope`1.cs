namespace Antlr.Runtime.Tree
{
    /// <summary>
    /// This is identical to the ParserRuleReturnScope except that
    /// the start property is a tree nodes not Token object
    /// when you are parsing trees.
    /// </summary>
    [Serializable]
    public class TreeRuleReturnScope<TTree> : IRuleReturnScope<TTree>
    {
        private TTree _start;

        /// <summary>Gets the first node or root node of tree matched for this rule.</summary>
        public TTree Start {
            get {
                return _start;
            }

            set {
                _start = value;
            }
        }

        object IRuleReturnScope.Start {
            get {
                return Start;
            }
        }

        TTree IRuleReturnScope<TTree>.Stop {
            get {
                return default(TTree);
            }
        }

        object IRuleReturnScope.Stop {
            get {
                return default(TTree);
            }
        }
    }
}
