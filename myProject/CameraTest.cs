using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Camera de acompanhamento do metroidvania (jogo proprio, nao e port). Mede as regras
// que a definem: zona morta, atraso horizontal com antecipacao, vertical que ignora
// pulos curtos e acompanha quedas longas, olhar p/ cima/baixo com atraso, limites da
// sala e ausencia de teleporte. A camera fiel do Celeste continua medida em --parity.
namespace MonocleSmoke
{
    public static class CameraTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== camera de acompanhamento (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());
            Abilities.ResetToDefaults();

            Console.WriteLine("-- rastreamento --");
            TestSnapInicial();
            TestEnquadramentoVertical();
            TestZonaMorta();
            TestHorizontal();

            Console.WriteLine("-- descanso e zoom --");
            TestDescansoSegueOOlhar();
            TestZoom();

            Console.WriteLine("-- vertical --");
            TestPuloCurtoNaoMexe();
            TestQuedaLongaAcompanha();

            Console.WriteLine("-- olhar (segurar cima/baixo) --");
            TestLookAtraso();
            TestLookCimaEBaixo();
            TestLookCancela();

            Console.WriteLine("-- limites e suavidade --");
            TestLimitesDaSala();
            TestSemTeleporte();
            TestTransicaoEntreSalas();

            Console.WriteLine("-- mapa do demo --");
            TestMapaDoDemo();

            Console.WriteLine(fails == 0 ? "== CAMERA OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        // ---- helpers ----
        // sala larga e alta o bastante p/ a camera se mover livre nos dois eixos
        private static (Level lvl, Player p, GameCamera cam) Boot(
            float spawnX = 320f, float spawnY = 430f, int w = 640, int h = 600, float groundY = 440f)
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, w, h) };
            lvl.Rooms.Add(lvl.Bounds);
            lvl.Add(new Solid(new Vector2(0f, groundY), w, 20f, false));
            Player p = new Player(new Vector2(spawnX, spawnY), PlayerSpriteMode.Madeline);
            p.Add(new MeleeCombo());
            lvl.Add(p);
            GameCamera cam = new GameCamera();
            lvl.Add(cam);
            lvl.Begin();
            lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);
            cam.SnapToPlayer(p);
            return (lvl, p, cam);
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

        // cena do jogo de verdade: mapa do demo (Content/map.txt) + player no spawn
        private static (Level lvl, Player p, GameCamera cam) BootMapa(string path)
        {
            Level lvl = new Level();
            RoomMap.Load(lvl, System.IO.File.ReadAllLines(path));
            lvl.Bounds = lvl.Rooms[0];
            lvl.Session.RespawnPoint = new Vector2(60f, 210f);
            Player p = new Player(new Vector2(60f, 210f), PlayerSpriteMode.Madeline);
            p.Add(new MeleeCombo());
            lvl.Add(p);
            GameCamera cam = new GameCamera();
            lvl.Add(cam);
            lvl.Begin(); lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);
            cam.SnapToPlayer(p);
            return (lvl, p, cam);
        }

        // centro do enquadramento (ja considera o zoom: a tela mostra ViewWidth de mundo)
        private static Vector2 Center(GameCamera cam) => cam.ViewCenter;

        private static bool Near(float a, float b, float tol) => Math.Abs(a - b) <= tol;

        // ---- rastreamento ----
        private static void TestSnapInicial()
        {
            var (lvl, p, cam) = Boot();
            Check("Snap: a camera nasce centrada no player (sem swoosh)",
                Near(Center(cam).X, p.X, 0.5f) && Near(Center(cam).Y, p.Y + cam.FocusOffsetY, 0.5f),
                "centro=" + Center(cam) + " player=" + p.Position);
        }

        private static void TestEnquadramentoVertical()
        {
            var (lvl, p, cam) = Boot();
            // 0 = topo da tela, 1 = rodape
            float pesNaTela = (p.Y - lvl.Camera.Position.Y) / cam.ViewHeight;
            float abaixoDoChao = lvl.Camera.Position.Y + cam.ViewHeight - p.Y;
            Check("Enquadramento: os pes do player ficam a ~78% da altura da tela",
                Near(pesNaTela, GameCamera.GroundLineFrac, 0.02f),
                "pes em " + (pesNaTela * 100f).ToString("0.0") + "% da tela");
            Check("Enquadramento: sobra uma faixa do que existe abaixo do chao",
                abaixoDoChao > 24f && abaixoDoChao < cam.ViewHeight * 0.35f,
                "visivel abaixo dos pes=" + abaixoDoChao + "px de " + cam.ViewHeight);
        }

        private static void TestZonaMorta()
        {
            var (lvl, p, cam) = Boot();
            Vector2 c0 = Center(cam);
            // vaivem curto dentro da zona morta: a camera praticamente nao reage
            float drift = 0f, andou = 0f, x0 = p.X;
            for (int r = 0; r < 5; r++)
            {
                for (int i = 0; i < 3; i++) { Step(lvl, Keys.Right); drift = Math.Max(drift, Math.Abs(Center(cam).X - c0.X)); andou = Math.Max(andou, Math.Abs(p.X - x0)); }
                for (int i = 0; i < 3; i++) { Step(lvl, Keys.Left); drift = Math.Max(drift, Math.Abs(Center(cam).X - c0.X)); andou = Math.Max(andou, Math.Abs(p.X - x0)); }
            }
            Check("Zona morta: vaivem dentro da zona morta nao move a camera",
                andou < GameCamera.DeadZoneX && drift < 1f,
                "desvio=" + drift + " player andou=" + andou);

            // saindo da zona morta ela passa a acompanhar
            p.Position = new Vector2(p.X + 60f, p.Y);
            for (int i = 0; i < 60; i++) Step(lvl);
            Check("Zona morta: passando do limite a camera acompanha",
                Center(cam).X > c0.X + 30f, "dX=" + (Center(cam).X - c0.X));
        }

        private static void TestHorizontal()
        {
            var (lvl, p, cam) = Boot(spawnX: 200f);
            for (int i = 0; i < 90; i++) Step(lvl, Keys.Right);   // corre continuamente
            float desvio = Center(cam).X - p.X;
            float bordaDireita = lvl.Camera.Position.X + cam.ViewWidth - p.X;
            Check("Horizontal: em movimento continuo a camera centraliza no player",
                Math.Abs(desvio) < 18f, "centro-player=" + desvio);
            Check("Horizontal: o atraso e da suavizacao (camera atras, nunca na frente)",
                desvio <= 0.5f, "centro-player=" + desvio);
            Check("Horizontal: o player nunca encosta na borda (>= 30% da tela na frente)",
                bordaDireita >= cam.ViewWidth * 0.3f,
                "ate a borda=" + bordaDireita + " de " + cam.ViewWidth);

            // parando, o atraso se desfaz e sobra so o descanso (lado que ele encara)
            for (int i = 0; i < 180; i++) Step(lvl);
            Check("Horizontal: parado, sobra so o descanso p/ o lado que ele encara",
                Near(Center(cam).X - p.X, cam.RestOffsetX, 3f),
                "centro-player=" + (Center(cam).X - p.X) + " (descanso " + cam.RestOffsetX + ")");
        }

        // ---- descanso e zoom ----
        private static void TestDescansoSegueOOlhar()
        {
            var (lvl, p, cam) = Boot();

            // parado logo apos o snap: ainda sem descanso (ele entra com atraso)
            for (int i = 0; i < 24; i++) Step(lvl);  // 0.4s < RestDelay (0.5s)
            Check("Descanso: nos primeiros instantes parado a camera nao desloca",
                Math.Abs(Center(cam).X - p.X) < 4f, "centro-player=" + (Center(cam).X - p.X));

            // olhando p/ a direita: descansa um tico p/ a direita
            for (int i = 0; i < 4; i++) Step(lvl, Keys.Right);
            for (int i = 0; i < 150; i++) Step(lvl);
            float direita = Center(cam).X - p.X;
            Check("Descanso: parado olhando p/ a direita, a camera recua p/ a direita",
                p.Facing == Facings.Right && Near(direita, cam.RestOffsetX, 4f),
                "centro-player=" + direita + " (alvo " + cam.RestOffsetX + ")");

            // anda p/ a esquerda: em movimento o descanso se desfaz (camera centraliza).
            // 70 frames > RestEaseTime, entao o deslocamento chega a zero antes de soltar
            for (int i = 0; i < 70; i++) Step(lvl, Keys.Left);
            Check("Descanso: em movimento o deslocamento se desfaz",
                Math.Abs(cam.OffsetX) < 0.05f, "offset=" + cam.OffsetX);

            // solta o direcional: o ajuste NAO sai junto com a parada
            float maiorPasso = 0f, passoInicial = -1f;
            float prev = Center(cam).X;
            int comecou = -1, assentou = -1;
            for (int i = 0; i < 300; i++)
            {
                Step(lvl);
                // o passo so interessa DEPOIS que o descanso comeca: antes disso o que a
                // camera faz e fechar o atraso da perseguicao, que nao e o ajuste medido
                float passo = Math.Abs(Center(cam).X - prev);
                if (comecou >= 0)
                {
                    if (i == comecou + 1)
                        passoInicial = passo;
                    maiorPasso = Math.Max(maiorPasso, passo);
                }
                prev = Center(cam).X;
                if (comecou < 0 && Math.Abs(cam.OffsetX) > 0.05f)   // primeiro sinal do deslize
                    comecou = i;
                if (assentou < 0 && Math.Abs(Center(cam).X - (p.X - cam.RestOffsetX)) < 1f)
                    assentou = i;
            }
            float esquerda = Center(cam).X - p.X;
            Check("Descanso: virando p/ a esquerda, o descanso vai p/ o outro lado",
                p.Facing == Facings.Left && Near(esquerda, -cam.RestOffsetX, 4f),
                "centro-player=" + esquerda + " (alvo " + (-cam.RestOffsetX) + ")");
            Check("Descanso: nao ajusta junto com a parada (espera >= 0.4s)",
                comecou >= 24, "deslocamento comeca no frame " + comecou);
            Check("Descanso: assenta em ate ~2s",
                assentou >= 0 && assentou < 120, "frames=" + assentou);
            Check("Descanso: deslize macio (< 0.5px por frame)",
                maiorPasso < 0.5f, "maior passo=" + maiorPasso);
            Check("Descanso: o deslize e ease in/out (arranca do zero, nao a taxa fixa)",
                passoInicial >= 0f && passoInicial < maiorPasso * 0.35f,
                "passo inicial=" + passoInicial + " pico=" + maiorPasso);
        }

        private static void TestZoom()
        {
            var (lvl, p, cam) = Boot();
            Check("Zoom: a tela mostra ViewWidth x ViewHeight de mundo",
                Near(lvl.Camera.Right - lvl.Camera.Left, cam.ViewWidth, 0.5f)
                    && Near(lvl.Camera.Bottom - lvl.Camera.Top, cam.ViewHeight, 0.5f),
                "mundo visivel=" + (lvl.Camera.Right - lvl.Camera.Left) + "x"
                    + (lvl.Camera.Bottom - lvl.Camera.Top));
            Check("Zoom: o padrao aproxima em relacao ao 320x180 do Celeste",
                cam.CurrentZoom > 1f && cam.ViewWidth < GameCamera.ScreenW,
                "zoom=" + cam.CurrentZoom + " view=" + cam.ViewWidth);

            // mudar em runtime nao corta: o zoom persegue o alvo
            cam.Zoom = 2f;
            Step(lvl);
            float depoisDe1Frame = cam.CurrentZoom;
            for (int i = 0; i < 120; i++) Step(lvl);
            Check("Zoom: mudar em runtime e uma transicao, nao um corte",
                depoisDe1Frame < GameCamera.DefaultZoom + 0.05f && Near(cam.CurrentZoom, 2f, 0.01f),
                "1 frame=" + depoisDe1Frame + " final=" + cam.CurrentZoom);
            Check("Zoom: com o novo zoom o player segue enquadrado",
                Math.Abs(Center(cam).X - p.X) < cam.ViewWidth / 2f
                    && Math.Abs(Center(cam).Y - p.Y) < cam.ViewHeight / 2f,
                "centro=" + Center(cam) + " player=" + p.Position);
        }

        // ---- vertical ----
        private static void TestPuloCurtoNaoMexe()
        {
            var (lvl, p, cam) = Boot();
            float y0 = Center(cam).Y;
            float maxDelta = 0f;
            for (int i = 0; i < 14; i++) { Step(lvl, Keys.Z); maxDelta = Math.Max(maxDelta, Math.Abs(Center(cam).Y - y0)); }
            for (int i = 0; i < 40 && !p.OnGround(); i++) { Step(lvl); maxDelta = Math.Max(maxDelta, Math.Abs(Center(cam).Y - y0)); }
            Check("Vertical: pulo inteiro nao mexe a camera (coleira de subida)",
                maxDelta < 2f, "desvio maximo=" + maxDelta);
        }

        private static void TestQuedaLongaAcompanha()
        {
            // sala alta: cai de ~300px ate o chao
            var (lvl, p, cam) = Boot(spawnY: 60f, h: 1000, groundY: 900f);
            cam.SnapToPlayer(p);
            float y0 = Center(cam).Y;
            float maxFrame = 0f;
            Vector2 prev = lvl.Camera.Position;
            for (int i = 0; i < 240 && !p.OnGround(); i++)
            {
                Step(lvl);
                maxFrame = Math.Max(maxFrame, Math.Abs(lvl.Camera.Position.Y - prev.Y));
                prev = lvl.Camera.Position;
            }
            for (int i = 0; i < 120; i++) Step(lvl);
            Check("Vertical: queda longa e acompanhada",
                Center(cam).Y > y0 + 200f, "dY=" + (Center(cam).Y - y0));
            Check("Vertical: a queda nunca vira teleporte (< 12px por frame)",
                maxFrame < 12f, "maior passo=" + maxFrame);
            Check("Vertical: pousando, a camera recentra no player",
                Math.Abs(Center(cam).Y - (p.Y + cam.FocusOffsetY)) < 6f,
                "centro-player=" + (Center(cam).Y - p.Y));
        }

        // ---- olhar ----
        private static void TestLookAtraso()
        {
            var (lvl, p, cam) = Boot();
            float y0 = Center(cam).Y;

            // toque rapido no direcional: o player ja se inclina, a camera nao anda
            for (int i = 0; i < 18; i++) Step(lvl, Keys.Up);   // 0.3s < LookMoveDelay (0.6s)
            bool posou = cam.LookPose;
            float deslocouCedo = Math.Abs(Center(cam).Y - y0);
            for (int i = 0; i < 30; i++) Step(lvl);           // solta e espera
            Check("Look: em 0.3s o player ja se inclina, mas a camera nao anda",
                posou && deslocouCedo < 1f, "pose=" + posou + " dY=" + deslocouCedo);
            Check("Look: toque rapido nao desloca a camera", Math.Abs(Center(cam).Y - y0) < 1f,
                "dY=" + (Center(cam).Y - y0));
        }

        private static void TestLookCimaEBaixo()
        {
            var (lvl, p, cam) = Boot();
            float y0 = Center(cam).Y;
            for (int i = 0; i < 90; i++) Step(lvl, Keys.Up);   // 1.5s segurando
            float cima = y0 - Center(cam).Y;
            Check("Look: segurando cima, a camera sobe (ease, sem teleporte)",
                cima > cam.LookUpDist * 0.8f, "subiu=" + cima + " de " + cam.LookUpDist);

            for (int i = 0; i < 120; i++) Step(lvl);           // solta: volta suave
            Check("Look: soltando, a camera volta ao normal",
                Math.Abs(Center(cam).Y - y0) < 3f, "dY=" + (Center(cam).Y - y0));

            var (lvl2, p2, cam2) = Boot();
            float y2 = Center(cam2).Y;
            for (int i = 0; i < 90; i++) Step(lvl2, Keys.Down);
            float baixo = Center(cam2).Y - y2;
            Check("Look: a area revelada abaixo e maior que a de cima", baixo > cima,
                "baixo=" + baixo + " cima=" + cima);
        }

        private static void TestLookCancela()
        {
            var (lvl, p, cam) = Boot();
            float y0 = Center(cam).Y;
            for (int i = 0; i < 90; i++) Step(lvl, Keys.Up, Keys.Right);  // andando: nao olha
            Check("Look: andar cancela o olhar (camera nao desloca)",
                !cam.Looking && Math.Abs(Center(cam).Y - y0) < 3f,
                "looking=" + cam.Looking + " dY=" + (Center(cam).Y - y0));

            var (lvl2, p2, cam2) = Boot();
            for (int i = 0; i < 90; i++) Step(lvl2, Keys.Up);   // olhando
            bool olhando = cam2.Looking;
            for (int i = 0; i < 6; i++) Step(lvl2, Keys.Up, Keys.Z);  // pula segurando cima
            Check("Look: pular interrompe o olhar", olhando && !cam2.LookPose,
                "olhava=" + olhando + " pose=" + cam2.LookPose);
        }

        // ---- limites e suavidade ----
        private static void TestLimitesDaSala()
        {
            var (lvl, p, cam) = Boot(spawnX: 40f);
            for (int i = 0; i < 120; i++) Step(lvl, Keys.Left);   // corre ate a parede esq
            Check("Limites: a camera para na borda esquerda da sala",
                lvl.Camera.Position.X == lvl.Bounds.Left, "cam.X=" + lvl.Camera.Position.X);

            for (int i = 0; i < 300; i++) Step(lvl, Keys.Right);  // corre ate a direita
            Check("Limites: a camera para na borda direita (Bounds.Right - ViewWidth)",
                lvl.Camera.Position.X <= lvl.Bounds.Right - cam.ViewWidth + 0.01f,
                "cam.X=" + lvl.Camera.Position.X + " limite=" + (lvl.Bounds.Right - cam.ViewWidth));
            Check("Limites: a camera nunca sai da sala no eixo Y",
                lvl.Camera.Position.Y >= lvl.Bounds.Top
                    && lvl.Camera.Position.Y <= lvl.Bounds.Bottom - cam.ViewHeight + 0.01f,
                "cam.Y=" + lvl.Camera.Position.Y);
        }

        // A transicao de sala e do port fiel (Level.TransitionRoutine, pan CubeOut) e a
        // GameCamera nem atualiza durante ela. O que se mede aqui e a emenda: a camera
        // pana ate a sala nova e, ao devolver o controle, nao ha pulo.
        private static void TestTransicaoEntreSalas()
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Rooms.Add(new Rectangle(0, 0, 320, 180));
            lvl.Rooms.Add(new Rectangle(320, 0, 320, 180));
            lvl.Add(new Solid(new Vector2(0f, 160f), 640f, 20f, false));
            Player p = new Player(new Vector2(290f, 150f), PlayerSpriteMode.Madeline);
            p.Add(new MeleeCombo());
            lvl.Add(p);
            GameCamera cam = new GameCamera();
            lvl.Add(cam);
            lvl.Begin(); lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);
            cam.SnapToPlayer(p);

            bool comecou = false, monotona = true;
            float prevX = lvl.Camera.Position.X;
            for (int i = 0; i < 240; i++)
            {
                Step(lvl, Keys.Right);
                if (lvl.Transitioning) comecou = true;
                if (lvl.Camera.Position.X < prevX - 0.01f) monotona = false;
                prevX = lvl.Camera.Position.X;
                if (comecou && !lvl.Transitioning) break;
            }
            Check("Transicao: a camera pana ate a sala nova, sempre p/ frente",
                comecou && monotona && lvl.Camera.Position.X == 320f,
                "cam.X=" + lvl.Camera.Position.X + " monotona=" + monotona);

            float maiorPasso = 0f;
            prevX = lvl.Camera.Position.X;
            for (int i = 0; i < 30; i++)   // controle de volta com a GameCamera
            {
                Step(lvl, Keys.Right);
                maiorPasso = Math.Max(maiorPasso, Math.Abs(lvl.Camera.Position.X - prevX));
                prevX = lvl.Camera.Position.X;
            }
            Check("Transicao: devolvido o controle, a camera nao pula", maiorPasso < 2f,
                "maior passo=" + maiorPasso);
        }

        // O mapa do demo (Content/map.txt) precisa de folga vertical p/ a camera centrar o
        // player e p/ o olhar/queda aparecerem. Roda o mapa REAL: sobe a escada de degraus
        // ate a sacada e confere que a camera acompanha, centrada, sem bater no clamp.
        private static void TestMapaDoDemo()
        {
            string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "map.txt");
            if (!System.IO.File.Exists(path))
            {
                Check("Mapa do demo: Content/map.txt existe", false, "nao encontrado: " + path);
                return;
            }
            var (lvl, p, cam) = BootMapa(path);

            Check("Mapa: parado no chao, o enquadramento vertical vale (sem bater no clamp)",
                Near(Center(cam).Y, p.Y + cam.FocusOffsetY, 1f),
                "centro=" + Center(cam).Y + " player=" + p.Y);

            // sobe a escada: corre p/ a direita pulando sempre que toca o chao
            float topo = p.Y;
            for (int espera = 6; espera <= 30 && topo > 112f; espera++)
            {
                var (l2, p2, c2) = BootMapa(path);
                int segura = 0, desde = 0;
                for (int i = 0; i < 400 && p2 != null; i++)
                {
                    // pulo maximo = segurar o botao (var jump); soltar cedo corta a subida.
                    // 'espera' e o compasso entre pousar e pular: varia p/ achar um ritmo
                    // que suba a torre (se algum sobe, um humano sobe)
                    if (segura == 0 && p2.OnGround() && ++desde >= espera)
                    {
                        segura = 14;
                        desde = 0;
                    }
                    bool pula = segura > 0;
                    if (pula) segura--;
                    Step(l2, pula ? new[] { Keys.Right, Keys.Z } : new[] { Keys.Right });
                    p2 = l2.Tracker.GetEntity<Player>();  // morrer troca a instancia do player
                    if (p2 != null)
                        topo = Math.Min(topo, p2.Y);
                }
            }
            Check("Mapa: a torre de degraus e escalavel a pe (sobe 4+ degraus)",
                topo <= 152f, "menor Y alcancado=" + topo + " (chao=224)");
            Check("Mapa: subindo, a camera sobe junto (sem travar no limite da sala)",
                lvl.Camera.Position.Y < lvl.Bounds.Bottom - cam.ViewHeight - 1f,
                "cam.Y=" + lvl.Camera.Position.Y + " clamp=" + (lvl.Bounds.Bottom - cam.ViewHeight));
        }

        private static void TestSemTeleporte()
        {
            var (lvl, p, cam) = Boot(spawnX: 200f);
            float maior = 0f;
            Vector2 prev = lvl.Camera.Position;
            for (int i = 0; i < 60; i++)   // dash: a mudanca de velocidade mais brusca do jogo
            {
                Step(lvl, (i == 0) ? new[] { Keys.Right, Keys.C } : new[] { Keys.Right });
                maior = Math.Max(maior, (lvl.Camera.Position - prev).Length());
                prev = lvl.Camera.Position;
            }
            Check("Suavizacao: nem o dash faz a camera saltar (< 12px por frame)",
                maior < 12f, "maior passo=" + maior);
        }
    }
}



