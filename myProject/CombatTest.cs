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

        private static bool Near(float a, float b, float tol) => Math.Abs(a - b) <= tol;

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
            TestInfiniteDummy();
            TestSpeeds();
            TestResetByTimeout();
            TestResetAfterFinisher();
            TestComboMoveReset();
            TestJumpResetsGroundCombo();
            TestMovementLock();
            TestFacingReset();
            TestAerialSolto();
            TestDoisSlashesNaQueda();
            TestDirectionalAttacks();
            TestVerticalSingle();
            TestRanges();
            TestDive();
            TestDiveLanding();
            TestDiveBounceRhythm();
            TestRecoil();
            TestDashNaoEInterrompido();

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
                // finito: os testes medem dano no HP (no jogo os bonecos sao infinitos)
                dummy = new TrainingDummy(new Vector2(60f, 160f), infinite: false);
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

        // roda ate a hitbox do golpe nascer (passa a anticipacao) e devolve
        private static AttackHitbox WaitHitbox(Scene s, params Keys[] hold)
        {
            for (int i = 0; i < 30; i++)
            {
                AttackHitbox atk = s.Entities.FindFirst<AttackHitbox>();
                if (atk != null)
                    return atk;
                Step(s, hold);
            }
            return null;
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

        private static void TestInfiniteDummy()
        {
            // boneco do jogo (padrao infinito): leva hit, pisca, mas nunca perde vida/morre
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(44f, 150f), PlayerSpriteMode.Madeline);
            MeleeCombo combo = new MeleeCombo();
            p.Add(combo);
            lvl.Add(p);
            TrainingDummy dummy = new TrainingDummy(new Vector2(60f, 160f)); // padrao: infinito
            lvl.Add(dummy);
            lvl.Begin(); lvl.BeforeUpdate();
            for (int i = 0; i < 60 && !p.OnGround(); i++) Step(lvl);

            int hits = 0;
            dummy.Health.OnDamaged = (int dmg) => hits++;

            Swing(lvl, combo);
            Check("Boneco infinito: registra o hit sem perder vida",
                hits == 1 && dummy.Health.Current == dummy.Health.Max,
                "hits=" + hits + " HP=" + dummy.Health.Current);

            for (int i = 0; i < 12; i++) Swing(lvl, combo); // muito dano acumulado
            Check("Boneco infinito: nunca morre nem some",
                hits > 1 && dummy.Collidable && dummy.Visible
                    && dummy.Health.Current == dummy.Health.Max,
                "hits=" + hits + " HP=" + dummy.Health.Current + " vivo=" + dummy.Collidable);
        }

        private static void TestSpeeds()
        {
            var (lvl, p, combo, dummy) = Boot();
            int f1 = Swing(lvl, combo);
            int f2 = Swing(lvl, combo);
            int f3 = Swing(lvl, combo);
            Check("Velocidades: 1o rapido < 2o intermediario < 3o lento",
                f1 < f2 && f2 < f3, "frames=" + f1 + "/" + f2 + "/" + f3);
            // frames esperados vem das constantes (+-1 por acumulo de float no timer)
            int e1 = (int)Math.Round(MeleeCombo.Duration[0] * 60f);
            int e2 = (int)Math.Round(MeleeCombo.Duration[1] * 60f);
            int e3 = (int)Math.Round(MeleeCombo.Duration[2] * 60f);
            Check("Duracoes batem com Duration[] (" + e1 + "/" + e2 + "/" + e3 + " frames +-1)",
                Math.Abs(f1 - e1) <= 1 && Math.Abs(f2 - e2) <= 1 && Math.Abs(f3 - e3) <= 1,
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

            Step(lvl, Keys.A); // logo apos o finisher: ainda em recuperacao
            Check("Finisher: recuperacao bloqueia o ataque seguinte",
                !combo.Attacking && combo.Recovering, "Recovering=" + combo.Recovering);

            int g = 0;
            while (combo.Recovering && g++ < 60) Step(lvl);      // espera a recuperacao
            Step(lvl, Keys.A);                                   // dentro da janela: recomeca
            Check("Apos a recuperacao o combo recomeca no estagio 1",
                combo.Attacking && combo.Stage == 0, "Stage=" + combo.Stage + " frames=" + g);
        }

        private static void TestComboMoveReset()
        {
            // ajuste curto p/ fechar o gap mantem o combo; andar demais quebra
            var (lvl, p, combo, _) = Boot(withDummy: false);
            Swing(lvl, combo);                                   // golpe 1
            float x0 = p.X;
            for (int i = 0; i < 8; i++) Step(lvl, Keys.Right);   // passo curto de ajuste
            Step(lvl, Keys.A);
            Check("Movimento: ajuste curto (< " + MeleeCombo.MoveAllowance + "px) mantem o combo",
                combo.Attacking && combo.Stage == 1,
                "andou=" + (p.X - x0) + " Stage=" + combo.Stage);
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);

            float x1 = p.X;
            for (int i = 0; i < 24; i++) Step(lvl, Keys.Right);  // corre p/ longe
            Step(lvl, Keys.A);
            Check("Movimento: andar alem da margem reseta o combo",
                combo.Attacking && combo.Stage == 0,
                "andou=" + (p.X - x1) + " Stage=" + combo.Stage);
        }

        private static void TestJumpResetsGroundCombo()
        {
            // sequencia comecada no solo nao sobrevive a um pulo
            var (lvl, p, combo, _) = Boot(withDummy: false);
            Swing(lvl, combo);                                   // golpe 1 no solo
            Check("Pulo: antes de pular a sequencia segue viva (proximo = estagio 2)",
                combo.NextStage == 1, "NextStage=" + combo.NextStage);

            Step(lvl, Keys.C);                                   // pula
            for (int i = 0; i < 4; i++) Step(lvl, Keys.C);       // ja fora do chao
            Check("Pulo: sair do solo reseta o combo",
                !p.OnGround() && combo.NextStage == 0,
                "ar=" + !p.OnGround() + " NextStage=" + combo.NextStage);

            Step(lvl, Keys.A);                                   // ataque aereo seguinte
            Check("Pulo: o golpe seguinte sai como estagio 1",
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

        private static void TestAerialSolto()
        {
            // no ar o golpe e um slash solto: nao trava o player, que continua caindo
            var (lvl, p, combo, dummy) = Boot(withDummy: false);

            for (int i = 0; i < 14; i++) Step(lvl, Keys.C);       // pulo maximo (var jump = 12f)
            for (int i = 0; i < 30 && p.Speed.Y < 0f; i++) Step(lvl);   // sobe ate o apice
            float y0 = p.Y;
            Step(lvl, Keys.A);                       // slash aereo
            bool locked = p.StateMachine.State == 11;
            for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);
            Check("Aereo: o slash nao trava o player (segue no estado Normal)",
                !locked && p.StateMachine.State == 0, "state=" + p.StateMachine.State);
            Check("Aereo: o player continua caindo durante o slash", p.Y > y0,
                "dY=" + (p.Y - y0));
        }

        private static void TestDirectionalAttacks()
        {
            // chao + segurando CIMA: golpe acima da cabeca
            var (lvl, p, combo, dummy) = Boot(withDummy: false);
            Step(lvl, Keys.Up, Keys.A);
            AttackHitbox atk = WaitHitbox(lvl, Keys.Up);
            Check("Direcional: chao + cima = golpe acima do player",
                atk != null && atk.Dir == -Vector2.UnitY && atk.Bottom <= p.Top + 0.01f,
                atk == null ? "atk=null" : "Dir=" + atk.Dir + " atkBottom=" + atk.Bottom + " pTop=" + p.Top);

            // ar + segurando BAIXO: mergulho com golpe abaixo dos pes
            var (lvl2, p2, combo2, _) = Boot(withDummy: false);
            for (int i = 0; i < 8; i++) Step(lvl2, Keys.C); // pulo alto
            Step(lvl2, Keys.Down, Keys.A);
            AttackHitbox atk2 = WaitHitbox(lvl2, Keys.Down);
            Check("Direcional: ar + baixo = golpe abaixo do player",
                atk2 != null && atk2.Dir == Vector2.UnitY && atk2.Top >= p2.Bottom - 0.01f,
                atk2 == null ? "atk=null" : "Dir=" + atk2.Dir + " atkTop=" + atk2.Top + " pBottom=" + p2.Bottom);

            // ar + segurando CIMA: golpe acima SEM travar (player segue no estado Normal)
            var (lvl3, p3, combo3, _) = Boot(withDummy: false);
            for (int i = 0; i < 8; i++) Step(lvl3, Keys.C);
            Step(lvl3, Keys.Up, Keys.A);
            AttackHitbox atk3 = WaitHitbox(lvl3, Keys.Up);
            Check("Direcional: ar + cima = golpe acima sem travar (Normal)",
                atk3 != null && atk3.Dir == -Vector2.UnitY && p3.StateMachine.State == 0,
                atk3 == null ? "atk=null" : "Dir=" + atk3.Dir + " state=" + p3.StateMachine.State);
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

        private static void TestRanges()
        {
            // alcance: 1o e 2o golpes iguais; so o 3o (finisher) ligeiramente maior
            var (lvl, p, combo, _) = Boot(withDummy: false);
            float[] w = new float[3];
            for (int s = 0; s < 3; s++)
            {
                Step(lvl, Keys.A);
                AttackHitbox atk = WaitHitbox(lvl);
                w[s] = (atk != null) ? atk.Width : -1f;
                for (int i = 0; i < 60 && combo.Attacking; i++) Step(lvl);
            }
            Check("Alcance: 1o == 2o e 3o ligeiramente maior",
                w[0] > 0 && w[0] == w[1] && w[2] > w[1],
                "larguras=" + w[0] + "/" + w[1] + "/" + w[2]);
        }

        private static void TestDive()
        {
            // mergulho (Gwendolyn): desce reto a DiveSpeed; acertar cancela e impulsiona
            // p/ cima com o Bounce (Hornet em Silksong: -140 + var jump + refill)
            var (lvl, p, combo, dummy) = Boot(playerX: 60f); // sobre o boneco
            for (int i = 0; i < 8; i++) Step(lvl, Keys.C);   // pulo alto
            Step(lvl, Keys.Down, Keys.A);
            Check("Mergulho: desce reto a DiveSpeed (240) travado",
                combo.Diving && p.Speed.Y == MeleeCombo.DiveSpeed && p.StateMachine.State == 11,
                "Diving=" + combo.Diving + " Speed.Y=" + p.Speed.Y);

            p.Dashes = 0; // p/ verificar que o acerto NAO restaura o dash
            bool hit = false;
            for (int i = 0; i < 90 && combo.Attacking; i++)
            {
                Step(lvl, Keys.Down);
                if (dummy.Health.Current < 30) { hit = true; break; }
            }
            Check("Mergulho: acerta o alvo e causa 5 (HP 30 -> 25)",
                hit && dummy.Health.Current == 25, "HP=" + dummy.Health.Current);
            Check("Mergulho: acerto cancela o golpe e impulsiona p/ cima (Bounce)",
                !combo.Attacking && p.Speed.Y <= -100f && p.StateMachine.State == 0,
                "Speed.Y=" + p.Speed.Y + " state=" + p.StateMachine.State);
            Check("Mergulho: o impulso NAO restaura o dash",
                p.Dashes == 0, "Dashes=" + p.Dashes);
        }

        private static void TestDiveLanding()
        {
            // sem alvo, o mergulho persiste ate pousar e termina no chao
            var (lvl, p, combo, _) = Boot(withDummy: false);
            for (int i = 0; i < 8; i++) Step(lvl, Keys.C);
            Step(lvl, Keys.Down, Keys.A);
            for (int i = 0; i < 120 && combo.Attacking; i++) Step(lvl);
            Step(lvl); // flush da remocao adiada da hitbox
            Check("Mergulho: termina ao pousar (controle volta no chao)",
                !combo.Attacking && p.OnGround() && p.StateMachine.State == 0
                    && lvl.Entities.FindFirst<AttackHitbox>() == null,
                "ground=" + p.OnGround() + " state=" + p.StateMachine.State);
        }

        private static void TestDashNaoEInterrompido()
        {
            // ataque nenhum pode cortar o dash aereo. O caso ruim era o aperto BUFFERIZADO:
            // slash aereo -> dash no meio do slash -> apertar baixo+ataque; ao terminar o
            // slash o buffer disparava o mergulho e matava o dash (state 2 -> 11, Speed zerado)
            // caso direto: apertar baixo+ataque no meio do dash, sem golpe em andamento
            var (l0, p0, c0, _) = Boot(withDummy: false);
            for (int i = 0; i < 14; i++) Step(l0, Keys.C);
            for (int i = 0; i < 30 && p0.Speed.Y < 0f; i++) Step(l0);
            Step(l0, Keys.Right, Keys.X);                              // dash aereo
            bool cortou = false;
            for (int i = 0; i < 12 && p0.StateMachine.State == 2; i++)
            {
                Step(l0, (i == 2) ? new[] { Keys.Down, Keys.A } : new[] { Keys.Down });
                if (c0.Diving || p0.StateMachine.State == 11) cortou = true;
            }
            Check("Dash: baixo+ataque no meio do dash nao corta o dash", !cortou,
                "dive=" + c0.Diving + " state=" + p0.StateMachine.State);

            var (lvl, p, combo, _) = Boot(withDummy: false);
            for (int i = 0; i < 14; i++) Step(lvl, Keys.C);            // pulo maximo
            for (int i = 0; i < 30 && p.Speed.Y < 0f; i++) Step(lvl);  // ate o apice
            Step(lvl, Keys.A);                                         // slash aereo
            Step(lvl, Keys.Right, Keys.X);                             // dash durante o slash

            bool dashInteiro = true, mergulhou = false;
            float peak = 0f;
            int frames = 0;
            for (int i = 0; i < 12 && p.StateMachine.State == 2; i++)  // enquanto o dash roda
            {
                Step(lvl, (i == 1) ? new[] { Keys.Down, Keys.A } : new[] { Keys.Down });
                frames++;
                peak = Math.Max(peak, p.Speed.X);
                if (combo.Diving) mergulhou = true;
                if (p.StateMachine.State == 11) dashInteiro = false;
            }
            Check("Dash: baixo+ataque bufferizado nao vira mergulho durante o dash",
                !mergulhou && dashInteiro, "dive=" + mergulhou + " state=" + p.StateMachine.State);
            Check("Dash: mantem a velocidade ate o fim (240 por ~11 frames)",
                Near(peak, 240f, 1f) && frames >= 9, "peak=" + peak + " frames=" + frames);

            // acabou o dash: o mergulho volta a sair normalmente
            Step(lvl);                                                 // frame neutro
            Step(lvl, Keys.Down, Keys.A);
            Check("Dash: acabado o dash, o mergulho dispara normalmente",
                combo.Diving && p.StateMachine.State == 11,
                "dive=" + combo.Diving + " state=" + p.StateMachine.State);
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
        }

        private static void TestDiveBounceRhythm()
        {
            // anti-bounce-infinito: depois do impulso, mergulho so no apice (quando cai)
            var (lvl, p, combo, dummy) = Boot(playerX: 60f);
            for (int i = 0; i < 8; i++) Step(lvl, Keys.C);
            Step(lvl, Keys.Down, Keys.A);                     // mergulho 1
            for (int i = 0; i < 90 && combo.Attacking; i++)
            {
                Step(lvl, Keys.Down);
                if (dummy.Health.Current < 30) break;         // bounce disparou
            }

            Step(lvl, Keys.Down, Keys.A);                     // aperto na SUBIDA
            Check("Bounce: apertar na subida nao dispara novo mergulho",
                !combo.Attacking && p.Speed.Y < 0f,
                "Attacking=" + combo.Attacking + " Speed.Y=" + p.Speed.Y);

            int g = 0;
            while (p.Speed.Y < 0f && g++ < 90) Step(lvl);     // espera o apice
            Step(lvl, Keys.Down, Keys.A);                     // comecou a cair: pode
            Check("Bounce: no inicio da queda o mergulho volta a disparar",
                combo.Attacking && combo.Diving, "Diving=" + combo.Diving);
        }

        private static void TestDoisSlashesNaQueda()
        {
            // requisito de design: do apice do pulo maximo ate o chao cabem DOIS slashes
            // (medida da queda: 18 frames; cada slash gasta ~8)
            var (lvl, p, combo, _) = Boot(withDummy: false);
            for (int i = 0; i < 14; i++) Step(lvl, Keys.C);              // pulo maximo (var jump = 12f)
            for (int i = 0; i < 30 && p.Speed.Y < 0f; i++) Step(lvl);    // sobe ate o apice

            // conta hitboxes nascidas: 1 por slash, e so nascem se o golpe passou a
            // anticipacao (encadear bufferizado nao deixa Attacking piscar entre os dois)
            int slashes = 0, queda = 0;
            AttackHitbox last = null;
            for (int i = 0; i < 120 && !p.OnGround(); i++)   // masha do apice ate pousar
            {
                Step(lvl, (i % 2 == 0) ? new[] { Keys.A } : new Keys[0]);
                queda++;
                AttackHitbox atk = lvl.Entities.FindFirst<AttackHitbox>();
                if (atk != null && atk != last) { slashes++; last = atk; }
            }
            Check("Aereo: cabem 2 slashes do apice ate o chao", slashes >= 2,
                "slashes=" + slashes + " queda=" + queda + "f");
            Check("Aereo: o slash nao encadeia combo (sempre estagio 1)",
                combo.Stage == 0 && combo.NextStage == 0,
                "Stage=" + combo.Stage + " NextStage=" + combo.NextStage);

            // mashar no ar nao sustenta o voo: o player pousa do mesmo jeito
            Check("Aereo: mashando no ar o player desce e pousa", p.OnGround(),
                "ground=" + p.OnGround());
        }
    }
}
