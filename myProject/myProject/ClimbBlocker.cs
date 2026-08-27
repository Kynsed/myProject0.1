using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: bloqueia escalada).
    [Tracked(false)]
    public class ClimbBlocker : Component
    {
        public bool Blocking = true;
        public bool Edge;

        public ClimbBlocker(bool edge) : base(false, false)
        {
            Edge = edge;
        }

        public static bool Check(Scene scene, Entity entity, Vector2 at)
        {
            Vector2 position = entity.Position;
            entity.Position = at;
            bool result = Check(scene, entity);
            entity.Position = position;
            return result;
        }

        public static bool Check(Scene scene, Entity entity)
        {
            foreach (Component component in scene.Tracker.GetComponents<ClimbBlocker>())
            {
                ClimbBlocker climbBlocker = (ClimbBlocker)component;
                if (climbBlocker.Blocking && entity.CollideCheck(climbBlocker.Entity))
                    return true;
            }
            return false;
        }

        public static bool EdgeCheck(Scene scene, Entity entity, int dir)
        {
            foreach (Component component in scene.Tracker.GetComponents<ClimbBlocker>())
            {
                ClimbBlocker climbBlocker = (ClimbBlocker)component;
                if (climbBlocker.Blocking && climbBlocker.Edge && entity.CollideCheck(climbBlocker.Entity, entity.Position + Vector2.UnitX * dir))
                    return true;
            }
            return false;
        }
    }
}
