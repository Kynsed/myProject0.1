using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject
{
    // Port do subsistema de input do Celeste: buffers, deadzones e thresholds preservados.
    // Movimento (MoveX/MoveY/Aim/Feather/Jump/Dash/Grab/Talk/CrouchDash) e os controles de
    // sistema e menu (Pause/ESC/QuickRestart/Menu*) com os mesmos valores do original.
    //
    // NOTE: podas — MountainAim (cena da montanha, que nao existe aqui) e os prefixos de
    // icone de botao (GuiInputPrefix e afins, que sao conteudo de UI).
    //
    // NOTE: jogo proprio — UpdateGamepadSlot (escolha automatica do slot do controle).
    // O Celeste faz essa escolha na tela de titulo, que nao existe aqui.
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

            // Sistema. ESC e binding fixo no Celeste (nao sai do Settings), so teclado.
            Binding escBinding = new Binding();
            escBinding.Add(Keys.Escape);
            ESC = new VirtualButton(escBinding, Gamepad, 0.1f, 0.2f);
            Pause = new VirtualButton(Settings.Instance.Pause, Gamepad, 0.1f, 0.2f);
            QuickRestart = new VirtualButton(Settings.Instance.QuickRestart, Gamepad, 0.1f, 0.2f);

            // Menu: buffer 0, threshold 0.4 e repeticao 0.4/0.1 nos direcionais.
            MenuLeft = new VirtualButton(Settings.Instance.MenuLeft, Gamepad, 0f, 0.4f);
            MenuLeft.SetRepeat(0.4f, 0.1f);
            MenuRight = new VirtualButton(Settings.Instance.MenuRight, Gamepad, 0f, 0.4f);
            MenuRight.SetRepeat(0.4f, 0.1f);
            MenuUp = new VirtualButton(Settings.Instance.MenuUp, Gamepad, 0f, 0.4f);
            MenuUp.SetRepeat(0.4f, 0.1f);
            MenuDown = new VirtualButton(Settings.Instance.MenuDown, Gamepad, 0f, 0.4f);
            MenuDown.SetRepeat(0.4f, 0.1f);
            MenuJournal = new VirtualButton(Settings.Instance.Journal, Gamepad, 0f, 0.2f);
            MenuConfirm = new VirtualButton(Settings.Instance.Confirm, Gamepad, 0f, 0.2f);
            MenuCancel = new VirtualButton(Settings.Instance.Cancel, Gamepad, 0f, 0.2f);
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
            ESC?.Deregister();
            Pause?.Deregister();
            QuickRestart?.Deregister();
            MenuLeft?.Deregister();
            MenuRight?.Deregister();
            MenuUp?.Deregister();
            MenuDown?.Deregister();
            MenuJournal?.Deregister();
            MenuConfirm?.Deregister();
            MenuCancel?.Deregister();
        }

        // --- escolha do slot do controle ---
        //
        // NOTE: jogo proprio. No Celeste quem define Input.Gamepad e a tela de titulo (o
        // controle que apertar Confirm). Sem tela de titulo aqui, a regra e "o ultimo
        // controle usado manda": um aperto em qualquer outro slot faz dele o slot ativo.
        // O teclado nao entra nisso — ele vale sempre, com qualquer slot.
        //
        // So BORDAS contam (botao que desceu, gatilho ou analogico que cruzou o limiar),
        // nunca o nivel: um dispositivo com eixo preso longe do centro nao rouba o slot
        // nem impede a troca. E a borda precisa de duas amostras conectadas, entao o
        // frame em que o controle conecta (ou em que a janela volta ao foco) nao conta.
        public const float SlotTriggerThreshold = 0.2f;   // o mesmo das acoes (Jump/Dash/Grab)
        public const float SlotStickThreshold = 0.3f;     // o mesmo do MoveX

        private static readonly Buttons[] slotButtons = new Buttons[]
        {
            Buttons.A, Buttons.B, Buttons.X, Buttons.Y, Buttons.Start, Buttons.Back,
            Buttons.LeftShoulder, Buttons.RightShoulder, Buttons.LeftStick, Buttons.RightStick,
            Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight
        };

        // Chamar uma vez por frame, depois do MInput.Update e antes de ler qualquer input.
        // Devolve true no frame em que o slot ativo mudou.
        public static bool UpdateGamepadSlot()
        {
            // Empate no mesmo frame: quem ja esta jogando fica com o controle.
            if (JustUsed(MInput.GamePads[gamepad]))
                return false;

            for (int i = 0; i < MInput.GamePads.Length; i++)
            {
                if (i == gamepad || !JustUsed(MInput.GamePads[i]))
                    continue;

                Gamepad = i;   // reconstroi os virtuais apontando p/ o novo slot

                // Os virtuais acabaram de nascer zerados. Atualizar agora faz o frame da
                // troca valer como um frame normal: a direcao segurada ja move e o aperto
                // que causou a troca entra no buffer em vez de se perder.
                foreach (VirtualInput virtualInput in MInput.VirtualInputs)
                    virtualInput.Update();
                return true;
            }

            return false;
        }

        private static bool JustUsed(MInput.GamePadData pad)
        {
            GamePadState now = pad.CurrentState;
            GamePadState was = pad.PreviousState;
            if (!now.IsConnected || !was.IsConnected)
                return false;

            foreach (Buttons button in slotButtons)
                if (now.IsButtonDown(button) && was.IsButtonUp(button))
                    return true;

            return Crossed(now.Triggers.Left, was.Triggers.Left, SlotTriggerThreshold)
                || Crossed(now.Triggers.Right, was.Triggers.Right, SlotTriggerThreshold)
                || Crossed(now.ThumbSticks.Left.Length(), was.ThumbSticks.Left.Length(), SlotStickThreshold)
                || Crossed(now.ThumbSticks.Right.Length(), was.ThumbSticks.Right.Length(), SlotStickThreshold);
        }

        private static bool Crossed(float now, float was, float threshold)
        {
            return now >= threshold && was < threshold;
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

        public static VirtualButton ESC;
        public static VirtualButton Pause;
        public static VirtualButton QuickRestart;
        public static VirtualButton MenuLeft;
        public static VirtualButton MenuRight;
        public static VirtualButton MenuUp;
        public static VirtualButton MenuDown;
        public static VirtualButton MenuJournal;
        public static VirtualButton MenuConfirm;
        public static VirtualButton MenuCancel;

        private static bool grabToggle;
        public static Vector2 LastAim;

        private static float[] rumbleStrengths = new float[] { 0.15f, 0.4f, 1f, 0.05f };
        private static float[] rumbleLengths = new float[] { 0.1f, 0.25f, 0.5f, 1f, 2f };
    }
}
