using Microsoft.Xna.Framework.Audio;

// NOTE: isto NAO e FMOD. E o handle do nosso Audio vestindo o nome e o namespace que o
// codigo portado espera — o Player.cs tipa dois membros como EventInstance (o retorno de
// Player.Play e o campo idleSfx). Manter o nome deixa o arquivo do port intocado, que e
// o mesmo criterio usado no SpriteBank.Create virtual: divergir num shim nosso em vez de
// no Player.cs.
//
// O Celeste implementa isso com FMOD Studio (banks, buses, VCAs, snapshots, listener 3D).
// Nao ha FMOD aqui e o celeste_source nao traz os banks — so o C# que os carrega. Entao
// a API tem a forma do Celeste e a implementacao e SoundEffect do MonoGame.
namespace FMOD.Studio
{
    public class EventInstance
    {
        internal SoundEffectInstance Instance;
        internal string EventPath;

        public bool Playing
        {
            get { return Instance != null && Instance.State == SoundState.Playing; }
        }

        // Handle mudo: sem dispositivo de audio, sem som no banco, ou evento inexistente.
        // Continua sendo um objeto valido — quem chamou nao precisa checar null.
        public bool IsSilent
        {
            get { return Instance == null; }
        }

        public void Pause()
        {
            if (Instance != null && Instance.State == SoundState.Playing)
                Instance.Pause();
        }

        public void Resume()
        {
            if (Instance != null && Instance.State == SoundState.Paused)
                Instance.Resume();
        }

        public void release()
        {
            if (Instance == null)
                return;
            Instance.Stop(true);
            Instance.Dispose();
            Instance = null;
        }

        public void setParameterValue(string name, float value)
        {
            if (Instance == null)
                return;

            // NOTE: o Celeste manda o parametro p/ o FMOD, que decide o que fazer com ele
            // (camada de musica, variacao de superficie, etc). Aqui so os parametros com
            // significado direto em SoundEffectInstance tem efeito; o resto e ignorado.
            if (name == "volume")
                Instance.Volume = Calc(value, 0f, 1f);
            else if (name == "pan")
                Instance.Pan = Calc(value, -1f, 1f);
            else if (name == "pitch")
                Instance.Pitch = Calc(value, -1f, 1f);
        }

        private static float Calc(float v, float min, float max)
        {
            return (v < min) ? min : ((v > max) ? max : v);
        }
    }
}
