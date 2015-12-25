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

            var metadata = packageLookup.Lookup(new PackageRequestRef("local-template://C:\\Some\\Path", "master", "Windows", false));

            _assert.IsType<FolderPackageMetadata>(metadata);

            var folderMetadata = (FolderPackageMetadata) metadata;

            _assert.Equal("C:\\Some\\Path", folderMetadata.Folder);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, folderMetadata.PackageType);
        }

        public void LocalGitTemplateSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(new PackageRequestRef("local-template-git://C:\\Some\\Path", "master", "Windows", false));

            _assert.IsType<GitPackageMetadata>(metadata);

            var gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("C:\\Some\\Path", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);
        }

        public void GitSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(new PackageRequestRef("local-git://C:\\Some\\Path", "master", "Windows", false));

            _assert.IsType<GitPackageMetadata>(metadata);

            var gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("C:\\Some\\Path", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);

            metadata = packageLookup.Lookup(new PackageRequestRef("http-git://domain.org/git", "master", "Windows", false));

            _assert.IsType<GitPackageMetadata>(metadata);

            gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("http://domain.org/git", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);

            metadata = packageLookup.Lookup(new PackageRequestRef("https-git://domain.org/git", "master", "Windows", false));

            _assert.IsType<GitPackageMetadata>(metadata);

            gitMetadata = (GitPackageMetadata)metadata;

            _assert.Equal("https://domain.org/git", gitMetadata.CloneURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, gitMetadata.PackageType);
            _assert.Equal("master", gitMetadata.GitRef);
        }

        public void NuGetSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(new PackageRequestRef("http-nuget://domain.org/git", "master", "Windows", false));

            _assert.IsType<NuGetPackageMetadata>(metadata);

            var nugetMetadata = (NuGetPackageMetadata)metadata;

            _assert.Equal("http://domain.org/git", nugetMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, nugetMetadata.PackageType);

            metadata = packageLookup.Lookup(new PackageRequestRef("https-nuget://domain.org/git", "master", "Windows", false));

            _assert.IsType<NuGetPackageMetadata>(metadata);

            nugetMetadata = (NuGetPackageMetadata)metadata;

            _assert.Equal("https://domain.org/git", nugetMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, nugetMetadata.PackageType);
        }

        public void ProtobuildSchemeResolvesToCorrectProtocol()
        {
            // TODO: Make these tests not depend on a network connection.

            var packageLookup = GetPackageLookup();

            var metadata = packageLookup.Lookup(new PackageRequestRef("http://protobuild.org/hach-que/TestEmptyPackage", "master", "Windows", false));

            _assert.IsType<ProtobuildPackageMetadata>(metadata);

            var protobuildMetadata = (ProtobuildPackageMetadata)metadata;

            _assert.Equal("http://protobuild.org/hach-que/TestEmptyPackage", protobuildMetadata.ReferenceURI);
            _assert.Equal("50a2a4e9b12739b20932d152e211239db88cbb49", protobuildMetadata.GitCommit);
            _assert.Equal(PackageManager.ARCHIVE_FORMAT_TAR_LZMA, protobuildMetadata.BinaryFormat);
            _assert.Equal("https://storage.googleapis.com/protobuild-packages/6011817390243840.pkg", protobuildMetadata.BinaryURI);
            _assert.Null(protobuildMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, protobuildMetadata.PackageType);
            _assert.Equal("Windows", protobuildMetadata.Platform);
            _assert.Null(protobuildMetadata.Transformer);

            metadata = packageLookup.Lookup(new PackageRequestRef("https://protobuild.org/hach-que/TestEmptyPackage", "master", "Windows", false));

            _assert.IsType<ProtobuildPackageMetadata>(metadata);

            protobuildMetadata = (ProtobuildPackageMetadata)metadata;

            _assert.Equal("https://protobuild.org/hach-que/TestEmptyPackage", protobuildMetadata.ReferenceURI);
            _assert.Equal("50a2a4e9b12739b20932d152e211239db88cbb49", protobuildMetadata.GitCommit);
            _assert.Equal(PackageManager.ARCHIVE_FORMAT_TAR_LZMA, protobuildMetadata.BinaryFormat);
            _assert.Equal("https://storage.googleapis.com/protobuild-packages/6011817390243840.pkg", protobuildMetadata.BinaryURI);
            _assert.Null(protobuildMetadata.SourceURI);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, protobuildMetadata.PackageType);
            _assert.Equal("Windows", protobuildMetadata.Platform);
            _assert.Null(protobuildMetadata.Transformer);
        }
    }
}

