using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do SolidTiles do Celeste: geometria da sala como
    // Solid com collider Grid 8x8 construido de VirtualMap<char> ('0' = vazio).
    // Flags fieis: Tags.Global, Depth -10000, EnableAssistModeChecks=false, AllowStaticMovers=false.
    // NOTE: podas de conteudo — Autotiler/TileGrid/AnimatedTiles (visual; o hitbox e desenhado
    // pelo HitboxRenderer) e SurfaceSoundIndexAt (som de superficie por tile).
    [Tracked(false)]
    public class SolidTiles : Solid
    {
        public Grid Grid;
        public VirtualMap<char> tileTypes;

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
        }
    }
}
