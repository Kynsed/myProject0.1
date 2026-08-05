using Monocle;

namespace myProject
{
    // NOTE: stub de graficos/atlas (conteudo). Render real podado.
    public static class GFX
    {
        public static GameAtlasStub Game = new GameAtlasStub();
        public static SpriteBankStub SpriteBank = new SpriteBankStub();
    }

    public class GameAtlasStub
    {
        public MTexture this[string id] => null;
        public MTexture GetAtlasSubtexturesAt(string path, int index) => null;
    }

    public class SpriteBankStub
    {
        public Sprite Create(string id) => new PlayerSprite(PlayerSpriteMode.Madeline);
    }
}
