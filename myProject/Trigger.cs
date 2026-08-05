using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: base de conteudo podada. Player referencia OnEnter/OnStay/OnLeave/Triggered.
    [Tracked(true)]
    public abstract class Trigger : Entity
    {
        public bool Triggered;
        public bool PlayerIsInside { get; private set; }

        protected Trigger() : base(Vector2.Zero) { }

        public virtual void OnEnter(Player player) { PlayerIsInside = true; }
        public virtual void OnStay(Player player) { }
        public virtual void OnLeave(Player player) { PlayerIsInside = false; }
    }
}
