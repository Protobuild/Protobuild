using System;

namespace Protobuild
{
    public class DownloadProgressRenderer : BaseProgressRenderer
    {
        public void Update(int percentage, long kbReceived)
        {
            if (this.OutputAllowed)
            {
                Console.Write("\rDownloading package; " + percentage + "% complete (" + kbReceived + "kb received)");
            }

            base.Update();
        }
    }
}

