using System;
using Xunit;

namespace Protobuild.UnitTests
{
    public class PackageRefTests
    {
        [Fact]
        public void TestGitSHA1IsCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234c",
            };

            Assert.True(packageRef.IsCommitReference);
        }

        [Fact]
        public void TestTooLongGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234cd",
            };

            Assert.False(packageRef.IsCommitReference);
        }

        [Fact]
        public void TestTooShortGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234",
            };

            Assert.False(packageRef.IsCommitReference);
        }

        [Fact]
        public void TestInvalidCharacterGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "096932a9ea478c049802f5e2eb6538e37336234h",
            };

            Assert.False(packageRef.IsCommitReference);
        }

        [Fact]
        public void TestObviouslyNotGitSHA1IsNotCommitReference()
        {
            var packageRef = new PackageRef
            {
                GitRef = "v3",
            };

            Assert.False(packageRef.IsCommitReference);
        }
    }
}

