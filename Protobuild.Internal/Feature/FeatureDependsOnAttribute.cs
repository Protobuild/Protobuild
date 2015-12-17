using System;

namespace Protobuild
{
    public class FeatureDependsOnAttribute : Attribute
    {
        public FeatureDependsOnAttribute(params Feature[] dependsOn)
        {
            this.DependsOn = dependsOn;
        }

        public Feature[] DependsOn { get; private set; }
    }
}

