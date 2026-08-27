using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: port podado. So o subset que afeta o movimento (Facing + IceMode, lidos pelo
    // Player no wall-slide). Sprites/SFX/CoreMode (conteudo) removidos.
    [Tracked(false)]
    public class WallBooster : Entity
    {
        public Facings Facing;
        public bool IceMode;

        public WallBooster(Vector2 position, float height, bool left, bool notCoreMode) : base(position)
        {
            if (left)
            {
                Facing = Facings.Left;
                Collider = new Hitbox(2f, height, 0f, 0f);
            }
            else
            {
                Facing = Facings.Right;
                Collider = new Hitbox(2f, height, 6f, 0f);
            }
        }
    }
}
