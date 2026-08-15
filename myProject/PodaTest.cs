using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Podas de movimento p/ o level design do metroidvania (jogo proprio):
//   1. dash so na horizontal (sem diagonal, sem vertical)
//   2. sem wallclimb (nem agarrar parede, nem climb jump)
//   3. golpe p/ cima com timing proprio, mais lento que o 1o horizontal
// Mede o portao DESLIGADO (o jogo hoje) e LIGADO (o upgrade devolve o movimento do
// Celeste). O port continua intacto: --parity roda com Abilities.EnableAll().
namespace MonocleSmoke
{
    public static class PodaTest
    {
        private const float WallJumpH = 130f;   // private const WallJumpHSpeed (Player.cs)

        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        private static bool Near(float a, float b, float tol) => Math.Abs(a - b) <= tol;

        public static int Run()
        {
            Console.WriteLine("== podas de movimento (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());
            Abilities.ResetToDefaults();

            Console.WriteLine("-- dash so horizontal --");
            TestDashUpBecomesHorizontal();
            TestDashDiagonalBecomesHorizontal();
            TestDashUpgradesDevolvemAsDirecoes();

            Console.WriteLine("-- sem wallclimb --");
            TestNaoAgarraParede();
            TestWallJumpSobrevive();
            TestWallSlideSobrevive();
            TestUpgradeDevolveOClimb();

            Console.WriteLine("-- ataque p/ cima mais lento --");
            TestGolpeCimaMaisLento();

            Abilities.ResetToDefaults();
            Console.WriteLine(fails == 0 ? "== PODAS OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        // ---- helpers ----
        private static Player Boot(out Scene scene, Vector2 spawn, bool leftWall, bool withCombo = false)
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));   // chao y=160
            if (leftWall)
                lvl.Add(new Solid(new Vector2(0f, 0f), 8f, 180f, false));  // parede esq x[0,8]
            Player p = new Player(spawn, PlayerSpriteMode.Madeline);
            if (withCombo)
                p.Add(new MeleeCombo());
            lvl.Add(p);
            lvl.Begin();
            lvl.BeforeUpdate();
            scene = lvl;
            return p;
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

        private static void Settle(Scene scene, Player p)
        {
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(scene);
        }

        // dispara o dash com o input dado e devolve a DashDir resultante
        private static Vector2 Dash(Vector2 spawn, params Keys[] keys)
        {
            Player p = Boot(out Scene s, spawn, false);
            Settle(s, p);
            Step(s, keys);
            for (int i = 0; i < 3 && p.DashDir == Vector2.Zero; i++) Step(s, keys);
            return p.DashDir;
        }

        // frames ate a hitbox do golpe nascer (anticipacao)
        private static int FramesAteHitbox(Scene s, params Keys[] keys)
        {
            for (int i = 1; i <= 40; i++)
            {
                Step(s, keys);
                if (s.Entities.FindFirst<AttackHitbox>() != null)
                    return i;
            }
            return -1;
        }

        // frames de Attacking a partir do aperto (golpe inteiro)
        private static int FramesDoGolpe(Scene s, MeleeCombo combo, params Keys[] keys)
        {
            Step(s, keys);
            int frames = combo.Attacking ? 1 : 0;
            for (int i = 0; i < 60 && combo.Attacking; i++)
            {
                Step(s);
                if (combo.Attacking) frames++;
            }
            return frames;
        }

        // ---- dash ----
        private static void TestDashUpBecomesHorizontal()
        {
            Vector2 dir = Dash(new Vector2(40f, 150f), Keys.Up, Keys.X);
            Check("Dash p/ cima vira dash horizontal (lado do sprite)",
                dir == new Vector2(1f, 0f), "DashDir=" + dir);
        }

        private static void TestDashDiagonalBecomesHorizontal()
        {
            Vector2 up = Dash(new Vector2(40f, 150f), Keys.Up, Keys.Right, Keys.X);
            Check("Dash diagonal p/ cima cai p/ a horizontal", up == new Vector2(1f, 0f), "DashDir=" + up);

            // diagonal p/ baixo e a entrada do hyper dash: sem ela o hyper nao existe
            Vector2 down = Dash(new Vector2(40f, 150f), Keys.Down, Keys.Right, Keys.X);
            Check("Dash diagonal p/ baixo cai p/ a horizontal (sem hyper dash)",
                down == new Vector2(1f, 0f), "DashDir=" + down);
        }

        private static void TestDashUpgradesDevolvemAsDirecoes()
        {
            Abilities.DashDiagonal = true;
            Vector2 diag = Dash(new Vector2(40f, 150f), Keys.Up, Keys.Right, Keys.X);
            Check("Upgrade DashDiagonal devolve a diagonal", diag.X > 0f && diag.Y < 0f, "DashDir=" + diag);
            Abilities.DashDiagonal = false;

            Abilities.DashVertical = true;
            Vector2 vert = Dash(new Vector2(40f, 150f), Keys.Up, Keys.X);
            Check("Upgrade DashVertical devolve o dash p/ cima", vert == new Vector2(0f, -1f), "DashDir=" + vert);
            Abilities.DashVertical = false;
        }

        // ---- wallclimb ----
        private static void TestNaoAgarraParede()
        {
            // x=12 deixa a hitbox (8 larg, offset -4) adjacente a parede x[0,8], sem sobrepor:
            // dentro do solido o Actor nao consegue subir e o teste de escalada mediria zero
            Player p = Boot(out Scene s, new Vector2(12f, 150f), true);
            Settle(s, p);
            for (int i = 0; i < 4; i++) Step(s, Keys.Left, Keys.Z);   // tenta agarrar
            bool climbing = p.StateMachine.State == 1;
            float stamina = p.Stamina;
            for (int i = 0; i < 20; i++) Step(s, Keys.Left, Keys.Z, Keys.Up);  // tenta escalar
            Check("Grab na parede nao entra no estado Climb (1)", !climbing, "state=" + p.StateMachine.State);
            Check("Sem climb, segurar grab nao drena stamina", Near(p.Stamina, stamina, 0.01f),
                "Stamina=" + p.Stamina);
            Check("Sem climb, o player nao sobe a parede", p.Y >= 140f, "Y=" + p.Y);
        }

        private static void TestWallJumpSobrevive()
        {
            // com grab segurado, o climb jump (sobe reto colado na parede) some;
            // o wall jump normal continua e empurra p/ longe da parede
            Player p = Boot(out Scene s, new Vector2(12f, 90f), true);
            for (int i = 0; i < 6; i++) Step(s);
            bool air = !p.OnGround();
            Step(s, Keys.Left, Keys.Z, Keys.C);
            Check("Grab + pulo na parede = wall jump (Speed.X ~ +130), nao climb jump",
                air && Near(p.Speed.X, WallJumpH, 8f), "air=" + air + " Speed.X=" + p.Speed.X);
        }

        private static void TestWallSlideSobrevive()
        {
            // wall slide nao passa pelo ClimbCheck: segue valendo e freia a queda
            Player p = Boot(out Scene s, new Vector2(12f, 20f), true);
            for (int i = 0; i < 40; i++) Step(s, Keys.Left);
            Check("Wall slide continua (queda freada, abaixo de MaxFall 160)",
                p.Speed.Y < 160f, "Speed.Y=" + p.Speed.Y);
        }

        private static void TestUpgradeDevolveOClimb()
        {
            Abilities.WallClimb = true;
            // x=12 deixa a hitbox (8 larg, offset -4) adjacente a parede x[0,8], sem sobrepor:
            // dentro do solido o Actor nao consegue subir e o teste de escalada mediria zero
            Player p = Boot(out Scene s, new Vector2(12f, 150f), true);
            Settle(s, p);
            for (int i = 0; i < 4; i++) Step(s, Keys.Left, Keys.Z);
            bool climbing = p.StateMachine.State == 1;
            float y0 = p.Y;
            for (int i = 0; i < 20; i++) Step(s, Keys.Left, Keys.Z, Keys.Up);
            Check("Upgrade WallClimb devolve o estado Climb (1)", climbing, "state=" + p.StateMachine.State);
            Check("Upgrade WallClimb volta a escalar (Y sobe)", p.Y < y0, "dY=" + (p.Y - y0));
            Abilities.WallClimb = false;
        }

        // ---- ataque p/ cima ----
        private static void TestGolpeCimaMaisLento()
        {
            Player p = Boot(out Scene s, new Vector2(44f, 150f), false, withCombo: true);
            Settle(s, p);
            int hitH = FramesAteHitbox(s, Keys.A);

            Player p2 = Boot(out Scene s2, new Vector2(44f, 150f), false, withCombo: true);
            Settle(s2, p2);
            int hitUp = FramesAteHitbox(s2, Keys.Up, Keys.A);
            Check("Golpe p/ cima demora mais p/ a hitbox nascer que o horizontal 1",
                hitH > 0 && hitUp > hitH, "horizontal=" + hitH + " cima=" + hitUp);

            Player p3 = Boot(out Scene s3, new Vector2(44f, 150f), false, withCombo: true);
            MeleeCombo c3 = p3.Get<MeleeCombo>();
            Settle(s3, p3);
            int durH = FramesDoGolpe(s3, c3, Keys.A);

            Player p4 = Boot(out Scene s4, new Vector2(44f, 150f), false, withCombo: true);
            MeleeCombo c4 = p4.Get<MeleeCombo>();
            Settle(s4, p4);
            int durUp = FramesDoGolpe(s4, c4, Keys.Up, Keys.A);
            Check("Golpe p/ cima dura mais que o horizontal 1 (18 vs 12 frames)",
                durUp > durH, "horizontal=" + durH + " cima=" + durUp);
            Check("Golpe p/ cima segue com dano/estagio do 1o golpe", c4.NextStage == 0,
                "NextStage=" + c4.NextStage);
        }
    }
}
