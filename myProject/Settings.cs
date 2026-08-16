using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject
{
    // Bindings do jogo + opcoes lidas pelo Input/Player.
    // NOTE: video/audio/menu/idioma removidos. Os defaults NAO seguem mais o Celeste:
    // o esquema e o do jogo (Z pula, X ataca, C dash, ESC pausa | A, X, RT, Start).
    public class Settings
    {
        public static Settings Instance = new Settings();

        public Binding Left = new Binding();
        public Binding Right = new Binding();
        public Binding Up = new Binding();
        public Binding Down = new Binding();
        public Binding Jump = new Binding();
        public Binding Dash = new Binding();
        public Binding Grab = new Binding();
        public Binding Talk = new Binding();
        public Binding DemoDash = new Binding();
        public Binding Attack = new Binding();
        public Binding Pause = new Binding();

        public Binding LeftMoveOnly = new Binding();
        public Binding RightMoveOnly = new Binding();
        public Binding UpMoveOnly = new Binding();
        public Binding DownMoveOnly = new Binding();
        public Binding LeftDashOnly = new Binding();
        public Binding RightDashOnly = new Binding();
        public Binding UpDashOnly = new Binding();
        public Binding DownDashOnly = new Binding();

        public RumbleAmount Rumble = RumbleAmount.On;
        public GrabModes GrabMode;
        public CrouchDashModes CrouchDashMode;

        public Settings()
        {
            SetDefaultKeyboardControls();
            SetDefaultGamepadControls();
        }

        // Teclado: setas movem | Z pula | X ataca | C dash | ESC pausa.
        public void SetDefaultKeyboardControls()
        {
            Left.Add(new Keys[] { Keys.Left });
            Right.Add(new Keys[] { Keys.Right });
            Down.Add(new Keys[] { Keys.Down });
            Up.Add(new Keys[] { Keys.Up });
            Jump.Add(new Keys[] { Keys.Z });
            Attack.Add(new Keys[] { Keys.X });
            Dash.Add(new Keys[] { Keys.C });
            Pause.Add(new Keys[] { Keys.Escape });
            // NOTE: agarrar so serve p/ Holdable (a escalada esta podada); Z virou pulo
            Grab.Add(new Keys[] { Keys.V });
            Talk.Add(new Keys[] { Keys.E });
        }

        // Xbox: A pula | X ataca | RT dash | Start (3 tracos) pausa.
        // Direcoes no d-pad e no analogico esquerdo — sem elas o controle nao anda.
        public void SetDefaultGamepadControls()
        {
            Left.Add(new Buttons[] { Buttons.DPadLeft, Buttons.LeftThumbstickLeft });
            Right.Add(new Buttons[] { Buttons.DPadRight, Buttons.LeftThumbstickRight });
            Up.Add(new Buttons[] { Buttons.DPadUp, Buttons.LeftThumbstickUp });
            Down.Add(new Buttons[] { Buttons.DPadDown, Buttons.LeftThumbstickDown });
            Jump.Add(new Buttons[] { Buttons.A });
            Attack.Add(new Buttons[] { Buttons.X });
            Dash.Add(new Buttons[] { Buttons.RightTrigger });
            Pause.Add(new Buttons[] { Buttons.Start });
            Grab.Add(new Buttons[] { Buttons.LeftTrigger });
        }
    }
}
