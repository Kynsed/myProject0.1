using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Fase C — paridade de movimento. Dirige o Player real (headless, input simulado) e mede
// o comportamento contra as constantes do Celeste (Player.cs). Tolerancias p/ discretizacao 60fps.
namespace MonocleSmoke
{
    public static class ParityTest
    {
        // valores-fonte (Player.cs): comparados ao comportamento medido.
        private const float MaxRun = 90f;        // public const MaxRun
        private const float MaxFall = 160f;      // public const MaxFall
        private const float JumpSpeed = -105f;   // private const JumpSpeed
        private const float DashSpeed = 240f;    // private const DashSpeed
        private const float DashTimeS = 0.15f;   // private const DashTime
        private const float WallJumpH = 130f;    // private const WallJumpHSpeed
        private const float ClimbMaxStamina = 110f;

        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        private static bool Near(float a, float b, float tol) => Math.Abs(a - b) <= tol;

        public static int Run()
        {
            Console.WriteLine("== Fase C: paridade de movimento (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            // headless: Engine.Pooler so nasce no ctor do Engine; remocao de entidade precisa dele
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());

            TestMaxRun();
            TestMaxFall();
            TestJump();
            TestDash();
            TestWallJump();
            TestClimbStamina();

            Console.WriteLine("-- precisao (feel) --");
            TestCoyote();
            TestVarJump();
            TestDashRefill();
            TestSuperDash();
            TestHyperDash();
            TestJumpBuffer();
            TestCornerCorrection();

            Console.WriteLine("-- camera --");
            TestCamera();

            Console.WriteLine("-- salas (transicao) --");
            TestRooms();

            Console.WriteLine("-- tiles (SolidTiles/Grid) --");
            TestTiles();

            Console.WriteLine("-- respawn (morte) --");
            TestRespawn();

            Console.WriteLine("-- spikes (hazard) --");
            TestSpikes();

            Console.WriteLine(fails == 0 ? "== PARIDADE OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        // ---- helpers ----
        private static Player Boot(out Scene scene, Vector2 spawn, bool leftWall)
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));   // chao y=160
            if (leftWall)
                lvl.Add(new Solid(new Vector2(0f, 0f), 8f, 180f, false));  // parede esq x[0,8]
            Player p = new Player(spawn, PlayerSpriteMode.Madeline);
            lvl.Add(p);
            lvl.Begin();
            lvl.BeforeUpdate();   // flush adds -> Player.Added
            scene = lvl;
            return p;
        }

        private static void Step(Scene scene, params Keys[] keys)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState(keys);
            foreach (VirtualInput vi in MInput.VirtualInputs) vi.Update();
            scene.BeforeUpdate();
            scene.Update();
            scene.AfterUpdate();
        }

        // assenta no chao (cai ate OnGround), cap de seguranca.
        private static void Settle(Scene scene, Player p)
        {
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(scene);
        }

        // ---- testes ----
        private static void TestMaxRun()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            for (int i = 0; i < 30; i++) Step(s, Keys.Right);    // corre
            Check("MaxRun: Speed.X satura em 90", Near(p.Speed.X, MaxRun, 1.5f), "Speed.X=" + p.Speed.X);
        }

        private static void TestMaxFall()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 10f), false);
            for (int i = 0; i < 40; i++) Step(s);                // queda livre
            Check("MaxFall: Speed.Y satura em 160", Near(p.Speed.Y, MaxFall, 1.5f), "Speed.Y=" + p.Speed.Y);
        }

        private static void TestJump()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            bool ground = p.OnGround();
            Step(s, Keys.C);                                     // pula
            Check("Jump: impulso inicial == JumpSpeed (-105)", ground && Near(p.Speed.Y, JumpSpeed, 2f), "OnGround=" + ground + " Speed.Y=" + p.Speed.Y);
        }

        private static void TestDash()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            Step(s, Keys.Right, Keys.X);                         // dash p/ direita
            float peak = 0f;
            int dashState = -1;
            for (int i = 0; i < 12; i++)
            {
                Step(s, Keys.Right);
                if (p.StateMachine.State == 2) dashState = 2;
                peak = Math.Max(peak, Math.Abs(p.Speed.X));
            }
            Check("Dash: entra no estado Dash (2)", dashState == 2, "state=" + p.StateMachine.State);
            Check("Dash: velocidade de pico == DashSpeed (240)", Near(peak, DashSpeed, 4f), "peakSpeedX=" + peak);
        }

        private static void TestWallJump()
        {
            // wall jump = estado Normal, no ar, encostado na parede (nao agarrando).
            // hitbox 8 larg, offset -4 => spawn x=12 deixa o lado esquerdo em x=8 (adjacente a parede x[0,8]).
            Player p = Boot(out Scene s, new Vector2(12f, 90f), true);
            for (int i = 0; i < 6; i++) Step(s);                       // cai ao lado da parede
            bool air = !p.OnGround();
            Step(s, Keys.C);                                           // wall jump (neutro) -> p/ longe (+X)
            Check("WallJump: empurra p/ longe da parede (Speed.X ~ +130)", air && Near(p.Speed.X, WallJumpH, 8f), "air=" + air + " Speed.X=" + p.Speed.X);
            Check("WallJump: impulso vertical (Speed.Y == -105)", Near(p.Speed.Y, JumpSpeed, 4f), "Speed.Y=" + p.Speed.Y);
        }

        private static void TestClimbStamina()
        {
            Player p = Boot(out Scene s, new Vector2(9f, 150f), true);
            Settle(s, p);
            for (int i = 0; i < 4; i++) Step(s, Keys.Left, Keys.Z);  // agarra
            bool climbing = p.StateMachine.State == 1;
            float startStam = p.Stamina;
            for (int i = 0; i < 30; i++) Step(s, Keys.Left, Keys.Z, Keys.Up); // escala subindo
            Check("Climb: entra no estado Climb (1)", climbing, "state=" + p.StateMachine.State);
            Check("Climb: stamina inicia em 110 e drena", startStam <= ClimbMaxStamina + 0.1f && p.Stamina < startStam, "start=" + startStam + " agora=" + p.Stamina);
        }

        // ---- precisao ----

        // cena com plataforma estreita (borda em x=100) p/ andar e sair pela borda.
        private static Player Platform(out Scene scene)
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 220) };
            lvl.Add(new Solid(new Vector2(40f, 100f), 60f, 8f, false));   // plataforma x[40,100] topo y=100
            Player p = new Player(new Vector2(60f, 92f), PlayerSpriteMode.Madeline);
            lvl.Add(p);
            lvl.Begin();
            lvl.BeforeUpdate();
            scene = lvl;
            return p;
        }

        private static void TestCoyote()
        {
            // positivo: pula DENTRO da janela de coyote (JumpGraceTime=0.1s)
            Player p = Platform(out Scene s);
            Settle(s, p);
            int g = 0;
            while (p.OnGround() && g++ < 80) Step(s, Keys.Right);   // anda ate sair da borda
            Step(s, Keys.Right);                                    // 1 frame no ar
            Step(s, Keys.Right, Keys.C);                            // pula (~0.03s apos sair)
            Check("Coyote: pula logo apos sair da borda (Speed.Y ~ -105)", Near(p.Speed.Y, JumpSpeed, 6f), "Speed.Y=" + p.Speed.Y);

            // negativo: espera a janela passar e tenta pular
            Player p2 = Platform(out Scene s2);
            Settle(s2, p2);
            g = 0;
            while (p2.OnGround() && g++ < 80) Step(s2, Keys.Right);
            for (int i = 0; i < 12; i++) Step(s2, Keys.Right);      // >0.1s no ar
            Step(s2, Keys.Right, Keys.C);
            Check("Coyote: apos a janela NAO pula (continua caindo)", p2.Speed.Y > 0f, "Speed.Y=" + p2.Speed.Y);
        }

        private static void TestVarJump()
        {
            // tap: solta o pulo imediatamente -> apice mais baixo
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            float groundY = p.Y, tapMin = p.Y;
            Step(s, Keys.C);                                        // 1 frame de pulo, depois solta
            for (int i = 0; i < 45; i++) { Step(s); tapMin = Math.Min(tapMin, p.Y); }
            float tapH = groundY - tapMin;

            // held: segura o pulo durante a janela (VarJumpTime=0.2s) -> apice mais alto
            Player p2 = Boot(out Scene s2, new Vector2(40f, 150f), false);
            Settle(s2, p2);
            float groundY2 = p2.Y, heldMin = p2.Y;
            for (int i = 0; i < 14; i++) { Step(s2, Keys.C); heldMin = Math.Min(heldMin, p2.Y); }
            for (int i = 0; i < 31; i++) { Step(s2); heldMin = Math.Min(heldMin, p2.Y); }
            float heldH = groundY2 - heldMin;

            Check("Var-jump: segurar pula mais alto que tap", heldH > tapH + 3f, "tapH=" + tapH + " heldH=" + heldH);
        }

        private static void TestDashRefill()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 100f), false);  // no ar (chao y=160)
            for (int i = 0; i < 3; i++) Step(s);
            int before = p.Dashes;
            Step(s, Keys.Right, Keys.X);                            // dash no ar consome
            int during = p.Dashes;
            Settle(s, p);
            for (int i = 0; i < 14; i++) Step(s);                  // assenta + cooldown de refill (0.1s)
            int after = p.Dashes;
            Check("Dash refill: 1 -> 0 ao dashar", before == 1 && during == 0, "before=" + before + " during=" + during);
            Check("Dash refill: volta ao max ao tocar o chao", after >= 1, "after=" + after);
        }

        private static void TestSuperDash()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            Step(s, Keys.Right, Keys.X);                            // dash no chao p/ direita
            Step(s, Keys.Right, Keys.C);                            // pula durante o dash -> super
            Check("Super dash: Speed.X ~ +260 (SuperJumpH)", Near(p.Speed.X, 260f, 12f), "Speed.X=" + p.Speed.X);
            Check("Super dash: Speed.Y ~ -105", Near(p.Speed.Y, JumpSpeed, 8f), "Speed.Y=" + p.Speed.Y);
        }

        private static void TestHyperDash()
        {
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);
            Step(s, Keys.Down);                                    // agacha
            Step(s, Keys.Down, Keys.Right, Keys.X);                // dash agachado
            Step(s, Keys.Right, Keys.C);                           // pula -> hyper (x1.25 / y0.5)
            Check("Hyper dash: Speed.X ~ +325 (260 x1.25)", Near(p.Speed.X, 325f, 18f), "Speed.X=" + p.Speed.X);
            Check("Hyper dash: Speed.Y ~ -52.5 (-105 x0.5)", Near(p.Speed.Y, -52.5f, 10f), "Speed.Y=" + p.Speed.Y);
        }

        private static void TestJumpBuffer()
        {
            // buffer = SEGURAR o pulo enquanto cai; dispara ao tocar (soltar zera o buffer, igual Celeste).
            Player p = Boot(out Scene s, new Vector2(40f, 150f), false);
            Settle(s, p);                                          // estado Normal no chao
            p.Position.Y -= 6f;                                    // recoloca no ar
            p.Speed.Y = 140f;                                      // caindo rapido (pouso deterministico)
            bool fired = false;
            for (int i = 0; i < 8; i++) { Step(s, Keys.C); if (p.Speed.Y < -50f) fired = true; }  // segura C
            Check("Jump buffer: segurar o pulo no ar dispara ao tocar (buffer 0.08s)", fired, "fired=" + fired);
        }

        private static void TestCornerCorrection()
        {
            // Upward corner correction (OnCollideV, Speed.Y<0): ate 4px de nudge horizontal
            // p/ nao matar o pulo num quininho de teto. Setup: teto cobre x<58; player em
            // x=60 (Left=56) sobrepoe 2px -> ao bater, desloca +2 (X=62) e o pulo continua.
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));   // chao (topo 160)
            lvl.Add(new Solid(new Vector2(0f, 126f), 58f, 8f, false));     // teto, base y=134
            Player p = new Player(new Vector2(60f, 150f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);

            float xStart = p.X;
            for (int i = 0; i < 25; i++) Step(lvl, Keys.C);   // pula reto no quininho
            Check("Corner correction: nudge de +2px no quininho do teto (X 60 -> 62)",
                p.X == xStart + 2f, "X=" + p.X);
            Check("Corner correction: pulo continua (passa da linha do teto)",
                p.Top < 126f, "Top=" + p.Top);
        }

        private static void TestCamera()
        {
            // Camera segue o Player via Player.Update (fiel):
            //   pos += (CameraTarget - pos) * (1 - (0.01/num)^DeltaTime), num=1 fora do StRedDash.
            // CameraTarget = (X-160, Y-90) clampado em [Bounds.Left, Right-320] x [Top, Bottom-180].
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 640, 360) };
            lvl.Add(new Solid(new Vector2(0f, 340f), 640f, 20f, false));
            Player p = new Player(new Vector2(320f, 330f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);

            // lerp de 1 frame bate com a formula exata (player parado => alvo constante)
            lvl.Camera.Position = Vector2.Zero;
            Vector2 pos = lvl.Camera.Position;
            Vector2 target = p.CameraTarget;
            Vector2 expected = pos + (target - pos) * (1f - (float)Math.Pow(0.01f, 1f / 60f));
            Step(lvl);
            Check("Camera: lerp de 1 frame == formula do Celeste",
                Near(lvl.Camera.Position.X, expected.X, 0.01f) && Near(lvl.Camera.Position.Y, expected.Y, 0.01f),
                "cam=" + lvl.Camera.Position + " esperado=" + expected);

            // convergencia: parado ~3s, camera chega no alvo
            for (int i = 0; i < 180; i++) Step(lvl);
            Check("Camera: converge para CameraTarget",
                Near(lvl.Camera.Position.X, p.CameraTarget.X, 0.5f) && Near(lvl.Camera.Position.Y, p.CameraTarget.Y, 0.5f),
                "cam=" + lvl.Camera.Position + " alvo=" + p.CameraTarget);

            // clamp nas bordas do Bounds
            p.Position = new Vector2(20f, 330f);
            Check("Camera: CameraTarget clampa na borda esq (Bounds.Left)",
                p.CameraTarget.X == 0f, "CT.X=" + p.CameraTarget.X);
            p.Position = new Vector2(620f, 330f);
            Check("Camera: CameraTarget clampa na borda dir (Bounds.Right-320)",
                p.CameraTarget.X == 320f, "CT.X=" + p.CameraTarget.X);
        }

        private static void TestRooms()
        {
            // 2 salas lado a lado; chao continuo. Cruzar x=320 p/ direita deve disparar a
            // transicao fiel: Bounds troca, player desliza a 60px/s (Player.TransitionTo),
            // camera pousa no alvo da sala 2, OnTransition refila o dash.
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Rooms.Add(new Rectangle(0, 0, 320, 180));
            lvl.Rooms.Add(new Rectangle(320, 0, 320, 180));
            lvl.Add(new Solid(new Vector2(0f, 160f), 640f, 20f, false));
            Player p = new Player(new Vector2(290f, 150f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);

            bool started = false;
            for (int i = 0; i < 120 && !started; i++)
            {
                Step(lvl, Keys.Right);
                started = lvl.Transitioning;
            }
            Check("Salas: transicao dispara ao cruzar a borda", started,
                "Transitioning=" + lvl.Transitioning);

            // durante o glide o Player.Update nao roda; a rotina o move a 60px/s (1px/frame).
            // Bounds troca no 1o tick da rotina (igual ao Celeste: Session.Level muda na coroutine).
            p.Dashes = 0;
            float x0 = p.X;
            Step(lvl);
            Check("Salas: glide do player a 60px/s (Player.TransitionTo)",
                lvl.Transitioning && Near(p.X - x0, 1f, 0.01f), "dx=" + (p.X - x0));
            Check("Salas: Bounds troca para a sala 2 no inicio da rotina",
                lvl.Bounds == lvl.Rooms[1] && lvl.PreviousBounds == lvl.Rooms[0],
                "Bounds=" + lvl.Bounds);

            int frames = 0;
            while (lvl.Transitioning && frames++ < 120) Step(lvl);
            Check("Salas: transicao termina e camera pousa no alvo da sala 2",
                !lvl.Transitioning && Near(lvl.Camera.Position.X, 320f, 0.5f),
                "frames=" + frames + " cam.X=" + lvl.Camera.Position.X);
            Check("Salas: OnTransition refila o dash (0 -> 1)",
                p.Dashes == 1, "Dashes=" + p.Dashes);
        }

        private static void TestTiles()
        {
            // geometria por tiles (SolidTiles fiel: Solid + Grid 8x8). Colisao pixel-exata:
            // parede nas colunas 0-1 (borda direita x=16), chao na fileira 3 (topo y=24).
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            string[] map =
            {
                "room 0 0",
                "##......",
                "##......",
                "##......",
                "########",
            };
            RoomMap.Load(lvl, map);
            Player p = new Player(new Vector2(40f, 16f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);
            Check("Tiles: pousa exato no topo do tile (Bottom=24)",
                p.Bottom == 24f && p.OnGround(), "Bottom=" + p.Bottom);

            for (int i = 0; i < 40; i++) Step(lvl, Keys.Left);
            Check("Tiles: parede de tiles para o MoveH (Left=16)",
                p.Left == 16f, "Left=" + p.Left);

            for (int i = 0; i < 3; i++) Step(lvl, Keys.Left, Keys.Z);
            Check("Tiles: agarra a parede de tiles (Climb)",
                p.StateMachine.State == 1, "state=" + p.StateMachine.State);
        }

        private static void TestRespawn()
        {
            // chao pela metade: andar p/ direita cai no buraco -> morte no fundo (EnforceBounds)
            // -> PlayerDeadBody (timing fiel ~0.54s sem bounce) -> Level.Reload -> novo Player
            // no Session.RespawnPoint com IntroTypes.Respawn (estado 14) e controle de volta.
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 160f, 20f, false));
            lvl.Session.RespawnPoint = new Vector2(40f, 150f);
            Player p = new Player(new Vector2(40f, 150f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);

            // cai no buraco e sai por baixo
            bool died = false;
            for (int i = 0; i < 300 && !died; i++)
            {
                Step(lvl, Keys.Right);
                died = lvl.Tracker.GetEntity<Player>() == null;
            }
            Check("Respawn: morte remove o Player e cria o PlayerDeadBody",
                died && lvl.Entities.FindFirst<PlayerDeadBody>() != null,
                "died=" + died);

            // rotina do corpo (~33 frames) + Reload; espera o novo Player assumir o controle
            Player p2 = null;
            int frames = 0;
            for (; frames < 180; frames++)
            {
                Step(lvl);
                p2 = lvl.Tracker.GetEntity<Player>();
                if (p2 != null && p2.StateMachine.State == 0)
                    break;
            }
            Check("Respawn: novo Player nasce no RespawnPoint",
                p2 != null && Near(p2.X, 40f, 1f), p2 == null ? "p2=null" : "X=" + p2.X);
            Check("Respawn: intro termina e controle volta (estado Normal)",
                p2 != null && p2.StateMachine.State == 0,
                "frames=" + frames + (p2 == null ? "" : " state=" + p2.StateMachine.State));
            Check("Respawn: corpo removido da cena",
                lvl.Entities.FindFirst<PlayerDeadBody>() == null, "");
        }

        private static void TestSpikes()
        {
            // espinhos '^' via RoomMap: pisar mata (Die (0,-1), corpo com bounce/tween);
            // condicao fiel: Up so mata com Speed.Y >= 0 — subir por baixo nao mata.
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            string[] map =
            {
                "room 0 0",
                "....................",
                "....................",
                "..........^^^.......",
                "####################",
            };
            RoomMap.Load(lvl, map);
            lvl.Session.RespawnPoint = new Vector2(20f, 24f);
            Player p = new Player(new Vector2(20f, 20f), PlayerSpriteMode.Madeline);
            lvl.Add(p); lvl.Begin(); lvl.BeforeUpdate();
            Settle(lvl, p);

            // subir por baixo (pulando encostado na lateral) nao aciona; caminhar em cima mata
            bool died = false;
            for (int i = 0; i < 180 && !died; i++)
            {
                Step(lvl, Keys.Right);
                died = lvl.Tracker.GetEntity<Player>() == null;
            }
            Check("Spikes: pisar nos espinhos mata (PlayerCollider + Die)",
                died && lvl.Entities.FindFirst<PlayerDeadBody>() != null, "died=" + died);

            // corpo com bounce (0,-1): freeze + tween 0.375s + efeito 0.542s -> respawn
            Player p2 = null;
            int frames = 0;
            for (; frames < 240; frames++)
            {
                Step(lvl);
                p2 = lvl.Tracker.GetEntity<Player>();
                if (p2 != null && p2.StateMachine.State == 0)
                    break;
            }
            Check("Spikes: respawn apos morte com bounce",
                p2 != null && p2.StateMachine.State == 0 && Near(p2.X, 20f, 1f),
                "frames=" + frames + (p2 == null ? " p2=null" : " X=" + p2.X));
        }
    }
}
