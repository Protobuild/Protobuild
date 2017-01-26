namespace Protobuild
{
    internal interface IAutomatedBuildRuntime
    {
        object Parse(string text);
        int Execute(string workingDirectory, object handle);
    }
}
