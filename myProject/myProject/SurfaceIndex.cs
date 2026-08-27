using System.Collections.Generic;
using Monocle;

namespace myProject
{
    // NOTE: port podado. GetPlatformByPriority fiel (usado pelo Player); removidos os ~36
    // consts de indice de superficie (Snow, Dirt, Resort*...) e TileToIndex — so mapeiam
    // sons de passo por bioma do Celeste, sem efeito no movimento.
    public class SurfaceIndex
    {
        public const string Param = "surface_index";

        public static Platform GetPlatformByPriority(List<Entity> platforms)
        {
            Platform platform = null;
            foreach (Entity entity in platforms)
            {
                if (entity is Platform && (platform == null || (entity as Platform).SurfaceSoundPriority > platform.SurfaceSoundPriority))
                    platform = entity as Platform;
            }
            return platform;
        }
    }
}
