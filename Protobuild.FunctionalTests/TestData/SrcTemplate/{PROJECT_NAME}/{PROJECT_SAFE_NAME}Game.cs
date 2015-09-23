namespace {PROJECT_SAFE_NAME}
{
    using Ninject;

    using Protogame;

    public class {PROJECT_SAFE_NAME}Game : CoreGame<{PROJECT_SAFE_NAME}World, Default2DWorldManager>
    {
        public {PROJECT_SAFE_NAME}Game(StandardKernel kernel)
            : base(kernel)
        {
        }
    }
}
