using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal interface INuGetRepositoriesConfigGenerator
    {
        void Generate(
            string solutionPath,
            IEnumerable<string> repositoryPaths);
    }
}

