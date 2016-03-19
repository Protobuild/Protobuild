namespace Protobuild
{
    internal interface IProjectTemplateApplier
    {
        void Apply(string stagingFolder, string templateName);
    }
}