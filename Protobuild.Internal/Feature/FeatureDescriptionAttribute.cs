﻿using System;

namespace Protobuild
{
    public class FeatureDescriptionAttribute : Attribute
    {
        public FeatureDescriptionAttribute(string description, string[] functionalityDisabledIfNotPresent)
        {
            this.Description = description;
            this.FunctionalityDisabledIfNotPresent = functionalityDisabledIfNotPresent;
        }

        public string Description { get; private set; }

        public string[] FunctionalityDisabledIfNotPresent { get; private set; }
    }
}
