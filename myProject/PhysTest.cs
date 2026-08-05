using System;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Fase D — validacao headless da fisica de movimento (Actor + Solid).
// Sem janela/GPU: MoveH/MoveV recebem pixels, nao dependem de DeltaTime.
namespace MonocleSmoke
{
    public static class PhysTest
    {
        private class TestActor : Actor
        {
            public TestActor(Vector2 position) : base(position)
            {
                Collider = new Hitbox(8f, 8f, 0f, 0f);
            }
        }

        private static int fails;

        private static void Check(string name, bool ok, string detail = "")
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + (detail.Length > 0 ? "  [" + detail + "]" : ""));
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== Fase D: fisica de movimento (headless) ==");
            Tracker.Initialize();
            Scene scene = new Scene();

            Solid floor = new Solid(new Vector2(0f, 100f), 320f, 8f, false);
            scene.Add(floor);
            Solid wall = new Solid(new Vector2(80f, 0f), 8f, 100f, false);
            scene.Add(wall);
            scene.BeforeUpdate();

            // 1. Queda e pouso exato sobre o Solid.
            TestActor a = new TestActor(new Vector2(40f, 0f));
            scene.Add(a);
            scene.BeforeUpdate();
            bool landed = a.MoveV(500f);
            Check("queda colide com o chao", landed);
            Check("pousa exatamente (bottom no topo do solid)", a.Bottom == 100f, "Bottom=" + a.Bottom);

            // 2. Sub-pixel: arredondamento bankers (MidpointRounding.ToEven).
            TestActor b = new TestActor(new Vector2(40f, 0f));
            scene.Add(b);
            scene.BeforeUpdate();
            bool moved05 = b.MoveV(0.5f);          // 0.5 -> ToEven -> 0
            Check("0.5px nao move (banker's rounding)", !moved05 && b.Y == 0f, "Y=" + b.Y);
            b.MoveV(0.5f);                          // acumula 1.0 -> move 1px (retorno=colisao, nao=movimento)
            Check("acumulo sub-pixel move 1px em 1.0", b.Y == 1f, "Y=" + b.Y);

            // 3. Parede: MoveH para ao colidir, posicao adjacente exata.
            TestActor c = new TestActor(new Vector2(40f, 50f));
            scene.Add(c);
            scene.BeforeUpdate();
            bool blocked = c.MoveH(500f);          // parede em x=[80,88], actor 8 largura
            Check("MoveH colide com a parede", blocked);
            Check("para adjacente a parede (Right==80)", c.Right == 80f, "Right=" + c.Right);

            // 4. Solid carrega rider (lift). Actor pousado, chao sobe.
            TestActor r = new TestActor(new Vector2(160f, 0f));
            scene.Add(r);
            scene.BeforeUpdate();
            r.MoveV(500f);                          // pousa em Bottom=100 (Y=92)
            float beforeY = r.Y;
            bool riding = r.IsRiding(floor);
            Check("actor reconhecido como rider do solid", riding);
            floor.MoveV(-10f);                      // chao sobe 10px, deve carregar o rider
            Check("rider carregado junto com o solid", r.Y == beforeY - 10f, "Y=" + r.Y + " (esperado " + (beforeY - 10f) + ")");

            Console.WriteLine(fails == 0 ? "== TODOS OS TESTES PASSARAM ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }
    }
}
