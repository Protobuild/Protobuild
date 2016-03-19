using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Protobuild
{
    internal class GenerationFunctionsProvider : IGenerationFunctionsProvider
    {
        public string ConvertGenerationFunctionsToXSLT(string prefix, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var begin = input.IndexOf("// **begin**", StringComparison.InvariantCulture) + "// **begin**".Length;
            var end = input.IndexOf("// **end**", StringComparison.InvariantCulture);

            var code = input.Substring(begin, end - begin);

            // Read the assembly and using comments at the start.
            var assemblies = new List<string>();
            var usings = new List<string>();
            foreach (Match assembly in Regex.Matches(input, "^// assembly (.*)$", RegexOptions.Multiline))
            {
                assemblies.Add(assembly.Groups[1].Value.Trim());
            }
            foreach (Match @using in Regex.Matches(input, "^// using (.*)$", RegexOptions.Multiline))
            {
                usings.Add(@using.Groups[1].Value.Trim());
            }

            return @"
  <msxsl:script language=""C#"" implements-prefix=""" + prefix + @""">
" + assemblies.Select(x => "<msxsl:assembly name=\"" + x + "\" />\r\n").Aggregate((a, b) => a + " " + b) +
  usings.Select(x => "<msxsl:using namespace=\"" + x + "\" />\r\n").Aggregate((a, b) => a + " " + b) + @"
<![CDATA[
" + code + @"
]]>
</msxsl:script>";
        }
    }
}