using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace myProject
{
    // Graficos do jogo. A arte sao PNGs soltos em Content/Graphics/**: o Atlas do Monocle
    // ja varre a pasta (Atlas.FromDirectory) e indexa por caminho relativo sem extensao,
    // e o VirtualTexture carrega .png direto. Nao ha ferramenta de empacotamento nem
    // formato de metadados p/ manter em dia — trocar a arte e trocar o PNG.
    //
    // Frames de animacao seguem o sufixo numerico que o Atlas entende:
    //   Content/Graphics/player/idle00.png, idle01.png -> GetAtlasSubtextures("player/idle")
    public static class GFX
    {
        public const string GraphicsDir = "Graphics";

        public static Atlas Game;
        // nunca null: sem arte carregada e um banco vazio, e quem pede animacao cai no
        // fallback do PlayerSprite (harness headless cria Player sem passar por Load)
        public static SpriteBank SpriteBank = new SpriteBank();

        public static bool Loaded => Game != null;

        // Carrega o atlas e o banco de animacoes. Sem GraphicsDevice (harness headless)
        // nao da p/ criar textura: o Load fica no-op e quem depende disso checa Loaded.
        public static void Load()
        {
            if (Game != null)
                return;

            string dir = Path.Combine(Engine.ContentDirectory, GraphicsDir);
            if (!Directory.Exists(dir) || Engine.Graphics == null)
                return;

            Game = Atlas.FromDirectory(GraphicsDir);

            string bank = Path.Combine(Engine.ContentDirectory, GraphicsDir, "player.anim");
            if (File.Exists(bank))
                SpriteBank = SpriteBank.FromFile(bank);
        }

        public static void Unload()
        {
            Game?.Dispose();
            Game = null;
            SpriteBank = null;
        }

        // Banco lido sem depender de GraphicsDevice — o harness confere o contrato de
        // animacoes (ids, alias) sem abrir janela.
        public static SpriteBank LoadBankOnly(string contentDirectory)
        {
            string bank = Path.Combine(contentDirectory, GraphicsDir, "player.anim");
            return File.Exists(bank) ? SpriteBank.FromFile(bank) : null;
        }
    }
}
