using System;

namespace Monocle
{
    public static class Tiler
    {
        public enum EdgeBehavior
        {
            True,
            False,
            Wrap
        }

        public static int TileX { get; private set; }
        public static int TileY { get; private set; }

        public static bool Left { get; private set; }
        public static bool Right { get; private set; }
        public static bool Up { get; private set; }
        public static bool Down { get; private set; }
        public static bool UpLeft { get; private set; }
        public static bool UpRight { get; private set; }
        public static bool DownLeft { get; private set; }
        public static bool DownRight { get; private set; }

        public static int[,] Tile(bool[,] bits, Func<int> tileDecider, Action<int> tileOutput, int tileWidth, int tileHeight, EdgeBehavior edges)
        {
            int width = bits.GetLength(0);
            int height = bits.GetLength(1);
            int[,] tiles = new int[width, height];

            for (TileX = 0; TileX < width; TileX++)
            {
                for (TileY = 0; TileY < height; TileY++)
                {
                    if (bits[TileX, TileY])
                    {
                        switch (edges)
                        {
                        case EdgeBehavior.True:
                            Left = TileX == 0 || bits[TileX - 1, TileY];
                            Right = TileX == width - 1 || bits[TileX + 1, TileY];
                            Up = TileY == 0 || bits[TileX, TileY - 1];
                            Down = TileY == height - 1 || bits[TileX, TileY + 1];
                            UpLeft = TileX == 0 || TileY == 0 || bits[TileX - 1, TileY - 1];
                            UpRight = TileX == width - 1 || TileY == 0 || bits[TileX + 1, TileY - 1];
                            DownLeft = TileX == 0 || TileY == height - 1 || bits[TileX - 1, TileY + 1];
                            DownRight = TileX == width - 1 || TileY == height - 1 || bits[TileX + 1, TileY + 1];
                            break;

                        case EdgeBehavior.False:
                            Left = TileX != 0 && bits[TileX - 1, TileY];
                            Right = TileX != width - 1 && bits[TileX + 1, TileY];
                            Up = TileY != 0 && bits[TileX, TileY - 1];
                            Down = TileY != height - 1 && bits[TileX, TileY + 1];
                            UpLeft = TileX != 0 && TileY != 0 && bits[TileX - 1, TileY - 1];
                            UpRight = TileX != width - 1 && TileY != 0 && bits[TileX + 1, TileY - 1];
                            DownLeft = TileX != 0 && TileY != height - 1 && bits[TileX - 1, TileY + 1];
                            DownRight = TileX != width - 1 && TileY != height - 1 && bits[TileX + 1, TileY + 1];
                            break;

                        case EdgeBehavior.Wrap:
                            Left = bits[(TileX + width - 1) % width, TileY];
                            Right = bits[(TileX + 1) % width, TileY];
                            Up = bits[TileX, (TileY + height - 1) % height];
                            Down = bits[TileX, (TileY + 1) % height];
                            UpLeft = bits[(TileX + width - 1) % width, (TileY + height - 1) % height];
                            UpRight = bits[(TileX + 1) % width, (TileY + height - 1) % height];
                            DownLeft = bits[(TileX + width - 1) % width, (TileY + 1) % height];
                            DownRight = bits[(TileX + 1) % width, (TileY + 1) % height];
                            break;
                        }

                        int tile = tileDecider();
                        tileOutput(tile);
                        tiles[TileX, TileY] = tile;
                    }
                }
            }

            return tiles;
        }

        public static int[,] Tile(bool[,] bits, bool[,] mask, Func<int> tileDecider, Action<int> tileOutput, int tileWidth, int tileHeight, EdgeBehavior edges)
        {
            int width = bits.GetLength(0);
            int height = bits.GetLength(1);
            int[,] tiles = new int[width, height];

            for (TileX = 0; TileX < width; TileX++)
            {
                for (TileY = 0; TileY < height; TileY++)
                {
                    if (bits[TileX, TileY])
                    {
                        switch (edges)
                        {
                        case EdgeBehavior.True:
                            Left = TileX == 0 || bits[TileX - 1, TileY] || mask[TileX - 1, TileY];
                            Right = TileX == width - 1 || bits[TileX + 1, TileY] || mask[TileX + 1, TileY];
                            Up = TileY == 0 || bits[TileX, TileY - 1] || mask[TileX, TileY - 1];
                            Down = TileY == height - 1 || bits[TileX, TileY + 1] || mask[TileX, TileY + 1];
                            UpLeft = TileX == 0 || TileY == 0 || bits[TileX - 1, TileY - 1] || mask[TileX - 1, TileY - 1];
                            UpRight = TileX == width - 1 || TileY == 0 || bits[TileX + 1, TileY - 1] || mask[TileX + 1, TileY - 1];
                            DownLeft = TileX == 0 || TileY == height - 1 || bits[TileX - 1, TileY + 1] || mask[TileX - 1, TileY + 1];
                            DownRight = TileX == width - 1 || TileY == height - 1 || bits[TileX + 1, TileY + 1] || mask[TileX + 1, TileY + 1];
                            break;

                        case EdgeBehavior.False:
                            Left = TileX != 0 && (bits[TileX - 1, TileY] || mask[TileX - 1, TileY]);
                            Right = TileX != width - 1 && (bits[TileX + 1, TileY] || mask[TileX + 1, TileY]);
                            Up = TileY != 0 && (bits[TileX, TileY - 1] || mask[TileX, TileY - 1]);
                            Down = TileY != height - 1 && (bits[TileX, TileY + 1] || mask[TileX, TileY + 1]);
                            UpLeft = TileX != 0 && TileY != 0 && (bits[TileX - 1, TileY - 1] || mask[TileX - 1, TileY - 1]);
                            UpRight = TileX != width - 1 && TileY != 0 && (bits[TileX + 1, TileY - 1] || mask[TileX + 1, TileY - 1]);
                            DownLeft = TileX != 0 && TileY != height - 1 && (bits[TileX - 1, TileY + 1] || mask[TileX - 1, TileY + 1]);
                            DownRight = TileX != width - 1 && TileY != height - 1 && (bits[TileX + 1, TileY + 1] || mask[TileX + 1, TileY + 1]);
                            break;

                        case EdgeBehavior.Wrap:
                            Left = bits[(TileX + width - 1) % width, TileY] || mask[(TileX + width - 1) % width, TileY];
                            Right = bits[(TileX + 1) % width, TileY] || mask[(TileX + 1) % width, TileY];
                            Up = bits[TileX, (TileY + height - 1) % height] || mask[TileX, (TileY + height - 1) % height];
                            Down = bits[TileX, (TileY + 1) % height] || mask[TileX, (TileY + 1) % height];
                            UpLeft = bits[(TileX + width - 1) % width, (TileY + height - 1) % height] || mask[(TileX + width - 1) % width, (TileY + height - 1) % height];
                            UpRight = bits[(TileX + 1) % width, (TileY + height - 1) % height] || mask[(TileX + 1) % width, (TileY + height - 1) % height];
                            DownLeft = bits[(TileX + width - 1) % width, (TileY + 1) % height] || mask[(TileX + width - 1) % width, (TileY + 1) % height];
                            DownRight = bits[(TileX + 1) % width, (TileY + 1) % height] || mask[(TileX + 1) % width, (TileY + 1) % height];
                            break;
                        }

                        int tile = tileDecider();
                        tileOutput(tile);
                        tiles[TileX, TileY] = tile;
                    }
                }
            }

            return tiles;
        }

        public static int[,] Tile(bool[,] bits, AutotileData autotileData, Action<int> tileOutput, int tileWidth, int tileHeight, EdgeBehavior edges)
        {
            return Tile(bits, autotileData.TileHandler, tileOutput, tileWidth, tileHeight, edges);
        }

        public static int[,] Tile(bool[,] bits, bool[,] mask, AutotileData autotileData, Action<int> tileOutput, int tileWidth, int tileHeight, EdgeBehavior edges)
        {
            return Tile(bits, mask, autotileData.TileHandler, tileOutput, tileWidth, tileHeight, edges);
        }
    }
}
