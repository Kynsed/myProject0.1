using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Demo jogavel: Player real (fisica portada) sobre chao/paredes. Sprites em stub ->
// desenhamos os hitboxes. Teclado controla via Input. ESC fecha.
namespace MonocleSmoke
{
    // Desenha o collider de cada entidade (debug visual, sem sprites).
    // Renderiza pela camera do Level — e o Player.Update quem a move (lerp fiel do Celeste).
    public class HitboxRenderer : Renderer
    {
        private static readonly Camera fallback = new Camera();

        public override void Render(Scene scene)
        {
            Camera cam = (scene is Level lvl) ? lvl.Camera : fallback;
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, cam.Matrix * Engine.ScreenMatrix);
            foreach (Entity e in scene.Entities)
            {
                if (e.Collider == null)
                    continue;
                Color c = (e is Player) ? Color.Red : ((e is Solid) ? Color.LightGray : Color.Yellow);
                e.Collider.Render(cam, c);
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
            RoomMap.Load(this, System.IO.File.ReadAllLines(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "map.txt")));
            Bounds = Rooms[0];
            Session.RespawnPoint = new Vector2(60f, 210f);
            Add(new Player(new Vector2(60f, 210f), PlayerSpriteMode.Madeline));
        }

        // Como o Level.LoadLevel do Celeste: camera nasce no alvo, sem swoosh inicial.
        public override void Begin()
        {
            base.Begin();
            Entities.UpdateLists();
            Player p = Tracker.GetEntity<Player>();
            if (p != null)
                Camera.Position = p.CameraTarget;
        }
    }

    public class PlayGame : Engine
    {
        public PlayGame() : base(320, 180, 1280, 720, "myProject", false, true) { }

        protected override void Initialize()
        {
            base.Initialize();
            Input.Initialize();
            InitTags();
            Scene = new PlayScene();
            Console.WriteLine("Setas: mover | C: pular | X: dash | Z/V: agarrar | ESC: sair");
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
                    (5,  new[]{ Keys.X }),                               // dash parado
                    (60, new[]{ Keys.Left }),                            // andar ate parede esq
                    (20, new[]{ Keys.Left, Keys.Z }),                    // agarrar parede
                    (30, new[]{ Keys.Left, Keys.Z, Keys.Up }),           // escalar
                    (3,  new[]{ Keys.Z, Keys.C }),                       // wall jump
                    (5,  new[]{ Keys.Up, Keys.X }),                      // dash p/ cima
                    (5,  new[]{ Keys.Right, Keys.X }),                   // dash diagonal
                    (40, new[]{ Keys.Right }),                           // andar
                    (3,  new[]{ Keys.C }),                               // pular
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
