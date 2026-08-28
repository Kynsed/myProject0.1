using System;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Contrato do autotile. Cada mascara do ForegroundTiles.xml aponta p/ uma celula do
// tileset, e a celula fica gravada no ClipRect da MTexture — entao da p/ afirmar QUAL
// tile o autotiler escolheu, nao so que escolheu algum. Precisa de GraphicsDevice
// (o tileset e um PNG), entao abre janela por um instante.
namespace MonocleSmoke
{
    public static class TileTest
    {
        private static int fails;

        private const char Solid = (char)49;   // '1'
        private const char Empty = (char)48;   // '0'

        public static int Run()
        {
            Console.WriteLine("== tile-test (autotiler) ==");
            fails = 0;
            using (TileTestGame game = new TileTestGame())
                game.Run();
            Console.WriteLine(fails == 0 ? "== TILES OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        internal static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }

        // Celula do tileset que a textura veio, deduzida do recorte (celulas de 8x8).
        internal static string Cell(MTexture texture)
        {
            if (texture == null)
                return "null";
            return (texture.ClipRect.X / 8) + "," + (texture.ClipRect.Y / 8);
        }

        internal static VirtualMap<char> Map(params string[] rows)
        {
            VirtualMap<char> map = new VirtualMap<char>(rows[0].Length, rows.Length, Empty);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    map[x, y] = (rows[y][x] == (char)35) ? Solid : Empty;   // '#'
            return map;
        }
    }

    public class TileTestGame : Engine
    {
        public TileTestGame() : base(320, 180, 320, 180, "tile-test", false, false) { }

        protected override void LoadContent()
        {
            base.LoadContent();
            try
            {
                RunChecks();
            }
            catch (Exception e)
            {
                TileTest.Check("rodar sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Exit();
        }

        private static void RunChecks()
        {
            GFX.Load();

            TileTest.Check("GFX.FGAutotiler carregado do ForegroundTiles.xml",
                GFX.FGAutotiler != null, "FGAutotiler != null");

            // Bloco 3x3 solido cercado de vazio: exercita cantos, bordas e centro.
            VirtualMap<char> block = TileTest.Map(
                ".....",
                ".###.",
                ".###.",
                ".###.",
                ".....");
            Autotiler.Generated g = GFX.FGAutotiler.GenerateMap(block, true);

            TileTest.Check("TileGrid tem as dimensoes do mapa",
                g.TileGrid.TilesX == 5 && g.TileGrid.TilesY == 5,
                g.TileGrid.TilesX + "x" + g.TileGrid.TilesY);

            TileTest.Check("tile vazio nao recebe textura",
                g.TileGrid.Tiles[0, 0] == null, "[0,0]=" + TileTest.Cell(g.TileGrid.Tiles[0, 0]));

            TileTest.Check("centro do bloco usa a celula 'center' (1,1)",
                TileTest.Cell(g.TileGrid.Tiles[2, 2]) == "1,1",
                "[2,2]=" + TileTest.Cell(g.TileGrid.Tiles[2, 2]));

            TileTest.Check("canto superior-esquerdo usa (0,0)",
                TileTest.Cell(g.TileGrid.Tiles[1, 1]) == "0,0",
                "[1,1]=" + TileTest.Cell(g.TileGrid.Tiles[1, 1]));

            TileTest.Check("borda superior usa (1,0)",
                TileTest.Cell(g.TileGrid.Tiles[2, 1]) == "1,0",
                "[2,1]=" + TileTest.Cell(g.TileGrid.Tiles[2, 1]));

            TileTest.Check("canto superior-direito usa (2,0)",
                TileTest.Cell(g.TileGrid.Tiles[3, 1]) == "2,0",
                "[3,1]=" + TileTest.Cell(g.TileGrid.Tiles[3, 1]));

            TileTest.Check("borda esquerda usa (0,1)",
                TileTest.Cell(g.TileGrid.Tiles[1, 2]) == "0,1",
                "[1,2]=" + TileTest.Cell(g.TileGrid.Tiles[1, 2]));

            TileTest.Check("borda inferior usa (1,2)",
                TileTest.Cell(g.TileGrid.Tiles[2, 3]) == "1,2",
                "[2,3]=" + TileTest.Cell(g.TileGrid.Tiles[2, 3]));

            // Faixa horizontal de 1 tile de altura: o caso que o mapa do demo usa nas
            // plataformas. Sem as mascaras de faixa, estes tiles sairiam invisiveis.
            VirtualMap<char> strip = TileTest.Map(
                ".....",
                ".###.",
                ".....");
            Autotiler.Generated s = GFX.FGAutotiler.GenerateMap(strip, true);

            TileTest.Check("faixa horizontal: ponta esquerda usa (3,0)",
                TileTest.Cell(s.TileGrid.Tiles[1, 1]) == "3,0",
                "[1,1]=" + TileTest.Cell(s.TileGrid.Tiles[1, 1]));

            TileTest.Check("faixa horizontal: meio usa (3,1)",
                TileTest.Cell(s.TileGrid.Tiles[2, 1]) == "3,1",
                "[2,1]=" + TileTest.Cell(s.TileGrid.Tiles[2, 1]));

            TileTest.Check("faixa horizontal: ponta direita usa (3,2)",
                TileTest.Cell(s.TileGrid.Tiles[3, 1]) == "3,2",
                "[3,1]=" + TileTest.Cell(s.TileGrid.Tiles[3, 1]));

            // Tile solitario: as 4 ortogonais vazias.
            VirtualMap<char> lone = TileTest.Map(
                "...",
                ".#.",
                "...");
            Autotiler.Generated l = GFX.FGAutotiler.GenerateMap(lone, true);

            TileTest.Check("tile isolado usa (3,3)",
                TileTest.Cell(l.TileGrid.Tiles[1, 1]) == "3,3",
                "[1,1]=" + TileTest.Cell(l.TileGrid.Tiles[1, 1]));

            // Faixa vertical: parede de 1 tile de largura.
            VirtualMap<char> column = TileTest.Map(
                "...",
                ".#.",
                ".#.",
                ".#.",
                "...");
            Autotiler.Generated c = GFX.FGAutotiler.GenerateMap(column, true);

            TileTest.Check("faixa vertical: topo (0,3), meio (1,3), base (2,3)",
                TileTest.Cell(c.TileGrid.Tiles[1, 1]) == "0,3"
                    && TileTest.Cell(c.TileGrid.Tiles[1, 2]) == "1,3"
                    && TileTest.Cell(c.TileGrid.Tiles[1, 3]) == "2,3",
                TileTest.Cell(c.TileGrid.Tiles[1, 1]) + " / " + TileTest.Cell(c.TileGrid.Tiles[1, 2])
                    + " / " + TileTest.Cell(c.TileGrid.Tiles[1, 3]));

            // Nenhuma combinacao de vizinho ortogonal pode cair fora das mascaras: tile
            // solido sem textura vira buraco visivel no cenario.
            int holes = 0;
            for (int y = 0; y < block.Rows; y++)
                for (int x = 0; x < block.Columns; x++)
                    if (block[x, y] == (char)49 && g.TileGrid.Tiles[x, y] == null)
                        holes++;
            TileTest.Check("nenhum tile solido ficou sem textura", holes == 0, "buracos=" + holes);

            // SolidTiles monta collider E visual do mesmo VirtualMap.
            SolidTiles solid = new SolidTiles(Vector2.Zero, block);
            TileTest.Check("SolidTiles monta o TileGrid junto do collider",
                solid.Tiles != null && solid.Grid != null
                    && solid.Tiles.TilesX == 5 && solid.Grid.CellsX == 5,
                "Tiles=" + (solid.Tiles == null ? "null" : solid.Tiles.TilesX + "x" + solid.Tiles.TilesY)
                    + " Grid=" + solid.Grid.CellsX + "x" + solid.Grid.CellsY);

            TileTest.Check("SolidTiles usa VisualExtend 1 (fiel ao Celeste)",
                solid.Tiles.VisualExtend == 1, "VisualExtend=" + solid.Tiles.VisualExtend);

            GFX.Unload();
        }
    }
}
