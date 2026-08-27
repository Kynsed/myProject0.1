using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    [Tracked(true)]
    public abstract class Platform : Entity
    {
        public Vector2 LiftSpeed;
        public bool Safe;
        public bool BlockWaterfalls = true;
        public int SurfaceSoundIndex = 8;
        public int SurfaceSoundPriority;
        public DashCollision OnDashCollide;
        public Action<Vector2> OnCollide;

        private Vector2 movementCounter;
        private Vector2 shakeAmount;
        private bool shaking;
        private float shakeTimer;
        protected List<StaticMover> staticMovers = new List<StaticMover>();

        public Platform(Vector2 position, bool safe)
            : base(position)
        {
            Safe = safe;
            Depth = -9000;
        }

        public Vector2 Shake
        {
            get { return shakeAmount; }
        }

        public Hitbox Hitbox
        {
            get { return Collider as Hitbox; }
        }

        public Vector2 ExactPosition
        {
            get { return Position + movementCounter; }
        }

        public void ClearRemainder()
        {
            movementCounter = Vector2.Zero;
        }

        public override void Update()
        {
            base.Update();
            LiftSpeed = Vector2.Zero;
            if (shaking)
            {
                if (Scene.OnInterval(0.04f))
                {
                    Vector2 was = shakeAmount;
                    shakeAmount = Calc.Random.ShakeVector();
                    OnShake(shakeAmount - was);
                }
                if (shakeTimer > 0f)
                {
                    shakeTimer -= Engine.DeltaTime;
                    if (shakeTimer <= 0f)
                    {
                        shaking = false;
                        StopShaking();
                    }
                }
            }
        }

        public void StartShaking(float time = 0f)
        {
            shaking = true;
            shakeTimer = time;
        }

        public void StopShaking()
        {
            shaking = false;
            if (shakeAmount != Vector2.Zero)
            {
                OnShake(-shakeAmount);
                shakeAmount = Vector2.Zero;
            }
        }

        public virtual void OnShake(Vector2 amount)
        {
            ShakeStaticMovers(amount);
        }

        public void ShakeStaticMovers(Vector2 amount)
        {
            foreach (StaticMover staticMover in staticMovers)
                staticMover.Shake(amount);
        }

        public void MoveStaticMovers(Vector2 amount)
        {
            foreach (StaticMover staticMover in staticMovers)
                staticMover.Move(amount);
        }

        public void DestroyStaticMovers()
        {
            foreach (StaticMover staticMover in staticMovers)
                staticMover.Destroy();
            staticMovers.Clear();
        }

        public void DisableStaticMovers()
        {
            foreach (StaticMover staticMover in staticMovers)
                staticMover.Disable();
        }

        public void EnableStaticMovers()
        {
            foreach (StaticMover staticMover in staticMovers)
                staticMover.Enable();
        }

        public virtual void OnStaticMoverTrigger(StaticMover sm)
        {
        }

        public virtual int GetLandSoundIndex(Entity entity)
        {
            return SurfaceSoundIndex;
        }

        public virtual int GetWallSoundIndex(Player player, int side)
        {
            return SurfaceSoundIndex;
        }

        public virtual int GetStepSoundIndex(Entity entity)
        {
            return SurfaceSoundIndex;
        }

        public void MoveH(float moveH)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.X = 0f;
            else
                LiftSpeed.X = moveH / Engine.DeltaTime;

            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X);
            if (move != 0)
            {
                movementCounter.X -= move;
                MoveHExact(move);
            }
        }

        public void MoveH(float moveH, float liftSpeedH)
        {
            LiftSpeed.X = liftSpeedH;
            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X);
            if (move != 0)
            {
                movementCounter.X -= move;
                MoveHExact(move);
            }
        }

        public void MoveV(float moveV)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.Y = 0f;
            else
                LiftSpeed.Y = moveV / Engine.DeltaTime;

            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y);
            if (move != 0)
            {
                movementCounter.Y -= move;
                MoveVExact(move);
            }
        }

        public void MoveV(float moveV, float liftSpeedV)
        {
            LiftSpeed.Y = liftSpeedV;
            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y);
            if (move != 0)
            {
                movementCounter.Y -= move;
                MoveVExact(move);
            }
        }

        public void MoveToX(float x)
        {
            MoveH(x - ExactPosition.X);
        }

        public void MoveToX(float x, float liftSpeedX)
        {
            MoveH(x - ExactPosition.X, liftSpeedX);
        }

        public void MoveToY(float y)
        {
            MoveV(y - ExactPosition.Y);
        }

        public void MoveToY(float y, float liftSpeedY)
        {
            MoveV(y - ExactPosition.Y, liftSpeedY);
        }

        public void MoveTo(Vector2 position)
        {
            MoveToX(position.X);
            MoveToY(position.Y);
        }

        public void MoveTo(Vector2 position, Vector2 liftSpeed)
        {
            MoveToX(position.X, liftSpeed.X);
            MoveToY(position.Y, liftSpeed.Y);
        }

        public void MoveTowardsX(float x, float amount)
        {
            MoveToX(Calc.Approach(ExactPosition.X, x, amount));
        }

        public void MoveTowardsY(float y, float amount)
        {
            MoveToY(Calc.Approach(ExactPosition.Y, y, amount));
        }

        public abstract void MoveHExact(int move);

        public abstract void MoveVExact(int move);

        public void MoveToNaive(Vector2 position)
        {
            MoveToXNaive(position.X);
            MoveToYNaive(position.Y);
        }

        public void MoveToXNaive(float x)
        {
            MoveHNaive(x - ExactPosition.X);
        }

        public void MoveToYNaive(float y)
        {
            MoveVNaive(y - ExactPosition.Y);
        }

        public void MoveHNaive(float moveH)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.X = 0f;
            else
                LiftSpeed.X = moveH / Engine.DeltaTime;

            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X);
            if (move != 0)
            {
                movementCounter.X -= move;
                X += move;
                MoveStaticMovers(Vector2.UnitX * move);
            }
        }

        public void MoveVNaive(float moveV)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.Y = 0f;
            else
                LiftSpeed.Y = moveV / Engine.DeltaTime;

            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y);
            if (move != 0)
            {
                movementCounter.Y -= move;
                Y += move;
                MoveStaticMovers(Vector2.UnitY * move);
            }
        }

        public bool MoveHCollideSolids(float moveH, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.X = 0f;
            else
                LiftSpeed.X = moveH / Engine.DeltaTime;

            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X);
            if (move != 0)
            {
                movementCounter.X -= move;
                return MoveHExactCollideSolids(move, thruDashBlocks, onCollide);
            }
            return false;
        }

        public bool MoveVCollideSolids(float moveV, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.Y = 0f;
            else
                LiftSpeed.Y = moveV / Engine.DeltaTime;

            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y);
            if (move != 0)
            {
                movementCounter.Y -= move;
                return MoveVExactCollideSolids(move, thruDashBlocks, onCollide);
            }
            return false;
        }

        public bool MoveHCollideSolidsAndBounds(Level level, float moveH, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.X = 0f;
            else
                LiftSpeed.X = moveH / Engine.DeltaTime;

            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X);
            if (move != 0)
            {
                movementCounter.X -= move;
                bool hitBounds;
                if (Left + move < level.Bounds.Left)
                {
                    hitBounds = true;
                    move = level.Bounds.Left - (int)Left;
                }
                else if (Right + move > level.Bounds.Right)
                {
                    hitBounds = true;
                    move = level.Bounds.Right - (int)Right;
                }
                else
                    hitBounds = false;

                return MoveHExactCollideSolids(move, thruDashBlocks, onCollide) || hitBounds;
            }
            return false;
        }

        public bool MoveVCollideSolidsAndBounds(Level level, float moveV, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null, bool checkBottom = true)
        {
            if (Engine.DeltaTime == 0f)
                LiftSpeed.Y = 0f;
            else
                LiftSpeed.Y = moveV / Engine.DeltaTime;

            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y);
            if (move != 0)
            {
                movementCounter.Y -= move;
                int bottomLimit = level.Bounds.Bottom + 32;
                bool hitBounds;
                if (Top + move < level.Bounds.Top)
                {
                    hitBounds = true;
                    move = level.Bounds.Top - (int)Top;
                }
                else if (checkBottom && Bottom + move > bottomLimit)
                {
                    hitBounds = true;
                    move = bottomLimit - (int)Bottom;
                }
                else
                    hitBounds = false;

                return MoveVExactCollideSolids(move, thruDashBlocks, onCollide) || hitBounds;
            }
            return false;
        }

        public bool MoveHExactCollideSolids(int moveH, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null)
        {
            float startX = X;
            int sign = Math.Sign(moveH);
            int moved = 0;
            Solid hit = null;

            while (moveH != 0)
            {
                // NOTE: bloco thruDashBlocks removido. DashBlock/Input.Rumble/Level.Shake sao
                // conteudo Celeste e nao afetam a fisica de movimento. Parametro mantido por
                // fidelidade de assinatura; reintroduzir o bloco ao portar DashBlock.
                hit = CollideFirst<Solid>(Position + Vector2.UnitX * sign);
                if (hit != null)
                    break;

                moved += sign;
                moveH -= sign;
                X += sign;
            }

            X = startX;
            MoveHExact(moved);
            if (hit != null && onCollide != null)
                onCollide(Vector2.UnitX * sign, Vector2.UnitX * moved, hit);

            return hit != null;
        }

        public bool MoveVExactCollideSolids(int moveV, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide = null)
        {
            float startY = Y;
            int sign = Math.Sign(moveV);
            int moved = 0;
            Platform hit = null;

            while (moveV != 0)
            {
                // NOTE: bloco thruDashBlocks removido (conteudo Celeste, ver MoveHExactCollideSolids).
                hit = CollideFirst<Solid>(Position + Vector2.UnitY * sign);
                if (hit != null)
                    break;

                if (moveV > 0)
                {
                    hit = CollideFirstOutside<JumpThru>(Position + Vector2.UnitY * sign);
                    if (hit != null)
                        break;
                }

                moved += sign;
                moveV -= sign;
                Y += sign;
            }

            Y = startY;
            MoveVExact(moved);
            if (hit != null && onCollide != null)
                onCollide(Vector2.UnitY * sign, Vector2.UnitY * moved, hit);

            return hit != null;
        }
    }
}
