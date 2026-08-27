using FMOD.Studio;
using Microsoft.Xna.Framework;

namespace myProject
{
    // NOTE: stub de audio (conteudo). No-op. Sem efeito no movimento.
    public static class Audio
    {
        public static bool MusicUnderwater;

        public static EventInstance Play(string path) => new EventInstance();
        public static EventInstance Play(string path, Vector2 position) => new EventInstance();
        public static EventInstance Play(string path, string param, float value) => new EventInstance();
        public static EventInstance Play(string path, Vector2 position, string param, float value) => new EventInstance();
        public static EventInstance Play(string path, Vector2 position, string param, float value, string param2, float value2) => new EventInstance();
        public static EventInstance Loop(string path) => new EventInstance();

        public static EventInstance Position(EventInstance instance, Vector2 position) => instance;
        public static void Stop(EventInstance instance, bool allowFadeOut = true) { }
        public static void Apply(bool immediate) { }
    }
}
