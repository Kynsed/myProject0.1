using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Contrato do input: os bindings do Celeste (teclado e controle), os VirtualInput que o
// Input.Initialize monta com os thresholds certos, o efeito real de uma tecla e de um
// estado de controle injetados, e a escolha automatica do slot do controle.
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
                Controller();
                GamepadSlot();
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

        // ---- controle (estado injetado) ----

        // Estado de um controle CONECTADO. Analogico no eixo do XNA: Y positivo = cima.
        private static GamePadState Pad(Vector2 left = default(Vector2), float lt = 0f, float rt = 0f,
            Buttons buttons = (Buttons)0, bool dpadLeft = false, bool dpadDown = false)
        {
            GamePadDPad dpad = new GamePadDPad(
                ButtonState.Released,
                dpadDown ? ButtonState.Pressed : ButtonState.Released,
                dpadLeft ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released);
            return new GamePadState(new GamePadThumbSticks(left, Vector2.Zero),
                new GamePadTriggers(lt, rt), new GamePadButtons(buttons), dpad);
        }

        // Slot vazio: o GamePadState default tem IsConnected = false.
        private static readonly GamePadState Unplugged = new GamePadState();

        // Um frame com o estado dos 4 slots, na ordem do Engine.Update: o MInput le os
        // dispositivos e atualiza os virtuais; so depois a cena roda (e e ela quem chama
        // Input.UpdateGamepadSlot).
        private static void StepPads(GamePadState s0, GamePadState s1, GamePadState s2, GamePadState s3)
        {
            GamePadState[] states = { s0, s1, s2, s3 };
            for (int i = 0; i < MInput.GamePads.Length; i++)
            {
                MInput.GamePads[i].PreviousState = MInput.GamePads[i].CurrentState;
                MInput.GamePads[i].CurrentState = states[i];
            }
            foreach (VirtualInput vi in MInput.VirtualInputs)
                vi.Update();
        }

        // So o slot indicado recebe o estado; os outros ficam conectados e neutros.
        private static void StepPad(int slot, GamePadState state)
        {
            GamePadState n = Pad();
            StepPads(slot == 0 ? state : n, slot == 1 ? state : n,
                slot == 2 ? state : n, slot == 3 ? state : n);
        }

        // Efeito real do controle no slot ativo — o equivalente do Behaviour() p/ teclado.
        private static void Controller()
        {
            Console.WriteLine("-- controle (estado injetado no slot 0) --");
            Input.Gamepad = 0;
            StepPad(0, Pad());
            StepPad(0, Pad());

            Check("estado injetado conta como controle conectado",
                MInput.GamePads[0].CurrentState.IsConnected && !Unplugged.IsConnected,
                "conectado=" + MInput.GamePads[0].CurrentState.IsConnected);

            StepPad(0, Pad(left: new Vector2(0.5f, 0f)));
            Check("analogico p/ a direita move o eixo p/ +1", Input.MoveX.Value == 1, "MoveX=" + Input.MoveX.Value);

            StepPad(0, Pad(left: new Vector2(0.2f, 0f)));
            Check("analogico abaixo do limiar do MoveX (0.3) nao move", Input.MoveX.Value == 0, "MoveX=" + Input.MoveX.Value);

            StepPad(0, Pad());
            StepPad(0, Pad(dpadLeft: true));
            Check("d-pad p/ a esquerda move o eixo p/ -1", Input.MoveX.Value == -1, "MoveX=" + Input.MoveX.Value);

            // MoveY tem limiar 0.7: inclinar o analogico na diagonal enquanto corre nao
            // pode agachar nem acelerar a queda sem querer.
            StepPad(0, Pad());
            StepPad(0, Pad(left: new Vector2(0f, 0.5f)));
            int halfUp = Input.MoveY.Value;
            StepPad(0, Pad(left: new Vector2(0f, 0.8f)));
            Check("MoveY so responde acima de 0.7 (cima = -1)",
                halfUp == 0 && Input.MoveY.Value == -1, "a0.5=" + halfUp + " a0.8=" + Input.MoveY.Value);

            StepPad(0, Pad());
            StepPad(0, Pad(dpadDown: true));
            Check("d-pad p/ baixo move o eixo p/ +1", Input.MoveY.Value == 1, "MoveY=" + Input.MoveY.Value);

            StepPad(0, Pad());
            StepPad(0, Pad(buttons: Buttons.A));
            Check("A dispara Jump.Pressed", Input.Jump.Pressed, "Pressed=" + Input.Jump.Pressed);

            StepPad(0, Pad());
            StepPad(0, Pad(buttons: Buttons.X));
            Check("X dispara Dash.Pressed", Input.Dash.Pressed, "Pressed=" + Input.Dash.Pressed);

            StepPad(0, Pad());
            StepPad(0, Pad(rt: 0.5f));
            bool triggerHeld = Input.Grab.Check;
            StepPad(0, Pad(rt: 0.1f));
            Check("gatilho segura Grab acima de 0.2 e solta abaixo",
                triggerHeld && !Input.Grab.Check, "a0.5=" + triggerHeld + " a0.1=" + Input.Grab.Check);

            StepPad(0, Pad());
            StepPad(0, Pad(buttons: Buttons.Start));
            Check("Start dispara Pause.Pressed", Input.Pause.Pressed, "Pressed=" + Input.Pause.Pressed);

            StepPad(0, Pad());
            StepPad(0, Pad(left: new Vector2(0.7f, 0.7f)));
            Vector2 aim = Input.GetAimVector(Facings.Right);
            Check("mira do dash: analogico cima-direita vira a diagonal (0.707, -0.707)",
                Math.Abs(aim.X - 0.7071f) < 0.01f && Math.Abs(aim.Y + 0.7071f) < 0.01f, "aim=" + aim);

            StepPad(0, Pad());
        }

        // Escolha automatica do slot (jogo proprio, ver Input.UpdateGamepadSlot): o ultimo
        // controle USADO manda, e uso e borda de aperto — nunca nivel.
        private static void GamepadSlot()
        {
            Console.WriteLine("-- escolha do slot do controle --");
            GamePadState n = Pad();
            Input.Gamepad = 0;
            StepPads(n, n, n, n);
            StepPads(n, n, n, n);
            Check("todos parados: o slot nao muda",
                !Input.UpdateGamepadSlot() && Input.Gamepad == 0, "slot=" + Input.Gamepad);

            // O caso que motivou a regra: o controle caiu num slot que nao e o 0.
            StepPad(1, Pad(left: new Vector2(0.9f, 0f)));
            int before = Input.MoveX.Value;   // o jogo ainda le o slot 0
            bool switched = Input.UpdateGamepadSlot();
            Check("usar um controle em outro slot faz dele o slot ativo",
                before == 0 && switched && Input.Gamepad == 1,
                "antes=" + before + " trocou=" + switched + " slot=" + Input.Gamepad);
            Check("a direcao segurada ja vale no frame da troca",
                Input.MoveX.Value == 1, "MoveX=" + Input.MoveX.Value);

            Input.Gamepad = 0;
            StepPads(n, n, n, n);
            StepPads(n, n, n, n);
            StepPad(1, Pad(buttons: Buttons.A));
            switched = Input.UpdateGamepadSlot();
            bool onSwitch = Input.Jump.Pressed;
            StepPad(1, Pad(buttons: Buttons.A));   // segue segurando
            Input.UpdateGamepadSlot();
            Check("o aperto que troca o slot dispara o pulo e entra no buffer",
                switched && onSwitch && Input.Jump.Pressed,
                "trocou=" + switched + " noFrame=" + onSwitch + " buffer=" + Input.Jump.Pressed);

            // ativo = 1. Os dois controles apertam no mesmo frame: quem ja joga fica.
            StepPads(n, n, n, n);
            StepPads(Pad(buttons: Buttons.A), Pad(buttons: Buttons.B), n, n);
            Check("empate no mesmo frame: o slot ativo fica",
                !Input.UpdateGamepadSlot() && Input.Gamepad == 1, "slot=" + Input.Gamepad);

            StepPads(n, n, n, n);
            StepPad(0, Pad(buttons: Buttons.A));
            Check("o controle anterior retoma quando e usado sozinho",
                Input.UpdateGamepadSlot() && Input.Gamepad == 0, "slot=" + Input.Gamepad);

            // Dispositivo com eixo preso (volante, pedal, analogico com defeito): nivel
            // alto o tempo todo, nenhuma borda depois de conectar.
            GamePadState stuck = Pad(left: new Vector2(0.9f, 0f), rt: 1f);
            StepPads(n, n, Unplugged, n);
            StepPads(n, n, Unplugged, n);
            bool stole = false;
            for (int i = 0; i < 6; i++)
            {
                StepPads(n, n, stuck, n);
                stole |= Input.UpdateGamepadSlot();
            }
            Check("dispositivo com eixo preso nao rouba o slot",
                !stole && Input.Gamepad == 0, "roubou=" + stole + " slot=" + Input.Gamepad);

            StepPads(n, Pad(buttons: Buttons.A), stuck, n);
            Check("eixo preso em outro slot nao impede a troca",
                Input.UpdateGamepadSlot() && Input.Gamepad == 1, "slot=" + Input.Gamepad);

            // Conectar (ou a janela voltar ao foco) com um botao ja apertado nao e uso:
            // falta a amostra anterior p/ haver borda.
            Input.Gamepad = 0;
            StepPads(n, n, n, Unplugged);
            StepPads(n, n, n, Unplugged);
            StepPads(n, n, n, Pad(buttons: Buttons.A));
            bool onConnect = Input.UpdateGamepadSlot();
            StepPads(n, n, n, n);
            StepPads(n, n, n, Pad(buttons: Buttons.A));
            bool onPress = Input.UpdateGamepadSlot();
            Check("conectar com botao apertado nao troca; o aperto seguinte troca",
                !onConnect && onPress && Input.Gamepad == 3,
                "naConexao=" + onConnect + " noAperto=" + onPress + " slot=" + Input.Gamepad);

            // O teclado nao depende de slot.
            StepPads(n, n, n, n);
            Step();
            Step(Keys.C);
            Check("teclado segue valendo com qualquer slot ativo",
                Input.Jump.Pressed, "slot=" + Input.Gamepad + " Pressed=" + Input.Jump.Pressed);
            Step();

            Check("trocar de slot nao acumula virtuais no MInput",
                MInput.VirtualInputs.Count == 20, "registrados=" + MInput.VirtualInputs.Count);

            Input.Gamepad = 0;
            StepPads(Unplugged, Unplugged, Unplugged, Unplugged);
        }
    }
}
