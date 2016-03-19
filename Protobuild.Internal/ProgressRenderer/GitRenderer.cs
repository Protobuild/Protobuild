using System;

namespace Protobuild
{
    internal class GitRenderer : BaseProgressRenderer
    {
        private string _lastLine;

        public void Update(string line)
        {
            if (this.OutputAllowed)
            {
                if (line == null)
                {
                    return;
                }

                line = line.Trim();
                if (_lastLine != null)
                {
                    Output(new string(' ', _lastLine.Length));
                }
                Output(line);
                _lastLine = line;
            }

            base.Update();
        }
    }
}

