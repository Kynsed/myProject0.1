using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do DreamBlock do Celeste. O dream dash em si mora no
    // Player (DreamDashCheck/StDreamDash); aqui ficam as partes que a fisica enxerga:
    // Solid safe com Depth -11000 (5000 se "below"), SurfaceSoundIndex 11/12, o vai-e-vem
    // por node (Tween YoyoLooping SineInOut, dist/12 e /3 se fastMoving, MoveTo quando
    // colidivel e MoveToNaive quando nao), OnPlayerExit com destruicao do oneUse, e
    // BlockedCheck/TryActorWiggleUp (empurra o ator ate 4px p/ cima antes de bloquear).
    // NOTE: podas de conteudo — particulas/shader/wobble/whiteFill, LightOcclude, shaker,
    // sons, Dust.Burst do OnPlayerExit e o ramo do TheoCrystal no BlockedCheck.
    [Tracked(false)]
    public class DreamBlock : Solid
    {
        private Vector2? node;
        private bool fastMoving;
        private bool oneUse;
        private bool playerHasDreamDash;

        public DreamBlock(Vector2 position, float width, float height, Vector2? node = null,
            bool fastMoving = false, bool oneUse = false, bool below = false)
            : base(position, width, height, true)
        {
            Depth = below ? 5000 : -11000;
            this.node = node;
            this.fastMoving = fastMoving;
            this.oneUse = oneUse;
            SurfaceSoundIndex = 11;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
            if (playerHasDreamDash && node != null)
            {
                Vector2 start = Position;
                Vector2 end = node.Value;
                float duration = Vector2.Distance(start, end) / 12f;
                if (fastMoving)
                    duration /= 3f;
                Tween tween = Tween.Create(Tween.TweenMode.YoyoLooping, Ease.SineInOut, duration, true);
                tween.OnUpdate = delegate (Tween t)
                {
                    if (Collidable)
                        MoveTo(Vector2.Lerp(start, end, t.Eased));
                    else
                        MoveToNaive(Vector2.Lerp(start, end, t.Eased));
                };
                Add(tween);
            }
        }

        public override void Update()
        {
            base.Update();
            if (playerHasDreamDash)
                SurfaceSoundIndex = 12;
        }

        public void OnPlayerExit(Player player)
        {
            if (oneUse)
                OneUseDestroy();
        }

        private void OneUseDestroy()
        {
            Collidable = Visible = false;
            DisableStaticMovers();
            RemoveSelf();
        }

        public bool BlockedCheck()
        {
            Player player = CollideFirst<Player>();
            return player != null && !TryActorWiggleUp(player);
        }

        private bool TryActorWiggleUp(Entity actor)
        {
            bool collidable = Collidable;
            Collidable = true;
            for (int i = 1; i <= 4; i++)
            {
                if (!actor.CollideCheck<Solid>(actor.Position - Vector2.UnitY * i))
                {
                    actor.Position -= Vector2.UnitY * i;
                    Collidable = collidable;
                    return true;
                }
            }
            Collidable = collidable;
            return false;
        }

        public void ActivateNoRoutine()
        {
            if (!playerHasDreamDash)
                playerHasDreamDash = true;
        }

        // NOTE: ripple visual (displacement) podado.
        public void FootstepRipple(Vector2 position) { }
    }
}
