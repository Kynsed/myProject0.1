using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub de iluminacao (visual). Sem efeito no movimento.
    public class VertexLight : Component
    {
        public Vector2 Position;
        public Color Color;
        public float Alpha;
        public int StartRadius;
        public int EndRadius;

        public VertexLight(Vector2 position, Color color, float alpha, int startFade, int endFade)
            : base(false, true)
        {
            Position = position;
            Color = color;
            Alpha = alpha;
            StartRadius = startFade;
            EndRadius = endFade;
        }
    }
}
