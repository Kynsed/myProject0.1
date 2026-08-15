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

        // metricas
        public const int RowHeight = 18;
        public const int Padding = 6;
        public const int Indent = 12;
        public const int SectionHeader = 20;
        public const int TitleBar = 24;
        public const int Toolbar = 22;
        public const float LabelRatio = 0.42f; // fracao da largura usada pelo rotulo
    }
}
