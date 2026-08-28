using System;
using System.IO;
using Monocle;

namespace myProject
{
    // Conteudo grafico do jogo.
    //
    // Atlas: PNGs soltos em Content/Graphics indexados por caminho relativo sem extensao
    // (ver Atlas.FromDirectory). Trocar a arte = trocar o PNG; sem empacotamento, sem
    // metadados. Frames de animacao usam sufixo numerico (idle00, idle01) e saem agrupados
    // por Atlas.GetAtlasSubtextures("player/idle").
    //
    // SpriteBank: Content/Graphics/Sprites.xml, schema do Celeste (ver Monocle/SpriteData.cs).
    //
    // NOTE: poda grande de conteudo em relacao ao GFX do Celeste. Ficou so o que o jogo
    // usa hoje. Fora:
    //   - atlases Gui / Opening / Misc / Portraits / ColorGrades, e os bancos
    //     GuiSpriteBank / PortraitsSpriteBank (nao ha UI nem retratos)
    //   - BGAutotiler e SceneryTiles (so primeiro plano por enquanto)
    //   - todos os Effect (FxDistort, FxLighting, FxGaussianBlur, FxMirrors...) mais
    //     LoadEffects/LoadFx, e os BlendState Subtract/DestinationTransparencySubtract
    //   - DrawVertices/DrawIndexedVertices (usados por lighting e displacement)
    //   - SplashScreen, MagicGlowNoise, CompleteScreensXml, PortraitSize
    //
    // NOTE: o Celeste separa Load() (atlases, na splash) de LoadData() (bancos), com os
    // flags Loaded/DataLoaded. Aqui e um Load() so — nao ha tela de carregamento que
    // justifique as duas fases.
    public static class GFX
    {
        public const string GameAtlasPath = "Graphics";
        public const string SpriteBankPath = "Graphics/Sprites.xml";
        public const string FGTilesPath = "Graphics/ForegroundTiles.xml";

        // Flash branco do StarFly, desenhado pelo Player.Render. Fica aqui porque era o
        // UNICO caminho de atlas hardcoded no projeto, e na convencao do Celeste
        // (characters/player/...) em vez da nossa. Arte opcional: sem ela o flash nao sai.
        public const string StarFlyWhitePath = "player/startStarFlyWhite";

        // Ficam null ate o LoadContent rodar: os harnesses headless constroem Player e
        // PlayScene sem GraphicsDevice, e nada de render acontece la. Quem indexar fora
        // do render deve checar Loaded antes.
        public static Atlas Game { get; private set; }

        // Autotiler do primeiro plano (SolidTiles). NOTE: sem BGAutotiler — nao ha camada
        // de fundo neste port. O banco de tiles animados e stub (ver AnimatedTiles.cs).
        public static Autotiler FGAutotiler { get; private set; }
        public static AnimatedTilesBank AnimatedTilesBank = new AnimatedTilesBank();

        // NUNCA null: o Player chama GFX.SpriteBank.Create("player_sweat") no construtor, e
        // os harnesses headless constroem Player sem atlas nenhum. O banco vazio devolve um
        // BankSprite tolerante, que aceita qualquer id sem lancar.
        private static readonly SpriteBank EmptyBank = new FallbackSpriteBank();
        public static SpriteBank SpriteBank { get; private set; } = EmptyBank;

        public static bool Loaded
        {
            get { return Game != null; }
        }

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
                Console.WriteLine("GFX: " + dir + " nao existe. Sem atlas, jogo sem arte.");
                return;
            }

            Game = Atlas.FromDirectory(GameAtlasPath);

            string bank = Path.Combine(Engine.ContentDirectory, SpriteBankPath);
            if (File.Exists(bank))
            {
                // Port fiel: SpriteBank(atlas, xmlPath) le o Sprites.xml e monta um
                // SpriteData por sprite. Ele proprio valida (id duplicado, path ausente,
                // start inexistente, Origin+Justify juntos) e lanca com mensagem clara.
                SpriteBank = new SpriteBank(Game, SpriteBankPath);
                PlayerSprite.ClearFramesMetadata();
                PlayerSprite.CreateFramesMetadata("player");
                FGAutotiler = new Autotiler(FGTilesPath);
                Console.WriteLine("GFX: atlas com " + Game.Sources.Count + " texturas, banco com "
                    + SpriteBank.SpriteData.Count + " sprites.");
            }
            else
            {
                Console.WriteLine("GFX: atlas com " + Game.Sources.Count + " texturas; "
                    + SpriteBankPath + " nao existe, sprites sem animacao.");
            }
        }

        public static void Unload()
        {
            SpriteBank = EmptyBank;
            FGAutotiler = null;
            if (Game == null)
                return;
            Game.Dispose();
            Game = null;
        }
    }
}
