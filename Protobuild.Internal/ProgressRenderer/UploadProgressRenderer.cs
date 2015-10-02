using System;

namespace Protobuild
{
    public class UploadProgressRenderer : BaseProgressRenderer
    {
        public void Update(int percentage, long kbUploaded)
        {
            if (this.OutputAllowed)
            {
                Output("Uploading package; " + percentage + "% complete (" + kbUploaded + "kb sent)");
            }

            base.Update();
        }
    }
}

