using System;
using Microsoft.Xna.Framework;
using myProject.Inspector.UI;

namespace myProject.Inspector.Drawers
{
    // Contexto de uma linha de edicao. Passado por ref p/ evitar alocacao por campo.
    public struct DrawContext
    {
        public Gui Gui;
        public Rectangle FieldRect;   // area do controle (a direita do rotulo)
        public string Path;           // identidade estavel do campo ("Player/Speed/X")
        public bool Enabled;          // false => somente leitura
        public Reflection.InspectedMember Member; // null em elementos aninhados
    }

    /// Desenha e edita um valor de um tipo. Implementacoes sao registradas no
    /// DrawerRegistry — e o ponto de extensao para drawers customizados.
    public interface IValueDrawer
    {
        bool CanDraw(Type type);
        /// Retorna true se o valor mudou; newValue recebe o valor editado.
        bool Draw(ref DrawContext ctx, Type type, object value, out object newValue);
        /// Altura extra em linhas (1 = uma linha padrao).
        int RowSpan(Type type, object value) => 1;
    }
}
