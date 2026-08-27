using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace myProject
{
    // Port fiel do subset de movimento do SwapBlock do Celeste: Solid safe (Depth -9999) que
    // corre de start ate node quando o player dasha (DashListener) e volta sozinho depois.
    // Velocidades fieis: maxForwardSpeed = 360/dist, maxBackwardSpeed = 40% disso; rampa de
    // 0.2s na ida e 1.5s na volta; returnTimer 0.8s; boost de saida via MoveTo com liftSpeed
    // (na ida usa sempre maxForwardSpeed, como no original); Swapping e StopPlayerRunIntoAnimation
    // conforme o lerp — os dois sao lidos pelo Player/Platform.
    // NOTE: podas de conteudo — nine-slice/sprites/tema Moon, LightOcclude, sons, displacement
    // burst e MoveParticles.
    [Tracked(false)]
    public class SwapBlock : Solid
    {
        public enum Themes { Normal, Moon }

        public static ParticleType P_Move = new ParticleType();

        public Themes Theme;
        public Vector2 Direction;
        public bool Swapping;

        private Vector2 start;
        private Vector2 end;
        private float lerp;
        private int target;
        private float speed;
        private float maxForwardSpeed;
        private float maxBackwardSpeed;
        private float returnTimer;
        private Rectangle moveRect;

        public SwapBlock(Vector2 position, float width, float height, Vector2 node, Themes theme = Themes.Normal)
            : base(position, width, height, false)
        {
            Theme = theme;
            start = Position;
            end = node;
            maxForwardSpeed = 360f / Vector2.Distance(start, end);
            maxBackwardSpeed = maxForwardSpeed * 0.4f;
            Direction.X = Math.Sign(end.X - start.X);
            Direction.Y = Math.Sign(end.Y - start.Y);
            Add(new DashListener { OnDash = OnDash });

            int left = (int)MathHelper.Min(X, node.X);
            int top = (int)MathHelper.Min(Y, node.Y);
            int right = (int)MathHelper.Max(X + Width, node.X + Width);
            int bottom = (int)MathHelper.Max(Y + Height, node.Y + Height);
            moveRect = new Rectangle(left, top, right - left, bottom - top);
            Depth = -9999;
        }

        private void OnDash(Vector2 direction)
        {
            Swapping = lerp < 1f;
            target = 1;
            returnTimer = 0.8f;
            if (lerp >= 0.2f)
                speed = maxForwardSpeed;
            else
                speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
        }

        public override void Update()
        {
            base.Update();
            if (returnTimer > 0f)
            {
                returnTimer -= Engine.DeltaTime;
                if (returnTimer <= 0f)
                {
                    target = 0;
                    speed = 0f;
                }
            }

            if (target == 1)
                speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
            else
                speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);

            float prevLerp = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp != prevLerp)
            {
                Vector2 liftSpeed = (end - start) * speed;
                if (target == 1)
                    liftSpeed = (end - start) * maxForwardSpeed;
                if (lerp < prevLerp)
                    liftSpeed *= -1f;
                MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
            }

            if (Swapping && lerp >= 1f)
                Swapping = false;
            StopPlayerRunIntoAnimation = lerp <= 0f || lerp >= 1f;
        }
    }
}
