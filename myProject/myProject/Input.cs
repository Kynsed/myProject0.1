using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject
{
    // Port fiel do subsistema de input de MOVIMENTO (buffers/deadzones/timings preservados).
    // NOTE: GUI/menu/montanha/prefixos de botao podados (conteudo, sem efeito no movimento).
    public static class Input
    {
        public static int Gamepad
        {
            get { return gamepad; }
            set
            {
                int num = Calc.Clamp(value, 0, MInput.GamePads.Length - 1);
                if (gamepad != num)
                {
                    gamepad = num;
                    Initialize();
                }
            }
        }

        public static void Initialize()
        {
            bool flag = false;
            if (MoveX != null)
                flag = MoveX.Inverted;
            Deregister();
            MoveX = new VirtualIntegerAxis(Settings.Instance.Left, Settings.Instance.LeftMoveOnly, Settings.Instance.Right, Settings.Instance.RightMoveOnly, Gamepad, 0.3f, VirtualInput.OverlapBehaviors.TakeNewer);
            MoveX.Inverted = flag;
            MoveY = new VirtualIntegerAxis(Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Gamepad, 0.7f, VirtualInput.OverlapBehaviors.TakeNewer);
            GliderMoveY = new VirtualIntegerAxis(Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Gamepad, 0.3f, VirtualInput.OverlapBehaviors.TakeNewer);
            Aim = new VirtualJoystick(Settings.Instance.Up, Settings.Instance.UpDashOnly, Settings.Instance.Down, Settings.Instance.DownDashOnly, Settings.Instance.Left, Settings.Instance.LeftDashOnly, Settings.Instance.Right, Settings.Instance.RightDashOnly, Gamepad, 0.25f, VirtualInput.OverlapBehaviors.TakeNewer);
            Aim.InvertedX = flag;
            Feather = new VirtualJoystick(Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Settings.Instance.Left, Settings.Instance.LeftMoveOnly, Settings.Instance.Right, Settings.Instance.RightMoveOnly, Gamepad, 0.25f, VirtualInput.OverlapBehaviors.TakeNewer);
            Feather.InvertedX = flag;
            Jump = new VirtualButton(Settings.Instance.Jump, Gamepad, 0.08f, 0.2f);
            Dash = new VirtualButton(Settings.Instance.Dash, Gamepad, 0.08f, 0.2f);
            Talk = new VirtualButton(Settings.Instance.Talk, Gamepad, 0.08f, 0.2f);
            Grab = new VirtualButton(Settings.Instance.Grab, Gamepad, 0f, 0.2f);
            CrouchDash = new VirtualButton(Settings.Instance.DemoDash, Gamepad, 0.08f, 0.2f);
        }

        public static void Deregister()
        {
            Aim?.Deregister();
            Jump?.Deregister();
            Dash?.Deregister();
            Grab?.Deregister();
            Talk?.Deregister();
            CrouchDash?.Deregister();
            MoveX?.Deregister();
            MoveY?.Deregister();
            GliderMoveY?.Deregister();
            Feather?.Deregister();
        }

        public static void Rumble(RumbleStrength strength, RumbleLength length)
        {
            float num = 1f;
            if (Settings.Instance.Rumble == RumbleAmount.Half)
                num = 0.5f;
            if (Settings.Instance.Rumble != RumbleAmount.Off && MInput.GamePads.Length != 0 && !MInput.Disabled)
                MInput.GamePads[Gamepad].Rumble(rumbleStrengths[(int)strength] * num, rumbleLengths[(int)length]);
        }

        public static void RumbleSpecific(float strength, float time)
        {
            float num = 1f;
            if (Settings.Instance.Rumble == RumbleAmount.Half)
                num = 0.5f;
            if (Settings.Instance.Rumble != RumbleAmount.Off && MInput.GamePads.Length != 0 && !MInput.Disabled)
                MInput.GamePads[Gamepad].Rumble(strength * num, time);
        }

        public static bool GrabCheck
        {
            get
            {
                switch (Settings.Instance.GrabMode)
                {
                    default:
                        return Grab.Check;
                    case GrabModes.Invert:
                        return !Grab.Check;
                    case GrabModes.Toggle:
                        return grabToggle;
                }
            }
        }

        public static bool DashPressed
        {
            get
            {
                if (Settings.Instance.CrouchDashMode != CrouchDashModes.Hold)
                    return Dash.Pressed;
                return Dash.Pressed && !CrouchDash.Check;
            }
        }

        public static bool CrouchDashPressed
        {
            get
            {
                if (Settings.Instance.CrouchDashMode != CrouchDashModes.Hold)
                    return CrouchDash.Pressed;
                return Dash.Pressed && CrouchDash.Check;
            }
        }

        public static void UpdateGrab()
        {
            if (Settings.Instance.GrabMode == GrabModes.Toggle && Grab.Pressed)
                grabToggle = !grabToggle;
        }

        public static void ResetGrab()
        {
            grabToggle = false;
        }

        public static Vector2 GetAimVector(Facings defaultFacing = Facings.Right)
        {
            Vector2 value = Aim.Value;
            if (value == Vector2.Zero)
            {
                if (SaveData.Instance != null && SaveData.Instance.Assists.DashAssist)
                    return LastAim;
                LastAim = Vector2.UnitX * (float)defaultFacing;
            }
            else if (SaveData.Instance != null && SaveData.Instance.Assists.ThreeSixtyDashing)
            {
                LastAim = value.SafeNormalize();
            }
            else
            {
                float num = value.Angle();
                int num2 = (num < 0f) ? 1 : 0;
                float num3 = 0.3926991f - (float)num2 * 0.08726646f;
                if (Calc.AbsAngleDiff(num, 0f) < num3)
                    LastAim = new Vector2(1f, 0f);
                else if (Calc.AbsAngleDiff(num, 3.1415927f) < num3)
                    LastAim = new Vector2(-1f, 0f);
                else if (Calc.AbsAngleDiff(num, -1.5707964f) < num3)
                    LastAim = new Vector2(0f, -1f);
                else if (Calc.AbsAngleDiff(num, 1.5707964f) < num3)
                    LastAim = new Vector2(0f, 1f);
                else
                    LastAim = new Vector2(Math.Sign(value.X), Math.Sign(value.Y)).SafeNormalize();
            }
            return LastAim;
        }

        private static int gamepad = 0;

        public static VirtualIntegerAxis MoveX;
        public static VirtualIntegerAxis MoveY;
        public static VirtualIntegerAxis GliderMoveY;
        public static VirtualJoystick Aim;
        public static VirtualJoystick Feather;
        public static VirtualButton Jump;
        public static VirtualButton Dash;
        public static VirtualButton Grab;
        public static VirtualButton Talk;
        public static VirtualButton CrouchDash;

        private static bool grabToggle;
        public static Vector2 LastAim;

        private static float[] rumbleStrengths = new float[] { 0.15f, 0.4f, 1f, 0.05f };
        private static float[] rumbleLengths = new float[] { 0.1f, 0.25f, 0.5f, 1f, 2f };
    }
}
