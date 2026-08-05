using System;
using Microsoft.Xna.Framework;

namespace Monocle
{
    public class VirtualJoystick : VirtualInput
    {
        public Binding Up;
        public Binding Down;
        public Binding Left;
        public Binding Right;
        public Binding UpAlt;
        public Binding DownAlt;
        public Binding LeftAlt;
        public Binding RightAlt;
        public float Threshold;
        public int GamepadIndex;
        public OverlapBehaviors OverlapBehavior;
        public bool InvertedX;
        public bool InvertedY;

        private Vector2 value;
        private Vector2 previousValue;
        private bool hTurned;
        private bool vTurned;

        public Vector2 Value { get; private set; }
        public Vector2 PreviousValue { get; private set; }

        public VirtualJoystick(Binding up, Binding down, Binding left, Binding right, int gamepadIndex, float threshold, OverlapBehaviors overlapBehavior = OverlapBehaviors.TakeNewer)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
            GamepadIndex = gamepadIndex;
            Threshold = threshold;
            OverlapBehavior = overlapBehavior;
        }

        public VirtualJoystick(Binding up, Binding upAlt, Binding down, Binding downAlt, Binding left, Binding leftAlt, Binding right, Binding rightAlt, int gamepadIndex, float threshold, OverlapBehaviors overlapBehavior = OverlapBehaviors.TakeNewer)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
            UpAlt = upAlt;
            DownAlt = downAlt;
            LeftAlt = leftAlt;
            RightAlt = rightAlt;
            GamepadIndex = gamepadIndex;
            Threshold = threshold;
            OverlapBehavior = overlapBehavior;
        }

        public override void Update()
        {
            previousValue = value;

            if (!MInput.Disabled)
            {
                Vector2 next = value;

                float right = Right.Axis(GamepadIndex, 0);
                float left = Left.Axis(GamepadIndex, 0);
                float down = Down.Axis(GamepadIndex, 0);
                float up = Up.Axis(GamepadIndex, 0);

                if (right == 0 && RightAlt != null)
                    right = RightAlt.Axis(GamepadIndex, 0);
                if (left == 0 && LeftAlt != null)
                    left = LeftAlt.Axis(GamepadIndex, 0);
                if (down == 0 && DownAlt != null)
                    down = DownAlt.Axis(GamepadIndex, 0);
                if (up == 0 && UpAlt != null)
                    up = UpAlt.Axis(GamepadIndex, 0);

                // cancel out the smaller of opposing directions
                if (right > left)
                    left = 0;
                else if (left > right)
                    right = 0;
                if (down > up)
                    up = 0;
                else if (up > down)
                    down = 0;

                // horizontal
                if (right != 0 && left != 0)
                {
                    switch (OverlapBehavior)
                    {
                        case OverlapBehaviors.CancelOut:
                            next.X = 0;
                            break;

                        case OverlapBehaviors.TakeOlder:
                            if (next.X > 0)
                                next.X = right;
                            else if (next.X < 0)
                                next.X = left;
                            break;

                        case OverlapBehaviors.TakeNewer:
                            if (!hTurned)
                            {
                                if (next.X > 0)
                                    next.X = -left;
                                else if (next.X < 0)
                                    next.X = right;
                                hTurned = true;
                            }
                            else if (next.X > 0)
                                next.X = right;
                            else if (next.X < 0)
                                next.X = -left;
                            break;
                    }
                }
                else if (right != 0)
                {
                    hTurned = false;
                    next.X = right;
                }
                else if (left != 0)
                {
                    hTurned = false;
                    next.X = -left;
                }
                else
                {
                    hTurned = false;
                    next.X = 0;
                }

                // vertical
                if (down != 0 && up != 0)
                {
                    switch (OverlapBehavior)
                    {
                        case OverlapBehaviors.CancelOut:
                            next.Y = 0;
                            break;

                        case OverlapBehaviors.TakeOlder:
                            if (next.Y > 0)
                                next.Y = down;
                            else if (next.Y < 0)
                                next.Y = -up;
                            break;

                        case OverlapBehaviors.TakeNewer:
                            if (!vTurned)
                            {
                                if (next.Y > 0)
                                    next.Y = -up;
                                else if (next.Y < 0)
                                    next.Y = down;
                                vTurned = true;
                            }
                            else if (next.Y > 0)
                                next.Y = down;
                            else if (next.Y < 0)
                                next.Y = -up;
                            break;
                    }
                }
                else if (down != 0)
                {
                    vTurned = false;
                    next.Y = down;
                }
                else if (up != 0)
                {
                    vTurned = false;
                    next.Y = -up;
                }
                else
                {
                    vTurned = false;
                    next.Y = 0;
                }

                if (next.Length() < Threshold)
                    next = Vector2.Zero;

                value = next;
            }

            Value = new Vector2(InvertedX ? -value.X : value.X, InvertedY ? -value.Y : value.Y);
            PreviousValue = new Vector2(InvertedX ? -previousValue.X : previousValue.X, InvertedY ? -previousValue.Y : previousValue.Y);
        }

        public static implicit operator Vector2(VirtualJoystick joystick)
        {
            return joystick.Value;
        }
    }
}
