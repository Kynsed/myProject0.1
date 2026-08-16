using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Monocle;

namespace myProject
{
    // Jogo proprio (nao e port): banco de animacoes em texto, no espirito do RoomMap.
    // O SpriteBank do Celeste le um XML gigante de conteudo; aqui o formato e legivel e
    // diffavel, e os frames saem do Atlas (um PNG por frame, ver GFX).
    //
    //   origin <x> <y>                    ancora do quadro, em px
    //   anim <id> <fps> <loop|once> <path>  frames = <path>00, <path>01, ... no atlas
    //   alias <id> <destino>              id que cai em outra animacao
    //   // comentario
    //
    // O alias existe por causa do Player herdado do port: ele chama Sprite.Play com 40
    // ids diferentes, e Sprite.Play de id inexistente joga excecao. Em vez de exigir 40
    // animacoes de arte no dia 1, os ids sem arte caem numa que existe.
    public class SpriteBank
    {
        public readonly Dictionary<string, AnimDef> Anims = new Dictionary<string, AnimDef>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        public Vector2Int Origin = new Vector2Int(0, 0);

        public struct Vector2Int
        {
            public int X, Y;
            public Vector2Int(int x, int y) { X = x; Y = y; }
        }

        public class AnimDef
        {
            public string Id;
            public string Path;
            public float Fps;
            public bool Loop;
        }

        public static SpriteBank FromFile(string path)
        {
            var bank = new SpriteBank();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//"))
                    continue;

                string[] p = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                switch (p[0])
                {
                    case "origin" when p.Length >= 3:
                        bank.Origin = new Vector2Int(int.Parse(p[1]), int.Parse(p[2]));
                        break;

                    case "anim" when p.Length >= 5:
                        bank.Anims[p[1]] = new AnimDef
                        {
                            Id = p[1],
                            Fps = float.Parse(p[2], CultureInfo.InvariantCulture),
                            Loop = p[3] == "loop",
                            Path = p[4]
                        };
                        break;

                    case "alias" when p.Length >= 3:
                        bank.Aliases[p[1]] = p[2];
                        break;
                }
            }
            return bank;
        }

        // Compatibilidade com o codigo herdado do port (Player pede "player_sweat"):
        // devolve um Sprite montado neste banco. NOTE: efeitos do Celeste (suor, cabelo)
        // nao tem arte propria aqui — vem vazio e nao desenha nada.
        public Sprite Create(string id)
        {
            var sprite = new PlayerSprite(PlayerSpriteMode.Madeline);
            return sprite;
        }

        // id -> animacao que existe de fato (segue a cadeia de alias)
        public string Resolve(string id)
        {
            string cur = id;
            for (int hops = 0; hops < 8; hops++)
            {
                if (Anims.ContainsKey(cur))
                    return cur;
                if (!Aliases.TryGetValue(cur, out string next))
                    return null;
                cur = next;
            }
            return null;
        }

        // Monta as animacoes num Sprite ja criado. Frames vem do atlas; animacao sem
        // frame nenhum e ignorada (arte ainda nao existe) e o Resolve cuida do resto.
        public void Build(Sprite sprite, Atlas atlas)
        {
            foreach (AnimDef def in Anims.Values)
            {
                List<MTexture> frames = atlas.GetAtlasSubtextures(def.Path);
                if (frames.Count == 0)
                    continue;

                float delay = def.Fps > 0f ? 1f / def.Fps : 0f;
                if (def.Loop)
                    sprite.AddLoop(def.Id, delay, frames.ToArray());
                else
                    sprite.Add(def.Id, delay, frames.ToArray());   // sem Goto: segura o ultimo frame
            }
            sprite.Justify = null;
            sprite.Origin = new Microsoft.Xna.Framework.Vector2(Origin.X, Origin.Y);
        }
    }
}
