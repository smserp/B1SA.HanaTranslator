namespace Antlr.Runtime
{
    /** <summary>
     *  Rules that return more than a single value must return an object
     *  containing all the values.  Besides the properties defined in
     *  RuleLabelScope.predefinedRulePropertiesScope there may be user-defined
     *  return values.  This class simply defines the minimum properties that
     *  are always defined and methods to access the others that might be
     *  available depending on output option such as template and tree.
     *  </summary>
     *
     *  <remarks>
     *  Note text is not an actual property of the return value, it is computed
     *  from start and stop using the input stream's toString() method.  I
     *  could add a ctor to this so that we can pass in and store the input
     *  stream, but I'm not sure we want to do that.  It would seem to be undefined
     *  to get the .text property anyway if the rule matches tokens from multiple
     *  input streams.
     *
     *  I do not use getters for fields of objects that are used simply to
     *  group values such as this aggregate.  The getters/setters are there to
     *  satisfy the superclass interface.
     *  </remarks>
     */
    public class ParserRuleReturnScope<TToken> : IRuleReturnScope<TToken>
    {
        private TToken _start;
        private TToken _stop;

        public TToken Start {
            get {
                return _start;
            }

            set {
                _start = value;
            }
        }

        public TToken Stop {
            get {
                return _stop;
            }

            set {
                _stop = value;
            }
        }

        object IRuleReturnScope.Start {
            get {
                return Start;
            }
        }

        object IRuleReturnScope.Stop {
            get {
                return Stop;
            }
        }
    }
}
