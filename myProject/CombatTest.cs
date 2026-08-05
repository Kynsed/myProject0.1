using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Teste headless do sistema de combate (jogo proprio): combo basico de 3 estagios,
// dano 5/7/9, velocidades crescentes em duracao, janela de continuacao e resets.
namespace MonocleSmoke
{
    public static class CombatTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== combate: combo basico de 3 estagios (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());

            TestDamagePerStage();
            TestSpeeds();
            TestResetByTimeout();
            TestResetAfterFinisher();

            Console.WriteLine(fails == 0 ? "== COMBATE OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        // ---- helpers ----
        private static (Level lvl, Player p, MeleeCombo combo, TrainingDummy dummy) Boot()
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(44f, 150f), PlayerSpriteMode.Madeline);
            MeleeCombo combo = new MeleeCombo();
            p.Add(combo);
            lvl.Add(p);
            TrainingDummy dummy = new TrainingDummy(new Vector2(60f, 160f));
            lvl.Add(dummy);
            lvl.Begin();
            lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);
            return (lvl, p, combo, dummy);
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

        // aperta ataque 1 frame e roda ate o golpe terminar; retorna frames de Attacking
        private static int Swing(Scene s, MeleeCombo combo)
        {
            Step(s, Keys.A);
            int frames = combo.Attacking ? 1 : 0;
            for (int i = 0; i < 60 && combo.Attacking; i++)
            {
                Step(s);
                if (combo.Attacking) frames++;
            }
            return frames;
        }

        // ---- testes ----
        private static void TestDamagePerStage()
        {
            var (lvl, p, combo, dummy) = Boot();

            Swing(lvl, combo);
            Check("Golpe 1: dano 5 (HP 30 -> 25)", dummy.Health.Current == 25,
                "HP=" + dummy.Health.Current);

            Swing(lvl, combo); // dentro da janela: continua o combo
            Check("Golpe 2: dano 7 (HP -> 18)", dummy.Health.Current == 18,
                "HP=" + dummy.Health.Current);

            Swing(lvl, combo);
            Check("Golpe 3: dano 9 (HP -> 9)", dummy.Health.Current == 9,
                "HP=" + dummy.Health.Current);

            Check("Cada golpe acerta o alvo uma unica vez", dummy.Health.Current == 9,
                "HP=" + dummy.Health.Current);
        }

        private static void TestSpeeds()
        {
            var (lvl, p, combo, dummy) = Boot();
            int f1 = Swing(lvl, combo);
            int f2 = Swing(lvl, combo);
            int f3 = Swing(lvl, combo);
            Check("Velocidades: 1o rapido < 2o intermediario < 3o lento",
                f1 < f2 && f2 < f3, "frames=" + f1 + "/" + f2 + "/" + f3);
            // 0.20/0.30/0.42s a 60fps = 12/18/25 frames (+-1 por acumulo de float no timer)
            Check("Duracoes ~0.20/0.30/0.42s (12/18/25 frames +-1)",
                Math.Abs(f1 - 12) <= 1 && Math.Abs(f2 - 18) <= 1 && Math.Abs(f3 - 25) <= 1,
                "frames=" + f1 + "/" + f2 + "/" + f3);
        }

        private static void TestResetByTimeout()
        {
            var (lvl, p, combo, dummy) = Boot();
            Swing(lvl, combo);                                  // golpe 1 (5)
            int hpAfter1 = dummy.Health.Current;
            for (int i = 0; i < 40; i++) Step(lvl);             // 40 frames > janela de 0.55s (33)
            Check("Reset por timeout: proximo aperto volta ao estagio 1",
                combo.NextStage == 0, "NextStage=" + combo.NextStage);
            Swing(lvl, combo);
            Check("Reset por timeout: dano volta a ser 5", hpAfter1 - dummy.Health.Current == 5,
                "dano=" + (hpAfter1 - dummy.Health.Current));
        }

        private static void TestResetAfterFinisher()
        {
            var (lvl, p, combo, dummy) = Boot();
            Swing(lvl, combo);
            Swing(lvl, combo);
            Swing(lvl, combo);                                  // combo completo (5+7+9 = 21)
            int hpAfterCombo = dummy.Health.Current;
            Swing(lvl, combo);                                  // dentro da janela: recomeca
            Check("Apos o finisher o combo recomeca no estagio 1 (dano 5)",
                hpAfterCombo - dummy.Health.Current == 5,
                "dano=" + (hpAfterCombo - dummy.Health.Current));
        }
    }
}
