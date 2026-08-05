using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: stub visual. Estende Monocle.Sprite (herda animacao/estado). Sem atlas real:
    // Play() registra uma animacao dummy (nao crasha) e Render() e no-op (sem textura).
    // O estado de animacao (CurrentAnimationID/Frame) fica valido para o Player ler.
    public class PlayerSprite : Sprite
    {
        public PlayerSpriteMode Mode { get; private set; }
        public Vector2 HairOffset;
        public float CarryYOffset;
        public int HairFrame;
        public bool HasHair = true;
        public bool Running;
        public bool DreamDashing;
        public int HairCount = 4;

        private static readonly MTexture DummyFrame = new MTexture();

        public PlayerSprite(PlayerSpriteMode mode) : base(null, null)
        {
            Mode = mode;
        }

        public override void Play(string id, bool restart = false, bool randomizeFrame = false)
        {
            if (!Has(id))
                AddLoop(id, 0.1f, DummyFrame);
            base.Play(id, restart, randomizeFrame);
        }

        // Sem textura real: nao desenha (o hitbox e desenhado pelo renderer de debug).
        public override void Render() { }
    }
}
