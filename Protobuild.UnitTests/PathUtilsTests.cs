using Xunit;

namespace Protobuild.Tests
{
    public class PathUtilsTests
    {
        [Fact]
        public void DirectoriesNextToEachOther()
        {
            Assert.Equal(
                "..\\target\\",
                PathUtils.GetRelativePath("/path/to/module/", "/path/to/target/"));
        }

        [Fact]
        public void Subdirectory()
        {
            Assert.Equal(
                "target\\",
                PathUtils.GetRelativePath("/path/to/module/", "/path/to/module/target/"));
        }

        [Fact]
        public void SameDirectory()
        {
            Assert.Equal(
                "",
                PathUtils.GetRelativePath("/path/to/module/", "/path/to/module/"));
        }

        [Fact]
        public void ParentDirectory()
        {
            Assert.Equal(
                "..\\",
                PathUtils.GetRelativePath("/path/to/module/", "/path/to/"));
        }
    }
}

