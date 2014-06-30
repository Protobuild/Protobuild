namespace Protobuild
{
    using System;

    [Serializable]
    public class GenerateSolutionFolders
    {
        public string Action { get; set; }
        public bool? SkipLastFolder { get; set; }

        public GenerateSolutionFolders()
        {
            Action = string.Empty;
            SkipLastFolder = false;
        }
    }
}

