namespace Protobuild
{
    public interface IGenerationFunctionsProvider
    {
        string ConvertGenerationFunctionsToXSLT(string prefix, string input);
    }
}