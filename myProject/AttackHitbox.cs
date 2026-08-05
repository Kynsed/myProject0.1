using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): hitbox de um golpe do combo. Segue o player, dura os frames
    // ativos do estagio e aplica dano uma unica vez por alvo (componente Health).
    // O alcance cresce no finisher (estagio 3).
    public class AttackHitbox : Entity
    {
        public int Stage;
        public int Damage;

        private static readonly Vector2[] Size =
        {
            new Vector2(20f, 14f),  // 1o golpe: rapido, curto
            new Vector2(22f, 14f),  // 2o golpe
            new Vector2(26f, 16f),  // 3o golpe: finisher, mais alcance
        };

        private Player owner;
        private float life;
        private HashSet<Entity> alreadyHit = new HashSet<Entity>();

        public AttackHitbox(Player owner, int stage, float activeTime, int damage)
            : base(owner.Position)
        {
            this.owner = owner;
            Stage = stage;
            Damage = damage;
            life = activeTime;
            Depth = -1000001; // na frente do player

            Vector2 s = Size[stage];
            float xOff = (owner.Facing == Facings.Right) ? 4f : -4f - s.X; // a partir da frente do corpo (8x11)
            float yOff = -11f + (11f - s.Y) / 2f;                          // centrado no corpo
            Collider = new Hitbox(s.X, s.Y, xOff, yOff);
        }

        public override void Update()
        {
            base.Update();
            if (owner.Scene == null || owner.Dead)
            {
                RemoveSelf();
                return;
            }

            Position = owner.Position; // acompanha o player durante o golpe

            foreach (Health health in Scene.Tracker.GetComponents<Health>())
            {
                Entity target = health.Entity;
                if (target == null || !target.Collidable || alreadyHit.Contains(target))
                    continue;
                if (CollideCheck(target))
                {
                    alreadyHit.Add(target);
                    health.Damage(Damage);
                }
            }

            life -= Engine.DeltaTime;
            if (life <= 0f)
                RemoveSelf();
        }
    }
}
