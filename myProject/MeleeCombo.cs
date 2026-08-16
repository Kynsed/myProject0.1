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
    //  - o combo de 3 estagios existe SO NO CHAO, e la o golpe trava o player (Dummy sem
    //    gravidade; movimento/queda so nos intervalos); virar de direcao no intervalo reseta
    //    o combo, assim como sair do chao (pulo/queda de borda) com sequencia iniciada no solo
    //  - NO AR (fora o mergulho) o golpe e um SLASH UNICO e solto: nao trava, nao encadeia
    //    combo e o player continua caindo com fisica/controle normais. O ritmo (Air*) cabe
    //    duas vezes na queda do pulo maximo — dois slashes do apice ate o chao
    //  - golpe p/ baixo no ar = ATAQUE DESCENDENTE (como o da Gwendolyn): mergulha reto
    //    ate pousar; acertar algo que interage com o golpe cancela o mergulho e IMPULSIONA
    //    o player p/ cima (Player.Bounce, como a Hornet em Silksong) SEM restaurar o dash
    //  - anti-bounce-infinito: depois do impulso, apertar/segurar ataque na SUBIDA nao
    //    dispara novo mergulho; ele so volta a poder no apice, quando o player comeca a
    //    cair (Speed.Y >= 0). Segurar o botao tambem nao dispara nada sozinho
    //  - sem anti-stall: o slash aereo nao trava nem segura o player no ar, entao mashar
    //    no ar nao sustenta o voo — o player cai igual
    //  - golpe nenhum interrompe outro estado do player: durante o dash (e dream dash,
    //    boost...) nem aperto novo nem aperto bufferizado disparam, e o recuo nao mexe no
    //    Speed. O mergulho so sai quando o dash acaba (ver CanAct)
    //  - verticais NAO fazem parte do combo: golpe unico de 5 que reseta a progressao
    //  - o golpe p/ cima NO CHAO tem timing proprio (Up*), o mais pesado do jogo: e golpe
    //    unico, cobre o teto e trava o player, entao custa o compromisso
    //  - acertar com horizontais da um pequeno recuo oposto ao golpe
    public class MeleeCombo : Component
    {
        // Ritmo de lanca (Gwendolyn): cada golpe e anticipacao -> hitbox ativa -> recuperacao.
        // Sequencia agil com rampa suave; o peso fica concentrado no fim do combo.
        //   estagio 1: 3 + 5 + 4 = 12 frames (o mais rapido)
        //   estagio 2: 4 + 5 + 7 = 16 frames (intermediario)
        //   estagio 3: 5 + 6 + 8 = 19 frames (ligeiramente mais lento) + 6 de recuperacao
        public static readonly int[] Damage = { 5, 7, 9 };
        public static readonly float[] Duration = { 0.20f, 0.26f, 0.32f };   // tempo total do golpe
        public static readonly float[] Windup = { 0.05f, 0.07f, 0.08f };     // antes da hitbox nascer
        public static readonly float[] ActiveTime = { 0.08f, 0.09f, 0.10f }; // hitbox ativa
        // golpe p/ cima NO CHAO: unico, cobre o teto e trava o player — o mais pesado do
        // jogo, no mesmo compromisso do finisher (24 frames: 9 + 7 + 8). Dano segue 5.
        public const float UpDuration = 0.40f;
        public const float UpWindup = 0.15f;
        public const float UpActiveTime = 0.12f;
        // slash aereo (fora o mergulho): unico e solto, o player continua caindo.
        // Calibrado p/ caberem DOIS na queda do pulo maximo — do apice ao chao sao 18
        // frames, e cada slash gasta 8 (2 de anticipacao + 4 ativos + 2 de recuperacao).
        public const float AirDuration = 0.13f;
        public const float AirWindup = 0.04f;
        public const float AirActiveTime = 0.07f;
        public const float FinisherRecovery = 0.10f; // pausa apos o 3o golpe antes de atacar de novo
        public const float ComboWindow = 0.55f;   // tempo apos o fim do golpe p/ continuar o combo
        public const float RecoilSpeed = 60f;     // recuo ao acertar com golpe horizontal (px/s)
        public const float RecoilFriction = 300f; // decaimento do recuo (px/s^2)
        public const float DiveSpeed = 240f;      // velocidade do ataque descendente (px/s)
        // margem de reposicionamento entre golpes: da p/ ajustar o gap com o inimigo, mas
        // andar alem disso quebra o combo
        public const float MoveAllowance = 24f;

        public bool Attacking { get; private set; }
        public bool Recovering => recoveryTimer > 0f;
        public bool Diving { get; private set; }    // ataque descendente em andamento
        public int Stage { get; private set; }      // estagio do golpe em andamento (0..2)
        public int NextStage { get; private set; }  // estagio que o proximo aperto dispara

        private float attackTimer;   // tempo restante do golpe atual (nao usado no mergulho)
        private float windupTimer;   // anticipacao restante ate a hitbox nascer
        private Vector2 pendingDir;  // direcao do golpe que vai nascer apos a anticipacao
        private float pendingActive; // frames ativos do golpe que vai nascer
        private float windowTimer;   // janela p/ continuar o combo (conta fora do golpe)
        private float recoveryTimer; // pausa apos o finisher (bloqueia novo ataque)
        private float comboAnchorX;  // X do player no ultimo golpe (mede o quanto ele andou)
        private bool comboOnGround;  // a sequencia em andamento comecou no solo?
        private bool buffered;
        private bool lockedAttack;   // golpe atual trava o player? (so no chao)
        private bool diveLockedUntilApex; // pos-bounce: sem novo mergulho ate comecar a cair
        private Facings comboFacing; // direcao do combo em andamento
        private AttackHitbox currentAttack;
        private Player player;

        // Estados em que o combate pode mexer no player: Normal (0) e o Dummy (11), que e o
        // estado que o proprio combo usa p/ travar/mergulhar. Em qualquer outro — dash (2),
        // dream dash (9), boost (4), red dash (5)... — quem manda no movimento e o estado,
        // e golpe nenhum pode interromper: nem aperto novo, nem aperto bufferizado.
        private bool CanAct => player.StateMachine.State == 0 || player.StateMachine.State == 11;

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
                diveLockedUntilApex = false;
            else if (diveLockedUntilApex && player.Speed.Y >= 0f)
                diveLockedUntilApex = false; // apice do bounce: comecou a cair, mergulho liberado

            if (Attacking)
            {
                // so bufferiza se o golpe seguinte puder sair; senao o aperto fica no buffer
                // do proprio Input (0.08s) e dispara sozinho quando o player se soltar
                if (Input.Attack.Pressed && CanAct)
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

                // anticipacao: a hitbox so nasce depois dela (peso do golpe)
                if (windupTimer > 0f)
                {
                    windupTimer -= Engine.DeltaTime;
                    if (windupTimer <= 0f)
                        SpawnHitbox(pendingDir, pendingActive);
                }

                // no golpe travado, decai o recuo (unico movimento permitido);
                // no slash aereo a fisica normal cuida do movimento
                if (lockedAttack && player.StateMachine.State == 11)
                {
                    player.Speed.X = Calc.Approach(player.Speed.X, 0f, RecoilFriction * Engine.DeltaTime);
                    player.Speed.Y = Calc.Approach(player.Speed.Y, 0f, RecoilFriction * Engine.DeltaTime);
                }
                attackTimer -= Engine.DeltaTime;
                if (attackTimer <= 0f)
                    FinishAttack();
                return;
            }

            // sequencia iniciada no solo nao sobrevive a sair do chao (pulo/queda de borda)
            if (comboOnGround && !player.OnGround())
            {
                comboOnGround = false;
                NextStage = 0;
            }

            if (windowTimer > 0f)
            {
                windowTimer -= Engine.DeltaTime;
                if (windowTimer <= 0f)
                    NextStage = 0; // ficou sem apertar: combo reseta
            }

            if (recoveryTimer > 0f)
            {
                recoveryTimer -= Engine.DeltaTime; // pausa do finisher: nao ataca ainda
                return;
            }

            if (Input.Attack.Pressed && player.StateMachine.State == 0)
            {
                Input.Attack.ConsumeBuffer();
                // combo so continua se o player ficou por perto e olhando p/ o mesmo lado
                if (NextStage != 0 && (player.Facing != comboFacing
                    || Math.Abs(player.X - comboAnchorX) > MoveAllowance))
                    NextStage = 0;
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
            {
                if (diveLockedUntilApex)
                {
                    EndAttackState();
                    return; // na subida do bounce o mergulho nao dispara (nem outro golpe)
                }
                dir = Vector2.UnitY;
            }
            else
                dir = Vector2.UnitX * (int)player.Facing;

            // o combo de 3 estagios so existe no chao; no ar (fora o mergulho) todo golpe e
            // slash unico com o ritmo Air*, e o p/ cima no chao usa o ritmo pesado Up*.
            // Golpe unico = dano/alcance do 1o estagio e nao avanca a progressao.
            bool onGround = player.OnGround();
            bool vertical = dir.Y != 0f;
            bool dive = dir.Y > 0f;
            bool airSlash = !onGround && !dive;
            bool groundUp = onGround && dir.Y < 0f;
            if (vertical || airSlash)
                stage = 0;

            Attacking = true;
            buffered = false;
            Stage = stage;
            attackTimer = airSlash ? AirDuration : (groundUp ? UpDuration : Duration[stage]);
            NextStage = (vertical || airSlash) ? 0 : (stage + 1) % 3; // depois do 3o o combo recomeca
            comboFacing = player.Facing;
            comboAnchorX = player.X;
            comboOnGround = onGround;
            Diving = dive;

            // trava so no chao (combo horizontal e golpe p/ cima). O slash aereo fica solto:
            // o player continua caindo com fisica/controle normais. O mergulho usa o estado
            // Dummy p/ descer reto, sem controle, ate pousar/acertar.
            // DummyBegin reseta DummyGravity=true, entao desligar DEPOIS de setar o estado.
            lockedAttack = onGround;
            if (lockedAttack || Diving)
            {
                player.StateMachine.State = 11;
                player.DummyGravity = false;
                player.DummyFriction = false;
                player.Speed = Diving ? Vector2.UnitY * DiveSpeed : Vector2.Zero;
            }
            else
                EndAttackState(); // encadeou de um golpe travado p/ um solto: destrava ja

            currentAttack = null;
            if (Diving)
            {
                // mergulho: sem anticipacao (a hitbox lidera a descida) e sem expirar por
                // tempo — dura ate pousar/acertar
                windupTimer = 0f;
                SpawnHitbox(dir, -1f);
            }
            else
            {
                windupTimer = airSlash ? AirWindup : (groundUp ? UpWindup : Windup[stage]);
                pendingActive = airSlash ? AirActiveTime : (groundUp ? UpActiveTime : ActiveTime[stage]);
                pendingDir = dir;
            }
        }

        private void SpawnHitbox(Vector2 dir, float activeTime)
        {
            Audio.Play("attack", player.Center);   // som nasce com a hitbox, nao no aperto
            currentAttack = new AttackHitbox(player, this, dir, Stage, activeTime, Damage[Stage]);
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
                diveLockedUntilApex = true; // novo mergulho so quando comecar a cair
            }
            else if (!CanAct)
                return;                                      // dash e cia: o estado manda no Speed
            else if (lockedAttack)
                player.Speed = -attackDir * RecoilSpeed;
            else
                player.Speed.X = -attackDir.X * RecoilSpeed; // slash aereo: nao freia a queda
        }

        // encerra o golpe atual e abre a janela de combo (dispara o bufferizado, se houver)
        private void FinishAttack()
        {
            bool finisher = Stage == 2; // so golpes horizontais chegam ao 3o estagio
            Attacking = false;
            windupTimer = 0f;
            windowTimer = ComboWindow;
            if (finisher)
            {
                recoveryTimer = FinisherRecovery; // fim do combo: pausa antes do proximo
                buffered = false;
            }
            // o encadeamento bufferizado tambem respeita o estado: se o player entrou num
            // dash no meio do golpe, o aperto morre aqui em vez de cancelar o dash
            if (buffered && CanAct)
                Fire(NextStage);
            else
            {
                buffered = false;
                EndAttackState();
            }
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
