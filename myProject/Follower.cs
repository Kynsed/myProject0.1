using Monocle;

namespace myProject
{
    // NOTE: stub de conteudo (colecionaveis seguem o lider). Sem efeito no movimento do Player.
    public class Follower : Component
    {
        // Entity herdado de Component (entidade dona). Player so le.
        public Follower() : base(true, false) { }
    }
}
