using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): boneco de treino p/ testar o combo. Tem Health e pisca ao
    // levar hit (via Health.FlashTimer, lido pelo renderer). Por padrao tem vida infinita
    // (alvo permanente de treino); com infinite=false morre e renasce 2s depois.
    [Tracked(false)]
    public class TrainingDummy : Entity
    {
        public Health Health;
        public float RespawnTimer;

        public TrainingDummy(Vector2 position, bool infinite = true) : base(position)
        {
            Depth = 100;
            Collider = new Hitbox(12f, 16f, -6f, -16f); // pes na posicao (como o Player)
            Add(Health = new Health(30));
            Health.Infinite = infinite;
            Health.OnDeath = () =>
            {
                Collidable = false;
                Visible = false;
                RespawnTimer = 2f;
            };
        }

        public override void Update()
        {
            base.Update();
            if (RespawnTimer > 0f)
            {
                RespawnTimer -= Engine.DeltaTime;
                if (RespawnTimer <= 0f)
                {
                    Health.Current = Health.Max;
                    Collidable = true;
                    Visible = true;
                }
            }
        }
    }
}
