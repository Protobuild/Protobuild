namespace Protobuild.Services
{
    using System.Collections.Generic;
    using System.Xml;

    public class Service
    {
        public Service()
        {
            this.AddDefines = new List<string>();
            this.RemoveDefines = new List<string>();
            this.AddReferences = new List<string>();
            this.Requires = new List<string>();
            this.Recommends = new List<string>();
            this.Conflicts = new List<string>();
        }

        public string ProjectName { get; set; }

        public string ServiceName { get; set; }

        public XmlElement Declaration { get; set; }

        public string FullName
        {
            get
            {
                if (this.ServiceName == null)
                {
                    return this.ProjectName;
                }

                return this.ProjectName + "/" + this.ServiceName;
            }
        }

        public List<string> AddDefines { get; set; }

        public List<string> RemoveDefines { get; set; }

        public List<string> AddReferences { get; set; }

        public bool DefaultForRoot { get; set; }

        public List<string> Requires { get; set; }

        public List<string> Recommends { get; set; }

        public List<string> Conflicts { get; set; }

        public ServiceDesiredLevel DesiredLevel { get; set; }

        public bool InfersReference { get; set; }
    }
}