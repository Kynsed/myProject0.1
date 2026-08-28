using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Forma do SoundSource do Celeste: componente que toca um evento, segue a entidade e
    // morre com ela.
    //
    // NOTE: Is3D e IsOneshot ficaram de fora porque no Celeste vem do EventDescription do
    // FMOD (Audio.GetEventDescription), e nao ha equivalente aqui. DebugRender tambem saiu
    // (desenha a fonte do som com o console aberto).
    public class SoundSource : Component
    {
        public string EventName;
        public Vector2 Position = Vector2.Zero;
        public bool DisposeOnTransition = true;
        public bool RemoveOnOneshotEnd;

        private EventInstance instance;

        public SoundSource() : base(true, false) { }

        public SoundSource(string path) : this()
        {
            Play(path);
        }

        public SoundSource(Vector2 offset, string path) : this()
        {
            Position = offset;
            Play(path);
        }

        public bool Playing { get; private set; }

        public bool InstancePlaying
        {
            get { return instance != null && instance.Playing; }
        }

        public override void Added(Entity entity)
        {
            base.Added(entity);
            UpdateSfxPosition();
        }

        public SoundSource Play(string path, string param = null, float value = 0f)
        {
            Stop(true);

            EventName = path;
            Playing = true;
            instance = Audio.Play(path, SfxPosition);
            if (param != null)
                instance.setParameterValue(param, value);
            return this;
        }

        public SoundSource Stop(bool allowFadeout = true)
        {
            if (instance != null)
            {
                Audio.Stop(instance, allowFadeout);
                instance = null;
            }
            Playing = false;
            return this;
        }

        public SoundSource Pause()
        {
            if (instance != null)
                instance.Pause();
            return this;
        }

        public SoundSource Resume()
        {
            if (instance != null)
                instance.Resume();
            return this;
        }

        public SoundSource Param(string param, float value)
        {
            if (instance != null)
                instance.setParameterValue(param, value);
            return this;
        }

        public void UpdateSfxPosition()
        {
            if (instance != null)
                Audio.Position(instance, SfxPosition);
        }

        public override void Update()
        {
            base.Update();
            UpdateSfxPosition();

            if (RemoveOnOneshotEnd && Playing && instance != null && !instance.Playing)
            {
                Stop(false);
                RemoveSelf();
            }
        }

        public override void EntityRemoved(Scene scene)
        {
            base.EntityRemoved(scene);
            Stop(false);
        }

        public override void Removed(Entity entity)
        {
            base.Removed(entity);
            Stop(false);
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
            Stop(false);
        }

        private Vector2 SfxPosition
        {
            get { return (Entity != null) ? Entity.Position + Position : Position; }
        }
    }
}
