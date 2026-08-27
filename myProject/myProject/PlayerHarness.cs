using System;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Harnesses headless do Player (sem janela). A cena de jogo vive em PlayScene.cs.
namespace MonocleSmoke
{
    // Verificacao headless: roda o Player por N frames com DeltaTime simulado (sem janela).
    public static class PlayerSmoke
    {
        public static int Run()
        {
            Console.WriteLine("== player-smoke (headless, sem janela) ==");
            try
            {
                Tracker.Initialize();
                MInput.Initialize();
                Input.Initialize();
                PlayGame.InitTags();
                SetDelta(1f / 60f);

                PlayScene scene = new PlayScene();
                scene.Begin();
                for (int i = 0; i < 180; i++)
                {
                    MInput.UpdateNull();
                    scene.BeforeUpdate();
                    scene.Update();
                    scene.AfterUpdate();
                }
                Player p = scene.Tracker.GetEntity<Player>();
                Console.WriteLine("OK: 180 frames sem excecao.");
                if (p != null)
                    Console.WriteLine("Player apos cair: Y=" + p.Y + " OnGround=" + p.OnGround());
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("CRASH: " + e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static void SetDelta(float dt)
        {
            // DeltaTime/RawDeltaTime sao { get; private set; }; simular frames via reflection.
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, dt);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, dt);
        }
    }

    // Smoke com INPUT injetado: anda ate a parede, agarra, escala, pula, da dash.
    // Exercita Normal/Climb/Dash/WallJump headless p/ pegar NREs de stub.
    public static class PlayerFuzz
    {
        public static int Run()
        {
            Console.WriteLine("== player-fuzz (headless, input simulado) ==");
            try
            {
                Tracker.Initialize();
                MInput.Initialize();
                Input.Initialize();
                PlayGame.InitTags();
                typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
                typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);

                PlayScene scene = new PlayScene();
                scene.Begin();
                Player p = null;
                var seen = new System.Collections.Generic.HashSet<int>();

                // roteiro: (frames, teclas) — cobre andar, dash, agarrar parede esq, escalar, wall-jump
                (int n, Keys[] keys)[] script =
                {
                    (40, new Keys[0]),                                   // cair/pousar
                    (5,  new[]{ Keys.X }),                               // dash parado
                    (60, new[]{ Keys.Left }),                            // andar ate parede esq
                    (20, new[]{ Keys.Left, Keys.Z }),                    // agarrar parede
                    (30, new[]{ Keys.Left, Keys.Z, Keys.Up }),           // escalar
                    (3,  new[]{ Keys.Z, Keys.C }),                       // wall jump
                    (5,  new[]{ Keys.Up, Keys.X }),                      // dash p/ cima
                    (5,  new[]{ Keys.Right, Keys.X }),                   // dash diagonal
                    (40, new[]{ Keys.Right }),                           // andar
                    (3,  new[]{ Keys.C }),                               // pular
                };

                int frame = 0;
                foreach (var step in script)
                {
                    for (int k = 0; k < step.n; k++)
                    {
                        Inject(step.keys);
                        scene.BeforeUpdate();
                        scene.Update();
                        scene.AfterUpdate();
                        if (p == null) p = scene.Tracker.GetEntity<Player>();
                        if (p != null) seen.Add(p.StateMachine.State);
                        frame++;
                    }
                }

                Console.WriteLine("OK: " + frame + " frames sem excecao.");
                Console.WriteLine("Estados exercitados: " + string.Join(",", seen));
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("CRASH: " + e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }
        }

        private static void Inject(Keys[] keys)
        {
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = new KeyboardState(keys);
            foreach (VirtualInput vi in MInput.VirtualInputs)
                vi.Update();
        }
    }
}
