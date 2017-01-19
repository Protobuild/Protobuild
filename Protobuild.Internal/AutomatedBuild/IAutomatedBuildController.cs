namespace Protobuild
{
    internal interface IAutomatedBuildController
    {
        int Execute(string workingDirectory, string path);
    }
}