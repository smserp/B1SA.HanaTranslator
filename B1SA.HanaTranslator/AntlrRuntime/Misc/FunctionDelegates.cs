namespace Antlr.Runtime.Misc
{
    public delegate void Action();

    public delegate TResult Func<TResult>();

    public delegate TResult Func<T, TResult>(T arg);
}
