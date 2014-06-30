namespace Protobuild
{
    using System;

    [Serializable]
    public class GenerateSolutionFolders
    {
        public string Action { get; set; }
        public bool? SkipLast { get; set; }

        public GenerateSolutionFolders()
        {
            Action = string.Empty;
            SkipLast = false;
        }
    }
}

