using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Monocle;
using myProject.Inspector.UI;

namespace myProject.Inspector.Drawers
{
    public sealed class BoolDrawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t == typeof(bool);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            bool v = (bool)value;
            newValue = value;
            if (ctx.Gui.Checkbox(ctx.FieldRect, v, ctx.Enabled))
            {
                newValue = !v;
                return true;
            }
            return false;
        }
    }

    // int, float, double, byte, short, long... editados como numero com arraste.
    public sealed class NumericDrawer : IValueDrawer
    {
        public static bool IsNumeric(Type t)
            => t == typeof(float) || t == typeof(double) || t == typeof(decimal)
            || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong)
            || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);

        private static bool IsInteger(Type t)
            => t != typeof(float) && t != typeof(double) && t != typeof(decimal);

        public bool CanDraw(Type t) => IsNumeric(t);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            newValue = value;
            float current = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            int id = Gui.Id(ctx.Path);
            var range = ctx.Member?.Range;

            bool changed;
            float result;
            if (range != null)
            {
                changed = ctx.Gui.Slider(id, ctx.FieldRect, current, range.Min, range.Max, ctx.Enabled, out result);
                if (changed)
                    result = MathHelper.Clamp(result, range.Min, range.Max);
            }
            else
            {
                changed = ctx.Gui.DragNumber(id, ctx.FieldRect, current, ctx.Enabled,
                    IsInteger(type), out result, out _);
            }

            if (!changed)
                return false;
            newValue = Coerce(result, type);
            return true;
        }

        public static object Coerce(float v, Type t)
        {
            try
            {
                if (t == typeof(float)) return v;
                if (t == typeof(double)) return (double)v;
                if (t == typeof(decimal)) return (decimal)v;
                if (t == typeof(int)) return (int)Math.Round(v);
                if (t == typeof(uint)) return (uint)Math.Max(0, Math.Round(v));
                if (t == typeof(long)) return (long)Math.Round(v);
                if (t == typeof(ulong)) return (ulong)Math.Max(0, Math.Round(v));
                if (t == typeof(short)) return (short)Math.Round(v);
                if (t == typeof(ushort)) return (ushort)Math.Max(0, Math.Round(v));
                if (t == typeof(byte)) return (byte)MathHelper.Clamp((float)Math.Round(v), 0, 255);
                if (t == typeof(sbyte)) return (sbyte)MathHelper.Clamp((float)Math.Round(v), -128, 127);
            }
            catch { }
            return v;
        }
    }

    public sealed class StringDrawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t == typeof(string) || t == typeof(char);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            newValue = value;
            string s = value?.ToString() ?? string.Empty;
            if (ctx.Gui.TextField(Gui.Id(ctx.Path), ctx.FieldRect, s, ctx.Enabled, out string edited))
            {
                if (type == typeof(char))
                    newValue = string.IsNullOrEmpty(edited) ? '\0' : edited[0];
                else
                    newValue = edited;
                return true;
            }
            return false;
        }
    }

    // Enum: clique cicla; com Shift cicla p/ tras. Flags mostram o valor combinado.
    public sealed class EnumDrawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t.IsEnum;

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            newValue = value;
            string label = value?.ToString() ?? "-";
            if (ctx.Gui.Button(ctx.FieldRect, label, ctx.Enabled))
            {
                var values = Enum.GetValues(type);
                int idx = Array.IndexOf(values, value);
                int step = ctx.Gui.ShiftDown ? -1 : 1;
                idx = ((idx + step) % values.Length + values.Length) % values.Length;
                newValue = values.GetValue(idx);
                return true;
            }
            return false;
        }
    }

    // Vector2 como dois campos X/Y lado a lado.
    public sealed class Vector2Drawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t == typeof(Vector2);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            var v = (Vector2)value;
            newValue = value;
            var r = ctx.FieldRect;
            int half = (r.Width - 4) / 2;
            bool changed = false;

            var rx = new Rectangle(r.X + 10, r.Y, half - 10, r.Height);
            var ry = new Rectangle(r.X + half + 14, r.Y, half - 10, r.Height);
            ctx.Gui.Text("X", r.X, r.Y + 5, GuiStyle.TextDim);
            ctx.Gui.Text("Y", r.X + half + 4, r.Y + 5, GuiStyle.TextDim);

            if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + ".x"), rx, v.X, ctx.Enabled, false, out float nx, out _))
            {
                v.X = nx; changed = true;
            }
            if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + ".y"), ry, v.Y, ctx.Enabled, false, out float ny, out _))
            {
                v.Y = ny; changed = true;
            }
            if (changed)
                newValue = v;
            return changed;
        }
    }

    public sealed class ColorDrawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t == typeof(Color);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            var c = (Color)value;
            newValue = value;
            var r = ctx.FieldRect;

            var swatch = new Rectangle(r.X, r.Y + 2, 22, r.Height - 4);
            ctx.Gui.Rect(swatch, c);
            ctx.Gui.Border(swatch, GuiStyle.Border);

            int w = (r.Width - 28) / 4;
            bool changed = false;
            byte[] comp = { c.R, c.G, c.B, c.A };
            string[] names = { "r", "g", "b", "a" };
            for (int i = 0; i < 4; i++)
            {
                var cr = new Rectangle(r.X + 26 + i * w, r.Y, w - 2, r.Height);
                if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + "." + names[i]), cr, comp[i], ctx.Enabled, true,
                        out float nv, out _))
                {
                    comp[i] = (byte)MathHelper.Clamp(nv, 0, 255);
                    changed = true;
                }
            }
            if (changed)
                newValue = new Color(comp[0], comp[1], comp[2], comp[3]);
            return changed;
        }
    }

    public sealed class RectangleDrawer : IValueDrawer
    {
        public bool CanDraw(Type t) => t == typeof(Rectangle) || t == typeof(Point);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            newValue = value;
            var r = ctx.FieldRect;
            bool changed = false;

            if (type == typeof(Point))
            {
                var p = (Point)value;
                int half = (r.Width - 4) / 2;
                if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + ".x"), new Rectangle(r.X, r.Y, half, r.Height),
                        p.X, ctx.Enabled, true, out float px, out _)) { p.X = (int)px; changed = true; }
                if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + ".y"), new Rectangle(r.X + half + 4, r.Y, half, r.Height),
                        p.Y, ctx.Enabled, true, out float py, out _)) { p.Y = (int)py; changed = true; }
                if (changed) newValue = p;
                return changed;
            }

            var rect = (Rectangle)value;
            int q = (r.Width - 6) / 4;
            int[] vals = { rect.X, rect.Y, rect.Width, rect.Height };
            string[] names = { "x", "y", "w", "h" };
            for (int i = 0; i < 4; i++)
            {
                var cr = new Rectangle(r.X + i * (q + 2), r.Y, q, r.Height);
                if (ctx.Gui.DragNumber(Gui.Id(ctx.Path + "." + names[i]), cr, vals[i], ctx.Enabled, true,
                        out float nv, out _)) { vals[i] = (int)nv; changed = true; }
            }
            if (changed)
                newValue = new Rectangle(vals[0], vals[1], vals[2], vals[3]);
            return changed;
        }
    }

    // Referencia a outra entidade: mostra o tipo e permite selecionar com um clique.
    public sealed class EntityRefDrawer : IValueDrawer
    {
        public Action<object> OnSelect;

        public bool CanDraw(Type t) => typeof(Entity).IsAssignableFrom(t) || typeof(Component).IsAssignableFrom(t);

        public bool Draw(ref DrawContext ctx, Type type, object value, out object newValue)
        {
            newValue = value;
            string label = value == null ? "None (" + type.Name + ")" : value.GetType().Name;
            var r = ctx.FieldRect;
            var pick = new Rectangle(r.Right - 40, r.Y, 40, r.Height);
            var main = new Rectangle(r.X, r.Y, r.Width - 42, r.Height);

            ctx.Gui.Rect(main, GuiStyle.Field);
            ctx.Gui.Border(main, GuiStyle.Border);
            ctx.Gui.Text(label, main.X + 4, main.Y + 5,
                value == null ? GuiStyle.TextDim : GuiStyle.Text, main.Width - 8);

            if (value != null && ctx.Gui.Button(pick, "Select"))
                OnSelect?.Invoke(value);
            return false; // referencia nao e reatribuida pelo inspector
        }
    }
}
