using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub de conteudo (gerencia followers/colecionaveis). Sem efeito no movimento.
    public class Leader : Component
    {
        public Vector2 Position;
        public List<Follower> Followers = new List<Follower>();

        public Leader(Vector2 position) : base(true, false)
        {
            Position = position;
        }

        public void TransferFollowers() { }
        public void LoseFollowers() { }
    }
}
