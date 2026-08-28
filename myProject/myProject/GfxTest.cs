using System;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Primeiro harness que exercita a GPU. O pipeline grafico portado (VirtualTexture .png,
// Atlas.FromDirectory, MTexture) nunca havia rodado com asset real — os headless nao
// alcancam esse codigo porque ele precisa de GraphicsDevice. Abre uma janela por um
// instante, roda os asserts no LoadContent e fecha.
namespace MonocleSmoke
{
    public static class GfxTest
    {
        private static int fails;

        public static int Run()
        {
            Console.WriteLine("== gfx-test (atlas de PNGs soltos) ==");
            fails = 0;
            using (GfxTestGame game = new GfxTestGame())
                game.Run();
            Console.WriteLine(fails == 0 ? "== GFX OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        internal static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }
    }

    public class GfxTestGame : Engine
    {
        public GfxTestGame() : base(320, 180, 320, 180, "gfx-test", false, false) { }

        protected override void LoadContent()
        {
            base.LoadContent();
            try
            {
                RunChecks();
            }
            catch (Exception e)
            {
                GfxTest.Check("carregar o atlas sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Exit();
        }

        private static void RunChecks()
        {
            GfxTest.Check("GFX: comeca sem atlas (harness headless constroi Player sem isso)",
                !GFX.Loaded, "Loaded=" + GFX.Loaded);

            // Caminho de producao: e o LoadContent do PlayGame que chama isso.
            GFX.Load();

            GfxTest.Check("GFX.Load: atlas disponivel", GFX.Loaded, "Loaded=" + GFX.Loaded);

            Atlas atlas = GFX.Game;

            // Conta os arquivos em disco em vez de cravar um numero: adicionar arte nao
            // pode quebrar o teste, so arte que o atlas DEIXE de indexar.
            int onDisk = System.IO.Directory.GetFiles(
                System.IO.Path.Combine(Engine.ContentDirectory, GFX.GameAtlasPath),
                "*.png", System.IO.SearchOption.AllDirectories).Length;
            GfxTest.Check("Atlas: indexa todos os PNGs de Content/Graphics",
                atlas.Sources.Count == onDisk, "atlas=" + atlas.Sources.Count + " disco=" + onDisk);

            GfxTest.Check("Atlas: chave e o caminho relativo sem extensao, com '/'",
                atlas.Has("player/idle00") && atlas.Has("tiles/rock"),
                "player/idle00=" + atlas.Has("player/idle00") + " tiles/rock=" + atlas.Has("tiles/rock"));

            GfxTest.Check("Atlas: chave nao inclui a extensao",
                !atlas.Has("player/idle00.png"), "player/idle00.png=" + atlas.Has("player/idle00.png"));

            MTexture idle = atlas["player/idle00"];
            GfxTest.Check("VirtualTexture: PNG decodificado com as dimensoes certas",
                idle.Width == 16 && idle.Height == 16, "idle00=" + idle.Width + "x" + idle.Height);

            MTexture rock = atlas["tiles/rock"];
            GfxTest.Check("VirtualTexture: tile 8x8 decodificado",
                rock.Width == 8 && rock.Height == 8, "rock=" + rock.Width + "x" + rock.Height);

            // Ordem de canal: o branch .data do VirtualTexture faz swap BGRA->RGBA; o .png nao.
            // rock e solido (120,110,100,255) — se vier trocado, este assert pega.
            Color[] px = new Color[rock.Width * rock.Height];
            rock.Texture.Texture.GetData(px);
            Color c = px[0];
            GfxTest.Check("VirtualTexture: ordem de canal do .png preservada (RGBA)",
                c.R == 120 && c.G == 110 && c.B == 100 && c.A == 255, "pixel[0]=" + c);

            // Alpha zerado na borda: exercita o premultiply do branch .png.
            Color[] pi = new Color[idle.Width * idle.Height];
            idle.Texture.Texture.GetData(pi);
            GfxTest.Check("VirtualTexture: borda transparente sobrevive ao premultiply",
                pi[0].A == 0 && pi[0].R == 0, "canto=" + pi[0]);

            GfxTest.Check("Atlas: agrupa frames por sufixo numerico (run00..run03)",
                atlas.GetAtlasSubtextures("player/run").Count == 4,
                "run=" + atlas.GetAtlasSubtextures("player/run").Count);

            GfxTest.Check("Atlas: agrupa idle00..idle01",
                atlas.GetAtlasSubtextures("player/idle").Count == 2,
                "idle=" + atlas.GetAtlasSubtextures("player/idle").Count);

            GfxTest.Check("Atlas: acesso indexado a um frame (run[2])",
                atlas.GetAtlasSubtexturesAt("player/run", 2) != null, "run[2] != null");

            GfxTest.Check("Atlas: prefixo sem frames devolve lista vazia",
                atlas.GetAtlasSubtextures("player/naoexiste").Count == 0,
                "naoexiste=" + atlas.GetAtlasSubtextures("player/naoexiste").Count);

            GFX.Unload();
            GfxTest.Check("GFX.Unload: libera o atlas", !GFX.Loaded, "Loaded=" + GFX.Loaded);
        }
    }
}
