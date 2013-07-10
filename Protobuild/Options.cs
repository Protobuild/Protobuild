using System;
using System.Collections.Generic;

namespace Protobuild
{
    public class Options
    {
        private Dictionary<string, Action> m_Actions = new Dictionary<string, Action>();
        
        public void Parse(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-", StringComparison.InvariantCulture) ||
                    arg.StartsWith("/", StringComparison.InvariantCulture))
                {
                    var realArg = arg.TrimStart('-').TrimStart('/').ToLower();
                    if (this.m_Actions.ContainsKey(realArg))
                    {
                        this.m_Actions[realArg]();
                    }
                }
            }
        }
        
        public Action this[string key]
        {
            get { return this.m_Actions[key.ToLower()]; }
            set { this.m_Actions[key.ToLower()] = value; }
        }
    }
}

