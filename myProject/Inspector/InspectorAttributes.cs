using System;

namespace myProject.Inspector
{
    // Atributos de anotacao para o inspector. Equivalentes aos da Unity, definidos aqui
    // porque o projeto e MonoGame puro (nao existe UnityEngine).

    /// Expoe um campo privado no inspector (campos publicos ja aparecem por padrao).
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class SerializeFieldAttribute : Attribute { }

    /// Esconde do inspector um membro que apareceria por padrao.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class HideInInspectorAttribute : Attribute { }

    /// Titulo de secao desenhado acima do membro.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public readonly string Text;
        public HeaderAttribute(string text) { Text = text; }
    }

    /// Texto de ajuda exibido ao passar o mouse sobre o membro.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class TooltipAttribute : Attribute
    {
        public readonly string Text;
        public TooltipAttribute(string text) { Text = text; }
    }

    /// Faz o membro numerico ser editado por slider, com limites.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class RangeAttribute : Attribute
    {
        public readonly float Min;
        public readonly float Max;
        public RangeAttribute(float min, float max) { Min = min; Max = max; }
    }

    /// Mostra o membro sem permitir edicao.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class ReadOnlyAttribute : Attribute { }

    /// Coloca o membro num bloco nomeado do inspector. Tem prioridade sobre [Header]
    /// e sobre a classificacao automatica.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public sealed class InspectorGroupAttribute : Attribute
    {
        public readonly string Name;
        public readonly int Order; // menor aparece antes; empate resolve por declaracao
        public InspectorGroupAttribute(string name, int order = 0) { Name = name; Order = order; }
    }
}
