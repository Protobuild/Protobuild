using System;
using System.Collections.Generic;

namespace Protobuild
{
    using System.Linq;

    public class Options
    {
        private Dictionary<string, Action<string[]>> m_Actions = new Dictionary<string, Action<string[]>>();
        
        public void Parse(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("-", StringComparison.InvariantCulture) ||
                    arg.StartsWith("/", StringComparison.InvariantCulture))
                {
                    var realArg = arg.TrimStart('-').TrimStart('/').ToLower();
                    if (!this.m_Actions.Keys.Any(x => x.StartsWith(realArg + "@") || string.Compare(x, realArg, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        throw new InvalidOperationException("Unknown argument '" + arg + "'");
                    }

                    var takeArgs = this.GetParameterCountForArgument(realArg);
                    var actionArgs = new List<string>();
                    if (takeArgs > 0)
                    {
                        for (var v = 0; v < takeArgs && (i + 1) < args.Length; v++)
                        {
                            i++;

                            // We can't break on the / character, as this is used in paths
                            // on Linux and Mac OS.
                            if (args[i].StartsWith("-", StringComparison.InvariantCulture))
                                break;

                            actionArgs.Add(args[i]);
                        }
                    }
                    while (actionArgs.Count < takeArgs)
                        actionArgs.Add(null);
                    if (this.m_Actions.ContainsKey(realArg))
                        this.m_Actions[realArg](actionArgs.ToArray());
                    if (this.m_Actions.ContainsKey(realArg + "@" + takeArgs))
                        this.m_Actions[realArg + "@" + takeArgs](actionArgs.ToArray());
                }
            }
        }
        
        private int GetParameterCountForArgument(string arg)
        {
            foreach (string key in this.m_Actions.Keys)
                if (key.StartsWith(arg + "@", StringComparison.Ordinal))
                    return Convert.ToInt32(key.Split(new[]{'@'}, 2)[1]);
            return 0;
        }
        
        public Action<string[]> this[string key]
        {
            get { return this.m_Actions[key.ToLower()]; }
            set { this.m_Actions[key.ToLower()] = value; }
        }
    }
}

