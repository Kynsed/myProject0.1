using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel (celeste_source/Celeste/Autotiler.cs).
    //
    // Escolhe o tile pela vizinhanca 3x3: cada <set mask="..."> declara um padrao de 9
    // posicoes (0 vazio, 1 solido, x tanto faz) e o primeiro que casar decide a arte.
    // As mascaras sao ordenadas por quantidade de 'x' — as mais especificas primeiro.
    // "center" e "padding" atendem o caso em que os 9 vizinhos sao solidos.
    public class Autotiler
    {
        public List<Rectangle> LevelBounds = new List<Rectangle>();

        private Dictionary<char, TerrainType> lookup = new Dictionary<char, TerrainType>();
        private byte[] adjacent = new byte[9];

        public Autotiler(string filename)
        {
            Dictionary<char, XmlElement> loaded = new Dictionary<char, XmlElement>();

            foreach (object obj in Calc.LoadContentXML(filename).GetElementsByTagName("Tileset"))
            {
                XmlElement xml = (XmlElement)obj;
                char id = xml.AttrChar("id");
                Tileset tileset = new Tileset(GFX.Game["tilesets/" + xml.Attr("path")], 8, 8);
                TerrainType data = new TerrainType(id);

                ReadInto(data, tileset, xml);

                if (xml.HasAttr("copy"))
                {
                    char copy = xml.AttrChar("copy");
                    if (!loaded.ContainsKey(copy))
                        throw new Exception("Copied tilesets must be defined before the tilesets that copy them!");
                    ReadInto(data, tileset, loaded[copy]);
                }

                if (xml.HasAttr("ignores"))
                {
                    foreach (string ignore in xml.Attr("ignores").Split(','))
                        if (ignore.Length > 0)
                            data.Ignores.Add(ignore[0]);
                }

                loaded.Add(id, xml);
                lookup.Add(id, data);
            }
        }

        private void ReadInto(TerrainType data, Tileset tileset, XmlElement xml)
        {
            foreach (object obj in xml)
            {
                if (obj is XmlComment)
                    continue;

                XmlElement child = obj as XmlElement;
                string mask = child.Attr("mask");
                Tiles tiles;

                if (mask == "center")
                {
                    tiles = data.Center;
                }
                else if (mask == "padding")
                {
                    tiles = data.Padded;
                }
                else
                {
                    Masked masked = new Masked();
                    tiles = masked.Tiles;

                    int at = 0;
                    for (int i = 0; i < mask.Length; i++)
                    {
                        if (mask[i] == '0')
                            masked.Mask[at++] = 0;
                        else if (mask[i] == '1')
                            masked.Mask[at++] = 1;
                        else if (mask[i] == 'x' || mask[i] == 'X')
                            masked.Mask[at++] = 2;
                    }

                    data.Masked.Add(masked);
                }

                foreach (string coords in child.Attr("tiles").Split(';'))
                {
                    string[] parts = coords.Split(',');
                    int x = int.Parse(parts[0]);
                    int y = int.Parse(parts[1]);
                    tiles.Textures.Add(tileset[x, y]);
                }

                if (child.HasAttr("sprites"))
                {
                    foreach (string sprite in child.Attr("sprites").Split(','))
                        tiles.OverlapSprites.Add(sprite);
                    tiles.HasOverlays = true;
                }
            }

            // mascaras mais especificas (menos 'x') sao testadas primeiro
            data.Masked.Sort(delegate (Masked a, Masked b)
            {
                int ax = 0;
                int bx = 0;
                for (int i = 0; i < 9; i++)
                {
                    if (a.Mask[i] == 2)
                        ax++;
                    if (b.Mask[i] == 2)
                        bx++;
                }
                return ax - bx;
            });
        }

        public Generated GenerateMap(VirtualMap<char> mapData, bool paddingIgnoreOutOfLevel)
        {
            Behaviour behaviour = new Behaviour
            {
                EdgesExtend = true,
                EdgesIgnoreOutOfLevel = false,
                PaddingIgnoreOutOfLevel = paddingIgnoreOutOfLevel
            };
            return Generate(mapData, 0, 0, mapData.Columns, mapData.Rows, false, '0', behaviour);
        }

        public Generated GenerateMap(VirtualMap<char> mapData, Behaviour behaviour)
        {
            return Generate(mapData, 0, 0, mapData.Columns, mapData.Rows, false, '0', behaviour);
        }

        public Generated GenerateBox(char id, int tilesX, int tilesY)
        {
            return Generate(null, 0, 0, tilesX, tilesY, true, id, default(Behaviour));
        }

        public Generated GenerateOverlay(char id, int x, int y, int tilesX, int tilesY, VirtualMap<char> mapData)
        {
            Behaviour behaviour = new Behaviour
            {
                EdgesExtend = true,
                EdgesIgnoreOutOfLevel = true,
                PaddingIgnoreOutOfLevel = true
            };
            return Generate(mapData, x, y, tilesX, tilesY, true, id, behaviour);
        }

        private Generated Generate(VirtualMap<char> mapData, int startX, int startY, int tilesX, int tilesY,
            bool forceSolid, char forceID, Behaviour behaviour)
        {
            TileGrid grid = new TileGrid(8, 8, tilesX, tilesY);
            AnimatedTiles animated = new AnimatedTiles(tilesX, tilesY, GFX.AnimatedTilesBank);

            Rectangle forceFill = Rectangle.Empty;
            if (forceSolid)
                forceFill = new Rectangle(startX, startY, tilesX, tilesY);

            if (mapData != null)
            {
                for (int i = startX; i < startX + tilesX; i += 50)
                {
                    for (int j = startY; j < startY + tilesY; j += 50)
                    {
                        if (!mapData.AnyInSegmentAtTile(i, j))
                        {
                            j = j / 50 * 50;
                            continue;
                        }

                        int maxX = Math.Min(i + 50, startX + tilesX);
                        int maxY = Math.Min(j + 50, startY + tilesY);
                        for (int x = i; x < maxX; x++)
                        {
                            for (int y = j; y < maxY; y++)
                            {
                                Tiles tiles = TileHandler(mapData, x, y, forceFill, forceID, behaviour);
                                if (tiles == null)
                                    continue;

                                grid.Tiles[x - startX, y - startY] = Calc.Random.Choose(tiles.Textures);
                                if (tiles.HasOverlays)
                                    animated.Set(x - startX, y - startY, Calc.Random.Choose(tiles.OverlapSprites), 1f, 1f);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = startX; x < startX + tilesX; x++)
                {
                    for (int y = startY; y < startY + tilesY; y++)
                    {
                        Tiles tiles = TileHandler(null, x, y, forceFill, forceID, behaviour);
                        if (tiles == null)
                            continue;

                        grid.Tiles[x - startX, y - startY] = Calc.Random.Choose(tiles.Textures);
                        if (tiles.HasOverlays)
                            animated.Set(x - startX, y - startY, Calc.Random.Choose(tiles.OverlapSprites), 1f, 1f);
                    }
                }
            }

            return new Generated { TileGrid = grid, SpriteOverlay = animated };
        }

        private Tiles TileHandler(VirtualMap<char> mapData, int x, int y, Rectangle forceFill, char forceID, Behaviour behaviour)
        {
            char tile = GetTile(mapData, x, y, forceFill, forceID, behaviour);
            if (IsEmpty(tile))
                return null;

            TerrainType type = lookup[tile];
            bool allSolid = true;
            int at = 0;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    bool solid = CheckTile(type, mapData, x + j, y + i, forceFill, behaviour);
                    if (!solid && behaviour.EdgesIgnoreOutOfLevel && !CheckForSameLevel(x, y, x + j, y + i))
                        solid = true;

                    adjacent[at++] = (byte)(solid ? 1 : 0);
                    if (!solid)
                        allSolid = false;
                }
            }

            if (!allSolid)
            {
                foreach (Masked masked in type.Masked)
                {
                    bool matches = true;
                    for (int i = 0; i < 9 && matches; i++)
                        if (masked.Mask[i] != 2 && masked.Mask[i] != adjacent[i])
                            matches = false;

                    if (matches)
                        return masked.Tiles;
                }
                return null;
            }

            bool padded;
            if (!behaviour.PaddingIgnoreOutOfLevel)
            {
                padded = !CheckTile(type, mapData, x - 2, y, forceFill, behaviour)
                    || !CheckTile(type, mapData, x + 2, y, forceFill, behaviour)
                    || !CheckTile(type, mapData, x, y - 2, forceFill, behaviour)
                    || !CheckTile(type, mapData, x, y + 2, forceFill, behaviour);
            }
            else
            {
                padded = (!CheckTile(type, mapData, x - 2, y, forceFill, behaviour) && CheckForSameLevel(x, y, x - 2, y))
                    || (!CheckTile(type, mapData, x + 2, y, forceFill, behaviour) && CheckForSameLevel(x, y, x + 2, y))
                    || (!CheckTile(type, mapData, x, y - 2, forceFill, behaviour) && CheckForSameLevel(x, y, x, y - 2))
                    || (!CheckTile(type, mapData, x, y + 2, forceFill, behaviour) && CheckForSameLevel(x, y, x, y + 2));
            }

            return padded ? lookup[tile].Padded : lookup[tile].Center;
        }

        private bool CheckForSameLevel(int x1, int y1, int x2, int y2)
        {
            foreach (Rectangle bounds in LevelBounds)
                if (bounds.Contains(x1, y1) && bounds.Contains(x2, y2))
                    return true;
            return false;
        }

        private bool CheckTile(TerrainType set, VirtualMap<char> mapData, int x, int y, Rectangle forceFill, Behaviour behaviour)
        {
            if (forceFill.Contains(x, y))
                return true;

            if (mapData == null)
                return behaviour.EdgesExtend;

            if (x >= 0 && y >= 0 && x < mapData.Columns && y < mapData.Rows)
            {
                char tile = mapData[x, y];
                return !IsEmpty(tile) && !set.Ignore(tile);
            }

            if (!behaviour.EdgesExtend)
                return false;

            char clamped = mapData[Calc.Clamp(x, 0, mapData.Columns - 1), Calc.Clamp(y, 0, mapData.Rows - 1)];
            return !IsEmpty(clamped) && !set.Ignore(clamped);
        }

        private char GetTile(VirtualMap<char> mapData, int x, int y, Rectangle forceFill, char forceID, Behaviour behaviour)
        {
            if (forceFill.Contains(x, y))
                return forceID;

            if (mapData == null)
                return behaviour.EdgesExtend ? forceID : '0';

            if (x >= 0 && y >= 0 && x < mapData.Columns && y < mapData.Rows)
                return mapData[x, y];

            if (!behaviour.EdgesExtend)
                return '0';

            return mapData[Calc.Clamp(x, 0, mapData.Columns - 1), Calc.Clamp(y, 0, mapData.Rows - 1)];
        }

        private bool IsEmpty(char id)
        {
            return id == '0' || id == '\0';
        }

        private class TerrainType
        {
            public char ID;
            public HashSet<char> Ignores = new HashSet<char>();
            public List<Masked> Masked = new List<Masked>();
            public Tiles Center = new Tiles();
            public Tiles Padded = new Tiles();

            public TerrainType(char id)
            {
                ID = id;
            }

            public bool Ignore(char c)
            {
                return ID != c && (Ignores.Contains(c) || Ignores.Contains('*'));
            }
        }

        private class Masked
        {
            public byte[] Mask = new byte[9];
            public Tiles Tiles = new Tiles();
        }

        private class Tiles
        {
            public List<MTexture> Textures = new List<MTexture>();
            public List<string> OverlapSprites = new List<string>();
            public bool HasOverlays;
        }

        public struct Generated
        {
            public TileGrid TileGrid;
            public AnimatedTiles SpriteOverlay;
        }

        public struct Behaviour
        {
            public bool PaddingIgnoreOutOfLevel;
            public bool EdgesIgnoreOutOfLevel;
            public bool EdgesExtend;
        }
    }
}
