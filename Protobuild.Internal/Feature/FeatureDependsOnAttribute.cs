using System;

namespace Protobuild
{
    internal class FeatureDependsOnAttribute : Attribute
    {
        public FeatureDependsOnAttribute(params Feature[] dependsOn)
        {
            this.DependsOn = dependsOn;
        }

        public Feature[] DependsOn { get; private set; }
    }
}

