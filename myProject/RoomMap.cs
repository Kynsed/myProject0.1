using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Andaime PROPRIO do metroidvania (nao existe no Celeste): formato de mapa em texto.
    //
    //   // comentario
    //   room X Y          <- canto superior-esquerdo da sala, em pixels
    //   ##########        <- fileiras de tiles; 1 char = tile 8x8
    //   #...^..D.#           '#' ou '1' = solido | '.', '0' ou ' ' = vazio
    //   ##########           '^' 'v' '<' '>' = espinho (aponta na direcao do char)
    //                        'D' = boneco de treino (combate)
    //                     <- linha em branco (ou proximo "room") encerra a sala
    //
    // Cada sala vira: um Rectangle em Level.Rooms (transicoes/camera) + um SolidTiles
    // (port fiel: Solid com collider Grid 8x8) + um Spikes 8px por char de espinho.
    public static class RoomMap
    {
        public const int TileSize = 8;

        public static void Load(Level level, string[] lines)
        {
            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i].TrimEnd();
                if (line.StartsWith("room "))
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var origin = new Vector2(int.Parse(parts[1]), int.Parse(parts[2]));
                    var rows = new List<string>();
                    i++;
                    while (i < lines.Length)
                    {
                        string row = lines[i].TrimEnd();
                        if (row.Length == 0 || row.StartsWith("room ") || row.StartsWith("//"))
                            break;
                        rows.Add(row);
                        i++;
                    }
                    AddRoom(level, origin, rows);
                }
                else
                    i++;
            }
        }

        private static void AddRoom(Level level, Vector2 origin, List<string> rows)
        {
            int cols = 0;
            foreach (string row in rows)
                cols = Math.Max(cols, row.Length);
            if (cols == 0 || rows.Count == 0)
                return;

            var data = new VirtualMap<char>(cols, rows.Count, '0');
            for (int y = 0; y < rows.Count; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char c = rows[y][x];
                    if (c == '#' || c == '1')
                        data[x, y] = '1';
                    else if (c == '^')
                        level.Add(new Spikes(origin + new Vector2(x * TileSize, (y + 1) * TileSize), TileSize, Spikes.Directions.Up));
                    else if (c == 'v')
                        level.Add(new Spikes(origin + new Vector2(x * TileSize, y * TileSize), TileSize, Spikes.Directions.Down));
                    else if (c == '<')
                        level.Add(new Spikes(origin + new Vector2((x + 1) * TileSize, y * TileSize), TileSize, Spikes.Directions.Left));
                    else if (c == '>')
                        level.Add(new Spikes(origin + new Vector2(x * TileSize, y * TileSize), TileSize, Spikes.Directions.Right));
                    else if (c == 'D')
                        level.Add(new TrainingDummy(origin + new Vector2(x * TileSize + TileSize / 2f, (y + 1) * TileSize)));
                }
            }

            level.Rooms.Add(new Rectangle((int)origin.X, (int)origin.Y,
                cols * TileSize, rows.Count * TileSize));
            level.Add(new SolidTiles(origin, data));
        }
    }
}
