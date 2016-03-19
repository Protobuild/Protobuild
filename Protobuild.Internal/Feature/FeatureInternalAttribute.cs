using System;

namespace Protobuild
{
    internal class FeatureInternalAttribute : Attribute
    {
        public FeatureInternalAttribute(string internalId)
        {
            this.InternalId = internalId;
        }

        public string InternalId { get; private set; }
    }
}

