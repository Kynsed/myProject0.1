using System;
using Microsoft.Xna.Framework;
using Monocle;

// Cena de gameplay e aplicacao do jogo. Player real (fisica portada) sobre chao/paredes
// carregados de Content/map.txt. Sprites ainda em stub -> quem desenha e o HitboxRenderer.
namespace myProject
{
    public class PlayScene : Level
    {
        // Overlay de debug. F liga/desliga — util p/ auditar a arte por baixo.
        public HitboxRenderer Hitboxes;

        public override void Update()
        {
            // NOTE: jogo proprio. Antes de qualquer leitura de input do frame: se outro
            // controle foi usado, ele vira o slot ativo (ver Input.UpdateGamepadSlot).
            if (Input.UpdateGamepadSlot())
                Console.WriteLine("Controle ativo: slot " + Input.Gamepad);

            // Pause (Enter / Start no controle, como no Celeste). Os renderers seguem
            // rodando com a cena pausada, entao o overlay de debug continua utilizavel.
            // NOTE: o Level.Pause do Celeste abre um menu — conteudo que nao existe aqui.
            if (Input.Pause.Pressed || Input.ESC.Pressed)
            {
                // ConsumePress, nao ConsumeBuffer: o Pressed do VirtualButton e
                // (buffer > 0 || Binding.Pressed), e o buffer do Pause e 0.1s — sem
                // consumir o APERTO, o toggle dispararia varios frames seguidos.
                Input.Pause.ConsumePress();
                Input.ESC.ConsumePress();
                Paused = !Paused;
            }

            base.Update();

            if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.F))
                Hitboxes.Visible = !Hitboxes.Visible;
        }

        public PlayScene()
        {
            // Mundo carregado de Content/map.txt (RoomMap): 2 salas em tiles conectadas pela
            // borda x=640. Cruzar a direita dispara a transicao fiel (glide + pan + refil).
            // Port fiel do LevelLoader: o renderer nasce, entra na cena e vira dono da
            // camera. O HitboxRenderer vem depois p/ o debug ficar por cima.
            Add(GameplayRenderer = new GameplayRenderer());
            Camera = GameplayRenderer.Camera;
            // O Celeste posiciona o som pelo listener 3D do FMOD; aqui a camera da o pan.
            Audio.SetCamera(Camera);
            Add(Hitboxes = new HitboxRenderer());
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

        // O atlas precisa do GraphicsDevice, entao nasce aqui e nao no Initialize.
        protected override void LoadContent()
        {
            base.LoadContent();
            GFX.Load();
            Audio.Load();
        }

        protected override void UnloadContent()
        {
            Audio.Unload();
            GFX.Unload();
            base.UnloadContent();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Input.Initialize();
            // Fiel ao Celeste.cs: ESC nao fecha o jogo, ele pausa.
            Engine.ExitOnEscapeKeypress = false;
            InitTags();
            Scene = new PlayScene();
            Console.WriteLine("Setas: mover | C: pular | X: dash | Z/V/Shift: agarrar");
            Console.WriteLine("Enter ou ESC: pausar | F: hitboxes | ~: console");
        }

        // BitTags (Tags.Persistent etc.) devem existir antes do Scene dimensionar o TagLists.
        internal static void InitTags()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Tags).TypeHandle);
        }
    }
}
