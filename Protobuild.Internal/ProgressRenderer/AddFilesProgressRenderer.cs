namespace Protobuild
{
    internal class AddFilesProgressRenderer : BaseProgressRenderer
    {
        private readonly long _total;

        public AddFilesProgressRenderer(long total)
        {
            _total = total;
        }

        public void SetProgress(long current)
        {
            if (OutputAllowed)
            {
                var progress = (int)((current / (double)_total) * 100);
                Output("Adding files to package; " + progress + "% complete (" + current + " of " + _total + " files)");
            }

            Update();
        }
    }
}

