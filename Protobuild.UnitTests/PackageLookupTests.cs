using System;
using Prototest.Library.Version1;
using System.Collections.Generic;
using Protobuild.Internal;

namespace Protobuild.UnitTests
{
    public class PackageLookupTests
    {
        private readonly IAssert _assert;

        public PackageLookupTests(IAssert assert)
        {
            _assert = assert;
        }

        private IPackageLookup GetPackageLookup()
        {
            var kernel = new LightweightKernel();
            kernel.BindAll();
            return kernel.Get<IPackageLookup>();
        }

        public void LocalTemplateSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("local-template://C:\\Some\\Path", "master", "Windows", true, false));

            _assert.IsType<FolderPackageMetadata>(metadata);

            var folderMetadata = (FolderPackageMetadata) metadata;

            _assert.Equal("C:\\Some\\Path", folderMetadata.Folder);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, folderMetadata.PackageType);
        }

        public void LocalGitTemplateSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("local-template-git://C:\\Some\\Path", "master", "Windows", true, false));

            _assert.IsType<GitPackageMetadata>(metadata);

            var gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("C:\\Some\\Path", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);
        }

        public void GitSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("local-git://C:\\Some\\Path", "master", "Windows", true, false));

            _assert.IsType<GitPackageMetadata>(metadata);

            var gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("C:\\Some\\Path", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);

            metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("http-git://domain.org/git", "master", "Windows", true, false));

            _assert.IsType<GitPackageMetadata>(metadata);

            gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("http://domain.org/git", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);

            metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("https-git://domain.org/git", "master", "Windows", true, false));

            _assert.IsType<GitPackageMetadata>(metadata);

            gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("https://domain.org/git", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);
        }

        public void NuGetSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("http-nuget://domain.org/git", "master", "Windows", true, false));

            _assert.IsType<TransformedPackageMetadata>(metadata);

            var nugetMetadata = (TransformedPackageMetadata)metadata;

            _assert.Equal("http://domain.org/git", nugetMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, nugetMetadata.PackageType);
            _assert.IsType<NuGetPackageTransformer>(nugetMetadata.Transformer);
            _assert.Equal("master", nugetMetadata.GitRef);
            _assert.Equal("Windows", nugetMetadata.Platform);

            metadata = packageLookup.Lookup(Environment.CurrentDirectory, new PackageRequestRef("https-nuget://domain.org/git", "master", "Windows", true, false));

            _assert.IsType<TransformedPackageMetadata>(metadata);

            nugetMetadata = (TransformedPackageMetadata)metadata;

            _assert.Equal("https://domain.org/git", nugetMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, nugetMetadata.PackageType);
            _assert.IsType<NuGetPackageTransformer>(nugetMetadata.Transformer);
            _assert.Equal("master", nugetMetadata.GitRef);
            _assert.Equal("Windows", nugetMetadata.Platform);
        }
    }
}

