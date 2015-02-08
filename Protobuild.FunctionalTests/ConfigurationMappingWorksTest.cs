namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ConfigurationMappingWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ConfigurationMappingWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            var solutionContents = this.ReadFile(@"Module.Windows.sln");
            
            Assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", solutionContents);
            Assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.Build.0 = Debug|Any CPU", solutionContents);
            Assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.ActiveCfg = Release|Any CPU", solutionContents);
            Assert.DoesNotContain("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.Build.0 = Release|Any CPU", solutionContents);
            Assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.ActiveCfg = Debug|SomeConf", solutionContents);
            Assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Debug|Any CPU.Build.0 = Debug|SomeConf", solutionContents);
            Assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.ActiveCfg = Release|SomeConf", solutionContents);
            Assert.Contains("{73E1432F-7BB5-4E5A-B952-F9CBCEA7FG76}.Release|Any CPU.Build.0 = Release|SomeConf", solutionContents);
        }
    }
}