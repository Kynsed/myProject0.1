using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: chao seguro p/ respawn).
    [Tracked(false)]
    public class SafeGroundBlocker : Component
    {
        public bool Blocking = true;
        public Collider CheckWith;

        public SafeGroundBlocker(Collider checkWith = null) : base(false, false)
        {
            CheckWith = checkWith;
        }

        public bool Check(Player player)
        {
            if (!Blocking)
                return false;
            Collider collider = Entity.Collider;
            if (CheckWith != null)
                Entity.Collider = CheckWith;
            bool result = player.CollideCheck(Entity);
            Entity.Collider = collider;
            return result;
        }

        public override void DebugRender(Camera camera)
        {
            Collider collider = Entity.Collider;
            if (CheckWith != null)
                Entity.Collider = CheckWith;
            Entity.Collider.Render(camera, Color.Aqua);
            Entity.Collider = collider;
        }
    }
}
