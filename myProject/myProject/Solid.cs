using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    [Tracked(true)]
    public class Solid : Platform
    {
        public Vector2 Speed;
        public bool AllowStaticMovers = true;
        public bool EnableAssistModeChecks = true;
        public bool DisableLightsInside = true;
        public bool StopPlayerRunIntoAnimation = true;
        public bool SquishEvenInAssistMode;

        private static HashSet<Actor> riders = new HashSet<Actor>();

        public Solid(Vector2 position, float width, float height, bool safe)
            : base(position, safe)
        {
            Collider = new Hitbox(width, height, 0f, 0f);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            if (AllowStaticMovers)
            {
                bool wasCollidable = Collidable;
                Collidable = true;
                foreach (Component component in scene.Tracker.GetComponents<StaticMover>())
                {
                    StaticMover staticMover = (StaticMover)component;
                    if (staticMover.IsRiding(this) && staticMover.Platform == null)
                    {
                        staticMovers.Add(staticMover);
                        staticMover.Platform = this;
                        if (staticMover.OnAttach != null)
                            staticMover.OnAttach(this);
                    }
                }
                Collidable = wasCollidable;
            }
        }

        public override void Update()
        {
            base.Update();
            MoveH(Speed.X * Engine.DeltaTime);
            MoveV(Speed.Y * Engine.DeltaTime);

            // NOTE: bloco assist-mode removido. Dependia de SaveData.Assists.Invincible,
            // SolidOnInvinciblePlayer, TheoCrystal e Level — tudo conteudo Celeste, sem efeito
            // sobre a fisica de movimento. Reintroduzir ao portar o sistema de assists.
        }

        public bool HasRider()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                if (((Actor)entity).IsRiding(this))
                    return true;
            return false;
        }

        public Player GetPlayerRider()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Player>())
            {
                Player player = (Player)entity;
                if (player.IsRiding(this))
                    return player;
            }
            return null;
        }

        public bool HasPlayerRider()
        {
            return GetPlayerRider() != null;
        }

        public bool HasPlayerOnTop()
        {
            return GetPlayerOnTop() != null;
        }

        public Player GetPlayerOnTop()
        {
            return CollideFirst<Player>(Position - Vector2.UnitY);
        }

        public bool HasPlayerClimbing()
        {
            return GetPlayerClimbing() != null;
        }

        public Player GetPlayerClimbing()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Player>())
            {
                Player player = (Player)entity;
                if (player.StateMachine.State == 1)
                {
                    if (player.Facing == Facings.Left && CollideCheck(player, Position + Vector2.UnitX))
                        return player;
                    if (player.Facing == Facings.Right && CollideCheck(player, Position - Vector2.UnitX))
                        return player;
                }
            }
            return null;
        }

        public void GetRiders()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
            {
                Actor actor = (Actor)entity;
                if (actor.IsRiding(this))
                    riders.Add(actor);
            }
        }

        public override void MoveHExact(int move)
        {
            GetRiders();
            float right = Right;
            float left = Left;

            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move)
                && !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY))
                player.MoveV(1f, null, null);

            X += move;
            MoveStaticMovers(Vector2.UnitX * move);

            if (Collidable)
            {
                foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                {
                    Actor actor = (Actor)entity;
                    if (actor.AllowPushing)
                    {
                        bool wasCollidable = actor.Collidable;
                        actor.Collidable = true;

                        if (!actor.TreatNaive && CollideCheck(actor, Position))
                        {
                            int moveH;
                            if (move > 0)
                                moveH = move - (int)(actor.Left - right);
                            else
                                moveH = move - (int)(actor.Right - left);

                            Collidable = false;
                            actor.MoveHExact(moveH, actor.SquishCallback, this);
                            actor.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        }
                        else if (riders.Contains(actor))
                        {
                            Collidable = false;
                            if (actor.TreatNaive)
                                actor.NaiveMove(Vector2.UnitX * move);
                            else
                                actor.MoveHExact(move, null, null);
                            actor.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        }

                        actor.Collidable = wasCollidable;
                    }
                }
            }

            riders.Clear();
        }

        public override void MoveVExact(int move)
        {
            GetRiders();
            float bottom = Bottom;
            float top = Top;

            Y += move;
            MoveStaticMovers(Vector2.UnitY * move);

            if (Collidable)
            {
                foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                {
                    Actor actor = (Actor)entity;
                    if (actor.AllowPushing)
                    {
                        bool wasCollidable = actor.Collidable;
                        actor.Collidable = true;

                        if (!actor.TreatNaive && CollideCheck(actor, Position))
                        {
                            int moveV;
                            if (move > 0)
                                moveV = move - (int)(actor.Top - bottom);
                            else
                                moveV = move - (int)(actor.Bottom - top);

                            Collidable = false;
                            actor.MoveVExact(moveV, actor.SquishCallback, this);
                            actor.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        }
                        else if (riders.Contains(actor))
                        {
                            Collidable = false;
                            if (actor.TreatNaive)
                                actor.NaiveMove(Vector2.UnitY * move);
                            else
                                actor.MoveVExact(move, null, null);
                            actor.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        }

                        actor.Collidable = wasCollidable;
                    }
                }
            }

            riders.Clear();
        }
    }
}
