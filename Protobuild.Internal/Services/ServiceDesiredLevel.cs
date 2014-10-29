namespace Protobuild
{
    public enum ServiceDesiredLevel
    {
        /// <summary>
        /// The service is disabled explicitly; if a required service conflicts
        /// with a disabled service, Protobuild will exit with an error.
        /// </summary>
        Disabled,

        /// <summary>
        /// The service is not used or required by any service, and it is
        /// not explicitly enabled by the user.
        /// </summary>
        Unused,

        /// <summary>
        /// The service is enabled by default; it is either the default
        /// service, or it has DefaultForRoot set to true.
        /// </summary>
        Default,

        /// <summary>
        /// The service is recommended for all platforms it is allowed for.  The
        /// service will be automatically disabled if having it enabled would cause
        /// a conflict.
        /// </summary>
        Recommended,

        /// <summary>
        /// The service is required for all platforms it is allowed for.  Protobuild
        /// will exit with an error if there is a conflict with this service.
        /// </summary>
        Required
    }
}

