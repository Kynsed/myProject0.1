using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): hitbox de um golpe do combo. Segue o player, dura os frames
    // ativos do estagio e aplica dano uma unica vez por alvo (componente Health).
    // Direcional: horizontal (Facing), p/ cima (chao + segurando cima) ou p/ baixo
    // (ar + segurando baixo). No primeiro alvo atingido avisa o combo p/ aplicar o recuo.
    public class AttackHitbox : Entity
    {
        public int Stage;
        public int Damage;
        public Vector2 Dir; // unitario: (+-1,0), (0,-1) ou (0,1)

        // caixa horizontal por estagio; a vertical e a mesma rotacionada (w<->h)
        private static readonly Vector2[] Size =
        {
            new Vector2(20f, 14f),  // 1o golpe: rapido, curto
            new Vector2(22f, 14f),  // 2o golpe
            new Vector2(26f, 16f),  // 3o golpe: finisher, mais alcance
        };

        private Player owner;
        private MeleeCombo combo;
        private float life;
        private bool recoiled;
        private HashSet<Entity> alreadyHit = new HashSet<Entity>();

        public AttackHitbox(Player owner, MeleeCombo combo, Vector2 dir, int stage, float activeTime, int damage)
            : base(owner.Position)
        {
            this.owner = owner;
            this.combo = combo;
            Dir = dir;
            Stage = stage;
            Damage = damage;
            life = activeTime;
            Depth = -1000001; // na frente do player

            Vector2 s = Size[stage];
            if (dir.Y == 0f)
            {
                // horizontal: a partir da frente do corpo (8x11), centrado verticalmente
                float xOff = (dir.X > 0f) ? 4f : -4f - s.X;
                Collider = new Hitbox(s.X, s.Y, xOff, -11f + (11f - s.Y) / 2f);
            }
            else
            {
                // vertical: caixa rotacionada, centrada no eixo X do player
                float w = s.Y, h = s.X;
                float yOff = (dir.Y < 0f) ? -11f - h : 0f; // cima: acima da cabeca | baixo: abaixo dos pes
                Collider = new Hitbox(w, h, -w / 2f, yOff);
            }
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
                    if (!recoiled)
                    {
                        recoiled = true;
                        combo.ApplyRecoil(Dir); // recuo oposto ao golpe (1x por golpe)
                    }
                }
            }

            life -= Engine.DeltaTime;
            if (life <= 0f)
                RemoveSelf();
        }
    }
}
