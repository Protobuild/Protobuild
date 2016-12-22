namespace PROJECT_SAFE_NAME
{
    using Ninject;

    using Protogame;

    public class PROJECT_SAFE_NAMEGame : CoreGame<PROJECT_SAFE_NAMEWorld, Default2DWorldManager>
    {
        public PROJECT_SAFE_NAMEGame(StandardKernel kernel)
            : base(kernel)
        {
        }
    }
}
