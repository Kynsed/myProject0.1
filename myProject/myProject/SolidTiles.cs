using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do SolidTiles do Celeste: geometria da sala como Solid com collider Grid
    // 8x8 construido de VirtualMap<char> ('0' = vazio), mais o TileGrid visual gerado pelo
    // Autotiler a partir do MESMO VirtualMap.
    // Flags fieis: Tags.Global, Depth -10000, EnableAssistModeChecks=false, AllowStaticMovers=false.
    // NOTE: poda de conteudo — SurfaceSoundIndexAt (som de superficie por tile).
    [Tracked(false)]
    public class SolidTiles : Solid
    {
        public Grid Grid;
        public TileGrid Tiles;
        public AnimatedTiles AnimatedTiles;
        private VirtualMap<char> tileTypes;

        public SolidTiles(Vector2 position, VirtualMap<char> data)
            : base(position, 0f, 0f, true)
        {
            Tag = Tags.Global;
            Depth = -10000;
            tileTypes = data;
            EnableAssistModeChecks = false;
            AllowStaticMovers = false;
            Collider = (Grid = new Grid(data.Columns, data.Rows, 8f, 8f));
            for (int i = 0; i < data.Columns; i += 50)
            {
                for (int j = 0; j < data.Rows; j += 50)
                {
                    if (!data.AnyInSegmentAtTile(i, j))
                        continue;
                    int maxX = Math.Min(i + 50, data.Columns);
                    int maxY = Math.Min(j + 50, data.Rows);
                    for (int x = i; x < maxX; x++)
                        for (int y = j; y < maxY; y++)
                            if (data[x, y] != '0')
                                Grid[x, y] = true;
                }
            }

            // Visual: o Autotiler consome o mesmo VirtualMap<char> e devolve o TileGrid.
            // Sem conteudo carregado (harnesses headless) nao ha autotiler — a cena roda
            // so com o collider, que e o que esses testes medem.
            if (GFX.FGAutotiler == null)
                return;

            Autotiler.Generated generated = GFX.FGAutotiler.GenerateMap(data, true);
            Tiles = generated.TileGrid;
            Tiles.VisualExtend = 1;
            Add(Tiles);
            Add(AnimatedTiles = generated.SpriteOverlay);
        }

        // Port fiel. Sem ClipCamera o TileGrid.GetClippedRenderTiles devolve a grade
        // inteira e o Render desenha TODOS os tiles do mapa a cada frame, em vez de so
        // os visiveis.
        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (Tiles == null)
                return;
            Tiles.ClipCamera = SceneAs<Level>().Camera;
            AnimatedTiles.ClipCamera = Tiles.ClipCamera;
        }
    }
}
