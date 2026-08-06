using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do Water do Celeste: entidade trackeada com collider
    // retangular — e ela que os checks de natacao do Player (SwimCheck/SwimUnderwaterCheck/
    // SwimJumpCheck...) enxergam p/ entrar e sair do estado 3 (StSwim).
    // Flags fieis: Tags.TransitionUpdate, Depth -9999, collider Hitbox(width, height, 0, 0).
    // NOTE: podas de conteudo — superficie animada (ripples/tension/rays), grid de
    // displacement, sons de entrada/saida e o loop de WaterInteraction (usado por Holdables
    // e Seekers). Surface fica como stub p/ os DoRipple que o Player chama.
    [Tracked(false)]
    public class Water : Entity
    {
        public Surface TopSurface;
        public Surface BottomSurface;
        public List<Surface> Surfaces = new List<Surface>();

        public Water(Vector2 position, bool topSurface, bool bottomSurface, float width, float height)
            : base(position)
        {
            Tag = Tags.TransitionUpdate;
            Depth = -9999;
            Collider = new Hitbox(width, height, 0f, 0f);
            if (topSurface)
            {
                TopSurface = new Surface();
                Surfaces.Add(TopSurface);
            }
            if (bottomSurface)
            {
                BottomSurface = new Surface();
                Surfaces.Add(BottomSurface);
            }
        }

        public Water(Vector2 position, float width, float height)
            : this(position, true, false, width, height) { }

        // NOTE: superficie de agua (ripples/tension). Stub — so a API que o Player chama.
        public class Surface
        {
            public void DoRipple(Vector2 position, float resetTimeMultiplier) { }
        }
    }
}
