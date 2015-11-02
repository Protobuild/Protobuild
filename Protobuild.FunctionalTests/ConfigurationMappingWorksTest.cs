namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ConfigurationMappingWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ConfigurationMappingWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ConfigurationMappingWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            var solutionContents = this.ReadFile(@"Module.Windows.sln");

            _assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", solutionContents);
            _assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.Build.0 = Debug|Any CPU", solutionContents);
            _assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.ActiveCfg = Release|Any CPU", solutionContents);
            _assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.Build.0 = Release|Any CPU", solutionContents);
            _assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.ActiveCfg = Debug|SomeConf", solutionContents);
            _assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.Build.0 = Debug|SomeConf", solutionContents);
            _assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.ActiveCfg = Release|SomeConf", solutionContents);
            _assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.Build.0 = Release|SomeConf", solutionContents);
        }
    }
}