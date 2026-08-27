using System;
using System.IO;
using Monocle;

namespace myProject
{
    // Atlas do jogo: PNGs soltos em Content/Graphics indexados por caminho relativo sem
    // extensao (ver Atlas.FromDirectory). Trocar a arte = trocar o PNG; sem empacotamento,
    // sem metadados. Frames de animacao usam sufixo numerico (idle00, idle01) e saem
    // agrupados por Atlas.GetAtlasSubtextures("player/idle").
    //
    // NOTE: SpriteBank continua stub — o banco de animacoes vem no proximo passo.
    public static class GFX
    {
        public const string GameAtlasPath = "Graphics";

        // Fica null ate o LoadContent rodar: os harnesses headless constroem Player e
        // PlayScene sem GraphicsDevice, e nada de render acontece la. Quem indexar fora
        // do render deve checar Loaded antes.
        public static Atlas Game { get; private set; }

        public static bool Loaded
        {
            get { return Game != null; }
        }

        public static SpriteBankStub SpriteBank = new SpriteBankStub();

        // Precisa de GraphicsDevice (decodifica PNG) e de Engine.ContentDirectory —
        // chamar do LoadContent do Engine, depois do base.
        public static void Load()
        {
            Unload();

            string dir = Path.Combine(Engine.ContentDirectory, GameAtlasPath);
            if (!Directory.Exists(dir))
            {
                // Mesma tolerancia do Draw.DefaultFont: avisa e segue sem arte, em vez de
                // derrubar o jogo. O HitboxRenderer continua desenhando os colliders.
                Console.WriteLine("GFX: " + dir + " nao existe. Atlas vazio, jogo sem arte.");
                return;
            }

            Game = Atlas.FromDirectory(GameAtlasPath);
            Console.WriteLine("GFX: atlas carregado (" + Game.Sources.Count + " texturas de " + GameAtlasPath + ").");
        }

        public static void Unload()
        {
            if (Game == null)
                return;
            Game.Dispose();
            Game = null;
        }
    }

    // NOTE: stub do banco de sprites (conteudo). Sem .anim ainda: devolve o PlayerSprite
    // stub, que registra animacao dummy e nao desenha.
    public class SpriteBankStub
    {
        public Sprite Create(string id) => new PlayerSprite(PlayerSpriteMode.Madeline);
    }
}
