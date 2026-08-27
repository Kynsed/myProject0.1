using System;
using Microsoft.Xna.Framework;
using Monocle;

// Cena de gameplay e aplicacao do jogo. Player real (fisica portada) sobre chao/paredes
// carregados de Content/map.txt. Sprites ainda em stub -> quem desenha e o HitboxRenderer.
namespace myProject
{
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

        // O atlas precisa do GraphicsDevice, entao nasce aqui e nao no Initialize.
        protected override void LoadContent()
        {
            base.LoadContent();
            GFX.Load();
        }

        protected override void UnloadContent()
        {
            GFX.Unload();
            base.UnloadContent();
        }

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
}
