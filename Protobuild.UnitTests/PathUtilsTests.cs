using Prototest.Library.Version1;

namespace Protobuild.Tests
{
    public class PathUtilsTests
    {
        private readonly IAssert _assert;

        public PathUtilsTests(IAssert assert)
        {
            _assert = assert;
        }

        // We use file:// explicitly in these tests so that they work
        // on all platforms.  These tests also pass on Windows if the
        // paths are of "C:\path\to\module\" form, and also pass on UNIX
        // if they are of the "/path/to/module/" form.
        
        public void DirectoriesNextToEachOther()
        {
            _assert.Equal(
                "..\\target\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/target/"));
        }
        
        public void Subdirectory()
        {
            _assert.Equal(
                "target\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/module/target/"));
        }
        
        public void SameDirectory()
        {
            _assert.Equal(
                "",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/module/"));
        }
        
        public void ParentDirectory()
        {
            _assert.Equal(
                "..\\",
                PathUtils.GetRelativePath("file:///path/to/module/", "file:///path/to/"));
        }
    }
}

