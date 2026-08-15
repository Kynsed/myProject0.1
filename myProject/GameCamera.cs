using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Jogo proprio (nao e port): camera de acompanhamento do metroidvania.
    //
    // A camera do Celeste e presa a sala e reage direto ao Player (Player.Update ->
    // CameraTarget). Aqui o objetivo e outro: legibilidade em exploracao e combate, com
    // uma camera estavel, antecipatoria e discreta. Enquanto uma GameCamera existir na
    // cena ela e a DONA de Level.Camera; o follow fiel do Celeste continua no Player.cs,
    // desligado por Level.FollowCamera (os harnesses de paridade rodam sem GameCamera).
    //
    // Cadeia de um frame:
    //   foco (player) -> zona morta/coleira -> anchor -> + look ahead + look up/down
    //   -> suavizacao exponencial por eixo -> limite de folga -> limites da sala
    //
    // A transicao entre salas NAO passa por aqui: Level.TransitionRoutine (port fiel) pan
    // a camera em CubeOut e, durante ela, o Level so atualiza entidades TransitionUpdate.
    // Ao voltar, a GameCamera se re-sincroniza a partir de onde a transicao parou.
    public class GameCamera : Entity
    {
        public const float ScreenW = 320f;   // backbuffer do jogo (Engine)
        public const float ScreenH = 180f;

        // --- zoom ---
        // A tela e sempre 320x180; o zoom decide QUANTO MUNDO cabe nela. Enquadramento
        // estilo metroidvania (Hollow Knight): o personagem ocupa ~1/12 da altura.
        //   zoom 1.00 -> 320x180 de mundo (o do Celeste)
        //   zoom 1.35 -> 237x133 de mundo (padrao)
        //   zoom 2.00 -> 160x90  de mundo (bem colado)
        // Mudar Zoom em runtime nao corta: o valor atual persegue o alvo a ZoomRate.
        public const float DefaultZoom = 1.35f;
        public const float ZoomRate = 0.8f;   // unidades de zoom por segundo
        public const float ZoomMin = 0.5f;
        public const float ZoomMax = 4f;

        // --- foco / enquadramento vertical ---
        // O player NAO fica no meio da tela: a camera sobe um pouco, deixando ele mais p/
        // baixo. Assim o chao cai no rodape e sobra uma faixa do que existe abaixo dele -
        // enquadramento de metroidvania (Hollow Knight), que abre o cenario acima.
        // Player.Position sao os PES, e e onde esta a linha do chao.
        public const float GroundLineFrac = 0.78f;  // altura de tela onde ficam os pes
        public float FocusOffsetY => -(GroundLineFrac - 0.5f) * ViewHeight;

        // --- zona morta (meia-extensao, em px) ---
        // andar dentro dela nao arrasta a camera: corrigir posicao nao faz a tela tremer
        public const float DeadZoneX = 16f;
        public const float MoveRecenterX = 120f;   // px/s: em movimento continuo, alcanca o player
        public const float GroundRecenterX = 55f;  // px/s: parado no chao, o player volta ao centro
        public const float GroundRecenterY = 80f;  // px/s: no chao o eixo Y volta ao player
        public const float IdleSpeedX = 8f;        // abaixo disso o player conta como parado
        public const float IdleRecenterDelay = 0.5f; // parado de verdade, nao troca de direcao

        // --- coleira vertical no ar ---
        // subir/cair pouco nao mexe na camera; so passar da coleira e que arrasta
        public const float AirLeashUp = 40f;       // pulo inteiro (~19px) cabe aqui
        public const float AirLeashDown = 32f;
        public const float FallCatchUpTime = 0.35f; // caindo mais que isso, a coleira fecha
        public const float FallLeash = 8f;          // queda longa: a camera desce junto

        // --- suavizacao: fracao da distancia que SOBRA depois de 1s (menor = mais rapido) ---
        public const float SmoothX = 0.002f;        // horizontal: relativamente rapida
        public const float SmoothRest = 0.004f;     // parado: assenta no repouso bem macio
        public const float SmoothYGround = 0.02f;   // vertical no chao: mais calma
        public const float SmoothYAir = 0.15f;      // no ar: quase nao reage (espera confirmar)
        public const float SmoothYFall = 0.02f;     // queda longa: acompanha
        public const float SmoothLook = 0.02f;      // volta do look ao normal

        // --- enquadramento: fracoes da MEIA-TELA ---
        // Deslocar a camera e uma decisao de COMPOSICAO (o quanto o player sai do centro),
        // entao estes valores sao fracao da meia-tela e valem em qualquer zoom.
        // Sem zoom (meia-tela 160x90) dao os px entre parenteses.
        // Antecipacao DESLIGADA: em movimento a camera centraliza no player (decisao de
        // design). O regime continua no codigo - subir a fracao religa o espaco a frente.
        public const float LookAheadFracX = 0f;
        public const float RestFracX = 0.06f;       // parado: descanso p/ o lado que encara (10px)
        public const float LookUpFracY = 0.45f;     // area revelada acima (40px)...
        public const float LookDownFracY = 0.60f;   // ...e um pouco maior abaixo (54px)
        public const float MaxLagFracX = 0.60f;     // folga maxima: o player nao encosta na borda
        public const float MaxLagFracY = 0.75f;

        public float LookAheadX => HalfView.X * LookAheadFracX;
        public float RestOffsetX => HalfView.X * RestFracX;
        public float LookUpDist => HalfView.Y * LookUpFracY;
        public float LookDownDist => HalfView.Y * LookDownFracY;

        // --- look ahead horizontal (correndo) ---
        public const float LookAheadMinSpeed = 30f; // abaixo disso nao conta como "indo p/ la"
        public const float LookAheadDelay = 0.25f;  // corrida sustentada, nao toque no direcional
        public const float LookAheadRate = 70f;     // px/s de crescimento do offset

        // --- posicao de descanso (parado no chao) ---
        // A camera nao descansa exatamente no player: recua um tico p/ o lado que ele
        // encara, abrindo o cenario na direcao do olhar. Vira de lado -> desliza p/ o outro.
        // O ajuste NAO sai junto com a parada: espera uma batida (RestDelay) e so entao
        // desliza, com ease in/out (velocidade nasce e morre em zero, sem tranco).
        // Parar e andar de novo nao deve mexer na camera.
        public const float RestDelay = 0.5f;
        public const float RestEaseTime = 0.8f;     // duracao do deslize (ida ou volta)

        // --- olhar p/ cima/baixo (segurar direcional parado no chao) ---
        public const float LookPoseDelay = 0.25f;   // player se inclina (feedback imediato)
        public const float LookMoveDelay = 0.6f;    // so entao a camera comeca a andar
        public const float LookEaseTime = 0.55f;    // ease in/out do deslocamento

        // alvo de zoom: da p/ mexer em runtime (sala apertada, boss, cutscene)
        public float Zoom = DefaultZoom;

        // estado exposto (o inspector le, e a animacao de olhar pode consumir depois)
        public bool LookPose => lookDir != 0 && lookHoldTimer >= LookPoseDelay;
        public bool Looking => lookT > 0f;
        public int LookDir => lookDir;
        public Vector2 Anchor => anchor;
        public float OffsetX => offsetX;   // antecipacao (correndo) ou descanso (parado)
        public float CurrentZoom => zoom;
        public float ViewWidth => ScreenW / zoom;    // quanto mundo cabe na tela
        public float ViewHeight => ScreenH / zoom;
        public Vector2 ViewCenter => SceneAs<Level>().Camera.Position + HalfView;
        private Vector2 HalfView => new Vector2(ViewWidth / 2f, ViewHeight / 2f);

        private Vector2 anchor;        // ponto que a camera persegue (ja passou pela zona morta)
        private float zoom = DefaultZoom;
        private float offsetX;         // antecipacao (correndo) + descanso (parado)
        private float aheadX;          // parcela da antecipacao
        private float restT;           // 0..1 progresso do descanso (antes do ease)
        private int restSign = 1;      // lado do descanso em andamento
        private float lookHoldTimer;   // ha quanto tempo segura o direcional valido
        private int lookDir;           // -1 cima | +1 baixo | 0 nenhum
        private int lookSign = 1;      // lado do deslocamento em andamento (sobrevive ao soltar)
        private float lookT;           // 0..1 progresso do deslocamento (antes do ease)
        private float fallTimer;       // ha quanto tempo cai sem parar
        private float runTimer;        // ha quanto tempo corre p/ o mesmo lado
        private int runDir;            // lado dessa corrida
        private float idleTimer;       // ha quanto tempo esta parado no chao
        private Player lastPlayer;
        private Rectangle lastBounds;

        public GameCamera()
        {
            // depth baixo = ultimo da lista a atualizar: le o player ja movido neste frame
            Depth = -2000000;
            Visible = false;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Level lvl = (Level)scene;
            lvl.FollowCamera = this;
            // Camera.Left/Right/Top/Bottom saem do Viewport, que so nasce certo dentro do
            // Engine. Fixar aqui deixa a conta valida tambem nos harnesses headless.
            lvl.Camera.Viewport.Width = (int)ScreenW;
            lvl.Camera.Viewport.Height = (int)ScreenH;
        }

        public override void Removed(Scene scene)
        {
            Level lvl = scene as Level;
            if (lvl != null && lvl.FollowCamera == this)
                lvl.FollowCamera = null;
            base.Removed(scene);
        }

        // nascimento da sala/respawn: camera ja centrada, sem swoosh inicial
        public void SnapToPlayer(Player player)
        {
            Level lvl = SceneAs<Level>();
            zoom = MathHelper.Clamp(Zoom, ZoomMin, ZoomMax);   // no nascimento o zoom ja entra pronto
            lvl.Camera.Zoom = zoom;
            anchor = Focus(player);
            ResetOffsets();
            lvl.Camera.Position = ClampToBounds(anchor - HalfView, lvl.Bounds);
        }

        // depois de uma transicao de sala: adota a posicao que a transicao deixou, sem pulo
        public void ResyncFromCamera()
        {
            Level lvl = SceneAs<Level>();
            anchor = lvl.Camera.Position + HalfView;
            ResetOffsets();
        }

        private void ResetOffsets()
        {
            offsetX = 0f;
            aheadX = 0f;
            restT = 0f;
            lookT = 0f;
            lookHoldTimer = 0f;
            lookDir = 0;
            fallTimer = 0f;
            runTimer = 0f;
            runDir = 0;
            idleTimer = 0f;
        }

        private Vector2 Focus(Player player)
        {
            return new Vector2(player.X, player.Y + FocusOffsetY);
        }

        public override void Update()
        {
            base.Update();
            Level lvl = SceneAs<Level>();
            Player player = lvl.Tracker.GetEntity<Player>();
            if (player == null)
                return;

            // troca de sala/player: re-sincroniza em vez de perseguir de longe
            if (player != lastPlayer)
            {
                lastPlayer = player;
                lastBounds = lvl.Bounds;
                SnapToPlayer(player);
                return;
            }
            if (lvl.Bounds != lastBounds)
            {
                lastBounds = lvl.Bounds;
                ResyncFromCamera();
            }

            float dt = Engine.DeltaTime;
            bool onGround = player.OnGround();
            Vector2 focus = Focus(player);
            bool idle = onGround && Math.Abs(player.Speed.X) < IdleSpeedX && Input.MoveX.Value == 0;
            idleTimer = idle ? idleTimer + dt : 0f;

            // zoom persegue o alvo (mudar Zoom em runtime nao corta a imagem)
            zoom = Calc.Approach(zoom, MathHelper.Clamp(Zoom, ZoomMin, ZoomMax), ZoomRate * dt);
            lvl.Camera.Zoom = zoom;

            UpdateLook(player, onGround, dt);
            UpdateOffsetX(player, dt);

            // --- eixo X: zona morta; fora dela o anchor anda junto ---
            float dx = focus.X - anchor.X;
            if (dx > DeadZoneX)
                anchor.X = focus.X - DeadZoneX;
            else if (dx < -DeadZoneX)
                anchor.X = focus.X + DeadZoneX;

            // A sobra da zona morta nao pode virar deslocamento permanente: assim que o
            // movimento e CONTINUO (mesmo criterio da antecipacao), o anchor alcanca o
            // player e a camera passa a centralizar nele. Parado, a mesma coisa, mais
            // devagar - e so depois de parar de verdade, porque trocar de direcao passa
            // por velocidade zero e isso nao pode arrastar a camera.
            if (runTimer >= LookAheadDelay)
                anchor.X = Calc.Approach(anchor.X, focus.X, MoveRecenterX * dt);
            else if (idleTimer >= IdleRecenterDelay)
                anchor.X = Calc.Approach(anchor.X, focus.X, GroundRecenterX * dt);

            // --- eixo Y: no chao recentra devagar; no ar, coleira ---
            float smoothY;
            if (onGround)
            {
                fallTimer = 0f;
                anchor.Y = Calc.Approach(anchor.Y, focus.Y, GroundRecenterY * dt);
                // parado no chao o repouso assenta rapido nos dois eixos
                smoothY = (idleTimer >= RestDelay) ? SmoothRest : SmoothYGround;
            }
            else
            {
                fallTimer = (player.Speed.Y > 0f) ? fallTimer + dt : 0f;
                bool longFall = fallTimer > FallCatchUpTime;
                float up = AirLeashUp;
                float down = longFall ? FallLeash : AirLeashDown;
                float dy = focus.Y - anchor.Y;
                if (dy < -up)
                    anchor.Y = focus.Y + up;
                else if (dy > down)
                    anchor.Y = focus.Y - down;
                smoothY = longFall ? SmoothYFall : SmoothYAir;
            }
            // durante o look quem manda no eixo Y e o ease do proprio look
            if (lookT > 0f)
                smoothY = SmoothLook;

            // --- alvo final e suavizacao exponencial (independente de fps) ---
            Vector2 half = HalfView;
            Vector2 target = anchor + new Vector2(offsetX, LookOffsetY()) - half;
            Vector2 pos = lvl.Camera.Position;
            // parado nao ha movimento p/ filtrar: o eixo X assenta no descanso mais rapido
            pos.X += (target.X - pos.X) * Damp(idleTimer >= RestDelay ? SmoothRest : SmoothX, dt);
            pos.Y += (target.Y - pos.Y) * Damp(smoothY, dt);

            // folga maxima: por mais que a camera atrase, o player nao encosta na borda
            Vector2 center = pos + half;
            float lagX = half.X * MaxLagFracX;
            float lagY = half.Y * MaxLagFracY;
            center.X = MathHelper.Clamp(center.X, focus.X - lagX, focus.X + lagX);
            center.Y = MathHelper.Clamp(center.Y, focus.Y - lagY, focus.Y + lagY);
            pos = center - half;

            lvl.Camera.Position = ClampToBounds(pos, lvl.Bounds);
        }

        // Deslocamento horizontal da camera, em dois regimes:
        //   CORRENDO  -> antecipacao: abre espaco p/ onde o player vai. So conta corrida
        //                SUSTENTADA (LookAheadDelay); vaivem no direcional nao balanca nada.
        //                Cresce e decai a taxa fixa.
        //   PARADO    -> descanso: recua um tico p/ o lado que o player encara. Aqui a taxa
        //                fixa nao serve: comecar e parar de repente le como tranco mesmo
        //                devagar. O deslize usa um progresso 0..1 com Ease.CubeInOut, entao
        //                a velocidade nasce em zero, cresce e morre em zero.
        //                Virar de lado volta ao centro primeiro e so entao vai p/ o outro
        //                lado — nunca troca o sinal com o deslocamento levantado.
        private void UpdateOffsetX(Player player, float dt)
        {
            int dir = (Math.Abs(player.Speed.X) >= LookAheadMinSpeed) ? Math.Sign(player.Speed.X) : 0;
            if (dir != 0 && dir == runDir)
                runTimer += dt;
            else
            {
                runDir = dir;
                runTimer = 0f;
            }

            bool running = dir != 0 && runTimer >= LookAheadDelay;
            aheadX = Calc.Approach(aheadX, running ? LookAheadX * dir : 0f, LookAheadRate * dt);

            bool resting = !running && idleTimer >= RestDelay;
            int wantSign = (int)player.Facing;
            if (restT <= 0f)
                restSign = wantSign;      // no centro: adota o lado atual
            else if (wantSign != restSign)
                resting = false;          // virou de lado: primeiro desfaz o deslocamento
            restT = Calc.Approach(restT, resting ? 1f : 0f, dt / RestEaseTime);

            offsetX = aheadX + Ease.CubeInOut(restT) * RestOffsetX * restSign;
        }

        // Olhar p/ cima/baixo: so parado no chao e sob controle normal. O aperto curto nao
        // move nada (LookMoveDelay); o deslocamento entra e sai sempre com ease.
        private void UpdateLook(Player player, bool onGround, float dt)
        {
            MeleeCombo combo = player.Get<MeleeCombo>();
            bool canLook = onGround
                && player.StateMachine.State == 0
                && Math.Abs(player.Speed.X) < 8f
                && Input.MoveX.Value == 0
                && (combo == null || !combo.Attacking);

            int dir = canLook ? Input.MoveY.Value : 0;
            if (dir != 0)
            {
                if (dir != lookDir)
                {
                    lookDir = dir;
                    lookHoldTimer = 0f;
                }
                lookHoldTimer += dt;
            }
            else
            {
                lookDir = 0;
                lookHoldTimer = 0f;
            }

            float goal = (lookDir != 0 && lookHoldTimer >= LookMoveDelay) ? 1f : 0f;
            if (goal > 0f)
                lookSign = lookDir;   // o retorno mantem o lado ate zerar
            lookT = Calc.Approach(lookT, goal, dt / LookEaseTime);
        }

        private float LookOffsetY()
        {
            if (lookT <= 0f)
                return 0f;
            float dist = (lookSign < 0) ? -LookUpDist : LookDownDist;
            return Ease.CubeInOut(lookT) * dist;
        }

        // fracao da distancia coberta neste frame, dado o quanto deve SOBRAR em 1s
        private static float Damp(float remainAfter1s, float dt)
        {
            return 1f - (float)Math.Pow(remainAfter1s, dt);
        }

        // a camera para nos limites da sala e nunca mostra o lado de fora
        private Vector2 ClampToBounds(Vector2 pos, Rectangle bounds)
        {
            float maxX = Math.Max(bounds.Left, bounds.Right - ViewWidth);
            float maxY = Math.Max(bounds.Top, bounds.Bottom - ViewHeight);
            pos.X = MathHelper.Clamp(pos.X, bounds.Left, maxX);
            pos.Y = MathHelper.Clamp(pos.Y, bounds.Top, maxY);
            return pos;
        }
    }
}

