using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject.Inspector.UI;

namespace myProject.Inspector
{
    // Cola o inspector na engine: um Renderer do Monocle.
    // Desenha em resolucao de JANELA (nao os 320x180 do jogo) num passe proprio —
    // na escala do jogo a UI ficaria ilegivel.
    public sealed class InspectorRenderer : Renderer
    {
        public readonly InspectorPanel Panel = new InspectorPanel();
        public Keys ToggleKey = Keys.F1;

        private bool hooked;

        public bool Enabled
        {
            get => Panel.Visible;
            set => Panel.Visible = value;
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);
            HookTextInput();

            if (MInput.Keyboard.Pressed(ToggleKey))
            {
                Panel.Visible = !Panel.Visible;
                if (!Panel.Visible)
                    Panel.Gui.ClearFocus();
            }

            // O MonoGame esconde o cursor por padrao (IsMouseVisible = false):
            // sem isso nao da p/ mirar nos campos nem clicar nas entidades.
            if (Engine.Instance != null)
                Engine.Instance.IsMouseVisible = Panel.Visible;
            if (!Panel.Visible)
                return;

            Panel.Update(scene);
            TryPick(scene);
        }

        // O evento TextInput da janela trata shift/acentos/repeticao nativamente.
        private void HookTextInput()
        {
            if (hooked || Engine.Instance == null)
                return;
            Engine.Instance.Window.TextInput += (s, e) => Panel.Gui.OnTextInput(e.Character);
            hooked = true;
        }

        // Clique fora do painel seleciona a entidade sob o cursor.
        private void TryPick(Scene scene)
        {
            var mouse = MInput.Mouse;
            if (!mouse.PressedLeftButton)
                return;
            var screen = new Point(mouse.CurrentState.X, mouse.CurrentState.Y);
            var panelRect = new Rectangle(Engine.ViewWidth - Panel.Width, 0, Panel.Width, Engine.ViewHeight);
            if (panelRect.Contains(screen))
                return;

            Vector2 world = ScreenToWorld(scene, screen);
            var picked = Selection.PickAt(scene, world);
            if (picked != null)
                Panel.Selection.Select(picked);
        }

        private static Vector2 ScreenToWorld(Scene scene, Point screen)
        {
            // janela -> viewport -> resolucao interna -> mundo (via camera do Level)
            var vp = Engine.Viewport;
            float sx = (screen.X - vp.X) / (float)vp.Width * Engine.Width;
            float sy = (screen.Y - vp.Y) / (float)vp.Height * Engine.Height;
            var local = new Vector2(sx, sy);
            if (scene is Level level)
                return Vector2.Transform(local, Matrix.Invert(level.Camera.Matrix));
            return local;
        }

        public override void Render(Scene scene)
        {
            if (!Panel.Visible)
                return;
            GuiFont.Load(Engine.Instance.GraphicsDevice);

            // passe proprio, sem a matriz de escala do jogo
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
                Matrix.Identity);

            DrawSelectionMarker(scene);
            Panel.Render(scene, Draw.SpriteBatch, Engine.ViewWidth, Engine.ViewHeight);

            Draw.SpriteBatch.End();
        }

        // Contorno da entidade selecionada, desenhado em coordenadas de janela.
        private void DrawSelectionMarker(Scene scene)
        {
            if (!(Panel.Selection.Current is Entity e) || e.Scene == null)
                return;

            var vp = Engine.Viewport;
            float scaleX = vp.Width / (float)Engine.Width;
            float scaleY = vp.Height / (float)Engine.Height;
            Vector2 camOffset = (scene is Level lvl) ? lvl.Camera.Position : Vector2.Zero;

            Vector2 topLeft = e.Collider != null
                ? new Vector2(e.Collider.AbsoluteLeft, e.Collider.AbsoluteTop)
                : e.Position - Vector2.One * 4f;
            float w = e.Collider != null ? e.Collider.Width : 8f;
            float h = e.Collider != null ? e.Collider.Height : 8f;

            var r = new Rectangle(
                (int)((topLeft.X - camOffset.X) * scaleX + vp.X),
                (int)((topLeft.Y - camOffset.Y) * scaleY + vp.Y),
                Math.Max(2, (int)(w * scaleX)), Math.Max(2, (int)(h * scaleY)));

            var c = GuiStyle.Accent;
            Draw.Rect(r.X, r.Y, r.Width, 2, c);
            Draw.Rect(r.X, r.Bottom - 2, r.Width, 2, c);
            Draw.Rect(r.X, r.Y, 2, r.Height, c);
            Draw.Rect(r.Right - 2, r.Y, 2, r.Height, c);
        }
    }
}
