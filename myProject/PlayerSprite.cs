using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Sprite do player. Monta as animacoes do banco (Content/Graphics/player.anim) sobre
    // o atlas de PNGs; sem atlas (harness headless, ou arte ausente) volta ao modo antigo
    // de registrar animacao vazia, entao o Player nunca quebra por id inexistente.
    //
    // O Player herdado do port chama Play com 40 ids diferentes: os que a arte nao cobre
    // sao resolvidos por alias no banco (ver player.anim).
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

        // id pedido pelo Player antes do alias (o estado real do personagem)
        public string RequestedAnimationID { get; private set; }

        private static readonly MTexture DummyFrame = new MTexture();
        private readonly SpriteBank bank;

        public PlayerSprite(PlayerSpriteMode mode) : base(null, null)
        {
            Mode = mode;
            bank = GFX.SpriteBank;
            if (bank != null && GFX.Game != null)
                bank.Build(this, GFX.Game);
        }

        public override void Play(string id, bool restart = false, bool randomizeFrame = false)
        {
            RequestedAnimationID = id;
            string real = bank?.Resolve(id);
            if (real != null)
            {
                base.Play(real, restart, randomizeFrame);
                return;
            }
            // sem banco/arte: registra um quadro vazio p/ o estado de animacao seguir valido
            if (!Has(id))
                AddLoop(id, 0.1f, DummyFrame);
            base.Play(id, restart, randomizeFrame);
        }

        public override void Render()
        {
            if (Texture == null || Texture.Texture == null)
                return;   // sem arte: quem desenha e o renderer de hitbox
            base.Render();
        }
    }
}
