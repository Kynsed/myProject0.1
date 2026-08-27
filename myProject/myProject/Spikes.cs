using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento/kill do Spikes do Celeste: colliders por direcao,
    // condicoes de morte exatas (Up so mata caindo e com Bottom<=Bottom; Down so subindo;
    // Left/Right conforme o sinal de Speed.X), LedgeBlocker (Up/Left/Right) e StaticMover.
    // NOTE: podas de conteudo — sprites/tentacles/cores (visual; o HitboxRenderer desenha
    // o collider), OnShake (offset visual) e o tipo de textura ("default"/"tentacles").
    [Tracked(false)]
    public class Spikes : Entity
    {
        public enum Directions { Up, Down, Left, Right }

        public Directions Direction;
        private int size;
        private PlayerCollider pc;

        public Spikes(Vector2 position, int size, Directions direction) : base(position)
        {
            Depth = -1;
            Direction = direction;
            this.size = size;
            switch (direction)
            {
                case Directions.Up:
                    Collider = new Hitbox(size, 3f, 0f, -3f);
                    Add(new LedgeBlocker(null));
                    break;
                case Directions.Down:
                    Collider = new Hitbox(size, 3f, 0f, 0f);
                    break;
                case Directions.Left:
                    Collider = new Hitbox(3f, size, -3f, 0f);
                    Add(new LedgeBlocker(null));
                    break;
                case Directions.Right:
                    Collider = new Hitbox(3f, size, 0f, 0f);
                    Add(new LedgeBlocker(null));
                    break;
            }
            Add(pc = new PlayerCollider(OnCollide, null, null));
            Add(new StaticMover
            {
                SolidChecker = IsRiding,
                JumpThruChecker = IsRiding,
                OnEnable = OnEnable,
                OnDisable = OnDisable
            });
        }

        private void OnEnable()
        {
            Active = Visible = Collidable = true;
        }

        private void OnDisable()
        {
            Active = Collidable = false;
        }

        private void OnCollide(Player player)
        {
            switch (Direction)
            {
                case Directions.Up:
                    if (player.Speed.Y >= 0f && player.Bottom <= Bottom)
                        player.Die(new Vector2(0f, -1f));
                    break;
                case Directions.Down:
                    if (player.Speed.Y <= 0f)
                        player.Die(new Vector2(0f, 1f));
                    break;
                case Directions.Left:
                    if (player.Speed.X >= 0f)
                        player.Die(new Vector2(-1f, 0f));
                    break;
                case Directions.Right:
                    if (player.Speed.X <= 0f)
                        player.Die(new Vector2(1f, 0f));
                    break;
            }
        }

        private bool IsRiding(Solid solid)
        {
            switch (Direction)
            {
                case Directions.Up: return CollideCheckOutside(solid, Position + Vector2.UnitY);
                case Directions.Down: return CollideCheckOutside(solid, Position - Vector2.UnitY);
                case Directions.Left: return CollideCheckOutside(solid, Position + Vector2.UnitX);
                case Directions.Right: return CollideCheckOutside(solid, Position - Vector2.UnitX);
                default: return false;
            }
        }

        private bool IsRiding(JumpThru jumpThru)
        {
            return Direction == Directions.Up && CollideCheck(jumpThru, Position + Vector2.UnitY);
        }
    }
}
