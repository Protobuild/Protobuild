namespace {PROJECT_SAFE_NAME}
{
    using System.Collections.Generic;

    using Microsoft.Xna.Framework;

    using Protogame;

    public class {PROJECT_SAFE_NAME}World : IWorld
    {
        private readonly I2DRenderUtilities m_2DRenderUtilities;

        private readonly IAssetManager m_AssetManager;

        private readonly FontAsset m_DefaultFont;
        
        public {PROJECT_SAFE_NAME}World(
            I2DRenderUtilities twoDRenderUtilities,
            IAssetManagerProvider assetManagerProvider)
        {
            this.Entities = new List<IEntity>();

            this.m_2DRenderUtilities = twoDRenderUtilities;
            this.m_AssetManager = assetManagerProvider.GetAssetManager();
            this.m_DefaultFont = this.m_AssetManager.Get<FontAsset>("font.Default");
        }

        public IList<IEntity> Entities { get; private set; }

        public void Dispose()
        {
        }

        public void RenderAbove(IGameContext gameContext, IRenderContext renderContext)
        {
        }

        public void RenderBelow(IGameContext gameContext, IRenderContext renderContext)
        {
            gameContext.Graphics.GraphicsDevice.Clear(Color.Purple);

            this.m_2DRenderUtilities.RenderText(
                renderContext,
                new Vector2(10, 10),
                "Hello {PROJECT_NAME}!",
                this.m_DefaultFont);

            this.m_2DRenderUtilities.RenderText(
                renderContext,
                new Vector2(10, 30),
                "Running at " + gameContext.FPS + " FPS; " + gameContext.FrameCount + " frames counted so far",
                this.m_DefaultFont);
        }

        public void Update(IGameContext gameContext, IUpdateContext updateContext)
        {
        }
    }
}
