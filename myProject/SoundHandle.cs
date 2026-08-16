using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace myProject
{
    // Alca de um som tocando. Substitui o FMOD.Studio.EventInstance que o port usava:
    // o codigo herdado guarda a alca p/ parar/mover o som depois.
    //
    // Handle "mudo" (sem instancia) e valido: acontece quando o som nao esta mapeado no
    // banco ou nao ha dispositivo de audio. Quem chama nunca precisa checar null.
    public class SoundHandle
    {
        public readonly string Path;

        private SoundEffectInstance instance;
        private float baseVolume = 1f;

        public SoundHandle(string path = null)
        {
            Path = path;
        }

        public bool Playing => instance != null && instance.State == SoundState.Playing;

        internal void Attach(SoundEffectInstance inst, float volume)
        {
            instance = inst;
            baseVolume = volume;
        }

        // Estereo simples pela posicao na tela: pan pelo eixo X da camera, volume caindo
        // com a distancia. Nada de 3D — o jogo e 2D e a tela e pequena.
        public void SetPosition(Vector2 position)
        {
            // sem Engine (harness headless) nao ha camera p/ posicionar: fica mono
            if (instance == null || Engine.Instance == null)
                return;

            Level level = Engine.Scene as Level;
            if (level == null)
                return;

            float half = 160f;
            float dx = position.X - (level.Camera.Position.X + half);
            instance.Pan = MathHelper.Clamp(dx / half, -1f, 1f);

            float dy = position.Y - (level.Camera.Position.Y + 90f);
            float dist = new Vector2(dx, dy).Length();
            float fade = MathHelper.Clamp(1f - (dist - half) / (half * 2f), 0.15f, 1f);
            instance.Volume = MathHelper.Clamp(baseVolume * Audio.MasterVolume * fade, 0f, 1f);
        }

        public void SetVolume(float scale)
        {
            if (instance != null)
                instance.Volume = MathHelper.Clamp(baseVolume * Audio.MasterVolume * scale, 0f, 1f);
        }

        public void Stop()
        {
            if (instance == null)
                return;
            instance.Stop();
            instance.Dispose();
            instance = null;
        }

        // compatibilidade com as chamadas herdadas do port
        public void release() { Stop(); }
        public void setParameterValue(string name, float value) { }
    }
}
