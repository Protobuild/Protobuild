using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Protobuild
{
    public class GenerationFunctionsProvider : IGenerationFunctionsProvider
    {
        public string ConvertGenerationFunctionsToXSLT(string prefix, string input)
        {
            var begin = input.IndexOf("// **begin**", StringComparison.InvariantCulture) + "// **begin**".Length;
            var end = input.IndexOf("// **end**", StringComparison.InvariantCulture);

            var code = input.Substring(begin, input.Length - end - begin);

            // Read the assembly and using comments at the start.
            var assemblies = new List<string>();
            var usings = new List<string>();
            foreach (Match assembly in Regex.Matches(input, "^// assembly (.*)$", RegexOptions.Multiline))
            {
                assemblies.Add(assembly.Value);
            }
            foreach (Match @using in Regex.Matches(input, "^// using (.*)$", RegexOptions.Multiline))
            {
                usings.Add(@using.Value);
            }

            return @"
  <msxsl:script language=""C#"" implements-prefix=""" + prefix + @""">
" + assemblies.Select(x => "<msxsl:assembly name=\"" + x + "\" />") +
  usings.Select(x => "<msxsl:using namespace=\"" + x + "\" />") + @"
<![CDATA[
" + code + @"
]]>
</msxsl:script>";
        }
    }
}