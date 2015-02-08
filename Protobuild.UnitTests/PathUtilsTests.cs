using Xunit;

namespace Protobuild.Tests
{
    public class PathUtilsTests
    {
        // We use file:// explicitly in these tests so that they work
        // on all platforms.  These tests also pass on Windows if the
        // paths are of "C:\path\to\module\" form, and also pass on UNIX
        // if they are of the "/path/to/module/" form.

        [Fact]
        public void DirectoriesNextToEachOther()
        {
            Assert.Equal(
                "..\\target\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/target/"));
        }

        [Fact]
        public void Subdirectory()
        {
            Assert.Equal(
                "target\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/module/target/"));
        }

        [Fact]
        public void SameDirectory()
        {
            Assert.Equal(
                "",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/module/"));
        }

        [Fact]
        public void ParentDirectory()
        {
            Assert.Equal(
                "..\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/"));
        }
    }
}

