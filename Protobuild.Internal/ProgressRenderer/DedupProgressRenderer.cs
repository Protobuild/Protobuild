using System;

namespace Protobuild
{
    internal class DedupProgressRenderer : BaseProgressRenderer
    {
        private readonly long m_Total;

        public DedupProgressRenderer(long total)
        {
            this.m_Total = total;
        }

        public void SetProgress(long current)
        {
            if (this.OutputAllowed)
            {
                var progress = (int)((current / (double)this.m_Total) * 100);
                Output("Deduplicating files in package; " + progress + "% complete (" + current + " of " + this.m_Total + " files)");
            }

            base.Update();
        }
    }
}

