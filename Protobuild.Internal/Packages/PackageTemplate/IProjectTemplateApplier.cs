namespace Protobuild
{
    public interface IProjectTemplateApplier
    {
        void Apply(string stagingFolder, string templateName);
    }
}