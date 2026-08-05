using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: bloco de movimento especial (boost). Stub agora, portar fiel depois.
    // Superficie minima que o Player referencia (CurrentBooster/LastBooster).
    public class Booster : Entity
    {
        public bool BoostingPlayer;
        public bool Ch9HubTransition;

        public Booster(Vector2 position) : base(position) { }

        public void PlayerBoosted(Player player, Vector2 direction) { }
        public void PlayerReleased() { }
        public void PlayerDied() { }
    }
}
