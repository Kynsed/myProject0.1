using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (afeta movimento: bloqueia ledge grab/hop/dash correct).
    [Tracked(false)]
    public class LedgeBlocker : Component
    {
        public bool Blocking = true;
        public Func<Player, bool> BlockChecker;

        public LedgeBlocker(Func<Player, bool> blockChecker = null) : base(false, false)
        {
            BlockChecker = blockChecker;
        }

        public bool HopBlockCheck(Player player)
        {
            return Blocking && player.CollideCheck(Entity, player.Position + Vector2.UnitX * (float)player.Facing * 8f) && (BlockChecker == null || BlockChecker(player));
        }

        public bool JumpThruBoostCheck(Player player)
        {
            return Blocking && player.CollideCheck(Entity, player.Position - Vector2.UnitY * 2f) && (BlockChecker == null || BlockChecker(player));
        }

        public bool DashCorrectCheck(Player player)
        {
            return Blocking && player.CollideCheck(Entity, player.Position) && (BlockChecker == null || BlockChecker(player));
        }
    }
}
