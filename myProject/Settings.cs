using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject
{
    // NOTE: port podado. So os bindings de input de movimento + opcoes lidas pelo Input/Player.
    // Defaults de teclado iguais ao Celeste. Video/audio/menu/idioma removidos (conteudo).
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
        }

        public void SetDefaultKeyboardControls()
        {
            Left.Add(new Keys[] { Keys.Left });
            Right.Add(new Keys[] { Keys.Right });
            Down.Add(new Keys[] { Keys.Down });
            Up.Add(new Keys[] { Keys.Up });
            Grab.Add(new Keys[] { Keys.Z, Keys.V });
            Jump.Add(new Keys[] { Keys.C });
            Dash.Add(new Keys[] { Keys.X });
            Talk.Add(new Keys[] { Keys.X });
        }
    }
}
