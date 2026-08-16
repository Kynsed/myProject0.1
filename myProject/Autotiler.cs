using Monocle;

namespace myProject
{
    // Jogo proprio (o Autotiler do Celeste e data-driven por XML de conteudo e ficou de
    // fora do port): escolhe o tile de cada celula pela vizinhanca.
    //
    // Mascara de 4 bits — N=1, E=2, S=4, W=8 — com bit LIGADO quando o vizinho tambem e
    // solido. O indice no tileset e a propria mascara (16 tiles numa fita), entao o tile
    // desenha borda so nos lados expostos.
    //
    // Fora do mapa conta como solido: assim a borda da sala nao vira contorno de casca
    // grossa quando a sala continua na sala vizinha.
    public static class Autotiler
    {
        public const int Empty = -1;

        public const int North = 1;
        public const int East = 2;
        public const int South = 4;
        public const int West = 8;

        public static int MaskAt(VirtualMap<char> data, int x, int y)
        {
            if (!IsSolid(data, x, y))
                return Empty;

            int mask = 0;
            if (IsSolid(data, x, y - 1)) mask |= North;
            if (IsSolid(data, x + 1, y)) mask |= East;
            if (IsSolid(data, x, y + 1)) mask |= South;
            if (IsSolid(data, x - 1, y)) mask |= West;
            return mask;
        }

        public static int[,] Build(VirtualMap<char> data)
        {
            var tiles = new int[data.Columns, data.Rows];
            for (int x = 0; x < data.Columns; x++)
                for (int y = 0; y < data.Rows; y++)
                    tiles[x, y] = MaskAt(data, x, y);
            return tiles;
        }

        private static bool IsSolid(VirtualMap<char> data, int x, int y)
        {
            if (x < 0 || y < 0 || x >= data.Columns || y >= data.Rows)
                return true;   // fora do mapa: trata como cheio
            return data[x, y] != '0';
        }
    }
}
