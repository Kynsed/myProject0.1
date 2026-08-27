using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub de conteudo (objeto carregavel: Theo/jellyfish). A logica de carregar/soltar
    // vive no Player (fiel); aqui so a superficie minima. Sem entidades carregaveis no port.
    [Tracked(false)]
    public class Holdable : Component
    {
        // Entity herdado de Component (entidade carregada). Player so le.
        public bool SlowFall;
        public bool SlowRun;

        public Holdable() : base(true, false) { }

        public bool Check(Player player) => false;
        public bool Pickup(Player player) => false;
        public void Carry(Vector2 position) { }
        public void Release(Vector2 force) { }
    }
}
