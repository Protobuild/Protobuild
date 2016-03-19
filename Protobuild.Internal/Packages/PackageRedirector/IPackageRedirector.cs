namespace Protobuild
{
    internal interface IPackageRedirector
    {
        string RedirectPackageUrl(string url);

        void RegisterLocalRedirect(string original, string replacement);

        string GetRedirectionArguments();
    }
}

