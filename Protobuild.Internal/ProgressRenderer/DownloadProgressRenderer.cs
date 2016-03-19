using System;

namespace Protobuild
{
    internal class DownloadProgressRenderer : BaseProgressRenderer
    {
        public void Update(int percentage, long kbReceived)
        {
            if (this.OutputAllowed)
            {
                Output("Downloading package; " + percentage + "% complete (" + kbReceived + "kb received)");
            }

            base.Update();
        }
    }
}

