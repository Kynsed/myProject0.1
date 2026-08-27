using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub de bloom (visual). Sem efeito no movimento.
    public class BloomPoint : Component
    {
        public Vector2 Position;
        public float Alpha;
        public float Radius;

        public BloomPoint(Vector2 position, float alpha, float radius) : base(false, true)
        {
            Position = position;
            Alpha = alpha;
            Radius = radius;
        }
    }
}
