using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface INuGetRepositoriesConfigGenerator
    {
        void Generate(
            string solutionPath,
            IEnumerable<string> repositoryPaths);
    }
}

