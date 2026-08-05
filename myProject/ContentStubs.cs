using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stubs de conteudo (entidades/componentes). Usados pelo Player so como tipo/colisao
    // ou superficie minima. Sem efeito no movimento base. Portar fiel individualmente depois.

    [Tracked(false)] public class StrawberrySeed : Entity { }
    [Tracked(false)] public class SpeedRing : Entity
    {
        public SpeedRing Init(Vector2 position, float angle, Color color) => this;
    }
    [Tracked(false)] public class PlayerDashAssist : Entity
    {
        public float Direction;
        public Vector2 Offset;
        public float Scale;
    }
    [Tracked(false)] public class BlockField : Entity { }
    [Tracked(false)] public class Killbox : Entity { }

    [Tracked(false)]
    public class Strawberry : Entity
    {
        public bool Golden;
        public bool Winged;
        public EntityID ID;
    }

    // NOTE: stub de id de entidade (conteudo).
    public struct EntityID
    {
        public string Level;
        public int ID;
    }

    [Tracked(false)]
    public class CrystalStaticSpinner : Entity
    {
        public void Destroy(bool boss = false) { }
    }

    [Tracked(false)]
    public class BadelineBoost : Entity
    {
        public static ParticleType P_Move = new ParticleType();
    }

    [Tracked(false)]
    public class Lookout : Entity
    {
        public void StopInteracting() { }
    }

    [Tracked(false)]
    public class BadelineOldsite : Entity
    {
        public static readonly Color HairColor = new Color(154, 99, 188);
    }

    // NOTE: Spikes saiu daqui — port fiel em Spikes.cs (hazard com kill direcional).

    // NOTE: CameraLocker e Component em Celeste (Player le .Entity/.MaxXOffset/.MaxYOffset).
    [Tracked(false)]
    public class CameraLocker : Component
    {
        public float MaxXOffset;
        public float MaxYOffset;

        public CameraLocker() : base(false, false) { }
    }

    [Tracked(false)]
    public class TalkComponent : Component
    {
        public static TalkComponent PlayerOver;

        public TalkComponent() : base(true, false) { }
    }

    public class WaterInteraction : Component
    {
        public Func<bool> IsActive;

        public WaterInteraction(Func<bool> isActive) : base(false, false)
        {
            IsActive = isActive;
        }
    }

    [Tracked(false)]
    public class SpawnFacingTrigger : Trigger
    {
        public Facings Facing;
    }

    [Tracked(false)]
    public class NegaBlock : Solid
    {
        public NegaBlock() : base(Vector2.Zero, 0f, 0f, false) { }
    }

    // NOTE: SolidTiles saiu daqui — port fiel em SolidTiles.cs (geometria da sala por tiles).
}
