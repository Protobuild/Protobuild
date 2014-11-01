//-----------------------------------------------------------------------
// <copyright file="Options.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A simple command line option parser.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// The list of registered actions.
        /// </summary>
        private Dictionary<string, Action<string[]>> actions = new Dictionary<string, Action<string[]>>();

        /// <summary>
        /// Gets or sets the action to take for various command line arguments.
        /// </summary>
        /// <param name="key">The command line option.</param>
        /// <returns>The action to take.</returns>
        public Action<string[]> this[string key]
        {
            get { return this.actions[key.ToLower()]; }
            set { this.actions[key.ToLower()] = value; }
        }

        /// <summary>
        /// Parse the command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public void Parse(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("-", StringComparison.InvariantCulture) ||
                    arg.StartsWith("/", StringComparison.InvariantCulture))
                {
                    var realArg = arg.TrimStart('-').TrimStart('/').ToLower();
                    if (!this.actions.Keys.Any(x => x.StartsWith(realArg + "@") || string.Compare(x, realArg, StringComparison.OrdinalIgnoreCase) == 0))
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
                            {
                                // Untake this option so that when we move back to the outer
                                // for loop we can see it as an argument again.
                                i--;

                                break;
                            }

                            actionArgs.Add(args[i]);
                        }
                    }

                    while (actionArgs.Count < takeArgs)
                    {
                        actionArgs.Add(null);
                    }

                    if (this.actions.ContainsKey(realArg))
                    {
                        this.actions[realArg](actionArgs.ToArray());
                    }

                    if (this.actions.ContainsKey(realArg + "@" + takeArgs))
                    {
                        this.actions[realArg + "@" + takeArgs](actionArgs.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the parameter count for the specified argument key.
        /// </summary>
        /// <returns>The parameter count for the specified argument key.</returns>
        /// <param name="arg">The argument key.</param>
        private int GetParameterCountForArgument(string arg)
        {
            foreach (string key in this.actions.Keys)
            {
                if (key.StartsWith(arg + "@", StringComparison.Ordinal))
                {
                    return Convert.ToInt32(key.Split(new[] { '@' }, 2)[1]);
                }
            }

            return 0;
        }
    }
}
