using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Som preso a uma entidade (loops de wallslide, dream dash, star fly...). O port
    // guardava esses componentes e chamava Play/Stop/Param; agora eles tocam de verdade.
    // Segue a posicao da entidade e morre junto com ela.
    public class SoundSource : Component
    {
        public string EventName;
        public bool DisposeOnTransition = true;
        public Vector2 Position;

        private SoundHandle handle;

        public SoundSource() : base(true, false) { }

        public bool Playing => handle != null && handle.Playing;

        public SoundSource Play(string path, string param = null, float value = 0f)
        {
            Stop();
            EventName = path;
            handle = Audio.IsLoop(path) ? Audio.Loop(path) : Audio.Play(path);
            UpdateSfxPosition();
            return this;
        }

        public SoundSource Stop(bool allowFadeout = true)
        {
            handle?.Stop();
            handle = null;
            return this;
        }

        public SoundSource Param(string param, float value)
        {
            return this;
        }

        public void UpdateSfxPosition()
        {
            if (handle == null)
                return;
            Vector2 at = Entity != null ? Entity.Position + Position : Position;
            handle.SetPosition(at);
        }

        public override void Update()
        {
            base.Update();
            UpdateSfxPosition();
        }

        public override void Removed(Entity entity)
        {
            Stop();
            base.Removed(entity);
        }

        public override void EntityRemoved(Scene scene)
        {
            Stop();
            base.EntityRemoved(scene);
        }
    }
}
