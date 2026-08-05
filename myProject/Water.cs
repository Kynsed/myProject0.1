using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (agua/natacao). Stub agora, portar fiel depois.
    [Tracked(false)]
    public class Water : Entity
    {
        public Surface TopSurface = new Surface();

        // NOTE: superficie de agua (ripples). Stub.
        public class Surface
        {
            public void DoRipple(Vector2 position, float resetTimeMultiplier) { }
        }
    }
}
