using Microsoft.Xna.Framework;

namespace myProject.Inspector.UI
{
    // Paleta e metricas no estilo do Editor da Unity (tema escuro).
    public static class GuiStyle
    {
        // fundo
        public static readonly Color WindowBg = new Color(56, 56, 56);
        public static readonly Color HeaderBg = new Color(62, 62, 62);
        public static readonly Color SectionBg = new Color(51, 51, 51);
        public static readonly Color GroupBg = new Color(46, 46, 46); // bloco tematico
        public static readonly Color RowAlt = new Color(60, 60, 60);
        public static readonly Color Border = new Color(35, 35, 35);
        public static readonly Color Separator = new Color(74, 74, 74);

        // texto
        public static readonly Color Text = new Color(210, 210, 210);
        public static readonly Color TextDim = new Color(140, 140, 140);
        public static readonly Color TextHeader = new Color(235, 235, 235);

        // controles
        public static readonly Color Field = new Color(42, 42, 42);
        public static readonly Color FieldHover = new Color(50, 50, 50);
        public static readonly Color FieldFocus = new Color(30, 40, 55);
        public static readonly Color FieldDisabled = new Color(48, 48, 48);
        public static readonly Color Button = new Color(88, 88, 88);
        public static readonly Color ButtonHover = new Color(103, 103, 103);
        public static readonly Color ButtonActive = new Color(70, 96, 124);
        public static readonly Color Accent = new Color(70, 130, 200);   // azul de selecao
        public static readonly Color Modified = new Color(230, 190, 80); // barra de "alterado"
        public static readonly Color Invalid = new Color(200, 70, 70);   // valor invalido
        public static readonly Color Locked = new Color(120, 120, 120);  // somente leitura

        // Escala da UI. A fonte e 5x7: a 1x fica ilegivel numa janela de 1280x720.
        // Todas as metricas derivam daqui, entao mudar Scale reescala o painel inteiro.
        public static int Scale = 2;

        public static int TextHeight => GuiFont.GlyphHeight * Scale;
        public static int RowHeight => 6 * Scale + TextHeight;      // 26 em 2x
        public static int Padding => 3 * Scale;
        public static int Indent => 6 * Scale;
        public static int SectionHeader => 7 * Scale + TextHeight;
        public static int TitleBar => 9 * Scale + TextHeight;
        public static int Toolbar => 8 * Scale + TextHeight;
        public static int ButtonHeight => 4 * Scale + TextHeight;
        public static int StatusBar => 5 * Scale + TextHeight;
        public static int CheckboxSize => 6 * Scale;
        public static int DefaultPanelWidth => 260 * Scale;
        public const float LabelRatio = 0.42f; // fracao da largura usada pelo rotulo
    }
}
