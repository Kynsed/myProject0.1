using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: colisao Player x entidade).
    [Tracked(false)]
    public class PlayerCollider : Component
    {
        public Action<Player> OnCollide;
        public Collider Collider;
        public Collider FeatherCollider;

        public PlayerCollider(Action<Player> onCollide, Collider collider = null, Collider featherCollider = null) : base(false, false)
        {
            OnCollide = onCollide;
            Collider = collider;
            FeatherCollider = featherCollider;
        }

        public bool Check(Player player)
        {
            Collider collider = Collider;
            if (FeatherCollider != null && player.StateMachine.State == 19)
                collider = FeatherCollider;
            if (collider == null)
            {
                if (player.CollideCheck(Entity))
                {
                    OnCollide(player);
                    return true;
                }
                return false;
            }
            Collider collider2 = Entity.Collider;
            Entity.Collider = collider;
            bool flag = player.CollideCheck(Entity);
            Entity.Collider = collider2;
            if (flag)
            {
                OnCollide(player);
                return true;
            }
            return false;
        }

        public override void DebugRender(Camera camera)
        {
            if (Collider != null)
            {
                Collider collider = Entity.Collider;
                Entity.Collider = Collider;
                Collider.Render(camera, Color.HotPink);
                Entity.Collider = collider;
            }
        }
    }
}
