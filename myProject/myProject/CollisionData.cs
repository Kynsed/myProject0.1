using Microsoft.Xna.Framework;

namespace myProject
{
    // Dados de uma colisao de movimento. Fisica fiel.
    public struct CollisionData
    {
        public Vector2 Direction;
        public Vector2 Moved;
        public Vector2 TargetPosition;
        public Platform Hit;
        public Solid Pusher;

        public static readonly CollisionData Empty;
    }
}
