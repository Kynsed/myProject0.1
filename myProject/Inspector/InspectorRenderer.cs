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

        /// Marcador da entidade selecionada. Desligue se poluir a visualizacao.
        public bool ShowSelectionMarker = true;

        public override void Render(Scene scene)
        {
            if (!Panel.Visible)
                return;
            GuiFont.Load(Engine.Instance.GraphicsDevice);

            // 1) marcador no espaco do MUNDO (mesma matriz dos hitboxes): fica alinhado ao
            //    pixel com o collider, em vez de flutuar por cima em coordenadas de janela
            if (ShowSelectionMarker)
            {
                Camera cam = (scene is Level lvl) ? lvl.Camera : null;
                Matrix m = cam != null ? cam.Matrix * Engine.ScreenMatrix : Engine.ScreenMatrix;
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, m);
                DrawSelectionMarker();
                Draw.SpriteBatch.End();
            }

            // 2) painel em resolucao de janela, sem a escala do jogo
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
                Matrix.Identity);
            Panel.Render(scene, Draw.SpriteBatch, Engine.ViewWidth, Engine.ViewHeight);
            Draw.SpriteBatch.End();
        }

        // Cantos em "L" (estilo alca de selecao de editor) em vez de uma caixa cheia:
        // marca a entidade sem esconder o hitbox nem o que estiver atras.
        private void DrawSelectionMarker()
        {
            if (!(Panel.Selection.Current is Entity e) || e.Scene == null)
                return;

            float x, yTop, w, h;
            if (e.Collider != null)
            {
                x = e.Collider.AbsoluteLeft;
                yTop = e.Collider.AbsoluteTop;
                w = e.Collider.Width;
                h = e.Collider.Height;
            }
            else
            {
                x = e.X - 4f; yTop = e.Y - 4f; w = h = 8f;
            }
            // folga de 1px p/ o contorno nao cobrir a borda do proprio hitbox
            x -= 1f; yTop -= 1f; w += 2f; h += 2f;

            var c = GuiStyle.Accent;
            float arm = Math.Max(2f, Math.Min(w, h) * 0.35f); // tamanho do "L"
            // superior esquerdo / direito
            Draw.Rect(x, yTop, arm, 1f, c);
            Draw.Rect(x, yTop, 1f, arm, c);
            Draw.Rect(x + w - arm, yTop, arm, 1f, c);
            Draw.Rect(x + w - 1f, yTop, 1f, arm, c);
            // inferior esquerdo / direito
            Draw.Rect(x, yTop + h - 1f, arm, 1f, c);
            Draw.Rect(x, yTop + h - arm, 1f, arm, c);
            Draw.Rect(x + w - arm, yTop + h - 1f, arm, 1f, c);
            Draw.Rect(x + w - 1f, yTop + h - arm, 1f, arm, c);
        }
    }
}
