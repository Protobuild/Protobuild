using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protobuild
{
    internal interface IAutomatedBuildRuntime
    {
        object Parse(string text);
        int Execute(object handle);
    }
}
