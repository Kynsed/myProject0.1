using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Geometria da sala: Solid com collider Grid 8x8 construido de VirtualMap<char>
    // ('0' = vazio). Flags vindas do port: Tags.Global, Depth -10000,
    // EnableAssistModeChecks=false, AllowStaticMovers=false.
    //
    // Visual: TileGrid (Monocle) populado pelo Autotiler proprio. Nasce so quando ha
    // atlas carregado — harness headless segue sem visual. Fica Visible=false porque
    // quem desenha e o TileRenderer, antes das entidades (senao o tile, com Depth
    // -10000, cobriria o player).
    // NOTE: poda de conteudo — AnimatedTiles e SurfaceSoundIndexAt (som por tile).
    [Tracked(false)]
    public class SolidTiles : Solid
    {
        public Grid Grid;
        public VirtualMap<char> tileTypes;
        public TileGrid Visual;

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

            BuildVisual();
        }

        // Monta o TileGrid a partir da mascara de vizinhanca. Sem tileset carregado
        // (headless) fica sem visual e o hitbox continua sendo o unico desenho.
        public void BuildVisual()
        {
            if (GFX.Tiles == null)
                return;

            Visual = new TileGrid(8, 8, tileTypes.Columns, tileTypes.Rows);
            Visual.Visible = false;   // quem desenha e o TileRenderer
            Visual.Populate(GFX.Tiles, Autotiler.Build(tileTypes));
            Add(Visual);
        }
    }
}
