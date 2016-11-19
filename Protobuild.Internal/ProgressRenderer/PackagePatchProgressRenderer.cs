using System;

namespace Protobuild
{
    internal class PackagePatchProgressRenderer : BaseProgressRenderer
    {
        private readonly long _total;

        public PackagePatchProgressRenderer(long total)
        {
            _total = total;
        }

        public void SetProgress(long current)
        {
            if (OutputAllowed)
            {
                var progress = (int)((current / (double)_total) * 100);
                Output("Patching package file to include Git version information; " + progress + "% complete (" + current + " of " + _total + " files)");
            }

            Update();
        }
    }
}

