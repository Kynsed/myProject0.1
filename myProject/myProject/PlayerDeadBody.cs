using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do timing de morte do PlayerDeadBody do Celeste:
    // (com bounce) Freeze 0.05s + 1 frame + tween CubeOut 0.5s do corpo (espera 75%)
    // -> DeathEffect 0.834s (espera 65%) -> ActionDelay -> End -> DeathAction
    // (default Level.Reload, via DoScreenWipe).
    // NOTE: podas de conteudo — sprite/hair/light e DeathEffect visuais, sons, rumble,
    // Displacement.AddBurst e o skip por MenuConfirm (menus podados do Input).
    public class PlayerDeadBody : Entity
    {
        public Action DeathAction;
        public float ActionDelay;
        public bool HasGolden;

        private const float DeathEffectDuration = 0.834f; // DeathEffect.Duration do Celeste

        private Vector2 bounce;
        private bool finished;

        public PlayerDeadBody(Player player, Vector2 direction) : base(player.Position)
        {
            Depth = -1000000;
            bounce = direction;
            Add(new Coroutine(DeathRoutine()));
        }

        private IEnumerator DeathRoutine()
        {
            Level level = SceneAs<Level>();
            if (bounce != Vector2.Zero)
            {
                Celeste.Freeze(0.05f);
                yield return null;
                Vector2 from = Position;
                Vector2 to = from + bounce * 24f;
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 0.5f, true);
                Add(tween);
                tween.OnUpdate = (Tween t) => Position = from + (to - from) * t.Eased;
                yield return tween.Duration * 0.75f;
                tween.Stop();
            }
            Position += Vector2.UnitY * -5f;
            level.Shake(0.3f);
            yield return DeathEffectDuration * 0.65f;
            if (ActionDelay > 0f)
                yield return ActionDelay;
            End();
        }

        private void End()
        {
            if (finished)
                return;
            finished = true;
            Level level = SceneAs<Level>();
            if (DeathAction == null)
                DeathAction = level.Reload;
            level.DoScreenWipe(false, DeathAction);
        }
    }
}
