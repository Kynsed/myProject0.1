using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stubs estaticos de conteudo (particulas/efeitos/audio/stats). No-op.
    // Sem efeito no movimento. Agrupados; portar fiel individualmente se necessario.

    public static class Dust
    {
        public static void Burst(Vector2 position, float direction, int count = 4, ParticleType particleType = null) { }
        public static void BurstFG(Vector2 position, float direction, int count = 4, float range = 0f, ParticleType particleType = null) { }
    }

    public static class ParticleTypes
    {
        public static ParticleType Dust = new ParticleType();
        public static ParticleType SparkyDust = new ParticleType();
    }

    public static class SlashFx
    {
        public static void Burst(Vector2 position, float direction) { }
    }

    public static class TrailManager
    {
        public static void Add(Entity entity, Vector2 scale, Color color, float duration) { }
    }

    public static class FallEffects
    {
        public static void Show(bool show) { }
    }

    public static class DeathEffect
    {
        public static void Draw(Vector2 position, Color color, float ease) { }
    }

    public enum Stat { DASHES }

    public static class Stats
    {
        public static void Increment(Stat stat, int amount = 1) { }
    }

    public static class SFX
    {
        // NOTE: dicionario de mapeamento de sons (conteudo). Vazio no port.
        public static System.Collections.Generic.Dictionary<string, string> MadelineToBadelineSound
            = new System.Collections.Generic.Dictionary<string, string>();
    }

    // Port fiel do Celeste.Tags (mesmos 6 tags; criados juntos no cctor, antes de qualquer Scene).
    public static class Tags
    {
        public static readonly BitTag PauseUpdate = new BitTag("pauseUpdate");
        public static readonly BitTag FrozenUpdate = new BitTag("frozenUpdate");
        public static readonly BitTag TransitionUpdate = new BitTag("transitionUpdate");
        public static readonly BitTag HUD = new BitTag("hud");
        public static readonly BitTag Persistent = new BitTag("persistent");
        public static readonly BitTag Global = new BitTag("global");
    }
}
