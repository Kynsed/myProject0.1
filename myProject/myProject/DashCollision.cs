using Microsoft.Xna.Framework;

namespace myProject
{
    // Resposta de colisao de dash (Player da dash num Solid). Parte da fisica de movimento.
    public delegate DashCollisionResults DashCollision(Player player, Vector2 direction);

    public enum DashCollisionResults
    {
        NormalCollision,
        NormalOverride,
        Rebound,
        Bounce,
        Ignore
    }
}
