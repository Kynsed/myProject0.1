using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (swap block). Stub agora, portar fiel depois.
    [Tracked(false)]
    public class SwapBlock : Solid
    {
        public Vector2 Direction;
        public bool Swapping;

        public SwapBlock() : base(Vector2.Zero, 0f, 0f, false) { }
    }
}
