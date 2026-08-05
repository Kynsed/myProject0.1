using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // NOTE: andaime de conteudo (Scene de gameplay). So a superficie lida pelo Player/Platform.
    // Campos que afetam movimento (Bounds, Wind, Camera) sao reais; cutscenes/FX/load sao no-op.
    public class Level : Scene
    {
        public enum CameraLockModes { None, BoundsClamped, Center, FinalBoss, BoostSequence }

        public Rectangle Bounds;
        public Camera Camera = new Camera();

        // NOTE: substitui Session.MapData do Celeste — salas do metroidvania sao retangulos
        // conectados por borda, num mundo persistente (sem load/unload de entidades por sala).
        public List<Rectangle> Rooms = new List<Rectangle>();
        public Rectangle? PreviousBounds { get; private set; }
        public float NextTransitionDuration = 0.65f;
        private Coroutine transition;
        public bool Transitioning => transition != null;
        public Vector2 CameraOffset;
        public float CameraUpwardMaxY;
        public CameraLockModes CameraLockMode;
        public Vector2 Wind;
        public bool InSpace;
        public bool Raining;
        public bool CanRetry = true;
        public bool InCutscene;
        public Session.CoreModes CoreMode;
        public Session Session = new Session();

        // NOTE: sistemas de particulas reais (nao renderizados aqui), so p/ Emit nao dar NRE.
        public ParticleSystem Particles = new ParticleSystem(-8000, 400);
        public ParticleSystem ParticlesBG = new ParticleSystem(8000, 400);
        public ParticleSystem ParticlesFG = new ParticleSystem(-50000, 400);
        public DisplacementRenderer Displacement = new DisplacementRenderer();
        public FormationBackdrop FormationBackdrop = new FormationBackdrop();
        public Random HiccupRandom = new Random();

        public void Shake(float time = 0.3f) { }
        public void DirectionalShake(Vector2 dir, float time = 0.3f) { }
        public Vector2 GetSpawnPoint(Vector2 from) => from;

        // NOTE: wipe visual podado — callback adiado p/ o fim do frame (no Celeste o wipe
        // tambem adia: o callback dispara ao completar, fora do update das entidades).
        public void DoScreenWipe(bool wipeIn, Action onComplete = null, bool hiresSnow = false)
        {
            if (onComplete != null)
                OnEndOfFrame += () => onComplete();
        }

        // Subset do Level.Reload + LoadLevel(IntroTypes.Respawn) do Celeste: novo Player no
        // Session.RespawnPoint com intro fiel (estado 14) e camera snapada no alvo.
        // NOTE: mundo persistente — nao recarrega entidades da sala nem zera o timer da Session.
        public void Reload()
        {
            PlayerDeadBody body = Entities.FindFirst<PlayerDeadBody>();
            if (body != null)
                Remove(body);
            Engine.TimeRate = 1f;

            Vector2 spawn = Session.RespawnPoint
                ?? new Vector2(Bounds.Left, Bounds.Bottom); // DefaultSpawnPoint do Celeste
            Player player = new Player(spawn, PlayerSpriteMode.Madeline);
            player.IntroType = Player.IntroTypes.Respawn;
            player.Add(new MeleeCombo()); // NOTE: jogo proprio — combate acompanha o respawn
            Add(player);
            Entities.UpdateLists(); // como o LoadLevel: Added roda antes do snap da camera
            Camera.Position = GetFullCameraTargetAt(player, player.Position);
        }

        // Port fiel do branch de transicao do Level.Update do Celeste: durante a transicao
        // so entidades com Tags.TransitionUpdate atualizam; o Player e movido pela rotina.
        public override void Update()
        {
            if (transition == null)
            {
                base.Update();
                return;
            }
            foreach (Entity e in this[Tags.TransitionUpdate])
                e.Update();
            transition.Update();
            RendererList.Update();
        }

        // Sala que contem o ponto (Session.MapData.GetAt/CanTransitionTo do Celeste).
        private Rectangle? RoomAt(Vector2 at)
        {
            foreach (Rectangle r in Rooms)
                if (at.X >= r.Left && at.Y >= r.Top && at.X < r.Right && at.Y < r.Bottom)
                    return r;
            return null;
        }

        public bool CanTransitionTo(Vector2 at)
        {
            Rectangle? room = RoomAt(at);
            return room != null && room.Value != Bounds;
        }

        // Port fiel (Level.IsInBounds do Celeste).
        public bool IsInBounds(Vector2 position, Vector2 dirPad)
        {
            float padRight = Math.Max(dirPad.X, 0f);
            float padLeft = Math.Max(-dirPad.X, 0f);
            float padBottom = Math.Max(dirPad.Y, 0f);
            float padTop = Math.Max(-dirPad.Y, 0f);
            Rectangle bounds = Bounds;
            return position.X >= bounds.Left + padRight && position.Y >= bounds.Top + padBottom
                && position.X < bounds.Right - padLeft && position.Y < bounds.Bottom - padTop;
        }

        // Port fiel do Celeste. NOTE: CameraTargetTrigger/CameraOffsetTrigger podados (conteudo).
        public Vector2 GetFullCameraTargetAt(Player player, Vector2 at)
        {
            Vector2 old = player.Position;
            player.Position = at;
            Vector2 target = player.CameraTarget;
            player.Position = old;
            return target;
        }

        // Port fiel do Level.NextLevel. NOTE: Displacement.Clear/seeds podados (conteudo).
        public void NextLevel(Vector2 at, Vector2 dir)
        {
            Engine.TimeRate = 1f;
            Rectangle? next = RoomAt(at);
            if (next != null)
                TransitionTo(next.Value, dir);
        }

        // Port fiel do Level.TransitionTo. NOTE: sync de CoreMode podado (conteudo).
        public void TransitionTo(Rectangle next, Vector2 direction)
        {
            transition = new Coroutine(TransitionRoutine(next, direction));
        }

        // Port fiel da mecanica de movimento do Level.TransitionRoutine do Celeste:
        // glide do player a 60px/s (Player.TransitionTo), camera CubeOut em NextTransitionDuration
        // (snap em cameraAt>0.9), OnTransition (refil dash/stamina) ao final.
        // NOTE: podas de conteudo — UnloadEntities/LoadLevel (mundo persistente compartilha a
        // cena entre salas), TransitionListener, Lighting, particulas, SoundSource, RespawnPoint.
        private IEnumerator TransitionRoutine(Rectangle next, Vector2 direction)
        {
            Player player = Tracker.GetEntity<Player>();
            player.CleanUpTriggers();
            PreviousBounds = Bounds;
            Bounds = next;

            float cameraAt = 0f;
            Vector2 cameraFrom = Camera.Position;
            Vector2 dirPad = direction * 4f;
            if (direction == Vector2.UnitY)
                dirPad = direction * 12f;

            Vector2 playerTo = player.Position;
            while (direction.X != 0f && playerTo.Y >= Bounds.Bottom)
                playerTo.Y -= 1f;
            while (!IsInBounds(playerTo, dirPad))
                playerTo += direction;

            Vector2 cameraTo = GetFullCameraTargetAt(player, playerTo);
            bool cameraFinished = false;
            while (!player.TransitionTo(playerTo, direction) || cameraAt < 1f)
            {
                yield return null;
                if (!cameraFinished)
                {
                    cameraAt = Calc.Approach(cameraAt, 1f, Engine.DeltaTime / NextTransitionDuration);
                    if (cameraAt > 0.9f)
                        Camera.Position = cameraTo;
                    else
                        Camera.Position = Vector2.Lerp(cameraFrom, cameraTo, Ease.CubeOut(cameraAt));
                    if (cameraAt >= 1f)
                        cameraFinished = true;
                }
            }

            // NOTE: Celeste usa Spawns.ClosestTo(pos) do level data; aqui o respawn da sala
            // e o ponto de chegada da transicao.
            Session.RespawnPoint = playerTo;

            player.OnTransition();
            NextTransitionDuration = 0.65f;
            transition = null;
        }

        // Port fiel do Level.EnforceBounds do Celeste: clamps H/V, camera-lock FinalBoss,
        // assist Invincible, morte no fundo e transicoes entre salas (via Rooms).
        // NOTE: podas de conteudo — branch do TheoCrystal e DisableDownTransition.
        public void EnforceBounds(Player player)
        {
            Rectangle bounds = Bounds;
            Rectangle camRect = new Rectangle((int)Camera.Left, (int)Camera.Top, 320, 180);

            if (transition != null)
                return;

            if (CameraLockMode == CameraLockModes.FinalBoss && player.Left < camRect.Left)
            {
                player.Left = camRect.Left;
                player.OnBoundsH();
            }
            else if (player.Left < bounds.Left)
            {
                if (player.Top >= bounds.Top && player.Bottom < bounds.Bottom
                    && CanTransitionTo(player.Center + Vector2.UnitX * -8f))
                {
                    player.BeforeSideTransition();
                    NextLevel(player.Center + Vector2.UnitX * -8f, -Vector2.UnitX);
                    return;
                }
                player.Left = bounds.Left;
                player.OnBoundsH();
            }

            if (CameraLockMode == CameraLockModes.FinalBoss && player.Right > camRect.Right && camRect.Right < bounds.Right - 4)
            {
                player.Right = camRect.Right;
                player.OnBoundsH();
            }
            else if (player.Right > bounds.Right)
            {
                if (player.Top >= bounds.Top && player.Bottom < bounds.Bottom
                    && CanTransitionTo(player.Center + Vector2.UnitX * 8f))
                {
                    player.BeforeSideTransition();
                    NextLevel(player.Center + Vector2.UnitX * 8f, Vector2.UnitX);
                    return;
                }
                player.Right = bounds.Right;
                player.OnBoundsH();
            }

            if (CameraLockMode != CameraLockModes.None && player.Top < camRect.Top)
            {
                player.Top = camRect.Top;
                player.OnBoundsV();
            }
            else if (player.CenterY < bounds.Top)
            {
                if (CanTransitionTo(player.Center - Vector2.UnitY * 12f))
                {
                    player.BeforeUpTransition();
                    NextLevel(player.Center - Vector2.UnitY * 12f, -Vector2.UnitY);
                    return;
                }
                if (player.Top < bounds.Top - 24)
                {
                    player.Top = bounds.Top - 24;
                    player.OnBoundsV();
                }
            }

            if (CameraLockMode == CameraLockModes.None || camRect.Bottom >= bounds.Bottom - 4 || player.Top <= camRect.Bottom)
            {
                if (player.Bottom > bounds.Bottom && CanTransitionTo(player.Center + Vector2.UnitY * 12f))
                {
                    if (!player.CollideCheck<Solid>(player.Position + Vector2.UnitY * 4f))
                    {
                        player.BeforeDownTransition();
                        NextLevel(player.Center + Vector2.UnitY * 12f, Vector2.UnitY);
                    }
                    return;
                }
                if (player.Top > bounds.Bottom && SaveData.Instance.Assists.Invincible)
                {
                    player.Play("event:/game/general/assist_screenbottom");
                    player.Bounce(bounds.Bottom);
                    return;
                }
                if (player.Top > bounds.Bottom + 4)
                    player.Die(Vector2.Zero);
                return;
            }

            if (SaveData.Instance.Assists.Invincible)
            {
                player.Play("event:/game/general/assist_screenbottom");
                player.Bounce(camRect.Bottom);
                return;
            }
            player.Die(Vector2.Zero);
        }
        public void StartCutscene(Action<Level> onSkip, bool fadeInOnSkip = true, bool endingChapterAfterCutscene = false, bool resetZoomOnSkip = false) { }
        public void EndCutscene() { }
        public void CancelCutscene() { }
        public void UnloadLevel() { }
        public void LoadLevel(Player.IntroTypes playerIntro, bool isFromLoader = false) { }
    }

    // NOTE: stub de displacement (visual). No-op.
    public class DisplacementRenderer
    {
        public Burst AddBurst(Vector2 position, float duration, float radiusFrom, float radiusTo, float alpha = 1f, object a = null, object b = null) => new Burst();

        public class Burst
        {
            public Collider WorldClipCollider;
            public float WorldClipPadding;
        }
    }

    // NOTE: stub de formation backdrop (visual). No-op.
    public class FormationBackdrop
    {
        public bool Display;
        public float Alpha;
    }
}
