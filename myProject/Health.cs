using System;
using Monocle;

namespace myProject
{
    // Jogo proprio (combate): pontos de vida de uma entidade atacavel.
    [Tracked(false)]
    public class Health : Component
    {
        public int Max;
        public int Current;
        public float FlashTimer;          // janela de flash visual pos-hit (lida pelo renderer)
        public Action<int> OnDamaged;     // recebe o dano aplicado
        public Action OnDeath;

        public Health(int max) : base(true, false)
        {
            Max = Current = max;
        }

        public void Damage(int amount)
        {
            if (Current <= 0)
                return;
            Current -= amount;
            FlashTimer = 0.12f;
            OnDamaged?.Invoke(amount);
            if (Current <= 0)
                OnDeath?.Invoke();
        }

        public override void Update()
        {
            if (FlashTimer > 0f)
                FlashTimer -= Engine.DeltaTime;
        }
    }
}
