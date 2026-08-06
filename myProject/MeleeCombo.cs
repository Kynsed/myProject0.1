using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): combo basico de 3 estagios no botao de ataque, inspirado no
    // padrao de ataque da Gwendolyn (Odin Sphere Leifthrasir), reduzido de 4 p/ 3 golpes.
    //  - dano por estagio: 5 / 7 / 9
    //  - velocidade decrescente: 1o rapido, 2o intermediario, 3o lento
    //  - alcance: 1o e 2o iguais; so o 3o e ligeiramente maior
    //  - apertar de novo dentro da janela continua o combo; passar da janela reseta p/ o 1o
    //  - apertar durante um golpe fica bufferizado e dispara o proximo ao final
    //  - horizontais e o golpe p/ cima NO CHAO travam o player (Dummy sem gravidade;
    //    movimento/queda so nos intervalos); virar de direcao no intervalo reseta o combo
    //  - golpe p/ cima NO AR nao trava: player segue com fisica/controle normais
    //  - golpe p/ baixo no ar = ATAQUE DESCENDENTE (como o da Gwendolyn): mergulha reto
    //    ate pousar; acertar algo que interage com o golpe cancela o mergulho e IMPULSIONA
    //    o player p/ cima (Player.Bounce, como a Hornet em Silksong) SEM restaurar o dash
    //  - anti-stall: os golpes horizontais no ar sao limitados a UM ciclo do combo (3)
    //    por voo — depois disso apertar nao dispara e o player cai; pousar restaura
    //  - verticais NAO fazem parte do combo: golpe unico de 5 que reseta a progressao
    //  - acertar com horizontais da um pequeno recuo oposto ao golpe
    public class MeleeCombo : Component
    {
        public static readonly int[] Damage = { 5, 7, 9 };
        public static readonly float[] Duration = { 0.20f, 0.30f, 0.42f };   // tempo total do golpe
        public static readonly float[] ActiveTime = { 0.12f, 0.16f, 0.22f }; // hitbox ativa
        public const float ComboWindow = 0.55f;   // tempo apos o fim do golpe p/ continuar o combo
        public const float RecoilSpeed = 60f;     // recuo ao acertar com golpe horizontal (px/s)
        public const float RecoilFriction = 300f; // decaimento do recuo (px/s^2)
        public const float DiveSpeed = 240f;      // velocidade do ataque descendente (px/s)

        public bool Attacking { get; private set; }
        public bool Diving { get; private set; }    // ataque descendente em andamento
        public int Stage { get; private set; }      // estagio do golpe em andamento (0..2)
        public int NextStage { get; private set; }  // estagio que o proximo aperto dispara

        private float attackTimer;   // tempo restante do golpe atual (nao usado no mergulho)
        private float windowTimer;   // janela p/ continuar o combo (conta fora do golpe)
        private bool buffered;
        private bool lockedAttack;   // golpe atual trava o player?
        private int airComboLeft = 3; // golpes horizontais aereos restantes neste voo
        private Facings comboFacing; // direcao do combo em andamento
        private AttackHitbox currentAttack;
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

            if (player.OnGround())
                airComboLeft = 3; // pousar restaura o ciclo aereo

            if (Attacking)
            {
                if (Input.Attack.Pressed)
                {
                    Input.Attack.ConsumeBuffer();
                    buffered = true;
                }

                if (Diving)
                {
                    // mergulho termina ao pousar (o acerto termina via ApplyRecoil/Bounce)
                    if (player.OnGround())
                    {
                        EndDive();
                        FinishAttack();
                    }
                    return;
                }

                // no golpe travado, decai o recuo (unico movimento permitido);
                // no golpe solto (p/ cima aereo) a fisica normal cuida do movimento
                if (lockedAttack)
                {
                    player.Speed.X = Calc.Approach(player.Speed.X, 0f, RecoilFriction * Engine.DeltaTime);
                    player.Speed.Y = Calc.Approach(player.Speed.Y, 0f, RecoilFriction * Engine.DeltaTime);
                }
                attackTimer -= Engine.DeltaTime;
                if (attackTimer <= 0f)
                    FinishAttack();
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
            // direcao do golpe: cima = acima | ar + baixo = mergulho | senao horizontal
            Vector2 dir;
            if (Input.MoveY.Value == -1)
                dir = -Vector2.UnitY;
            else if (!player.OnGround() && Input.MoveY.Value == 1)
                dir = Vector2.UnitY;
            else
                dir = Vector2.UnitX * (int)player.Facing;

            // vertical = golpe unico: dano/timing do 1o estagio e nao avanca o combo
            bool vertical = dir.Y != 0f;
            if (vertical)
                stage = 0;

            // anti-stall: no ar, so um ciclo do combo horizontal por voo
            if (!vertical && !player.OnGround())
            {
                if (airComboLeft <= 0)
                {
                    EndAttackState(); // esgotou: nao dispara e o player segue caindo
                    return;
                }
                airComboLeft--;
            }

            Attacking = true;
            buffered = false;
            Stage = stage;
            attackTimer = Duration[stage];
            NextStage = vertical ? 0 : (stage + 1) % 3; // depois do 3o golpe o combo recomeca
            comboFacing = player.Facing;
            Diving = dir.Y > 0f;

            // trava: horizontais e golpe p/ cima no chao. Golpe p/ cima no ar fica solto;
            // o mergulho usa o estado Dummy p/ descer reto, sem controle, ate pousar/acertar.
            // DummyBegin reseta DummyGravity=true, entao desligar DEPOIS de setar o estado.
            lockedAttack = dir.Y == 0f || (dir.Y < 0f && player.OnGround());
            if (lockedAttack || Diving)
            {
                player.StateMachine.State = 11;
                player.DummyGravity = false;
                player.DummyFriction = false;
                player.Speed = Diving ? Vector2.UnitY * DiveSpeed : Vector2.Zero;
            }
            else
                EndAttackState(); // encadeou de um golpe travado p/ um solto: destrava ja

            // no mergulho a hitbox nao expira por tempo: dura ate pousar/acertar
            float active = Diving ? -1f : ActiveTime[stage];
            currentAttack = new AttackHitbox(player, this, dir, stage, active, Damage[stage]);
            Scene.Add(currentAttack);
        }

        // pequeno recuo oposto ao golpe; no mergulho, cancela e impulsiona p/ cima (Hornet)
        public void ApplyRecoil(Vector2 attackDir)
        {
            if (attackDir.Y > 0f)
            {
                EndDive();
                FinishAttack();
                // impulso p/ cima (Speed.Y=-140 + var jump), preservando o dash:
                // o Bounce fiel refila dash/stamina, mas o acerto do mergulho NAO deve
                int dashes = player.Dashes;
                player.Bounce(player.Bottom);
                player.Dashes = dashes;
            }
            else
                player.Speed = -attackDir * RecoilSpeed;
        }

        // encerra o golpe atual e abre a janela de combo (dispara o bufferizado, se houver)
        private void FinishAttack()
        {
            Attacking = false;
            windowTimer = ComboWindow;
            if (buffered)
                Fire(NextStage);
            else
                EndAttackState();
        }

        // encerra o mergulho: remove a hitbox persistente e zera a descida
        private void EndDive()
        {
            Diving = false;
            buffered = false; // pouso/acerto nao encadeia golpe bufferizado no meio do mergulho
            if (currentAttack != null && currentAttack.Scene != null)
                currentAttack.RemoveSelf();
            currentAttack = null;
            if (player.Speed.Y > 0f)
                player.Speed.Y = 0f;
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
