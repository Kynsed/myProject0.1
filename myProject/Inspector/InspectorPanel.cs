using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using myProject.Inspector.Drawers;
using myProject.Inspector.Reflection;
using myProject.Inspector.UI;

namespace myProject.Inspector
{
    // Orquestra selecao + reflexao + drawers + undo. Nao desenha primitivas: delega ao Gui.
    public sealed class InspectorPanel
    {
        public readonly Selection Selection = new Selection();
        public readonly UndoSystem Undo = new UndoSystem();
        public readonly DrawerRegistry Drawers;
        public readonly Gui Gui = new Gui();

        public int Width = GuiStyle.DefaultPanelWidth;
        public bool Visible;

        private const int MaxDepth = 4; // profundidade maxima de objetos aninhados

        private readonly HashSet<string> collapsed = new HashSet<string>(StringComparer.Ordinal);
        // Aninhados comecam FECHADOS (como na Unity): abrir por padrao explodiria em
        // grafos ciclicos (Entity -> Scene -> Entities -> Entity).
        private readonly HashSet<string> expanded = new HashSet<string>(StringComparer.Ordinal);
        // Guarda de ciclo: objetos ja abertos no caminho atual nao reabrem.
        private readonly List<object> openPath = new List<object>();
        private readonly List<object> sections = new List<object>();
        private string focusedRow;          // linha alvo dos botoes Reset/Copy/Paste
        private object clipboardValue;
        private Type clipboardType;
        private int scroll;
        private int contentHeight;
        private string status;
        private int statusFrames;

        public InspectorPanel()
        {
            Drawers = DrawerRegistry.CreateDefault(obj => Selection.Select(obj));
            Selection.Changed += _ =>
            {
                scroll = 0;
                focusedRow = null;
                expanded.Clear();
                Gui.ClearFocus();
            };
            Gui.PendingCommit = CommitPendingText;
        }

        // ---- ciclo ----

        public void Update(Scene scene)
        {
            if (!Visible)
                return;
            var kb = MInput.Keyboard;
            bool ctrl = kb.Check(Microsoft.Xna.Framework.Input.Keys.LeftControl)
                     || kb.Check(Microsoft.Xna.Framework.Input.Keys.RightControl);
            if (ctrl && kb.Pressed(Microsoft.Xna.Framework.Input.Keys.Z))
                Notify(Undo.Undo() ? "Undo" : "Nada para desfazer");
            if (ctrl && kb.Pressed(Microsoft.Xna.Framework.Input.Keys.Y))
                Notify(Undo.Redo() ? "Redo" : "Nada para refazer");
            if (statusFrames > 0)
                statusFrames--;
        }

        public void Render(Scene scene, SpriteBatch batch, int screenW, int screenH)
        {
            if (!Visible)
                return;

            var panel = new Rectangle(screenW - Width, 0, Width, screenH);
            var mouse = MInput.Mouse;
            Gui.Mouse = new Point(mouse.CurrentState.X, mouse.CurrentState.Y);
            Gui.MouseDown = mouse.CheckLeftButton;
            Gui.MousePressed = mouse.PressedLeftButton;
            Gui.MouseReleased = mouse.ReleasedLeftButton;
            Gui.MouseRightPressed = mouse.PressedRightButton;
            Gui.WheelDelta = mouse.WheelDelta;
            Gui.BeginFrame(batch, panel);

            if (Gui.MouseReleased)
                Undo.BreakMerge();

            // fundo
            Gui.Rect(panel, GuiStyle.WindowBg);
            Gui.Rect(new Rectangle(panel.X, 0, 1, screenH), GuiStyle.Border);

            int y = 0;
            y = DrawTitleBar(panel, y);
            y = DrawToolbar(panel, y);

            var content = new Rectangle(panel.X, y, panel.Width, screenH - y - GuiStyle.StatusBar);
            if (Gui.WantsMouse && Gui.WheelDelta != 0)
                scroll = Math.Max(0, scroll - Gui.WheelDelta / 4);
            int maxScroll = Math.Max(0, contentHeight - content.Height);
            scroll = Math.Min(scroll, maxScroll);

            Gui.SetClip(content);
            int startY = content.Y - scroll;
            int endY = DrawBody(content, startY);
            contentHeight = endY - startY;
            Gui.SetClip(panel);

            DrawScrollbar(content, maxScroll);
            DrawStatusBar(panel, screenH);
            Gui.EndFrame();
        }

        // ---- secoes ----

        private int DrawTitleBar(Rectangle panel, int y)
        {
            var bar = new Rectangle(panel.X, y, panel.Width, GuiStyle.TitleBar);
            Gui.Rect(bar, GuiStyle.HeaderBg);
            Gui.Rect(new Rectangle(bar.X, bar.Bottom - 1, bar.Width, 1), GuiStyle.Border);

            object sel = Selection.Current;
            // a fonte bitmap cobre so ASCII imprimivel: nada de acento/traco longo aqui
            string title = sel == null ? "Inspector - nada selecionado" : sel.GetType().Name;
            string pos = sel is Entity e
                ? "(" + Gui.FormatFloat(e.X) + ", " + Gui.FormatFloat(e.Y) + ")" : null;
            int posW = pos != null ? Gui.MeasureText(pos) + GuiStyle.Padding : 0;

            Gui.TextIn(title, bar.X + GuiStyle.Padding, bar, GuiStyle.TextHeader,
                bar.Width - GuiStyle.Padding * 2 - posW);
            if (pos != null)
                Gui.TextIn(pos, bar.Right - GuiStyle.Padding - Gui.MeasureText(pos), bar, GuiStyle.TextDim);
            return bar.Bottom;
        }

        private int DrawToolbar(Rectangle panel, int y)
        {
            var bar = new Rectangle(panel.X, y, panel.Width, GuiStyle.Toolbar);
            Gui.Rect(bar, GuiStyle.SectionBg);
            Gui.Rect(new Rectangle(bar.X, bar.Bottom - 1, bar.Width, 1), GuiStyle.Border);

            // larguras derivadas do texto: a barra se ajusta a qualquer escala de UI
            int bh = GuiStyle.ButtonHeight, gap = GuiStyle.Scale;
            int bx = bar.X + GuiStyle.Padding, by = bar.Y + (bar.Height - bh) / 2;

            int w = Gui.ButtonWidth("Expand");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Expand"))
                collapsed.Clear(); // secoes; aninhados abrem sob demanda (evita ciclos)
            bx += w + gap;

            w = Gui.ButtonWidth("Collapse");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Collapse"))
            {
                CollapseAll();
                expanded.Clear();
            }
            bx += w + gap;

            w = Gui.ButtonWidth("Refresh");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Refresh"))
            {
                TypeCache.Clear();
                Selection.CaptureBaseline();
                Notify("Metadados recarregados");
            }
            bx += w + gap;

            w = Gui.ButtonWidth("Undo");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Undo", Undo.CanUndo))
                Notify(Undo.Undo() ? "Undo" : null);
            bx += w + gap;

            w = Gui.ButtonWidth("Redo");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Redo", Undo.CanRedo))
                Notify(Undo.Redo() ? "Redo" : null);

            // segunda faixa: acoes sobre o campo focado
            var bar2 = new Rectangle(panel.X, bar.Bottom, panel.Width, GuiStyle.Toolbar);
            Gui.Rect(bar2, GuiStyle.SectionBg);
            Gui.Rect(new Rectangle(bar2.X, bar2.Bottom - 1, bar2.Width, 1), GuiStyle.Border);
            bool hasRow = focusedRow != null && pendingRows.ContainsKey(focusedRow);
            bx = bar2.X + GuiStyle.Padding;
            by = bar2.Y + (bar2.Height - bh) / 2;

            w = Gui.ButtonWidth("Reset");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Reset", hasRow))
                ResetFocused();
            bx += w + gap;

            w = Gui.ButtonWidth("Copy");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Copy", hasRow))
                CopyFocused();
            bx += w + gap;

            bool canPaste = hasRow && clipboardType != null
                && pendingRows[focusedRow].Member.ValueType == clipboardType;
            w = Gui.ButtonWidth("Paste");
            if (Gui.Button(new Rectangle(bx, by, w, bh), "Paste", canPaste))
                PasteFocused();
            bx += w + gap * 2;

            string rowLabel = hasRow ? pendingRows[focusedRow].Member.Label : "nenhum campo";
            Gui.TextIn(rowLabel, bx, bar2, GuiStyle.TextDim, bar2.Right - bx - GuiStyle.Padding);

            return bar2.Bottom;
        }

        private readonly Dictionary<string, RowRef> pendingRows = new Dictionary<string, RowRef>(StringComparer.Ordinal);
        private struct RowRef
        {
            public object Target;
            public InspectedMember Member;
        }

        private static bool ContainsRef(List<object> list, object o)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], o))
                    return true;
            return false;
        }

        private int DrawBody(Rectangle content, int y)
        {
            pendingRows.Clear();
            openPath.Clear();
            if (Selection.Current != null)
                openPath.Add(Selection.Current);
            object sel = Selection.Current;
            if (sel == null)
            {
                int lh = GuiStyle.TextHeight + GuiStyle.Scale * 2;
                Gui.Text("Clique numa entidade para inspecionar.", content.X + GuiStyle.Padding, y + lh, GuiStyle.TextDim, content.Width - GuiStyle.Padding * 2);
                Gui.Text("F1 alterna o inspector.", content.X + GuiStyle.Padding, y + lh * 2, GuiStyle.TextDim, content.Width - GuiStyle.Padding * 2);
                return y + lh * 4;
            }

            // secao do proprio objeto + uma por componente
            y = DrawSection(content, y, sel, sel.GetType().Name, "self");
            if (sel is Entity entity)
            {
                foreach (Component c in entity.Components)
                    y = DrawSection(content, y, c, c.GetType().Name, "c" + c.GetHashCode());
            }
            return y;
        }

        private int DrawSection(Rectangle content, int y, object target, string title, string key)
        {
            var header = new Rectangle(content.X, y, content.Width, GuiStyle.SectionHeader);
            bool open = !collapsed.Contains(key);
            if (Gui.Foldout(header, open, title, GuiStyle.SectionBg))
            {
                if (open) collapsed.Add(key); else collapsed.Remove(key);
                open = !open;
            }
            Gui.Rect(new Rectangle(header.X, header.Bottom - 1, header.Width, 1), GuiStyle.Border);
            y = header.Bottom;
            if (!open)
                return y;

            var type = TypeCache.Get(target.GetType());
            if (type.Members.Length == 0)
            {
                Gui.TextIn("(sem campos expostos)", content.X + GuiStyle.Indent,
                    new Rectangle(content.X, y, content.Width, GuiStyle.RowHeight), GuiStyle.TextDim);
                return y + GuiStyle.RowHeight;
            }
            foreach (var m in type.Members)
                y = DrawMemberRow(content, y, target, m, key + "/" + m.Name, 1);
            return y + 2;
        }

        private int DrawMemberRow(Rectangle content, int y, object target, InspectedMember member,
            string path, int indent, bool recordUndo = true)
        {
            bool _ignored;
            return DrawMemberRow(content, y, target, member, path, indent, recordUndo, out _ignored);
        }

        // recordUndo=false quando o membro vive dentro de uma struct: quem registra o undo
        // e o membro pai (com a struct inteira), senao o comando apontaria p/ a copia boxed.
        private int DrawMemberRow(Rectangle content, int y, object target, InspectedMember member,
            string path, int indent, bool recordUndo, out bool changedOut)
        {
            changedOut = false;
            if (member.Header != null)
            {
                Gui.TextIn(member.Header, content.X + GuiStyle.Padding,
                    new Rectangle(content.X, y, content.Width, GuiStyle.RowHeight), GuiStyle.TextHeader);
                Gui.Rect(new Rectangle(content.X + GuiStyle.Padding, y + GuiStyle.RowHeight - 2,
                    content.Width - GuiStyle.Padding * 2, GuiStyle.Scale), GuiStyle.Separator);
                y += GuiStyle.RowHeight;
            }

            object value = member.GetValue(target);
            Type vt = member.ValueType;
            var drawer = Drawers.Find(vt);
            bool composite = drawer == null && !TypeCache.IsAtomic(vt) && value != null;

            var row = new Rectangle(content.X, y, content.Width, GuiStyle.RowHeight);
            bool hovered = Gui.Hovered(row);
            if (hovered)
                Gui.Rect(row, GuiStyle.RowAlt);

            pendingRows[path] = new RowRef { Target = target, Member = member };
            if (hovered && Gui.MousePressed && Gui.Mouse.X < content.X + LabelWidth(content))
                focusedRow = path;

            // marcador de "modificado desde a selecao" e de campo focado
            bool modified = ReferenceEquals(target, Selection.Current) && Selection.IsModified(member.Name, value);
            if (modified)
                Gui.Rect(new Rectangle(row.X + 1, row.Y + 1, 2 * GuiStyle.Scale, row.Height - 2),
                    GuiStyle.Modified);
            if (focusedRow == path)
                Gui.Border(row, GuiStyle.Accent);

            int labelX = content.X + GuiStyle.Padding + indent * GuiStyle.Indent;
            int labelW = LabelWidth(content) - indent * GuiStyle.Indent - GuiStyle.Padding;
            Color labelColor = member.CanWrite ? GuiStyle.Text : GuiStyle.Locked;
            Gui.TextIn(member.Label, labelX, row, labelColor, labelW);

            var fieldRect = new Rectangle(content.X + LabelWidth(content), row.Y + 1,
                content.Width - LabelWidth(content) - GuiStyle.Padding - 8, row.Height - 2);

            if (composite)
            {
                bool isStruct = vt.IsValueType;
                // ciclo (a mesma instancia ja aberta acima) ou fundo demais: nao expande
                bool cyclic = !isStruct && ContainsRef(openPath, value);
                bool tooDeep = indent >= MaxDepth;
                bool expandable = !cyclic && !tooDeep;

                bool open = expandable && expanded.Contains(path);
                var fold = new Rectangle(fieldRect.X, row.Y, fieldRect.Width, row.Height);
                string caption = vt.Name + (cyclic ? "  (ciclo)" : tooDeep ? "  (profundo)" : "");
                if (Gui.Foldout(fold, open, caption, GuiStyle.WindowBg) && expandable)
                {
                    if (open) expanded.Remove(path); else expanded.Add(path);
                    open = !open;
                }
                y = row.Bottom;
                if (open)
                {
                    object box = value; // struct: copia boxed | classe: a propria referencia
                    if (!isStruct)
                        openPath.Add(value);
                    var nested = TypeCache.Get(vt);
                    bool anyChanged = false;
                    foreach (var nm in nested.Members)
                    {
                        y = DrawMemberRow(content, y, box, nm, path + "/" + nm.Name, indent + 1,
                            !isStruct, out bool childChanged);
                        anyChanged |= childChanged;
                    }
                    if (!isStruct)
                        openPath.RemoveAt(openPath.Count - 1);
                    // struct: escreve a copia inteira de volta no membro pai (1 undo)
                    if (isStruct && anyChanged)
                    {
                        ApplyChange(target, member, value, box, recordUndo);
                        changedOut = true;
                    }
                }
                return y;
            }

            if (drawer == null)
            {
                Gui.TextIn(value == null ? "null" : value.ToString(), fieldRect.X + 2 * GuiStyle.Scale, row,
                    GuiStyle.TextDim, fieldRect.Width - 4 * GuiStyle.Scale);
                return row.Bottom;
            }

            var ctx = new DrawContext
            {
                Gui = Gui,
                FieldRect = fieldRect,
                Path = path,
                Enabled = member.CanWrite,
                Member = member
            };
            if (drawer.Draw(ref ctx, vt, value, out object newValue) && member.CanWrite
                && !Equals(newValue, value))
            {
                ApplyChange(target, member, value, newValue, recordUndo);
                changedOut = true;
            }

            // tooltip do campo sob o mouse
            if (hovered && member.Tooltip != null)
                tooltip = member.Tooltip;
            return row.Bottom;
        }

        private string tooltip;

        private static int LabelWidth(Rectangle content) => (int)(content.Width * GuiStyle.LabelRatio);

        // ---- edicao ----

        private void ApplyChange(object target, InspectedMember member, object oldValue,
            object newValue, bool recordUndo = true)
        {
            var cmd = new SetMemberCommand(target, member, oldValue, newValue);
            cmd.Apply();
            if (recordUndo)
                Undo.Record(cmd);
        }

        private bool CommitPendingText(int id, string text) => false; // Enter ja confirma no widget

        private void ResetFocused()
        {
            if (focusedRow == null || !pendingRows.TryGetValue(focusedRow, out var r))
                return;
            object baseline = Selection.GetBaseline(r.Member.Name);
            if (baseline == null && !ReferenceEquals(r.Target, Selection.Current))
                return;
            object current = r.Member.GetValue(r.Target);
            if (Equals(current, baseline))
                return;
            ApplyChange(r.Target, r.Member, current, baseline);
            Undo.BreakMerge();
            Notify("Reset: " + r.Member.Label);
        }

        private void CopyFocused()
        {
            if (focusedRow == null || !pendingRows.TryGetValue(focusedRow, out var r))
                return;
            clipboardValue = r.Member.GetValue(r.Target);
            clipboardType = r.Member.ValueType;
            Notify("Copiado: " + r.Member.Label);
        }

        private void PasteFocused()
        {
            if (focusedRow == null || !pendingRows.TryGetValue(focusedRow, out var r))
                return;
            if (clipboardType == null || r.Member.ValueType != clipboardType || !r.Member.CanWrite)
                return;
            object current = r.Member.GetValue(r.Target);
            if (Equals(current, clipboardValue))
                return;
            ApplyChange(r.Target, r.Member, current, clipboardValue);
            Undo.BreakMerge();
            Notify("Colado: " + r.Member.Label);
        }

        private void CollapseAll()
        {
            collapsed.Add("self");
            if (Selection.Current is Entity e)
                foreach (Component c in e.Components)
                    collapsed.Add("c" + c.GetHashCode());
        }

        private void Notify(string msg)
        {
            if (msg == null)
                return;
            status = msg;
            statusFrames = 150;
        }

        // ---- decoracoes ----

        private void DrawScrollbar(Rectangle content, int maxScroll)
        {
            if (maxScroll <= 0)
                return;
            var track = new Rectangle(content.Right - 6, content.Y, 5, content.Height);
            Gui.Rect(track, GuiStyle.SectionBg);
            float visible = content.Height / (float)(content.Height + maxScroll);
            int h = Math.Max(20, (int)(track.Height * visible));
            int pos = maxScroll == 0 ? 0 : (int)((track.Height - h) * (scroll / (float)maxScroll));
            Gui.Rect(new Rectangle(track.X, track.Y + pos, track.Width, h), GuiStyle.Button);
        }

        private void DrawStatusBar(Rectangle panel, int screenH)
        {
            var bar = new Rectangle(panel.X, screenH - GuiStyle.StatusBar, panel.Width, GuiStyle.StatusBar);
            Gui.Rect(bar, GuiStyle.HeaderBg);
            Gui.Rect(new Rectangle(bar.X, bar.Y, bar.Width, 1), GuiStyle.Border);

            string left = tooltip ?? (statusFrames > 0 ? status : null)
                ?? (Selection.HasSelection ? "Arraste ou digite | Ctrl+Z/Y" : "F1 fecha");
            string counters = "U:" + Undo.UndoCount + " R:" + Undo.RedoCount;
            int cw = Gui.MeasureText(counters) + GuiStyle.Padding * 2;
            Gui.TextIn(left, bar.X + GuiStyle.Padding, bar, GuiStyle.TextDim, bar.Width - cw);
            Gui.TextIn(counters, bar.Right - GuiStyle.Padding - Gui.MeasureText(counters), bar, GuiStyle.TextDim);
            tooltip = null;
        }
    }
}
