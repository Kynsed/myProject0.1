using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    [Tracked(true)]
    public class Actor : Entity
    {
        public Collision SquishCallback;
        public bool TreatNaive;
        public bool IgnoreJumpThrus;
        public bool AllowPushing = true;
        public float LiftSpeedGraceTime = 0.16f;

        private Vector2 movementCounter;
        private Vector2 currentLiftSpeed;
        private Vector2 lastLiftSpeed;
        private float liftSpeedTimer;

        public Actor(Vector2 position)
            : base(position)
        {
            SquishCallback = OnSquish;
        }

        protected virtual void OnSquish(CollisionData data)
        {
            if (!TrySquishWiggle(data, 3, 3))
                RemoveSelf();
        }

        protected bool TrySquishWiggle(CollisionData data, int wiggleX = 3, int wiggleY = 3)
        {
            data.Pusher.Collidable = true;

            // tenta deslocar a partir da posicao atual
            for (int x = 0; x <= wiggleX; x++)
            {
                for (int y = 0; y <= wiggleY; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        for (int signX = 1; signX >= -1; signX -= 2)
                        {
                            for (int signY = 1; signY >= -1; signY -= 2)
                            {
                                Vector2 offset = new Vector2(x * signX, y * signY);
                                if (!CollideCheck<Solid>(Position + offset))
                                {
                                    Position += offset;
                                    data.Pusher.Collidable = false;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            // tenta deslocar a partir da posicao alvo do empurrao
            for (int x = 0; x <= wiggleX; x++)
            {
                for (int y = 0; y <= wiggleY; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        for (int signX = 1; signX >= -1; signX -= 2)
                        {
                            for (int signY = 1; signY >= -1; signY -= 2)
                            {
                                Vector2 offset = new Vector2(x * signX, y * signY);
                                if (!CollideCheck<Solid>(data.TargetPosition + offset))
                                {
                                    Position = data.TargetPosition + offset;
                                    data.Pusher.Collidable = false;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            data.Pusher.Collidable = false;
            return false;
        }

        public virtual bool IsRiding(JumpThru jumpThru)
        {
            return !IgnoreJumpThrus && CollideCheckOutside(jumpThru, Position + Vector2.UnitY);
        }

        public virtual bool IsRiding(Solid solid)
        {
            return CollideCheck(solid, Position + Vector2.UnitY);
        }

        public bool OnGround(int downCheck = 1)
        {
            return CollideCheck<Solid>(Position + Vector2.UnitY * downCheck)
                || (!IgnoreJumpThrus && CollideCheckOutside<JumpThru>(Position + Vector2.UnitY * downCheck));
        }

        public bool OnGround(Vector2 at, int downCheck = 1)
        {
            Vector2 was = Position;
            Position = at;
            bool result = OnGround(downCheck);
            Position = was;
            return result;
        }

        public Vector2 ExactPosition
        {
            get { return Position + movementCounter; }
        }

        public Vector2 PositionRemainder
        {
            get { return movementCounter; }
        }

        public void ZeroRemainderX()
        {
            movementCounter.X = 0f;
        }

        public void ZeroRemainderY()
        {
            movementCounter.Y = 0f;
        }

        public override void Update()
        {
            base.Update();
            LiftSpeed = Vector2.Zero;
            if (liftSpeedTimer > 0f)
            {
                liftSpeedTimer -= Engine.DeltaTime;
                if (liftSpeedTimer <= 0f)
                    lastLiftSpeed = Vector2.Zero;
            }
        }

        public Vector2 LiftSpeed
        {
            get
            {
                if (currentLiftSpeed == Vector2.Zero)
                    return lastLiftSpeed;
                return currentLiftSpeed;
            }
            set
            {
                currentLiftSpeed = value;
                if (value != Vector2.Zero && LiftSpeedGraceTime > 0f)
                {
                    lastLiftSpeed = value;
                    liftSpeedTimer = LiftSpeedGraceTime;
                }
            }
        }

        public void ResetLiftSpeed()
        {
            currentLiftSpeed = lastLiftSpeed = Vector2.Zero;
            liftSpeedTimer = 0f;
        }

        public bool MoveH(float moveH, Collision onCollide = null, Solid pusher = null)
        {
            movementCounter.X += moveH;
            int move = (int)Math.Round((double)movementCounter.X, MidpointRounding.ToEven);
            if (move != 0)
            {
                movementCounter.X -= move;
                return MoveHExact(move, onCollide, pusher);
            }
            return false;
        }

        public bool MoveV(float moveV, Collision onCollide = null, Solid pusher = null)
        {
            movementCounter.Y += moveV;
            int move = (int)Math.Round((double)movementCounter.Y, MidpointRounding.ToEven);
            if (move != 0)
            {
                movementCounter.Y -= move;
                return MoveVExact(move, onCollide, pusher);
            }
            return false;
        }

        public bool MoveHExact(int moveH, Collision onCollide = null, Solid pusher = null)
        {
            Vector2 targetPosition = Position + Vector2.UnitX * moveH;
            int sign = Math.Sign(moveH);
            int moved = 0;

            while (moveH != 0)
            {
                Solid solid = CollideFirst<Solid>(Position + Vector2.UnitX * sign);
                if (solid != null)
                {
                    movementCounter.X = 0f;
                    if (onCollide != null)
                        onCollide(new CollisionData
                        {
                            Direction = Vector2.UnitX * sign,
                            Moved = Vector2.UnitX * moved,
                            TargetPosition = targetPosition,
                            Hit = solid,
                            Pusher = pusher
                        });
                    return true;
                }

                moved += sign;
                moveH -= sign;
                X += sign;
            }

            return false;
        }

        public bool MoveVExact(int moveV, Collision onCollide = null, Solid pusher = null)
        {
            Vector2 targetPosition = Position + Vector2.UnitY * moveV;
            int sign = Math.Sign(moveV);
            int moved = 0;

            while (moveV != 0)
            {
                Platform platform = CollideFirst<Solid>(Position + Vector2.UnitY * sign);
                if (platform != null)
                {
                    movementCounter.Y = 0f;
                    if (onCollide != null)
                        onCollide(new CollisionData
                        {
                            Direction = Vector2.UnitY * sign,
                            Moved = Vector2.UnitY * moved,
                            TargetPosition = targetPosition,
                            Hit = platform,
                            Pusher = pusher
                        });
                    return true;
                }

                if (moveV > 0 && !IgnoreJumpThrus)
                {
                    platform = CollideFirstOutside<JumpThru>(Position + Vector2.UnitY * sign);
                    if (platform != null)
                    {
                        movementCounter.Y = 0f;
                        if (onCollide != null)
                            onCollide(new CollisionData
                            {
                                Direction = Vector2.UnitY * sign,
                                Moved = Vector2.UnitY * moved,
                                TargetPosition = targetPosition,
                                Hit = platform,
                                Pusher = pusher
                            });
                        return true;
                    }
                }

                moved += sign;
                moveV -= sign;
                Y += sign;
            }

            return false;
        }

        public void MoveTowardsX(float targetX, float maxAmount, Collision onCollide = null)
        {
            float toX = Calc.Approach(ExactPosition.X, targetX, maxAmount);
            MoveToX(toX, onCollide);
        }

        public void MoveTowardsY(float targetY, float maxAmount, Collision onCollide = null)
        {
            float toY = Calc.Approach(ExactPosition.Y, targetY, maxAmount);
            MoveToY(toY, onCollide);
        }

        public void MoveToX(float toX, Collision onCollide = null)
        {
            MoveH(toX - ExactPosition.X, onCollide, null);
        }

        public void MoveToY(float toY, Collision onCollide = null)
        {
            MoveV(toY - ExactPosition.Y, onCollide, null);
        }

        public void NaiveMove(Vector2 amount)
        {
            movementCounter += amount;
            int moveX = (int)Math.Round((double)movementCounter.X);
            int moveY = (int)Math.Round((double)movementCounter.Y);
            Position += new Vector2(moveX, moveY);
            movementCounter -= new Vector2(moveX, moveY);
        }
    }
}
