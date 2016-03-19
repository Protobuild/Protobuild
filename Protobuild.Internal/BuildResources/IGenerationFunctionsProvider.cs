namespace Protobuild
{
    internal interface IGenerationFunctionsProvider
    {
        string ConvertGenerationFunctionsToXSLT(string prefix, string input);
    }
}