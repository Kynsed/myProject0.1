using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Sistema de sprites (jogo proprio): PNGs soltos em Content/Graphics + banco de
// animacoes em texto. O contrato critico e o do Player herdado do port: ele chama
// Sprite.Play com 40 ids e id inexistente JOGA excecao. O teste le os ids direto do
// Player.cs, entao um id novo no codigo quebra a bateria ate ganhar arte ou alias.
namespace MonocleSmoke
{
    public static class SpriteTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== sprites e animacoes (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());

            Console.WriteLine("-- banco de animacoes --");
            TestBanco();
            TestContratoDoPlayer();

            Console.WriteLine("-- arte --");
            TestArquivosDeArte();

            Console.WriteLine("-- tiles (autotile) --");
            TestAutotiler();
            TestTilesDoMapaReal();

            Console.WriteLine("-- player sem arte (headless) --");
            TestFallbackSemAtlas();

            Console.WriteLine(fails == 0 ? "== SPRITES OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        private static string ContentDir => AppContext.BaseDirectory + "Content";

        private static SpriteBank Bank()
        {
            return GFX.LoadBankOnly(ContentDir);
        }

        private static void TestBanco()
        {
            SpriteBank bank = Bank();
            if (bank == null)
            {
                Check("Banco: Content/Graphics/player.anim existe", false, "nao encontrado");
                return;
            }
            Check("Banco: player.anim carrega", bank.Anims.Count > 0,
                "animacoes=" + bank.Anims.Count + " aliases=" + bank.Aliases.Count);
            Check("Banco: as animacoes base do jogo estao la",
                bank.Anims.ContainsKey("idle") && bank.Anims.ContainsKey("walk")
                    && bank.Anims.ContainsKey("run") && bank.Anims.ContainsKey("jump")
                    && bank.Anims.ContainsKey("fall") && bank.Anims.ContainsKey("dash"),
                "base ok");
            Check("Banco: origem ancora os pes do quadro 16x16",
                bank.Origin.X == 8 && bank.Origin.Y == 16,
                "origin=" + bank.Origin.X + "," + bank.Origin.Y);
            Check("Banco: nenhum alias aponta p/ id inexistente",
                AllAliasesResolve(bank, out string quebrado), "quebrado=" + quebrado);
        }

        private static bool AllAliasesResolve(SpriteBank bank, out string broken)
        {
            foreach (string id in bank.Aliases.Keys)
                if (bank.Resolve(id) == null)
                {
                    broken = id;
                    return false;
                }
            broken = "nenhum";
            return true;
        }

        // Le os Sprite.Play("...") do proprio Player.cs: o contrato vem do codigo, nao de
        // uma lista escrita a mao que envelhece.
        private static void TestContratoDoPlayer()
        {
            string src = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Player.cs");
            src = Path.GetFullPath(src);
            if (!File.Exists(src))
            {
                Console.WriteLine("  SKIP  contrato: Player.cs nao esta ao lado do binario ("
                    + src + ")");
                return;
            }

            var ids = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(File.ReadAllText(src),
                @"(?:Sprite|sweatSprite)\.Play\(""([A-Za-z]+)"""))
                ids.Add(m.Groups[1].Value);

            SpriteBank bank = Bank();
            var semAnim = new List<string>();
            foreach (string id in ids)
                if (bank == null || bank.Resolve(id) == null)
                    semAnim.Add(id);

            Check("Contrato: todo id que o Player toca resolve p/ uma animacao",
                semAnim.Count == 0,
                "ids no Player=" + ids.Count + " sem destino=" + string.Join(",", semAnim));
        }

        private static void TestArquivosDeArte()
        {
            string dir = Path.Combine(ContentDir, GFX.GraphicsDir, "player");
            if (!Directory.Exists(dir))
            {
                Check("Arte: Content/Graphics/player existe", false, dir);
                return;
            }
            string[] pngs = Directory.GetFiles(dir, "*.png");
            Check("Arte: PNGs do player copiados p/ o output", pngs.Length > 0,
                "arquivos=" + pngs.Length);

            // cada animacao do banco precisa do frame 00 (o Atlas indexa por sufixo)
            SpriteBank bank = Bank();
            var faltando = new List<string>();
            foreach (var def in bank.Anims.Values)
            {
                string first = Path.Combine(ContentDir, GFX.GraphicsDir,
                    def.Path.Replace('/', Path.DirectorySeparatorChar) + "00.png");
                if (!File.Exists(first))
                    faltando.Add(def.Id);
            }
            Check("Arte: toda animacao do banco tem pelo menos o frame 00",
                faltando.Count == 0, "sem arte=" + string.Join(",", faltando));
        }

        // Mapa de teste 3x3 com o miolo cheio:
        //   . . .
        //   . # .   -> a celula do meio nao tem vizinho nenhum
        //   . . .
        private static VirtualMap<char> Mapa(string[] rows)
        {
            var map = new VirtualMap<char>(rows[0].Length, rows.Length, '0');
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    map[x, y] = rows[y][x] == '#' ? '1' : '0';
            return map;
        }

        private static void TestAutotiler()
        {
            // 5x5 cheio: o miolo tem os 4 vizinhos
            var cheio = Mapa(new[] { "#####", "#####", "#####", "#####", "#####" });
            Check("Autotile: celula cercada usa o tile sem bordas (mascara 15)",
                Autotiler.MaskAt(cheio, 2, 2) == 15, "mascara=" + Autotiler.MaskAt(cheio, 2, 2));

            // topo do chao: sem vizinho em cima (N desligado) => 14
            var chao = Mapa(new[] { ".....", "#####", "#####" });
            Check("Autotile: topo do chao ganha borda so em cima (mascara 14)",
                Autotiler.MaskAt(chao, 2, 1) == 14, "mascara=" + Autotiler.MaskAt(chao, 2, 1));

            Check("Autotile: celula vazia nao vira tile",
                Autotiler.MaskAt(chao, 2, 0) == Autotiler.Empty,
                "valor=" + Autotiler.MaskAt(chao, 2, 0));

            // bloco solto no meio: nenhum vizinho => 0 (borda nos 4 lados)
            var solto = Mapa(new[] { ".....", "..#..", "....." });
            Check("Autotile: bloco isolado leva borda nos 4 lados (mascara 0)",
                Autotiler.MaskAt(solto, 2, 1) == 0, "mascara=" + Autotiler.MaskAt(solto, 2, 1));

            // fora do mapa conta como cheio: a celula do canto nao ganha borda externa
            var canto = Mapa(new[] { "##", "##" });
            Check("Autotile: fora do mapa conta como solido (borda de sala nao vira casca)",
                Autotiler.MaskAt(canto, 0, 0) == 15, "mascara=" + Autotiler.MaskAt(canto, 0, 0));

            int[,] tiles = Autotiler.Build(chao);
            int cheias = 0;
            foreach (int t in tiles)
                if (t != Autotiler.Empty)
                    cheias++;
            Check("Autotile: Build cobre exatamente as celulas solidas", cheias == 10,
                "solidas=" + cheias);
        }

        // O mapa do jogo tem que virar visual de verdade quando ha tileset carregado.
        // Headless nao ha, entao o que da p/ medir aqui e o inverso: sem tileset o
        // SolidTiles nasce sem visual e nada quebra.
        private static void TestTilesDoMapaReal()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "map.txt");
            if (!File.Exists(path))
            {
                Check("Tiles: Content/map.txt existe", false, path);
                return;
            }
            Level lvl = new Level();
            RoomMap.Load(lvl, File.ReadAllLines(path));
            lvl.Bounds = lvl.Rooms[0];
            lvl.Begin(); lvl.BeforeUpdate();

            var solids = lvl.Tracker.GetEntities<SolidTiles>();
            Check("Tiles: o mapa vira SolidTiles", solids.Count > 0, "salas=" + solids.Count);

            var st = (SolidTiles)solids[0];
            int[,] tiles = Autotiler.Build(st.tileTypes);
            int cheias = 0, comBorda = 0;
            foreach (int t in tiles)
            {
                if (t == Autotiler.Empty)
                    continue;
                cheias++;
                if (t != 15)
                    comBorda++;
            }
            Check("Tiles: o mapa real gera tiles e nem todos sao miolo",
                cheias > 100 && comBorda > 10,
                "solidas=" + cheias + " com borda=" + comBorda);
            Check("Tiles: sem tileset (headless) o SolidTiles nasce sem visual",
                st.Visual == null, "visual=" + (st.Visual == null ? "null" : "criado"));
        }

        // Sem GraphicsDevice nao ha atlas: o PlayerSprite tem que continuar aceitando
        // qualquer id (e o que mantem os outros harnesses rodando headless).
        private static void TestFallbackSemAtlas()
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(60f, 150f), PlayerSpriteMode.Madeline);
            lvl.Add(p);
            lvl.Begin(); lvl.BeforeUpdate();

            bool crashou = false;
            try
            {
                foreach (string id in new[] { "idle", "runFast", "dreamDashIn", "inexistente123" })
                    p.Sprite.Play(id, true, false);
            }
            catch (Exception e)
            {
                crashou = true;
                Console.WriteLine("      excecao: " + e.GetType().Name);
            }
            Check("Fallback: sem atlas, Play de qualquer id nao quebra", !crashou,
                "anim atual=" + p.Sprite.CurrentAnimationID);
            Check("Fallback: o id pedido fica visivel p/ quem consulta o estado",
                p.Sprite.RequestedAnimationID == "inexistente123",
                "requested=" + p.Sprite.RequestedAnimationID);
        }
    }
}
