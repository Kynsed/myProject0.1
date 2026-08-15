using System;
using System.Collections.Generic;
using Monocle;

namespace myProject.Inspector.Reflection
{
    // Classifica membros em blocos tematicos. Existe porque o codigo portado do Celeste
    // nao tem anotacoes: sem isso o Player despeja ~120 campos numa lista unica.
    // Quem escreve codigo novo pode ignorar a heuristica usando [InspectorGroup].
    public static class MemberClassifier
    {
        // Ordem canonica dos blocos automaticos no painel.
        public const string Transform = "Transform";
        public const string Movement = "Movimento";
        public const string Collision = "Colisao";
        public const string State = "Estado";
        public const string Camera = "Camera";
        public const string Visual = "Visual";
        public const string References = "Referencias";
        public const string Other = "Outros";

        private static readonly string[] Order =
        {
            Transform, Movement, Collision, State, Camera, Visual, References, Other
        };

        /// Posicao do bloco na ordem canonica; blocos autorais (nao listados) vem antes.
        public static int SortKey(string group)
        {
            int i = Array.IndexOf(Order, group);
            return i < 0 ? -1 : i;
        }

        // Nomes exatos: mais confiaveis que substring p/ os membros geometricos do Entity.
        private static readonly HashSet<string> TransformNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Position", "ExactPosition", "X", "Y", "Width", "Height",
            "Left", "Right", "Top", "Bottom", "CenterX", "CenterY",
            "TopLeft", "TopRight", "BottomLeft", "BottomRight",
            "Center", "CenterLeft", "CenterRight", "TopCenter", "BottomCenter",
            "Size", "HalfSize", "Origin"
        };

        private static readonly HashSet<string> VisualNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Visible", "Depth", "Color", "Alpha", "Scale", "Rotation", "Texture", "Sprite"
        };

        private static readonly HashSet<string> StateNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Active", "Dead", "Tag", "TagInt"
        };

        // Substrings, avaliadas na ordem em que aparecem aqui.
        private static readonly (string token, string group)[] Tokens =
        {
            // camera primeiro: CameraAnchor/CameraTarget nao devem cair em Transform
            ("camera",   Camera),

            // colisao antes de movimento: "CollideSolid" nao deve virar Movimento
            ("collid",   Collision),
            ("collision",Collision),
            ("hitbox",   Collision),
            ("collider", Collision),
            ("squish",   Collision),
            ("solid",    Collision),
            ("ground",   Collision),
            ("land",     Collision),
            ("wall",     Collision),
            // "ride" casaria dentro de "oveRIDE" (OverrideDashDirection/OverrideHairColor)
            ("riding",   Collision),
            ("rider",    Collision),
            ("push",     Collision),
            ("thru",     Collision),
            ("safe",     Collision),
            ("naive",    Collision),
            ("bounds",   Collision),

            ("position", Transform),
            ("remainder",Transform),

            ("speed",    Movement),
            ("velocity", Movement),
            ("dash",     Movement),
            ("jump",     Movement),
            ("run",      Movement),
            ("mov",      Movement), // "mov" cobre move/moving/movement
            ("climb",    Movement),
            ("stamina",  Movement),
            ("friction", Movement),
            ("gravity",  Movement),
            ("accel",    Movement),
            ("lift",     Movement),
            ("boost",    Movement),
            ("facing",   Movement),
            ("wind",     Movement),
            ("swim",     Movement),
            ("duck",     Movement),
            ("fly",      Movement),

            ("state",    State),
            ("dummy",    State), // estado Dummy do Player (cutscene/travas)
            ("timer",    State),
            ("cooldown", State),
            ("grace",    State),
            ("buffer",   State),
            ("control",  State),
            ("intro",    State),
            ("respawn",  State),
            ("counter",  State),
            ("paused",   State),
            ("frozen",   State),

            ("sprite",   Visual),
            ("reflect",  Visual),
            ("flip",     Visual),
            ("render",   Visual),
            ("light",    Visual),
            ("bloom",    Visual),
            ("hair",     Visual),
            ("color",    Visual),
            ("alpha",    Visual),
            ("flash",    Visual),
            ("particle", Visual),
            ("dust",     Visual),
        };

        public static string Classify(string name, Type valueType)
        {
            if (TransformNames.Contains(name)) return Transform;
            if (VisualNames.Contains(name)) return Visual;
            if (StateNames.Contains(name)) return State;

            // referencias a outras entidades/componentes/cena e colecoes
            if (valueType != null)
            {
                if (typeof(Entity).IsAssignableFrom(valueType)
                    || typeof(Component).IsAssignableFrom(valueType)
                    || typeof(Scene).IsAssignableFrom(valueType)
                    || typeof(ComponentList).IsAssignableFrom(valueType)
                    || typeof(System.Collections.IEnumerable).IsAssignableFrom(valueType)
                        && valueType != typeof(string))
                    return References;
            }

            string lower = name.ToLowerInvariant();
            for (int i = 0; i < Tokens.Length; i++)
                if (lower.Contains(Tokens[i].token))
                    return Tokens[i].group;

            return Other;
        }
    }
}
