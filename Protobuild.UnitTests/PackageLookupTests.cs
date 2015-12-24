using System;
using Prototest.Library.Version1;
using System.Collections.Generic;

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

            string sourceUri, sourceFormat, type;
            IPackageTransformer transformer;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;

            packageLookup.Lookup("local-template://C:\\Some\\Path", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);
            
            _assert.Equal("C:\\Some\\Path", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_DIRECTORY, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);
        }

        public void LocalGitTemplateSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            string sourceUri, sourceFormat, type;
            IPackageTransformer transformer;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;

            packageLookup.Lookup("local-template-git://C:\\Some\\Path", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("C:\\Some\\Path", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_TEMPLATE, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);
        }

        public void GitSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            string sourceUri, sourceFormat, type;
            IPackageTransformer transformer;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;

            packageLookup.Lookup("local-git://C:\\Some\\Path", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("C:\\Some\\Path", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);

            packageLookup.Lookup("http-git://domain.org/git", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("http://domain.org/git", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);

            packageLookup.Lookup("https-git://domain.org/git", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("https://domain.org/git", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);
        }

        public void NuGetSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            string sourceUri, sourceFormat, type;
            IPackageTransformer transformer;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;

            packageLookup.Lookup("http-nuget://domain.org/git", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("http://domain.org/git", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.IsType<NuGetPackageTransformer>(transformer);

            packageLookup.Lookup("https-nuget://domain.org/git", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.Equal("https://domain.org/git", sourceUri);
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.IsType<NuGetPackageTransformer>(transformer);
        }

        public void ProtobuildSchemeResolvesToCorrectProtocol()
        {
            var packageLookup = GetPackageLookup();

            string sourceUri, sourceFormat, type;
            IPackageTransformer transformer;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;

            packageLookup.Lookup("http://protobuild.org/hach-que/TestEmptyPackage", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.True(string.IsNullOrWhiteSpace(sourceUri));
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);

            packageLookup.Lookup("https://protobuild.org/hach-que/TestEmptyPackage", "Windows", true, out sourceUri,
                out sourceFormat, out type, out downloadMap, out archiveTypeMap, out resolvedHash, out transformer);

            _assert.True(string.IsNullOrWhiteSpace(sourceUri));
            _assert.Equal(PackageManager.SOURCE_FORMAT_GIT, sourceFormat);
            _assert.Equal(PackageManager.PACKAGE_TYPE_LIBRARY, type);
            _assert.Equal(0, downloadMap.Count);
            _assert.Equal(0, archiveTypeMap.Count);
            _assert.Equal(0, resolvedHash.Count);
            _assert.Null(transformer);
        }
    }
}

