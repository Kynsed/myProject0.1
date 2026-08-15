using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject.Inspector.UI
{
    // IMGUI minimo desenhado no SpriteBatch. Modo imediato combina com um inspector:
    // o estado vive nos objetos do jogo, a UI apenas reflete e edita a cada frame.
    // Identidade de widget = hash estavel (nome do membro + caminho), nao indice de ordem.
    public sealed class Gui
    {
        // ---- entrada do frame ----
        public Point Mouse;
        public bool MouseDown, MousePressed, MouseReleased, MouseRightPressed;
        public int WheelDelta;
        public bool CtrlDown, ShiftDown;

        // ---- estado persistente entre frames ----
        private int hotId, activeId, focusId;
        private string editBuffer;
        private bool editInvalid;
        private float dragAccum;
        private Point dragOrigin;

        private SpriteBatch batch;
        private Rectangle clip;
        private readonly StringBuilder typed = new StringBuilder(32);

        public int FocusId => focusId;
        public bool HasKeyboardFocus => focusId != 0;
        public bool WantsMouse { get; private set; }

        /// Recebe caracteres do evento TextInput da janela (trata shift/repeat nativamente).
        public void OnTextInput(char c)
        {
            if (focusId == 0)
                return;
            if (c == '\b')
            {
                if (editBuffer != null && editBuffer.Length > 0)
                    editBuffer = editBuffer.Substring(0, editBuffer.Length - 1);
                return;
            }
            if (c == '\r' || c == '\n' || c == '\t' || c == 27)
                return; // tratados como teclas
            if (!char.IsControl(c))
                typed.Append(c);
        }

        public void BeginFrame(SpriteBatch spriteBatch, Rectangle panelRect)
        {
            batch = spriteBatch;
            clip = panelRect;
            hotId = 0;
            WantsMouse = panelRect.Contains(Mouse);

            var kb = MInput.Keyboard;
            CtrlDown = kb.Check(Keys.LeftControl) || kb.Check(Keys.RightControl);
            ShiftDown = kb.Check(Keys.LeftShift) || kb.Check(Keys.RightShift);

            if (focusId != 0 && typed.Length > 0)
            {
                editBuffer = (editBuffer ?? string.Empty) + typed.ToString();
                editInvalid = false;
            }
            typed.Clear();

            if (MousePressed && !WantsMouse)
                CancelEdit();
        }

        public void EndFrame()
        {
            if (MouseReleased)
                activeId = 0;
        }

        // ---- primitivas ----

        public void Rect(Rectangle r, Color c)
        {
            r = Clamp(r);
            if (r.Width > 0 && r.Height > 0)
                batch.Draw(Draw.Pixel.Texture.Texture, r, Draw.Pixel.ClipRect, c);
        }

        public void Border(Rectangle r, Color c, int thickness = 1)
        {
            Rect(new Rectangle(r.X, r.Y, r.Width, thickness), c);
            Rect(new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            Rect(new Rectangle(r.X, r.Y, thickness, r.Height), c);
            Rect(new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
        }

        public void Text(string s, int x, int y, Color c, int maxWidth = int.MaxValue)
        {
            if (y + GuiFont.GlyphHeight < clip.Top || y > clip.Bottom)
                return;
            if (maxWidth != int.MaxValue)
                s = GuiFont.Fit(s, maxWidth);
            GuiFont.Draw(batch, s, new Vector2(x, y), c);
        }

        public void TextRight(string s, int right, int y, Color c)
            => Text(s, right - GuiFont.Measure(s), y, c);

        private Rectangle Clamp(Rectangle r)
        {
            int l = Math.Max(r.Left, clip.Left), t = Math.Max(r.Top, clip.Top);
            int rr = Math.Min(r.Right, clip.Right), b = Math.Min(r.Bottom, clip.Bottom);
            return new Rectangle(l, t, Math.Max(0, rr - l), Math.Max(0, b - t));
        }

        public void SetClip(Rectangle r) => clip = r;
        public Rectangle GetClip() => clip;

        // ---- helpers de interacao ----

        public bool Hovered(Rectangle r) => clip.Contains(Mouse) && r.Contains(Mouse);

        public static int Id(string path) => path == null ? 0 : path.GetHashCode() | 1;

        // ---- widgets ----

        public bool Button(Rectangle r, string label, bool enabled = true, string tooltip = null)
        {
            bool hover = enabled && Hovered(r);
            if (hover)
                hotId = Id(label + r.X + r.Y);
            bool clicked = hover && MousePressed;

            Color bg = !enabled ? GuiStyle.FieldDisabled : (hover ? (MouseDown ? GuiStyle.ButtonActive : GuiStyle.ButtonHover) : GuiStyle.Button);
            Rect(r, bg);
            Border(r, GuiStyle.Border);
            int tw = GuiFont.Measure(label);
            Text(label, r.X + (r.Width - tw) / 2, r.Y + (r.Height - GuiFont.GlyphHeight) / 2 + 1,
                enabled ? GuiStyle.Text : GuiStyle.Locked, r.Width - 4);
            return clicked;
        }

        public bool Checkbox(Rectangle r, bool value, bool enabled)
        {
            var box = new Rectangle(r.X, r.Y + (r.Height - 12) / 2, 12, 12);
            bool hover = enabled && Hovered(box);
            Rect(box, enabled ? (hover ? GuiStyle.FieldHover : GuiStyle.Field) : GuiStyle.FieldDisabled);
            Border(box, GuiStyle.Border);
            if (value)
            {
                Rect(new Rectangle(box.X + 3, box.Y + 3, 6, 6),
                    enabled ? GuiStyle.Accent : GuiStyle.Locked);
            }
            return enabled && hover && MousePressed;
        }

        /// Foldout no estilo Unity: triangulo + titulo, a linha inteira e clicavel.
        public bool Foldout(Rectangle r, bool open, string label, Color bg)
        {
            bool hover = Hovered(r);
            Rect(r, hover ? GuiStyle.RowAlt : bg);
            int cx = r.X + 6, cy = r.Y + r.Height / 2;
            // triangulo desenhado com linhas de 1px (aponta p/ baixo quando aberto)
            for (int i = 0; i < 4; i++)
            {
                if (open)
                    Rect(new Rectangle(cx + i, cy - 2 + i, 7 - i * 2, 1), GuiStyle.Text);
                else
                    Rect(new Rectangle(cx + 1 + i, cy - 3 + i, 1, 7 - i * 2), GuiStyle.Text);
            }
            Text(label, r.X + 18, r.Y + (r.Height - GuiFont.GlyphHeight) / 2, GuiStyle.TextHeader, r.Width - 24);
            return hover && MousePressed;
        }

        /// Campo de texto editavel. Devolve true quando o valor e confirmado (Enter/foco perdido).
        public bool TextField(int id, Rectangle r, string current, bool enabled, out string result)
        {
            result = current;
            bool hover = enabled && Hovered(r);
            bool focused = focusId == id;

            if (hover && MousePressed && !focused)
            {
                CommitPending();
                focusId = id;
                editBuffer = current ?? string.Empty;
                editInvalid = false;
                focused = true;
            }

            Color bg = !enabled ? GuiStyle.FieldDisabled
                : focused ? GuiStyle.FieldFocus : (hover ? GuiStyle.FieldHover : GuiStyle.Field);
            Rect(r, bg);
            Border(r, editInvalid && focused ? GuiStyle.Invalid : (focused ? GuiStyle.Accent : GuiStyle.Border));

            string shown = focused ? editBuffer : (current ?? string.Empty);
            Text(shown, r.X + 4, r.Y + (r.Height - GuiFont.GlyphHeight) / 2 + 1,
                enabled ? GuiStyle.Text : GuiStyle.Locked, r.Width - 8);

            if (focused)
            {
                // cursor piscante (30 frames aceso, 30 apagado)
                if (Engine.FrameCounter / 30 % 2 == 0)
                {
                    int cx = r.X + 4 + GuiFont.Measure(GuiFont.Fit(shown, r.Width - 8));
                    Rect(new Rectangle(Math.Min(cx, r.Right - 5), r.Y + 3, 1, r.Height - 6), GuiStyle.Text);
                }
                var kb = MInput.Keyboard;
                if (kb.Pressed(Keys.Enter))
                {
                    result = editBuffer;
                    ClearFocus();
                    return true;
                }
                if (kb.Pressed(Keys.Escape))
                {
                    ClearFocus();
                    return false;
                }
                if (kb.Pressed(Keys.Back) && editBuffer != null && editBuffer.Length > 0)
                    editBuffer = editBuffer.Substring(0, editBuffer.Length - 1);
            }
            return false;
        }

        public void MarkInvalid(int id)
        {
            if (focusId == id)
                editInvalid = true;
        }

        /// Slider horizontal para [Range]. Devolve true enquanto arrastado.
        public bool Slider(int id, Rectangle r, float value, float min, float max, bool enabled, out float result)
        {
            result = value;
            float t = max > min ? MathHelper.Clamp((value - min) / (max - min), 0f, 1f) : 0f;

            var track = new Rectangle(r.X, r.Y + r.Height / 2 - 2, r.Width - 44, 4);
            Rect(track, GuiStyle.Field);
            Border(track, GuiStyle.Border);
            Rect(new Rectangle(track.X, track.Y, (int)(track.Width * t), track.Height),
                enabled ? GuiStyle.Accent : GuiStyle.Locked);

            int hx = track.X + (int)(track.Width * t);
            var knob = new Rectangle(hx - 3, r.Y + 2, 7, r.Height - 4);
            bool hover = enabled && (Hovered(knob) || Hovered(track));
            Rect(knob, enabled ? (hover ? GuiStyle.ButtonHover : GuiStyle.Button) : GuiStyle.FieldDisabled);
            Border(knob, GuiStyle.Border);

            // valor numerico a direita
            TextRight(FormatFloat(value), r.Right, r.Y + (r.Height - GuiFont.GlyphHeight) / 2 + 1,
                enabled ? GuiStyle.TextDim : GuiStyle.Locked);

            if (enabled && hover && MousePressed)
                activeId = id;
            if (activeId == id && MouseDown && track.Width > 0)
            {
                float nt = MathHelper.Clamp((Mouse.X - track.X) / (float)track.Width, 0f, 1f);
                result = min + nt * (max - min);
                return Math.Abs(result - value) > 0.0001f;
            }
            return false;
        }

        /// Campo numerico com arraste horizontal (como o Unity) + edicao por texto.
        public bool DragNumber(int id, Rectangle r, float value, bool enabled, bool integer,
            out float result, out bool committedText)
        {
            result = value;
            committedText = false;

            // arraste so comeca fora do modo de edicao
            if (focusId != id)
            {
                bool hover = enabled && Hovered(r);
                if (hover && MousePressed)
                {
                    activeId = id;
                    dragOrigin = Mouse;
                    dragAccum = 0f;
                }
                if (activeId == id && MouseDown)
                {
                    int dx = Mouse.X - dragOrigin.X;
                    if (Math.Abs(dx) > 0)
                    {
                        dragOrigin = Mouse;
                        float step = integer ? 1f : (ShiftDown ? 1f : 0.25f);
                        dragAccum += dx * step;
                        float delta = integer ? (float)Math.Truncate(dragAccum) : dragAccum;
                        if (delta != 0f)
                        {
                            dragAccum -= delta;
                            result = value + delta;
                            return true;
                        }
                    }
                    return false;
                }
                // clique sem arraste entra em edicao
                if (activeId == 0 && hover && MouseReleased)
                {
                    CommitPending();
                    focusId = id;
                    editBuffer = FormatFloat(value);
                    editInvalid = false;
                }
            }

            if (TextField(id, r, FormatFloat(value), enabled, out string text))
            {
                if (float.TryParse(text, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                    || float.TryParse(text, out parsed))
                {
                    result = parsed;
                    committedText = true;
                    return true;
                }
                MarkInvalid(id);
            }
            return false;
        }

        public static string FormatFloat(float v)
        {
            if (float.IsNaN(v)) return "NaN";
            if (float.IsInfinity(v)) return v > 0 ? "Inf" : "-Inf";
            if (Math.Abs(v - (float)Math.Round(v)) < 0.0001f)
                return ((int)Math.Round(v)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---- foco ----

        public void ClearFocus()
        {
            focusId = 0;
            editBuffer = null;
            editInvalid = false;
        }

        private void CancelEdit() => ClearFocus();

        /// Chamado antes de trocar de campo: o painel decide se aplica o pendente.
        public Func<int, string, bool> PendingCommit;

        private void CommitPending()
        {
            if (focusId != 0 && editBuffer != null)
                PendingCommit?.Invoke(focusId, editBuffer);
            ClearFocus();
        }
    }
}
