using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (dream dash). Stub agora, portar fiel depois.
    [Tracked(false)]
    public class DreamBlock : Solid
    {
        public DreamBlock(Vector2 position, float width, float height)
            : base(position, width, height, false) { }

        public void OnPlayerExit(Player player) { }
        public void FootstepRipple(Vector2 position) { }
    }
}
