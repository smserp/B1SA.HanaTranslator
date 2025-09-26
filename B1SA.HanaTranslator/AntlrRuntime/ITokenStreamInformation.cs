namespace Antlr.Runtime
{
    public interface ITokenStreamInformation
    {
        IToken LastToken {
            get;
        }

        IToken LastRealToken {
            get;
        }

        int MaxLookBehind {
            get;
        }
    }
}
