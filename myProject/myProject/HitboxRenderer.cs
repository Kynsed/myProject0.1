using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace myProject
{
    // Desenha o collider de cada entidade (debug visual, sem sprites).
    // Renderiza pela camera do Level — e o Player.Update quem a move (lerp fiel do Celeste).
    public class HitboxRenderer : Renderer
    {
        private static readonly Camera fallback = new Camera();

        public override void Render(Scene scene)
        {
            Camera cam = (scene is Level lvl) ? lvl.Camera : fallback;
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, cam.Matrix * Engine.ScreenMatrix);
            foreach (Entity e in scene.Entities)
            {
                if (e.Collider == null)
                    continue;
                Color c = (e is Player) ? Color.Red : ((e is Solid) ? Color.LightGray : Color.Yellow);
                e.Collider.Render(cam, c);
            }
            Draw.SpriteBatch.End();
        }
    }
}
