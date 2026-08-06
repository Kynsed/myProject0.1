using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do FlyFeather do Celeste: collider 20x20 centrado,
    // PlayerCollider que dispara Player.StartStarFly() (estado 19, voo), variante shielded
    // que rebate (PointBounce) quando o player nao esta dashando, e respawn em 3s.
    // NOTE: podas de conteudo — sprite/outline/bloom/luz/wigglers, sons, particulas do
    // CollectRoutine e o Level.Shake da coleta.
    [Tracked(false)]
    public class FlyFeather : Entity
    {
        public static ParticleType P_Collect = new ParticleType();
        public static ParticleType P_Boost = new ParticleType();
        public static ParticleType P_Flying = new ParticleType();
        public static ParticleType P_Respawn = new ParticleType();

        private const float RespawnTime = 3f;

        private bool shielded;
        private bool singleUse;
        private float respawnTimer;

        public FlyFeather(Vector2 position, bool shielded = false, bool singleUse = false)
            : base(position)
        {
            this.shielded = shielded;
            this.singleUse = singleUse;
            Collider = new Hitbox(20f, 20f, -10f, -10f);
            Add(new PlayerCollider(OnPlayer, null, null));
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
            }
        }

        private void Respawn()
        {
            if (!Collidable)
                Collidable = true;
        }

        private void OnPlayer(Player player)
        {
            if (shielded && !player.DashAttacking)
            {
                player.PointBounce(Center); // bolha: rebate sem coletar
                return;
            }
            if (player.StartStarFly())
            {
                Collidable = false;
                if (!singleUse)
                    respawnTimer = RespawnTime;
            }
        }
    }
}
