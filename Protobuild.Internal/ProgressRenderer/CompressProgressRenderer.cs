using System;

namespace Protobuild
{
    public class CompressProgressRenderer : BaseProgressRenderer, LZMA.ICodeProgress
    {
        private readonly long m_InLength;

        public CompressProgressRenderer(long inLength)
        {
            this.m_InLength = inLength;
        }

        public void SetProgress(long inSize, long outSize)
        {
            if (this.OutputAllowed)
            {
                var progress = (int)((inSize / (double)this.m_InLength) * 100);
                var ratio = (int)((outSize / (double)inSize) * 100);
                Console.Write("\rCompressing package; " + progress + "% complete (" + (inSize / 1024) + "kb compressed to " + (outSize / 1024) + "kb, " + ratio + "% of it's original size)");
            }

            base.Update();
        }
    }
}

