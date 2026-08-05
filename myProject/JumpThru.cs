using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    [Tracked(true)]
    public class JumpThru : Platform
    {
        public JumpThru(Vector2 position, int width, bool safe)
            : base(position, safe)
        {
            Collider = new Hitbox(width, 5f, 0f, 0f);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
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
        }

        public bool HasRider()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                if (((Actor)entity).IsRiding(this))
                    return true;
            return false;
        }

        public bool HasPlayerRider()
        {
            foreach (Entity entity in Scene.Tracker.GetEntities<Player>())
                if (((Player)entity).IsRiding(this))
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

        public override void MoveHExact(int move)
        {
            if (Collidable)
            {
                foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                {
                    Actor actor = (Actor)entity;
                    if (actor.IsRiding(this))
                    {
                        if (actor.TreatNaive)
                            actor.NaiveMove(Vector2.UnitX * move);
                        else
                            actor.MoveHExact(move, null, null);
                    }
                }
            }
            X += move;
            MoveStaticMovers(Vector2.UnitX * move);
        }

        public override void MoveVExact(int move)
        {
            if (Collidable)
            {
                foreach (Entity entity in Scene.Tracker.GetEntities<Actor>())
                {
                    Actor actor = (Actor)entity;
                    if (actor.IsRiding(this))
                    {
                        // rider acompanha o movimento
                        Collidable = false;
                        if (actor.TreatNaive)
                            actor.NaiveMove(Vector2.UnitY * move);
                        else
                            actor.MoveVExact(move, null, null);
                        actor.LiftSpeed = LiftSpeed;
                        Collidable = true;
                    }
                    else if (move < 0 && !actor.TreatNaive
                             && CollideCheck(actor, Position + Vector2.UnitY * move) && !CollideCheck(actor))
                    {
                        // subindo: empurra ator que ficaria preso por dentro (goto IL_190 do decompilado)
                        Collidable = false;
                        actor.MoveVExact((int)(Top + move - actor.Bottom), null, null);
                        actor.LiftSpeed = LiftSpeed;
                        Collidable = true;
                    }
                }
            }
            Y += move;
            MoveStaticMovers(Vector2.UnitY * move);
        }
    }
}
