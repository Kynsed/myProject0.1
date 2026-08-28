using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Contrato do input: os bindings do Celeste (teclado e controle), os VirtualInput que o
// Input.Initialize monta com os thresholds certos, e o efeito real de uma tecla injetada.
// Headless — input nao precisa de GraphicsDevice.
namespace MonocleSmoke
{
    public static class InputTest
    {
        private static int fails;

        public static int Run()
        {
            Console.WriteLine("== input-test (bindings e virtual inputs) ==");
            fails = 0;

            Tracker.Initialize();
            MInput.Initialize();
            Settings.Instance = new Settings();
            Input.Initialize();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);

            try
            {
                Bindings();
                Virtuals();
                Behaviour();
            }
            catch (Exception e)
            {
                Check("rodar sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            Console.WriteLine(fails == 0 ? "== INPUT OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }

        private static string Keys_(Binding b)
        {
            return string.Join("+", b.Keyboard);
        }

        private static string Btns(Binding b)
        {
            return string.Join("+", b.Controller);
        }

        private static void Bindings()
        {
            Settings s = Settings.Instance;

            // Defaults de teclado do Celeste (Settings.SetDefaultKeyboardControls).
            Check("teclado: movimento nas setas",
                Keys_(s.Left) == "Left" && Keys_(s.Right) == "Right"
                    && Keys_(s.Up) == "Up" && Keys_(s.Down) == "Down",
                Keys_(s.Left) + " " + Keys_(s.Right) + " " + Keys_(s.Up) + " " + Keys_(s.Down));

            Check("teclado: Jump=C, Dash=X, Talk=X",
                Keys_(s.Jump) == "C" && Keys_(s.Dash) == "X" && Keys_(s.Talk) == "X",
                "Jump=" + Keys_(s.Jump) + " Dash=" + Keys_(s.Dash) + " Talk=" + Keys_(s.Talk));

            Check("teclado: Pause=Enter, Confirm=C, Cancel=X+Back",
                Keys_(s.Pause) == "Enter" && Keys_(s.Confirm) == "C" && Keys_(s.Cancel) == "X+Back",
                "Pause=" + Keys_(s.Pause) + " Confirm=" + Keys_(s.Confirm) + " Cancel=" + Keys_(s.Cancel));

            Check("teclado: Journal=Tab, QuickRestart=R",
                Keys_(s.Journal) == "Tab" && Keys_(s.QuickRestart) == "R",
                "Journal=" + Keys_(s.Journal) + " QuickRestart=" + Keys_(s.QuickRestart));

            Check("teclado: menu espelha as setas",
                Keys_(s.MenuLeft) == "Left" && Keys_(s.MenuUp) == "Up",
                "MenuLeft=" + Keys_(s.MenuLeft) + " MenuUp=" + Keys_(s.MenuUp));

            // Defaults de controle (Settings.SetDefaultButtonControls).
            Check("controle: movimento no analogico esquerdo + dpad",
                Btns(s.Left) == "LeftThumbstickLeft+DPadLeft"
                    && Btns(s.Up) == "LeftThumbstickUp+DPadUp",
                Btns(s.Left));

            Check("controle: Jump=A+Y, Dash=X+B, Talk=B",
                Btns(s.Jump) == "A+Y" && Btns(s.Dash) == "X+B" && Btns(s.Talk) == "B",
                "Jump=" + Btns(s.Jump) + " Dash=" + Btns(s.Dash) + " Talk=" + Btns(s.Talk));

            Check("controle: Pause=Start, Confirm=A, Cancel=B+X",
                Btns(s.Pause) == "Start" && Btns(s.Confirm) == "A" && Btns(s.Cancel) == "B+X",
                "Pause=" + Btns(s.Pause) + " Confirm=" + Btns(s.Confirm) + " Cancel=" + Btns(s.Cancel));

            // DemoDash (crouch dash) nasce sem binding no Celeste.
            Check("DemoDash nasce sem binding, como no Celeste",
                s.DemoDash.Keyboard.Count == 0 && s.DemoDash.Controller.Count == 0,
                "teclas=" + s.DemoDash.Keyboard.Count + " botoes=" + s.DemoDash.Controller.Count);

            // Grab: os valores do original nao sao recuperaveis do decompilado, mas os
            // TAMANHOS sao (Keys[3] e Buttons[4]) — isso da p/ afirmar.
            Check("Grab: 3 teclas e 4 botoes (tamanhos do original)",
                s.Grab.Keyboard.Count == 3 && s.Grab.Controller.Count == 4,
                "teclas=" + s.Grab.Keyboard.Count + " botoes=" + s.Grab.Controller.Count);

            // reset=true redefine mesmo com binding existente
            s.Left.Keyboard.Clear();
            s.Left.Add(Keys.Q);
            s.SetDefaultKeyboardControls(true);
            Check("SetDefaultKeyboardControls(true) redefine binding existente",
                Keys_(s.Left) == "Left", "Left=" + Keys_(s.Left));
        }

        private static void Virtuals()
        {
            Check("Input monta os virtuais de movimento",
                Input.MoveX != null && Input.MoveY != null && Input.Aim != null
                    && Input.Feather != null && Input.GliderMoveY != null,
                "MoveX/MoveY/Aim/Feather/GliderMoveY");

            Check("Input monta os virtuais de acao",
                Input.Jump != null && Input.Dash != null && Input.Grab != null
                    && Input.Talk != null && Input.CrouchDash != null,
                "Jump/Dash/Grab/Talk/CrouchDash");

            Check("Input monta os virtuais de sistema",
                Input.Pause != null && Input.ESC != null && Input.QuickRestart != null,
                "Pause/ESC/QuickRestart");

            Check("Input monta os virtuais de menu",
                Input.MenuLeft != null && Input.MenuRight != null && Input.MenuUp != null
                    && Input.MenuDown != null && Input.MenuConfirm != null
                    && Input.MenuCancel != null && Input.MenuJournal != null,
                "Menu*");

            Check("todos os virtuais entraram no MInput",
                MInput.VirtualInputs.Count >= 19, "registrados=" + MInput.VirtualInputs.Count);
        }

        private static void Step(params Keys[] keys)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState(keys);
            foreach (VirtualInput vi in MInput.VirtualInputs)
                vi.Update();
        }

        private static void Behaviour()
        {
            Step();
            Step(Keys.Left);
            Check("seta esquerda move o eixo p/ -1", Input.MoveX.Value == -1, "MoveX=" + Input.MoveX.Value);

            Step();
            Step(Keys.Right);
            Check("seta direita move o eixo p/ +1", Input.MoveX.Value == 1, "MoveX=" + Input.MoveX.Value);

            Step();
            Step(Keys.C);
            Check("C dispara Jump.Pressed", Input.Jump.Pressed, "Pressed=" + Input.Jump.Pressed);

            Step();
            Step(Keys.X);
            Check("X dispara Dash.Pressed", Input.Dash.Pressed, "Pressed=" + Input.Dash.Pressed);

            Step();
            Step(Keys.Z);
            Check("Z segura Grab.Check", Input.Grab.Check, "Check=" + Input.Grab.Check);

            Step();
            Step(Keys.Enter);
            Check("Enter dispara Pause.Pressed", Input.Pause.Pressed, "Pressed=" + Input.Pause.Pressed);

            Step();
            Step(Keys.Escape);
            Check("ESC dispara ESC.Pressed", Input.ESC.Pressed, "Pressed=" + Input.ESC.Pressed);

            // Buffer do pulo, 0.08s. A semantica do Celeste e contraintuitiva e vale travar:
            // o buffer so vale ENQUANTO o botao segue apertado. O ramo !flag do
            // VirtualButton.Update faz bufferCounter = 0, entao SOLTAR mata o buffer.
            // E por isso que o jump buffer do Celeste funciona segurando o pulo antes de
            // encostar no chao, e nao dando um toque antes.
            Step();
            Step(Keys.C);
            int heldFrames = 0;
            while (Input.Jump.Pressed && heldFrames < 20)
            {
                heldFrames++;
                Step(Keys.C);
            }
            Check("Jump: segurando, Pressed dura ~0.08s (4-6 frames a 60fps)",
                heldFrames >= 4 && heldFrames <= 6, "frames=" + heldFrames);

            Step();
            Step(Keys.C);
            bool pressedOnPress = Input.Jump.Pressed;
            Step();   // soltou
            Check("Jump: soltar zera o buffer (fiel ao ramo !flag do Celeste)",
                pressedOnPress && !Input.Jump.Pressed,
                "noAperto=" + pressedOnPress + " aposSoltar=" + Input.Jump.Pressed);

            // Pressed e (buffer > 0 || Repeating || Binding.Pressed). Entao ConsumeBuffer
            // mata SO a cauda bufferizada — no frame do aperto o Binding.Pressed ainda
            // responde. Quem derruba o aperto na hora e ConsumePress (seta consumed).
            Step();
            Step(Keys.C);
            bool beforeConsume = Input.Jump.Pressed;
            Input.Jump.ConsumeBuffer();
            bool sameFrame = Input.Jump.Pressed;
            Step(Keys.C);   // segue segurando, mas o buffer foi consumido
            bool nextFrame = Input.Jump.Pressed;
            Check("ConsumeBuffer mata a cauda do buffer, nao o aperto do frame",
                beforeConsume && sameFrame && !nextFrame,
                "antes=" + beforeConsume + " mesmoFrame=" + sameFrame + " proximo=" + nextFrame);

            Step();
            Step(Keys.C);
            bool beforePress = Input.Jump.Pressed;
            Input.Jump.ConsumePress();
            Check("ConsumePress derruba o Pressed na hora",
                beforePress && !Input.Jump.Pressed,
                "antes=" + beforePress + " depois=" + Input.Jump.Pressed);

            // Grab tem buffer 0 no Celeste — nao sobrevive a soltar.
            Step();
            Step(Keys.Z);
            Step();
            Check("Grab nao tem buffer (buffer 0 no Celeste)", !Input.Grab.Check,
                "Check=" + Input.Grab.Check);
        }
    }
}
