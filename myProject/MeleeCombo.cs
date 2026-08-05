using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): combo basico de 3 estagios no botao de ataque, inspirado no
    // padrao de ataque da Gwendolyn (Odin Sphere Leifthrasir), reduzido de 4 p/ 3 golpes.
    //  - dano por estagio: 5 / 7 / 9
    //  - velocidade decrescente: 1o rapido, 2o intermediario, 3o lento
    //  - apertar de novo dentro da janela continua o combo; passar da janela reseta p/ o 1o
    //  - apertar durante um golpe fica bufferizado e dispara o proximo ao final
    //  - durante o golpe o player NAO se move nem cai (estado Dummy sem gravidade);
    //    movimento/queda so nos intervalos entre os golpes
    //  - virar de direcao no intervalo reseta o combo p/ o 1o golpe
    //  - so ataca a partir do estado Normal (0): nada de atacar em dash/climb/etc.
    //  - direcional: chao + segurando CIMA = golpe acima; ar + segurando BAIXO = golpe abaixo
    //  - acertar algo que interage com o golpe (Health) da um pequeno recuo oposto ao golpe
    public class MeleeCombo : Component
    {
        public static readonly int[] Damage = { 5, 7, 9 };
        public static readonly float[] Duration = { 0.20f, 0.30f, 0.42f };   // tempo total do golpe
        public static readonly float[] ActiveTime = { 0.12f, 0.16f, 0.22f }; // hitbox ativa
        public const float ComboWindow = 0.55f;  // tempo apos o fim do golpe p/ continuar o combo
        public const float RecoilSpeed = 60f;    // impulso do recuo ao acertar (px/s)
        public const float RecoilFriction = 300f; // decaimento do recuo (px/s^2)

        public bool Attacking { get; private set; }
        public int Stage { get; private set; }      // estagio do golpe em andamento (0..2)
        public int NextStage { get; private set; }  // estagio que o proximo aperto dispara

        private float attackTimer;   // tempo restante do golpe atual
        private float windowTimer;   // janela p/ continuar o combo (conta fora do golpe)
        private bool buffered;
        private Facings comboFacing; // direcao do combo em andamento
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
                // decai o recuo (unico movimento permitido durante o golpe)
                player.Speed.X = Calc.Approach(player.Speed.X, 0f, RecoilFriction * Engine.DeltaTime);
                player.Speed.Y = Calc.Approach(player.Speed.Y, 0f, RecoilFriction * Engine.DeltaTime);
                attackTimer -= Engine.DeltaTime;
                if (attackTimer <= 0f)
                {
                    Attacking = false;
                    windowTimer = ComboWindow;
                    if (buffered)
                        Fire(NextStage); // encadeia sem intervalo: continua travado no ar
                    else
                        EndAttackState();
                }
                return;
            }

            if (windowTimer > 0f)
            {
                windowTimer -= Engine.DeltaTime;
                if (windowTimer <= 0f)
                    NextStage = 0; // ficou sem apertar: combo reseta
            }

            if (Input.Attack.Pressed && player.StateMachine.State == 0)
            {
                Input.Attack.ConsumeBuffer();
                if (NextStage != 0 && player.Facing != comboFacing)
                    NextStage = 0; // virou de direcao no intervalo: combo reseta
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
            comboFacing = player.Facing;

            // direcao do golpe: chao + cima = acima | ar + baixo = abaixo | senao horizontal
            Vector2 dir;
            if (player.OnGround() && Input.MoveY.Value == -1)
                dir = -Vector2.UnitY;
            else if (!player.OnGround() && Input.MoveY.Value == 1)
                dir = Vector2.UnitY;
            else
                dir = Vector2.UnitX * (int)player.Facing;

            // trava movimento e queda durante o golpe: estado Dummy (11) sem gravidade.
            // DummyBegin reseta DummyGravity=true, entao desligar DEPOIS de setar o estado.
            // DummyFriction fica desligado: o decaimento do recuo e feito aqui no combo.
            player.StateMachine.State = 11;
            player.DummyGravity = false;
            player.DummyFriction = false;
            player.Speed = Vector2.Zero;

            Scene.Add(new AttackHitbox(player, this, dir, stage, ActiveTime[stage], Damage[stage]));
        }

        // pequeno recuo oposto ao golpe quando o ataque acerta algo que interage com ele
        public void ApplyRecoil(Vector2 attackDir)
        {
            player.Speed = -attackDir * RecoilSpeed;
        }

        // devolve o controle ao player no intervalo entre golpes
        private void EndAttackState()
        {
            if (!player.Dead && player.StateMachine.State == 11)
            {
                player.DummyGravity = true;
                player.DummyFriction = true;
                player.StateMachine.State = 0;
            }
        }
    }
}
