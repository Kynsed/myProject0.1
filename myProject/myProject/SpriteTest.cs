using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Contrato de animacoes. Le os ids direto do Player.cs (fonte, nao lista hardcoded) e
// exige que cada um resolva no banco — id novo no codigo quebra a bateria ate ganhar arte
// ou alias. Precisa de GraphicsDevice (decodifica PNG), entao abre janela por um instante.
namespace MonocleSmoke
{
    public static class SpriteTest
    {
        private static int fails;

        public static int Run()
        {
            Console.WriteLine("== sprite-test (banco de animacoes) ==");
            fails = 0;
            using (SpriteTestGame game = new SpriteTestGame())
                game.Run();
            Console.WriteLine(fails == 0 ? "== SPRITES OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        internal static bool Throws(Action action)
        {
            try { action(); }
            catch (Exception) { return true; }
            return false;
        }

        internal static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }

        // O harness roda da pasta de build; sobe ate achar o Player.cs do projeto.
        internal static string FindPlayerSource()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                string candidate = Path.Combine(dir, "Player.cs");
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
            return null;
        }

        internal static List<string> IdsPlayedOn(string source, string target)
        {
            var found = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(source, Regex.Escape(target) + @"\.Play\(""([^""]+)"""))
            {
                string id = m.Groups[1].Value;
                if (id.StartsWith("event:") || !seen.Add(id))
                    continue;
                found.Add(id);
            }
            found.Sort(StringComparer.Ordinal);
            return found;
        }
    }

    public class SpriteTestGame : Engine
    {
        public SpriteTestGame() : base(320, 180, 320, 180, "sprite-test", false, false) { }

        protected override void LoadContent()
        {
            base.LoadContent();
            try
            {
                RunChecks();
            }
            catch (Exception e)
            {
                SpriteTest.Check("rodar sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Exit();
        }

        private static void RunChecks()
        {
            // Player.ctor faz GFX.SpriteBank.Create("player_sweat") — se o banco for null,
            // TODOS os harnesses headless morrem no construtor do Player.
            SpriteTest.Check("GFX.SpriteBank nunca e null, mesmo antes do Load",
                GFX.SpriteBank != null, "SpriteBank=" + (GFX.SpriteBank == null ? "null" : "vazio"));

            GFX.Load();

            SpriteTest.Check("GFX: Sprites.xml carregado (6 entradas)",
                GFX.SpriteBank != null && GFX.SpriteBank.SpriteData.Count == 6,
                "SpriteData=" + (GFX.SpriteBank == null ? "null" : GFX.SpriteBank.SpriteData.Count.ToString()));

            // As variantes de PlayerSpriteMode herdam por copy="player".
            SpriteTest.Check("banco: as 5 entradas de PlayerSpriteMode existem",
                GFX.SpriteBank.Has("player") && GFX.SpriteBank.Has("player_no_backpack")
                    && GFX.SpriteBank.Has("badeline") && GFX.SpriteBank.Has("player_badeline")
                    && GFX.SpriteBank.Has("player_playback"),
                "player/no_backpack/badeline/player_badeline/player_playback");

            SpriteTest.Check("banco: id inexistente lanca (comportamento do Celeste)",
                SpriteTest.Throws(() => GFX.SpriteBank.Create("naoexiste")), "Create(naoexiste) lancou");

            // copy="player" tem que HERDAR as animacoes, nao so criar a entrada.
            Sprite variant = GFX.SpriteBank.Create("player_no_backpack");
            SpriteTest.Check("banco: copy herda as animacoes do sprite copiado",
                variant.Has("idle") && variant.Has("run") && variant.Has("startStarFly"),
                "idle=" + variant.Has("idle") + " run=" + variant.Has("run")
                    + " startStarFly=" + variant.Has("startStarFly"));

            variant.Play("idle");
            SpriteTest.Check("banco: variante de copy resolve frames de verdade",
                variant.Texture != null && variant.Texture.Texture != null,
                "Texture=" + (variant.Texture == null ? "null" : "ok"));

            // Contrato de que a guarda do flash no Player.Render depende: indice alem do
            // fim devolve null em vez de lancar.
            bool threwOutOfRange = false;
            MTexture beyond = null;
            try { beyond = GFX.Game.GetAtlasSubtexturesAt("player/idle", 99); }
            catch (Exception) { threwOutOfRange = true; }
            SpriteTest.Check("atlas: frame alem do fim devolve null, nao lanca",
                !threwOutOfRange && beyond == null,
                "threw=" + threwOutOfRange + " result=" + (beyond == null ? "null" : "MTexture"));

            SpriteTest.Check("atlas: caminho inexistente devolve null, nao lanca",
                GFX.Game.GetAtlasSubtexturesAt(GFX.StarFlyWhitePath, 0) == null,
                "startStarFlyWhite[0]=null (sem arte ainda)");

            PlayerSprite sprite = new PlayerSprite(PlayerSpriteMode.Madeline);

            SpriteTest.Check("PlayerSprite: se preenche do banco (idle tem 2 frames)",
                sprite.Has("idle") && sprite.GetFrame("idle", 1) != null, "idle[1] != null");

            SpriteTest.Check("PlayerSprite: run tem os 4 frames da fita",
                sprite.Has("run") && sprite.GetFrame("run", 3) != null, "run[3] != null");

            SpriteTest.Check("Banco: ids com o mesmo path compartilham os frames",
                sprite.GetFrame("walk", 0) == sprite.GetFrame("run", 0), "walk[0] == run[0]");

            // justify 0.5 1 num frame 16x16 -> origem no pe, centrada
            sprite.Play("idle");
            SpriteTest.Check("Banco: justify vira Origin no frame (pe centrado)",
                sprite.Origin == new Vector2(8f, 16f), "Origin=" + sprite.Origin);

            // Propriedades CALCULADAS no Celeste (eram campos mortos no meu port anterior).
            sprite.Play("runFast");
            SpriteTest.Check("PlayerSprite.Running e calculado de LastAnimationID",
                sprite.Running, "runFast -> Running=" + sprite.Running);
            sprite.Play("idle");
            SpriteTest.Check("PlayerSprite.Running falso fora de corrida",
                !sprite.Running, "idle -> Running=" + sprite.Running);
            sprite.Play("dreamDashIn");
            SpriteTest.Check("PlayerSprite.DreamDashing e calculado de LastAnimationID",
                sprite.DreamDashing, "dreamDashIn -> DreamDashing=" + sprite.DreamDashing);
            sprite.Play("idle");

            // FrameMetadata: <Metadata><Frames hair carry> indexado por MTexture.AtlasPath.
            SpriteTest.Check("PlayerSprite.HairOffset vem do Metadata do Sprites.xml",
                sprite.HairOffset == new Vector2(0f, -6f), "HairOffset=" + sprite.HairOffset);
            SpriteTest.Check("PlayerSprite.HasHair vem do Metadata",
                sprite.HasHair, "HasHair=" + sprite.HasHair);

            SpriteTest.Check("PlayerSprite: Play troca a textura corrente",
                sprite.Texture != null && sprite.Texture.Texture != null,
                "Texture=" + (sprite.Texture == null ? "null" : "ok"));

            // ---- contrato: todo id que o Player pede precisa existir no banco ----
            string path = SpriteTest.FindPlayerSource();
            SpriteTest.Check("contrato: Player.cs localizado a partir da pasta de build",
                path != null, path ?? "NAO ENCONTRADO");
            if (path == null)
                return;

            string source = File.ReadAllText(path);

            List<string> main = SpriteTest.IdsPlayedOn(source, "this.Sprite");
            List<string> missing = new List<string>();
            foreach (string id in main)
                if (!sprite.Has(id))
                    missing.Add(id);
            SpriteTest.Check("contrato: os " + main.Count + " ids de this.Sprite estao no banco",
                missing.Count == 0, missing.Count == 0 ? "nenhum faltando" : ("faltam: " + string.Join(", ", missing)));

            Sprite sweat = GFX.SpriteBank.Create("player_sweat");
            List<string> sweatIds = SpriteTest.IdsPlayedOn(source, "this.sweatSprite");
            List<string> missingSweat = new List<string>();
            foreach (string id in sweatIds)
                if (!sweat.Has(id))
                    missingSweat.Add(id);
            SpriteTest.Check("contrato: os " + sweatIds.Count + " ids de this.sweatSprite estao no banco",
                missingSweat.Count == 0, missingSweat.Count == 0 ? "nenhum faltando" : ("faltam: " + string.Join(", ", missingSweat)));

            // ---- tolerancia headless: sem atlas, Play nao pode lancar ----
            TolerantSprite headless = new TolerantSprite();
            bool threw = false;
            try { headless.Play("idAbsurdoQueNaoExiste"); }
            catch (Exception) { threw = true; }
            SpriteTest.Check("headless: sem atlas, Play em id desconhecido nao lanca",
                !threw, "threw=" + threw);

            GFX.Unload();
            SpriteTest.Check("GFX.Unload: banco volta ao vazio, nao a null",
                GFX.SpriteBank != null, "SpriteBank=" + (GFX.SpriteBank == null ? "null" : "vazio"));
        }
    }
}
