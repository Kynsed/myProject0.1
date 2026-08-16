using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Sistema de input do jogo (nao e mais o esquema do Celeste):
//   teclado: setas movem | Z pula | X ataca | C dash | ESC pausa | V agarra
//   xbox:    dpad/analogico movem | A pula | X ataca | RT dash | Start pausa
// Mede o binding (o que esta mapeado) E o efeito (apertar a tecla faz a acao).
namespace MonocleSmoke
{
    public static class InputTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== sistema de input (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());
            Abilities.ResetToDefaults();

            Console.WriteLine("-- mapeamento --");
            TestTeclado();
            TestGamepad();

            Console.WriteLine("-- efeito no jogo (teclado) --");
            TestAcoesRespondem();

            Console.WriteLine("-- efeito no jogo (xbox) --");
            TestGamepadResponde();

            Console.WriteLine("-- pausa --");
            TestPausa();

            Console.WriteLine(fails == 0 ? "== INPUT OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        private static void Step(Scene scene, params Keys[] keys)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState(keys);
            foreach (VirtualInput vi in MInput.VirtualInputs) vi.Update();
            scene.BeforeUpdate();
            scene.Update();
            scene.AfterUpdate();
        }

        private static (Level lvl, Player p, MeleeCombo combo) Boot()
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(60f, 150f), PlayerSpriteMode.Madeline);
            MeleeCombo combo = new MeleeCombo();
            p.Add(combo);
            lvl.Add(p);
            lvl.Begin(); lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);
            return (lvl, p, combo);
        }

        // ---- mapeamento ----
        private static void TestTeclado()
        {
            Settings s = Settings.Instance;
            Check("Teclado: Z pula", s.Jump.Keyboard.Contains(Keys.Z),
                "Jump=" + string.Join(",", s.Jump.Keyboard));
            Check("Teclado: X ataca", s.Attack.Keyboard.Contains(Keys.X),
                "Attack=" + string.Join(",", s.Attack.Keyboard));
            Check("Teclado: C da dash", s.Dash.Keyboard.Contains(Keys.C),
                "Dash=" + string.Join(",", s.Dash.Keyboard));
            Check("Teclado: ESC pausa", s.Pause.Keyboard.Contains(Keys.Escape),
                "Pause=" + string.Join(",", s.Pause.Keyboard));
            Check("Teclado: setas movem", s.Left.Keyboard.Contains(Keys.Left)
                && s.Right.Keyboard.Contains(Keys.Right) && s.Up.Keyboard.Contains(Keys.Up)
                && s.Down.Keyboard.Contains(Keys.Down), "setas ok");
            Check("Teclado: nenhuma tecla faz duas acoes de uma vez",
                !s.Jump.Keyboard.Contains(Keys.X) && !s.Dash.Keyboard.Contains(Keys.Z)
                && !s.Attack.Keyboard.Contains(Keys.C) && !s.Grab.Keyboard.Contains(Keys.Z),
                "Grab=" + string.Join(",", s.Grab.Keyboard));
        }

        private static void TestGamepad()
        {
            Settings s = Settings.Instance;
            Check("Xbox: A pula", s.Jump.Controller.Contains(Buttons.A),
                "Jump=" + string.Join(",", s.Jump.Controller));
            Check("Xbox: X ataca", s.Attack.Controller.Contains(Buttons.X),
                "Attack=" + string.Join(",", s.Attack.Controller));
            Check("Xbox: RT da dash", s.Dash.Controller.Contains(Buttons.RightTrigger),
                "Dash=" + string.Join(",", s.Dash.Controller));
            Check("Xbox: Start (3 tracos) pausa", s.Pause.Controller.Contains(Buttons.Start),
                "Pause=" + string.Join(",", s.Pause.Controller));
            Check("Xbox: dpad e analogico esquerdo movem",
                s.Left.Controller.Contains(Buttons.DPadLeft)
                && s.Left.Controller.Contains(Buttons.LeftThumbstickLeft)
                && s.Right.Controller.Contains(Buttons.DPadRight)
                && s.Up.Controller.Contains(Buttons.DPadUp)
                && s.Down.Controller.Contains(Buttons.DPadDown),
                "Left=" + string.Join(",", s.Left.Controller));
        }

        // ---- efeito ----
        private static void TestAcoesRespondem()
        {
            var (lvl, p, combo) = Boot();
            Step(lvl, Keys.Z);
            Check("Z pula de verdade (Speed.Y negativo)", p.Speed.Y < -50f, "Speed.Y=" + p.Speed.Y);

            var (l2, p2, c2) = Boot();
            Step(l2, Keys.Right, Keys.C);
            Check("C dasha de verdade (estado Dash)", p2.StateMachine.State == 2,
                "state=" + p2.StateMachine.State);

            var (l3, p3, c3) = Boot();
            Step(l3, Keys.X);
            Check("X ataca de verdade (combo dispara)", c3.Attacking, "Attacking=" + c3.Attacking);

            // a tecla velha nao pode mais responder (esquema antigo do Celeste)
            var (l4, p4, c4) = Boot();
            Step(l4, Keys.A);
            Check("A (esquema antigo) nao ataca mais", !c4.Attacking, "Attacking=" + c4.Attacking);
        }

        // Injeta um estado de controle (o mesmo caminho que MInput.Update alimenta com
        // GamePad.GetState no jogo real) e roda um frame.
        private static void StepPad(Scene scene, GamePadState pad)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState();
            MInput.GamePadData gp = MInput.GamePads[0];
            gp.PreviousState = gp.CurrentState;
            gp.CurrentState = pad;
            foreach (VirtualInput vi in MInput.VirtualInputs) vi.Update();
            scene.BeforeUpdate();
            scene.Update();
            scene.AfterUpdate();
        }

        private static GamePadState Pad(Buttons buttons = 0, float rightTrigger = 0f,
            float stickX = 0f, bool dpadLeft = false)
        {
            return new GamePadState(
                new GamePadThumbSticks(new Vector2(stickX, 0f), Vector2.Zero),
                new GamePadTriggers(0f, rightTrigger),
                new GamePadButtons(buttons),
                new GamePadDPad(ButtonState.Released, ButtonState.Released,
                    dpadLeft ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released));
        }

        // Exercita o caminho do controle de verdade: binding -> MInput.GamePads[0] ->
        // VirtualButton -> acao. O que fica de fora e so a camada de driver (SDL/MonoGame
        // enxergar o controle), que precisa de hardware.
        private static void TestGamepadResponde()
        {
            var (lvl, p, combo) = Boot();
            StepPad(lvl, Pad(Buttons.A));
            Check("Xbox: A pula de verdade (Speed.Y negativo)", p.Speed.Y < -50f,
                "Speed.Y=" + p.Speed.Y);

            var (l2, p2, c2) = Boot();
            StepPad(l2, Pad(rightTrigger: 1f, dpadLeft: true));
            Check("Xbox: RT dasha de verdade (estado Dash)", p2.StateMachine.State == 2,
                "state=" + p2.StateMachine.State);

            var (l3, p3, c3) = Boot();
            StepPad(l3, Pad(Buttons.X));
            Check("Xbox: X ataca de verdade (combo dispara)", c3.Attacking,
                "Attacking=" + c3.Attacking);

            var (l4, p4, c4) = Boot();
            for (int i = 0; i < 6; i++) StepPad(l4, Pad(dpadLeft: true));
            bool andouDpad = p4.Speed.X < -20f;
            var (l5, p5, c5) = Boot();
            for (int i = 0; i < 6; i++) StepPad(l5, Pad(stickX: -1f));
            Check("Xbox: dpad e analogico esquerdo movem",
                andouDpad && p5.Speed.X < -20f,
                "dpad Speed.X=" + p4.Speed.X + " analogico Speed.X=" + p5.Speed.X);

            // RT abaixo do limiar do VirtualButton (0.2) nao pode disparar o dash
            var (l6, p6, c6) = Boot();
            StepPad(l6, Pad(rightTrigger: 0.1f, dpadLeft: true));
            Check("Xbox: RT mal encostado nao dasha (limiar 0.2)", p6.StateMachine.State != 2,
                "state=" + p6.StateMachine.State);
        }

        // ---- pausa ----
        private static void TestPausa()
        {
            PlayScene play = new PlayScene();
            play.Begin();
            play.BeforeUpdate();
            Player p = play.Tracker.GetEntity<Player>();
            for (int i = 0; i < 60 && p != null && !p.OnGround(); i++) Step(play);

            Step(play, Keys.Escape);
            Check("ESC pausa o jogo", play.Paused, "Paused=" + play.Paused);

            // pausado o mundo nao anda: joga o player no ar e ele nao cai
            p.Position = new Vector2(p.X, p.Y - 40f);
            float y = p.Y;
            for (int i = 0; i < 30; i++) Step(play);
            Check("Pausado, as entidades congelam (player nao cai)", p.Y == y,
                "dY=" + (p.Y - y));

            Step(play);                    // frame neutro: Pressed precisa da transicao
            Step(play, Keys.Escape);
            Check("ESC de novo despausa", !play.Paused, "Paused=" + play.Paused);

            for (int i = 0; i < 30; i++) Step(play);
            Check("Despausado, o jogo volta a rodar (player cai)", p.Y > y, "dY=" + (p.Y - y));

            StepPad(play, Pad(Buttons.Start));
            Check("Xbox: Start pausa de verdade", play.Paused, "Paused=" + play.Paused);
            StepPad(play, Pad());
            StepPad(play, Pad(Buttons.Start));
            Check("Xbox: Start de novo despausa", !play.Paused, "Paused=" + play.Paused);
        }
    }
}
