namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;
    using System.Security;

    public class PackageUnifiedNuGetLocalInstallNoDedupTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageUnifiedNuGetLocalInstallNoDedupTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void CommandRunsSuccessfully()
        {
            this.SetupTest("PackageUnifiedNuGetLocalInstallNoDedup");

            if (File.Exists(this.GetPath(Path.Combine("Build", "Module.xml"))))
            {
                File.Delete(this.GetPath(Path.Combine("Build", "Module.xml")));
            }

            using (var reader = new StreamReader(this.GetPath(Path.Combine("Build", "Module.Template.xml"))))
            {
                var content = reader.ReadToEnd();
                content = content.Replace("{PATH_TO_NUGET_PKG}", SecurityElement.Escape(this.GetPath("Protoinject.nupkg")));
                using (var writer = new StreamWriter(this.GetPath(Path.Combine("Build", "Module.xml"))))
                {
                    writer.Write(content);
                }
            }

            if (Directory.Exists(this.GetPath("TestInstall")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("TestInstall"));
            }

            this.OtherMode("resolve", "Windows");

            _assert.True(Directory.Exists(this.GetPath("TestInstall")));
            _assert.True(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows"))));
            _assert.False(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows", "_rels"))));
            _assert.True(Directory.Exists(this.GetPath(Path.Combine("TestInstall", "Windows", "Build"))));
        }
    }
}
