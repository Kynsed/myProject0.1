using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do Booster do Celeste: collider Circle(10, 0, 2),
    // Depth -8500, PlayerCollider que joga o player no estado 4 (StBoost) via Player.Boost/
    // RedBoost, DashListener que solta o boost ao dashar, e a rotina que segura o player
    // enquanto ele estiver em StBoost/StRedDash — soltando com PlayerReleased ao sair.
    // Timers fieis: cannotUseTimer 0.45s, respawnTimer 1s.
    // NOTE: podas de conteudo — sprite/outline/bloom/luz/wiggler, sons, particulas e todo
    // o caso especial do Ch9HubBooster (LockBlock/backdrops do capitulo 9).
    [Tracked(false)]
    public class Booster : Entity
    {
        public static ParticleType P_Burst = new ParticleType();
        public static ParticleType P_BurstRed = new ParticleType();
        public static ParticleType P_Appear = new ParticleType();
        public static ParticleType P_RedAppear = new ParticleType();
        public static readonly Vector2 playerOffset = new Vector2(0f, -2f);

        public bool BoostingPlayer { get; private set; }
        public bool Ch9HubBooster;
        public bool Ch9HubTransition;

        private bool red;
        private float respawnTimer;
        private float cannotUseTimer;
        private Coroutine dashRoutine;
        private DashListener dashListener;

        public Booster(Vector2 position, bool red = false) : base(position)
        {
            Depth = -8500;
            Collider = new Circle(10f, 0f, 2f);
            this.red = red;
            Add(new PlayerCollider(OnPlayer, null, null));
            Add(dashRoutine = new Coroutine(false));
            Add(dashListener = new DashListener());
            dashListener.OnDash = OnPlayerDashed;
        }

        public override void Update()
        {
            base.Update();
            if (cannotUseTimer > 0f)
                cannotUseTimer -= Engine.DeltaTime;
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
            }
        }

        public void Respawn()
        {
            // NOTE: so o lado logico — visual/particulas do reaparecimento podados.
        }

        private void OnPlayer(Player player)
        {
            if (respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer)
            {
                cannotUseTimer = 0.45f;
                if (red)
                    player.RedBoost(this);
                else
                    player.Boost(this);
            }
        }

        public void PlayerBoosted(Player player, Vector2 direction)
        {
            BoostingPlayer = true;
            Tag = Tags.Persistent | Tags.TransitionUpdate;
            dashRoutine.Replace(BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(Player player, Vector2 dir)
        {
            // segura enquanto o player estiver dashando a partir do boost (2 = StDash,
            // 5 = StRedDash); ao sair, solta e espera a transicao antes de limpar a tag
            while ((player.StateMachine.State == 2 || player.StateMachine.State == 5) && BoostingPlayer)
                yield return null;

            PlayerReleased();
            while (SceneAs<Level>().Transitioning)
                yield return null;
            Tag = 0;
        }

        public void OnPlayerDashed(Vector2 direction)
        {
            if (BoostingPlayer)
                BoostingPlayer = false;
        }

        public void PlayerReleased()
        {
            cannotUseTimer = 0f;
            respawnTimer = 1f;
            BoostingPlayer = false;
        }

        public void PlayerDied()
        {
            if (BoostingPlayer)
            {
                PlayerReleased();
                dashRoutine.Active = false;
                Tag = 0;
            }
        }
    }
}
