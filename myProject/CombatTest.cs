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
            TestMovementLock();
            TestFacingReset();
            TestAerialHover();
            TestDirectionalAttacks();
            TestVerticalSingle();
            TestDownAttackCharge();
            TestDownUnlocked();
            TestRecoil();

            Console.WriteLine(fails == 0 ? "== COMBATE OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        // ---- helpers ----
        private static (Level lvl, Player p, MeleeCombo combo, TrainingDummy dummy) Boot(
            bool withDummy = true, float playerX = 44f)
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(playerX, 150f), PlayerSpriteMode.Madeline);
            MeleeCombo combo = new MeleeCombo();
            p.Add(combo);
            lvl.Add(p);
            TrainingDummy dummy = null;
            if (withDummy)
            {
                dummy = new TrainingDummy(new Vector2(60f, 160f));
                lvl.Add(dummy);
            }
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
            Check("Combo completo causa 21 (HP 30 -> 9)", dummy.Health.Current == 9,
                "HP=" + dummy.Health.Current);
            Step(lvl, Keys.A);                                  // dentro da janela: recomeca
            Check("Apos o finisher o combo recomeca no estagio 1",
                combo.Attacking && combo.Stage == 0, "Stage=" + combo.Stage);
        }

        private static void TestMovementLock()
        {
            var (lvl, p, combo, dummy) = Boot(withDummy: false); // sem alvo: golpe nao gera recuo

            Step(lvl, Keys.A); // dispara o golpe 1
            float x0 = p.X;
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl, Keys.Right);
            Check("Trava: segurar direita NAO move durante o golpe", p.X == x0, "dX=" + (p.X - x0));

            for (int i = 0; i < 10; i++) Step(lvl, Keys.Right); // intervalo
            Check("Trava: movimento volta no intervalo entre golpes", p.X > x0, "dX=" + (p.X - x0));
        }

        private static void TestFacingReset()
        {
            var (lvl, p, combo, dummy) = Boot();

            Swing(lvl, combo); // golpe 1 olhando p/ direita
            for (int i = 0; i < 6; i++) Step(lvl, Keys.Left); // vira no intervalo (dentro da janela)
            Step(lvl, Keys.A); // ataca olhando p/ esquerda
            Check("Virar de direcao reseta o combo (dispara estagio 1, nao 2)",
                combo.Attacking && combo.Stage == 0, "Stage=" + combo.Stage);
        }

        private static void TestAerialHover()
        {
            var (lvl, p, combo, dummy) = Boot();

            Step(lvl, Keys.C);                       // pula
            for (int i = 0; i < 8; i++) Step(lvl);   // subindo
            Step(lvl, Keys.A);                       // ataque aereo
            float y0 = p.Y;
            bool moved = false;
            for (int i = 0; i < 60 && combo.Attacking; i++)
            {
                Step(lvl);
                if (p.Y != y0) moved = true;
            }
            Check("Aereo: paira durante o golpe (Y constante, sem queda)", !moved,
                "Y=" + p.Y + " y0=" + y0);

            float yEnd = p.Y;
            for (int i = 0; i < 12; i++) Step(lvl);  // intervalo: gravidade volta
            Check("Aereo: cai no intervalo entre golpes", p.Y > yEnd, "dY=" + (p.Y - yEnd));
        }

        private static void TestDirectionalAttacks()
        {
            // chao + segurando CIMA: golpe acima da cabeca
            var (lvl, p, combo, dummy) = Boot(withDummy: false);
            Step(lvl, Keys.Up, Keys.A);
            Step(lvl, Keys.Up); // flush do Scene.Add
            AttackHitbox atk = lvl.Entities.FindFirst<AttackHitbox>();
            Check("Direcional: chao + cima = golpe acima do player",
                atk != null && atk.Dir == -Vector2.UnitY && atk.Bottom <= p.Top + 0.01f,
                atk == null ? "atk=null" : "Dir=" + atk.Dir + " atkBottom=" + atk.Bottom + " pTop=" + p.Top);

            // ar + segurando BAIXO: golpe abaixo dos pes
            var (lvl2, p2, combo2, _) = Boot(withDummy: false);
            Step(lvl2, Keys.C);                          // pula
            for (int i = 0; i < 8; i++) Step(lvl2);      // subindo
            Step(lvl2, Keys.Down, Keys.A);
            Step(lvl2, Keys.Down);
            AttackHitbox atk2 = lvl2.Entities.FindFirst<AttackHitbox>();
            Check("Direcional: ar + baixo = golpe abaixo do player",
                atk2 != null && atk2.Dir == Vector2.UnitY && atk2.Top >= p2.Bottom - 0.01f,
                atk2 == null ? "atk=null" : "Dir=" + atk2.Dir + " atkTop=" + atk2.Top + " pBottom=" + p2.Bottom);
        }

        private static void TestVerticalSingle()
        {
            // vertical e golpe UNICO: dispara sempre com timing/dano do 1o estagio e
            // reseta a progressao do combo horizontal
            var (lvl, p, combo, dummy) = Boot(withDummy: false);
            Swing(lvl, combo);                    // horizontal 1: proximo seria o estagio 2
            Step(lvl, Keys.Up, Keys.A);           // vertical dentro da janela
            Check("Vertical: dispara como golpe unico (estagio 1, dano 5)",
                combo.Attacking && combo.Stage == 0, "Stage=" + combo.Stage);
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl, Keys.Up);
            Check("Vertical: nao avanca o combo (proximo horizontal = estagio 1)",
                combo.NextStage == 0, "NextStage=" + combo.NextStage);
        }

        private static void TestDownAttackCharge()
        {
            // como o dash aereo: 1 carga por voo, recarrega tocando o chao.
            // (segura C p/ subir alto; sem segurar baixo durante o golpe p/ nao fast-fallar)
            var (lvl, p, combo, _) = Boot(withDummy: false);
            for (int i = 0; i < 8; i++) Step(lvl, Keys.C);        // pulo alto (var jump)
            Step(lvl, Keys.Down, Keys.A);                         // 1o golpe p/ baixo: usa a carga
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);

            Step(lvl, Keys.Down, Keys.A);                         // ainda no ar, sem carga
            Step(lvl, Keys.Down);
            AttackHitbox atk = lvl.Entities.FindFirst<AttackHitbox>();
            Check("Carga: 2o aperto p/ baixo no mesmo voo sai horizontal (sem carga)",
                atk != null && atk.Dir.Y == 0f && !p.OnGround(),
                atk == null ? "atk=null" : "Dir=" + atk.Dir + " ar=" + !p.OnGround());
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);

            for (int i = 0; i < 120 && !p.OnGround(); i++) Step(lvl); // pousa: recarrega
            for (int i = 0; i < 8; i++) Step(lvl, Keys.C);        // pula de novo
            Step(lvl, Keys.Down, Keys.A);
            Step(lvl, Keys.Down);
            AttackHitbox atk2 = lvl.Entities.FindFirst<AttackHitbox>();
            Check("Carga: tocar o chao recarrega o golpe p/ baixo",
                atk2 != null && atk2.Dir == Vector2.UnitY,
                atk2 == null ? "atk=null" : "Dir=" + atk2.Dir);
        }

        private static void TestRecoil()
        {
            // horizontal: acertar o boneco desliza o player p/ tras (recuo com decaimento)
            var (lvl, p, combo, dummy) = Boot();
            float x0 = p.X;
            Step(lvl, Keys.A);
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);
            Check("Recuo: acertar com golpe horizontal empurra o player p/ tras",
                p.X < x0, "dX=" + (p.X - x0));

            // pogo: golpe p/ baixo acertando quica o player p/ cima (fisica normal, sem trava)
            var (lvl2, p2, combo2, dummy2) = Boot(playerX: 60f); // em cima do boneco
            for (int i = 0; i < 8; i++) Step(lvl2, Keys.C);      // pulo alto (var jump)
            Step(lvl2, Keys.Down, Keys.A);
            bool bounced = false;
            for (int i = 0; i < 60 && combo2.Attacking; i++)
            {
                Step(lvl2); // sem segurar baixo: fast fall atrapalharia a leitura do quique
                if (dummy2.Health.Current < 30 && p2.Speed.Y < 0f)
                    bounced = true; // no frame do hit ganhou velocidade p/ cima
            }
            Check("Recuo: pogo — acertar quica o player p/ cima (Speed.Y < 0)",
                bounced, "bounced=" + bounced);
            Check("Pogo: golpe unico causa 5 (HP 30 -> 25)",
                dummy2.Health.Current == 25, "HP=" + dummy2.Health.Current);

            // o hit devolveu a carga: da p/ pogar de novo no mesmo voo
            Step(lvl2, Keys.Down, Keys.A);
            Step(lvl2, Keys.Down);
            AttackHitbox atk = lvl2.Entities.FindFirst<AttackHitbox>();
            Check("Pogo: acertar devolve a carga (novo golpe p/ baixo no mesmo voo)",
                atk != null && atk.Dir == Vector2.UnitY && !p2.OnGround(),
                atk == null ? "atk=null" : "Dir=" + atk.Dir + " ar=" + !p2.OnGround());
        }

        private static void TestDownUnlocked()
        {
            // o golpe p/ baixo aereo NAO trava: player segue no estado Normal e continua caindo
            var (lvl, p, combo, _) = Boot(withDummy: false);
            Step(lvl, Keys.C);                           // pula
            for (int i = 0; i < 8; i++) Step(lvl);
            Step(lvl, Keys.Down, Keys.A);
            float yFire = p.Y;
            bool stayedNormal = true, fell = false;
            for (int i = 0; i < 60 && combo.Attacking; i++)
            {
                Step(lvl, Keys.Down);
                if (p.StateMachine.State != 0) stayedNormal = false;
                if (p.Y > yFire) fell = true;
            }
            Check("Solto: golpe p/ baixo aereo nao trava (Normal + segue caindo)",
                stayedNormal && fell, "normal=" + stayedNormal + " caiu=" + fell);
        }
    }
}
