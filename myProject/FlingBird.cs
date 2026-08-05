using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (fling bird). Stub agora, portar fiel depois.
    public class FlingBird : Entity
    {
        public static Vector2 FlingSpeed = new Vector2(380f, -100f);

        public FlingBird(Vector2 position) : base(position) { }
    }
}
