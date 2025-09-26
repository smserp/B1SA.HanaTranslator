namespace Antlr.Runtime
{
    /// <summary>
    /// Rules can have start/stop info.
    /// </summary>
    /// <typeparam name="TLabel">The element type of the input stream.</typeparam>
    public interface IRuleReturnScope<TLabel> : IRuleReturnScope
    {
        /// <summary>
        /// Gets the start element from the input stream
        /// </summary>
        new TLabel Start {
            get;
        }

        /// <summary>
        /// Gets the stop element from the input stream
        /// </summary>
        new TLabel Stop {
            get;
        }
    }
}
