using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub de audio (conteudo). Sem efeito no movimento.
    public class SoundSource : Component
    {
        public string EventName;
        public bool DisposeOnTransition = true;
        public Vector2 Position;

        public SoundSource() : base(true, false) { }

        public bool Playing { get; private set; }

        public SoundSource Play(string path, string param = null, float value = 0f)
        {
            EventName = path;
            Playing = true;
            return this;
        }

        public SoundSource Stop(bool allowFadeout = true)
        {
            Playing = false;
            return this;
        }

        public SoundSource Param(string param, float value)
        {
            return this;
        }

        public void UpdateSfxPosition() { }
    }
}
