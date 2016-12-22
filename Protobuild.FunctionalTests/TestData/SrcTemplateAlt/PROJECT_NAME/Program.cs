#if PLATFORM_WINDOWS || PLATFORM_MACOS || PLATFORM_LINUX || PLATFORM_WEB

namespace {PROJECT_SAFE_NAME}
{
    using Ninject;

    using Protogame;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var kernel = new StandardKernel();
            kernel.Load<Protogame2DIoCModule>();
            kernel.Load<ProtogameAssetIoCModule>();
            AssetManagerClient.AcceptArgumentsAndSetup<GameAssetManagerProvider>(kernel, args);

            using (var game = new {PROJECT_SAFE_NAME}Game(kernel))
            {
                game.Run();
            }
        }
    }
}

#endif
