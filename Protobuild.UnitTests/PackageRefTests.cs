using System;
using Prototest.Library.Version1;

namespace Protobuild.UnitTests
{
    public class PackageRefTests
    {
        private readonly IAssert _assert;

        public PackageRefTests(IAssert assert)
        {
            _assert = assert;
        }

        public void TestGitSHA1IsCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234c",
            };

            _assert.True(packageRef.IsStaticReference);
        }
        
        public void TestTooLongGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234cd",
            };

            _assert.False(packageRef.IsStaticReference);
        }
        
        public void TestTooShortGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234",
            };

            _assert.False(packageRef.IsStaticReference);
        }
        
        public void TestInvalidCharacterGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234h",
            };

            _assert.False(packageRef.IsStaticReference);
        }
        
        public void TestObviouslyNotGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "v3",
            };

            _assert.False(packageRef.IsStaticReference);
        }
    }
}

