using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace myProject
{
    // Port fiel (celeste_source/Celeste/GameplayRenderer.cs). O renderer e DONO da camera;
    // o Level aponta p/ ela (LevelLoader: Level.Camera = Level.GameplayRenderer.Camera).
    // Begin/End sao estaticos porque entidades que trocam de shader no meio do render
    // fecham e reabrem o batch (LavaRect, LightningRenderer, FinalBossBeam no Celeste).
    //
    // NOTE: unica divergencia — o Celeste desenha num GameplayBuffers e o Level compoe
    // depois, entao usa Camera.Matrix puro. Sem render targets aqui, vai direto no
    // backbuffer e precisa do Engine.ScreenMatrix.
    public class GameplayRenderer : Renderer
    {
        public Camera Camera;

        private static GameplayRenderer instance;

        public GameplayRenderer()
        {
            instance = this;
            Camera = new Camera(320, 180);
        }

        public static void Begin()
        {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null,
                instance.Camera.Matrix * Engine.ScreenMatrix);
        }

        public static void End()
        {
            Draw.SpriteBatch.End();
        }

        public override void Render(Scene scene)
        {
            Begin();
            scene.Entities.RenderExcept(Tags.HUD);
            if (Engine.Commands.Open)
                scene.Entities.DebugRender(Camera);
            End();
        }
    }
}
