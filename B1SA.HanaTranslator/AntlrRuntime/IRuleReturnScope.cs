namespace Antlr.Runtime
{
    /// <summary>
    /// Rules can have start/stop info.
    /// </summary>
    public interface IRuleReturnScope
    {
        /// <summary>
        /// Gets the start element from the input stream
        /// </summary>
        object Start {
            get;
        }

        /// <summary>
        /// Gets the stop element from the input stream
        /// </summary>
        object Stop {
            get;
        }
    }
}
