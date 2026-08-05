using System;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): combo basico de 3 estagios no botao de ataque, inspirado no
    // padrao de ataque da Gwendolyn (Odin Sphere Leifthrasir), reduzido de 4 p/ 3 golpes.
    //  - dano por estagio: 5 / 7 / 9
    //  - velocidade decrescente: 1o rapido, 2o intermediario, 3o lento
    //  - apertar de novo dentro da janela continua o combo; passar da janela reseta p/ o 1o
    //  - apertar durante um golpe fica bufferizado e dispara o proximo ao final
    public class MeleeCombo : Component
    {
        public static readonly int[] Damage = { 5, 7, 9 };
        public static readonly float[] Duration = { 0.20f, 0.30f, 0.42f };   // tempo total do golpe
        public static readonly float[] ActiveTime = { 0.12f, 0.16f, 0.22f }; // hitbox ativa
        public const float ComboWindow = 0.55f; // tempo apos o fim do golpe p/ continuar o combo

        public bool Attacking { get; private set; }
        public int Stage { get; private set; }      // estagio do golpe em andamento (0..2)
        public int NextStage { get; private set; }  // estagio que o proximo aperto dispara

        private float attackTimer;   // tempo restante do golpe atual
        private float windowTimer;   // janela p/ continuar o combo (conta fora do golpe)
        private bool buffered;
        private Player player;

        public MeleeCombo() : base(true, false) { }

        public override void Added(Entity entity)
        {
            base.Added(entity);
            player = (Player)entity;
        }

        public override void Update()
        {
            if (player.Dead)
                return;

            if (Attacking)
            {
                if (Input.Attack.Pressed)
                {
                    Input.Attack.ConsumeBuffer();
                    buffered = true;
                }
                attackTimer -= Engine.DeltaTime;
                if (attackTimer <= 0f)
                {
                    Attacking = false;
                    windowTimer = ComboWindow;
                    if (buffered)
                        Fire(NextStage);
                }
                return;
            }

            if (windowTimer > 0f)
            {
                windowTimer -= Engine.DeltaTime;
                if (windowTimer <= 0f)
                    NextStage = 0; // ficou sem apertar: combo reseta
            }

            if (Input.Attack.Pressed)
            {
                Input.Attack.ConsumeBuffer();
                Fire(NextStage);
            }
        }

        private void Fire(int stage)
        {
            Attacking = true;
            buffered = false;
            Stage = stage;
            attackTimer = Duration[stage];
            NextStage = (stage + 1) % 3; // depois do 3o golpe o combo recomeca
            Scene.Add(new AttackHitbox(player, stage, ActiveTime[stage], Damage[stage]));
        }
    }
}
