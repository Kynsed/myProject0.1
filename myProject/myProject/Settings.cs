using Microsoft.Xna.Framework.Input;
using Monocle;

namespace myProject
{
    // Port do subsistema de bindings do Settings do Celeste: os 26 Binding e os defaults
    // de teclado e de controle, com a mesma semantica de "so preenche se estiver vazio"
    // (o parametro reset forca a redefinicao).
    //
    // NOTE: poda de conteudo. O Settings do Celeste tambem guarda video, volume, idioma,
    // acessibilidade, opcoes de menu e o serializador do arquivo de save — nada disso
    // existe aqui ainda. Ficaram os bindings e as tres opcoes que o Input/Player leem
    // (Rumble, GrabMode, CrouchDashMode).
    //
    // NOTE: os defaults de Grab NAO sao recuperaveis do decompilado — estao como blob
    // estatico (RuntimeHelpers.InitializeArray sobre <PrivateImplementationDetails>), e o
    // decompilador so revela os tamanhos: 3 teclas e 4 botoes. Os valores abaixo sao a
    // melhor reconstrucao, nao o original.
    public class Settings
    {
        public static Settings Instance = new Settings();

        // --- movimento ---
        public Binding Left = new Binding();
        public Binding Right = new Binding();
        public Binding Down = new Binding();
        public Binding Up = new Binding();

        // --- acoes ---
        public Binding Grab = new Binding();
        public Binding Jump = new Binding();
        public Binding Dash = new Binding();
        public Binding Talk = new Binding();
        public Binding DemoDash = new Binding();

        // --- menu / sistema ---
        public Binding Pause = new Binding();
        public Binding Confirm = new Binding();
        public Binding Cancel = new Binding();
        public Binding Journal = new Binding();
        public Binding QuickRestart = new Binding();
        public Binding MenuLeft = new Binding();
        public Binding MenuRight = new Binding();
        public Binding MenuDown = new Binding();
        public Binding MenuUp = new Binding();

        // --- desambiguacao de eixo (movimento vs mira do dash) ---
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
            SetDefaultKeyboardControls(false);
            SetDefaultButtonControls(false);
        }

        public void SetDefaultKeyboardControls(bool reset)
        {
            if (reset || Left.Keyboard.Count <= 0)
            {
                Left.Keyboard.Clear();
                Left.Add(Keys.Left);
            }
            if (reset || Right.Keyboard.Count <= 0)
            {
                Right.Keyboard.Clear();
                Right.Add(Keys.Right);
            }
            if (reset || Down.Keyboard.Count <= 0)
            {
                Down.Keyboard.Clear();
                Down.Add(Keys.Down);
            }
            if (reset || Up.Keyboard.Count <= 0)
            {
                Up.Keyboard.Clear();
                Up.Add(Keys.Up);
            }
            if (reset || MenuLeft.Keyboard.Count <= 0)
            {
                MenuLeft.Keyboard.Clear();
                MenuLeft.Add(Keys.Left);
            }
            if (reset || MenuRight.Keyboard.Count <= 0)
            {
                MenuRight.Keyboard.Clear();
                MenuRight.Add(Keys.Right);
            }
            if (reset || MenuDown.Keyboard.Count <= 0)
            {
                MenuDown.Keyboard.Clear();
                MenuDown.Add(Keys.Down);
            }
            if (reset || MenuUp.Keyboard.Count <= 0)
            {
                MenuUp.Keyboard.Clear();
                MenuUp.Add(Keys.Up);
            }
            if (reset || Grab.Keyboard.Count <= 0)
            {
                // NOTE: 3 teclas no original, valores nao recuperaveis (ver cabecalho).
                Grab.Keyboard.Clear();
                Grab.Add(Keys.Z, Keys.V, Keys.LeftShift);
            }
            if (reset || Jump.Keyboard.Count <= 0)
            {
                Jump.Keyboard.Clear();
                Jump.Add(Keys.C);
            }
            if (reset || Dash.Keyboard.Count <= 0)
            {
                Dash.Keyboard.Clear();
                Dash.Add(Keys.X);
            }
            if (reset || Talk.Keyboard.Count <= 0)
            {
                Talk.Keyboard.Clear();
                Talk.Add(Keys.X);
            }
            if (reset || Pause.Keyboard.Count <= 0)
            {
                Pause.Keyboard.Clear();
                Pause.Add(Keys.Enter);
            }
            if (reset || Confirm.Keyboard.Count <= 0)
            {
                Confirm.Keyboard.Clear();
                Confirm.Add(Keys.C);
            }
            if (reset || Cancel.Keyboard.Count <= 0)
            {
                Cancel.Keyboard.Clear();
                Cancel.Add(Keys.X, Keys.Back);
            }
            if (reset || Journal.Keyboard.Count <= 0)
            {
                Journal.Keyboard.Clear();
                Journal.Add(Keys.Tab);
            }
            if (reset || QuickRestart.Keyboard.Count <= 0)
            {
                QuickRestart.Keyboard.Clear();
                QuickRestart.Add(Keys.R);
            }
            // NOTE: DemoDash nasce sem binding no Celeste — o Clear existe, o Add nao.
            if (reset)
                DemoDash.Keyboard.Clear();
        }

        public void SetDefaultButtonControls(bool reset)
        {
            if (reset || Left.Controller.Count <= 0)
            {
                Left.Controller.Clear();
                Left.Add(Buttons.LeftThumbstickLeft, Buttons.DPadLeft);
            }
            if (reset || Right.Controller.Count <= 0)
            {
                Right.Controller.Clear();
                Right.Add(Buttons.LeftThumbstickRight, Buttons.DPadRight);
            }
            if (reset || Down.Controller.Count <= 0)
            {
                Down.Controller.Clear();
                Down.Add(Buttons.LeftThumbstickDown, Buttons.DPadDown);
            }
            if (reset || Up.Controller.Count <= 0)
            {
                Up.Controller.Clear();
                Up.Add(Buttons.LeftThumbstickUp, Buttons.DPadUp);
            }
            if (reset || MenuLeft.Controller.Count <= 0)
            {
                MenuLeft.Controller.Clear();
                MenuLeft.Add(Buttons.LeftThumbstickLeft, Buttons.DPadLeft);
            }
            if (reset || MenuRight.Controller.Count <= 0)
            {
                MenuRight.Controller.Clear();
                MenuRight.Add(Buttons.LeftThumbstickRight, Buttons.DPadRight);
            }
            if (reset || MenuDown.Controller.Count <= 0)
            {
                MenuDown.Controller.Clear();
                MenuDown.Add(Buttons.LeftThumbstickDown, Buttons.DPadDown);
            }
            if (reset || MenuUp.Controller.Count <= 0)
            {
                MenuUp.Controller.Clear();
                MenuUp.Add(Buttons.LeftThumbstickUp, Buttons.DPadUp);
            }
            if (reset || Grab.Controller.Count <= 0)
            {
                // NOTE: 4 botoes no original, valores nao recuperaveis (ver cabecalho).
                Grab.Controller.Clear();
                Grab.Add(Buttons.LeftTrigger, Buttons.RightTrigger,
                         Buttons.LeftShoulder, Buttons.RightShoulder);
            }
            if (reset || Jump.Controller.Count <= 0)
            {
                Jump.Controller.Clear();
                Jump.Add(Buttons.A, Buttons.Y);
            }
            if (reset || Dash.Controller.Count <= 0)
            {
                Dash.Controller.Clear();
                Dash.Add(Buttons.X, Buttons.B);
            }
            if (reset || Talk.Controller.Count <= 0)
            {
                Talk.Controller.Clear();
                Talk.Add(Buttons.B);
            }
            if (reset || Pause.Controller.Count <= 0)
            {
                Pause.Controller.Clear();
                Pause.Add(Buttons.Start);
            }
            if (reset || Confirm.Controller.Count <= 0)
            {
                Confirm.Controller.Clear();
                Confirm.Add(Buttons.A);
            }
            if (reset || Cancel.Controller.Count <= 0)
            {
                Cancel.Controller.Clear();
                Cancel.Add(Buttons.B, Buttons.X);
            }
            if (reset || Journal.Controller.Count <= 0)
            {
                Journal.Controller.Clear();
                Journal.Add(Buttons.LeftTrigger);
            }
            // NOTE: QuickRestart e DemoDash nascem sem binding de controle no Celeste.
            if (reset)
            {
                QuickRestart.Controller.Clear();
                DemoDash.Controller.Clear();
            }
        }
    }
}
