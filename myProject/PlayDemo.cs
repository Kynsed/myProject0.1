using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Demo jogavel: Player real sobre chao/paredes. Sprites em stub -> desenhamos os
// hitboxes. Teclado e controle via Input (Z pula, X ataca, C dash, ESC pausa).
namespace MonocleSmoke
{
    // Desenha o collider de cada entidade (debug visual, sem sprites).
    // Renderiza pela camera do Level — e o Player.Update quem a move (lerp fiel do Celeste).
    public class HitboxRenderer : Renderer
    {
        private static readonly Camera fallback = new Camera();

        // cores dos golpes do combo (1o/2o/3o)
        private static readonly Color[] attackColors = { Color.Cyan, Color.Orange, Color.Magenta };

        public override void Render(Scene scene)
        {
            Camera cam = (scene is Level lvl) ? lvl.Camera : fallback;
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, cam.Matrix * Engine.ScreenMatrix);
            foreach (Entity e in scene.Entities)
            {
                if (e.Collider == null || !e.Visible)
                    continue;

                if (e is AttackHitbox atk)
                {
                    // golpe: retangulo preenchido translucido + contorno, cor por estagio
                    Color ac = attackColors[atk.Stage];
                    Draw.Rect(atk.Collider, ac * 0.35f);
                    atk.Collider.Render(cam, ac);
                    continue;
                }

                if (e is TrainingDummy dummy)
                {
                    // boneco de treino: corpo cinza (flash branco no hit). Barra de vida so
                    // p/ bonecos finitos — os infinitos ficariam sempre cheios.
                    bool flash = dummy.Health.FlashTimer > 0f;
                    Draw.Rect(dummy.Collider, flash ? Color.White : Color.DarkGray);
                    dummy.Collider.Render(cam, flash ? Color.White : Color.Gray);
                    if (!dummy.Health.Infinite)
                    {
                        float pct = MathHelper.Clamp(dummy.Health.Current / (float)dummy.Health.Max, 0f, 1f);
                        Draw.Rect(dummy.Left - 2f, dummy.Top - 6f, dummy.Width + 4f, 3f, Color.Black * 0.6f);
                        Draw.Rect(dummy.Left - 2f, dummy.Top - 6f, (dummy.Width + 4f) * pct, 3f, Color.LimeGreen);
                    }
                    continue;
                }

                Color c = (e is Player) ? Color.Red : ((e is Solid) ? Color.LightGray : Color.Yellow);
                e.Collider.Render(cam, c);
            }
            Draw.SpriteBatch.End();
        }
    }

    // Vinheta da pausa: escurece a tela e escreve PAUSA no centro. Usa a fonte bitmap do
    // inspector (mesma solucao de texto que o projeto ja tem; nao ha fonte de conteudo).
    public class PauseRenderer : Renderer
    {
        public override void Render(Scene scene)
        {
            if (!(scene is PlayScene play) || !play.Paused)
                return;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
            Draw.Rect(0f, 0f, Engine.Width, Engine.Height, Color.Black * 0.55f);
            if (!myProject.Inspector.UI.GuiFont.Ready)
                myProject.Inspector.UI.GuiFont.Load(Engine.Instance.GraphicsDevice);
            if (myProject.Inspector.UI.GuiFont.Ready)
            {
                const string text = "PAUSA";
                int scale = 2;
                int w = myProject.Inspector.UI.GuiFont.Measure(text, scale);
                myProject.Inspector.UI.GuiFont.Draw(Draw.SpriteBatch, text,
                    new Vector2((Engine.Width - w) / 2f, Engine.Height / 2f - 8f), Color.White, scale);
            }
            Draw.SpriteBatch.End();
        }
    }

    public class PlayScene : Level
    {
        public PlayScene()
        {
            // Mundo carregado de Content/map.txt (RoomMap): 2 salas em tiles conectadas pela
            // borda x=640. Cruzar a direita dispara a transicao fiel (glide + pan + refil).
            Add(new HitboxRenderer());
            Add(new myProject.Inspector.InspectorRenderer()); // F1 abre o inspector
            RoomMap.Load(this, System.IO.File.ReadAllLines(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "map.txt")));
            Bounds = Rooms[0];
            Session.RespawnPoint = new Vector2(60f, 210f);
            Player player = new Player(new Vector2(60f, 210f), PlayerSpriteMode.Madeline);
            player.Add(new MeleeCombo()); // combate (jogo proprio)
            Add(player);
            Add(new GameCamera());        // camera do metroidvania (jogo proprio)
            Add(new PauseRenderer());
        }

        public bool Paused { get; private set; }

        // Pausa: congela as entidades (o jogo inteiro para) mas segue desenhando e
        // atualizando os renderers, entao o inspector continua utilizavel pausado.
        public override void Update()
        {
            if (Input.Pause.Pressed)
            {
                Input.Pause.ConsumeBuffer();
                Paused = !Paused;
            }
            if (Paused)
            {
                RendererList.Update();
                return;
            }
            base.Update();
        }

        // Como o Level.LoadLevel do Celeste: camera nasce no alvo, sem swoosh inicial.
        public override void Begin()
        {
            base.Begin();
            Entities.UpdateLists();
            Player p = Tracker.GetEntity<Player>();
            if (p == null)
                return;
            if (FollowCamera != null)
                FollowCamera.SnapToPlayer(p);
            else
                Camera.Position = p.CameraTarget;
        }
    }

    public class PlayGame : Engine
    {
        // --inspector-shot: abre o inspector, seleciona o Player, salva um PNG do
        // backbuffer e sai. Verifica o caminho de desenho real sem automacao de janela.
        public string ScreenshotPath;
        private int frames;
        private PlayScene pendingShotScene;

        public PlayGame() : base(320, 180, 1280, 720, "myProject", false, true) { }

        protected override void Draw(GameTime gameTime)
        {
            // prepara o shot assim que a cena entrou em vigor e as listas foram populadas
            if (pendingShotScene != null && Scene == pendingShotScene)
            {
                pendingShotScene.RendererList.UpdateLists();
                var insp = pendingShotScene.RendererList.Renderers
                    .Find(r => r is myProject.Inspector.InspectorRenderer)
                    as myProject.Inspector.InspectorRenderer;
                if (insp != null)
                {
                    insp.Enabled = true;
                    var player = pendingShotScene.Tracker.GetEntity<Player>();
                    if (player != null)
                    {
                        insp.Panel.Selection.Select(player);
                        pendingShotScene = null;
                    }
                }
            }

            base.Draw(gameTime);
            if (ScreenshotPath == null || ++frames < 20)
                return;
            var gd = GraphicsDevice;
            int w = gd.PresentationParameters.BackBufferWidth;
            int h = gd.PresentationParameters.BackBufferHeight;
            var data = new Color[w * h];
            gd.GetBackBufferData(data);
            using (var tex = new Texture2D(gd, w, h))
            using (var fs = System.IO.File.Create(ScreenshotPath))
            {
                tex.SetData(data);
                tex.SaveAsPng(fs, w, h);
            }
            Console.WriteLine("screenshot: " + ScreenshotPath + " (" + w + "x" + h + ")");
            Exit();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Input.Initialize();
            InitTags();
            Abilities.ResetToDefaults();   // jogo: dash so horizontal, sem escalar parede
            // Engine.Scene so troca no fim do Update: guarda a referencia p/ preparar o shot
            var built = new PlayScene();
            Scene = built;
            pendingShotScene = ScreenshotPath != null ? built : null;
            ExitOnEscapeKeypress = false;   // ESC agora pausa
            Console.WriteLine("Teclado: setas movem | Z pula | X ataca | C dash (so horizontal) | ESC pausa | V agarra");
            Console.WriteLine("Xbox: A pula | X ataca | RT dash | Start pausa | dpad/analogico movem");
            Console.WriteLine("F1: inspector (clique numa entidade) | fechar a janela p/ sair");
        }

        // BitTags (Tags.Persistent etc.) devem existir antes do Scene dimensionar o TagLists.
        internal static void InitTags()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Tags).TypeHandle);
        }
    }

    // Verificacao headless: roda o Player por N frames com DeltaTime simulado (sem janela).
    public static class PlayerSmoke
    {
        public static int Run()
        {
            Console.WriteLine("== player-smoke (headless, sem janela) ==");
            try
            {
                Tracker.Initialize();
                MInput.Initialize();
                Input.Initialize();
                PlayGame.InitTags();
                SetDelta(1f / 60f);

                PlayScene scene = new PlayScene();
                scene.Begin();
                for (int i = 0; i < 180; i++)
                {
                    MInput.UpdateNull();
                    scene.BeforeUpdate();
                    scene.Update();
                    scene.AfterUpdate();
                }
                Player p = scene.Tracker.GetEntity<Player>();
                Console.WriteLine("OK: 180 frames sem excecao.");
                if (p != null)
                    Console.WriteLine("Player apos cair: Y=" + p.Y + " OnGround=" + p.OnGround());
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("CRASH: " + e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static void SetDelta(float dt)
        {
            // DeltaTime/RawDeltaTime sao { get; private set; }; simular frames via reflection.
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, dt);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, dt);
        }
    }

    // Smoke com INPUT injetado: anda ate a parede, agarra, escala, pula, da dash.
    // Exercita Normal/Climb/Dash/WallJump headless p/ pegar NREs de stub.
    public static class PlayerFuzz
    {
        public static int Run()
        {
            Console.WriteLine("== player-fuzz (headless, input simulado) ==");
            try
            {
                Tracker.Initialize();
                MInput.Initialize();
                Input.Initialize();
                PlayGame.InitTags();
                typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
                typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);

                PlayScene scene = new PlayScene();
                scene.Begin();
                Player p = null;
                var seen = new System.Collections.Generic.HashSet<int>();

                // roteiro: (frames, teclas) — cobre andar, dash, agarrar parede esq, escalar, wall-jump
                (int n, Keys[] keys)[] script =
                {
                    (40, new Keys[0]),                                   // cair/pousar
                    (5,  new[]{ Keys.C }),                               // dash parado
                    (60, new[]{ Keys.Left }),                            // andar ate parede esq
                    (20, new[]{ Keys.Left, Keys.V }),                    // agarrar parede
                    (30, new[]{ Keys.Left, Keys.V, Keys.Up }),           // escalar
                    (3,  new[]{ Keys.V, Keys.Z }),                       // wall jump
                    (5,  new[]{ Keys.Up, Keys.C }),                      // dash p/ cima
                    (5,  new[]{ Keys.Right, Keys.C }),                   // dash diagonal
                    (40, new[]{ Keys.Right }),                           // andar
                    (3,  new[]{ Keys.Z }),                               // pular
                };

                int frame = 0;
                foreach (var step in script)
                {
                    for (int k = 0; k < step.n; k++)
                    {
                        Inject(step.keys);
                        scene.BeforeUpdate();
                        scene.Update();
                        scene.AfterUpdate();
                        if (p == null) p = scene.Tracker.GetEntity<Player>();
                        if (p != null) seen.Add(p.StateMachine.State);
                        frame++;
                    }
                }

                Console.WriteLine("OK: " + frame + " frames sem excecao.");
                Console.WriteLine("Estados exercitados: " + string.Join(",", seen));
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("CRASH: " + e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static void Inject(Keys[] keys)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState(keys);
            foreach (VirtualInput vi in MInput.VirtualInputs)
                vi.Update();
        }
    }
}
