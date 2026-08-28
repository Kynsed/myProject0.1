using Monocle;

namespace myProject
{
    // NOTE: stub de tiles animados (visual puro: cachoeiras, tochas, cristais piscando).
    // O Autotiler os cria pelo atributo "sprites" do <set>, que o ForegroundTiles.xml
    // placeholder nao usa. Mantido com a API que o Autotiler e o SolidTiles tocam, p/ o
    // port do Autotiler ficar fiel linha a linha. Portar de verdade exige AnimatedTilesBank
    // com XML proprio e arte animada.
    public class AnimatedTiles : Component
    {
        public Camera ClipCamera;

        public AnimatedTiles(int columns, int rows, AnimatedTilesBank bank) : base(true, true)
        {
        }

        public void Set(int x, int y, string name, float scaleX = 1f, float scaleY = 1f)
        {
        }
    }

    // NOTE: stub do banco de tiles animados (conteudo).
    public class AnimatedTilesBank
    {
    }
}
