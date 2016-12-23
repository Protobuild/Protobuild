namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;
    using System.Security;

    public class PackageUnifiedNuGetLocalInstallTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageUnifiedNuGetLocalInstallTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void CommandRunsSuccessfully()
        {
            this.SetupTest("PackageUnifiedNuGetLocalInstall");

            if (File.Exists(this.GetPath(Path.Combine("Build", "Module.xml"))))
            {
                File.Delete(this.GetPath(Path.Combine("Build", "Module.xml")));
            }

            using (var reader = new StreamReader(this.GetPath(Path.Combine("Build", "Module.Template.xml"))))
            {
                var content = reader.ReadToEnd();
                content.Replace("{PATH_TO_NUGET_PKG}", SecurityElement.Escape(this.GetPath("TestInstall.nupkg")));
                using (var writer = new StreamWriter(this.GetPath(Path.Combine("Build", "Module.xml"))))
                {
                    writer.Write(content);
                }
            }

            this.OtherMode("resolve", "Windows");

            _assert.True(Directory.Exists(this.GetPath("TestInstall")));
            _assert.True(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows"))));
            _assert.False(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows", "_rels"))));
            _assert.True(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows", "Build"))));
        }
    }
}
