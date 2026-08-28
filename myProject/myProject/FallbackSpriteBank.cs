using System.Xml;
using Monocle;

namespace myProject
{
    // Banco vazio usado quando nao ha conteudo carregado (harnesses headless: sem
    // GraphicsDevice nao ha atlas, sem atlas nao ha banco). O Player.ctor chama
    // GFX.SpriteBank.Create("player_sweat") no construtor, entao GFX.SpriteBank nunca
    // pode ser null. No Celeste isso nao existe — o jogo so constroi Player com conteudo.
    public class FallbackSpriteBank : SpriteBank
    {
        public FallbackSpriteBank() : base(null, EmptyDocument())
        {
        }

        private static XmlDocument EmptyDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("Sprites"));
            return doc;
        }

        public override Sprite Create(string id)
        {
            return new TolerantSprite();
        }

        public override Sprite CreateOn(Sprite sprite, string id)
        {
            return sprite;
        }
    }

    // Sprite sem animacao nenhuma que TOLERA Play de qualquer id, registrando um frame
    // vazio em vez de lancar (Sprite.Play faz animations[id]). So existe p/ o modo
    // headless: com conteudo carregado, o --sprite-test garante que todo id resolve.
    public class TolerantSprite : Sprite
    {
        private static readonly MTexture DummyFrame = new MTexture();

        public TolerantSprite() : base(null, "") { }

        public override void Play(string id, bool restart = false, bool randomizeFrame = false)
        {
            if (!Has(id))
                AddLoop(id, 0.1f, DummyFrame);
            base.Play(id, restart, randomizeFrame);
        }

        // O frame dummy nao tem textura — desenhar lancaria NullReference.
        public override void Render()
        {
            if (Texture != null && Texture.Texture != null)
                base.Render();
        }
    }
}
