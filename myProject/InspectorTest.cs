using System;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;
using myProject.Inspector;
using myProject.Inspector.Reflection;
using myProject.Inspector.UI;

// Teste headless das camadas nao-graficas do inspector: reflexao+cache, atributos,
// leitura/escrita de valores, structs aninhadas e undo/redo.
namespace MonocleSmoke
{
    public static class InspectorTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        // Alvo de teste exercitando todos os atributos suportados.
        private class Dummy
        {
            [Header("Movimento")]
            [Tooltip("Velocidade maxima")]
            public float MaxSpeed = 90f;

            [Range(0f, 1f)]
            public float Friction = 0.5f;

            [SerializeField]
            private int hiddenButSerialized = 7;

            [HideInInspector]
            public int NeverShown = 99;

            public bool Enabled = true;
            public string Name = "dummy";
            public Vector2 Position;
            public Facings Facing = Facings.Right;
            public int ReadOnlyValue => 42;   // propriedade sem setter

            private int trulyPrivate = 1;     // sem [SerializeField]: nao aparece

            public int PeekPrivate() => hiddenButSerialized;
            public int PeekTrulyPrivate() => trulyPrivate;
        }

        public static int Run()
        {
            Console.WriteLine("== inspector: reflexao, atributos e undo (headless) ==");
            TypeCache.Clear();

            TestMemberDiscovery();
            TestAttributes();
            TestReadWrite();
            TestCache();
            TestUndoRedo();
            TestSelectionBaseline();
            TestLabels();

            Console.WriteLine(fails == 0 ? "== INSPECTOR OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        private static InspectedMember Find(InspectedType t, string name)
            => Array.Find(t.Members, m => m.Name == name);

        private static void TestMemberDiscovery()
        {
            var t = TypeCache.Get(typeof(Dummy));

            Check("Campo publico aparece", Find(t, "MaxSpeed") != null, "MaxSpeed");
            Check("[SerializeField] expoe campo privado",
                Find(t, "hiddenButSerialized") != null, "hiddenButSerialized");
            Check("[HideInInspector] esconde o campo",
                Find(t, "NeverShown") == null, "NeverShown");
            Check("Privado sem atributo nao aparece",
                Find(t, "trulyPrivate") == null, "trulyPrivate");
            Check("Propriedade sem setter aparece como somente-leitura",
                Find(t, "ReadOnlyValue") != null && !Find(t, "ReadOnlyValue").CanWrite,
                "ReadOnlyValue.CanWrite=" + (Find(t, "ReadOnlyValue")?.CanWrite));
        }

        private static void TestAttributes()
        {
            var t = TypeCache.Get(typeof(Dummy));
            var speed = Find(t, "MaxSpeed");
            var friction = Find(t, "Friction");

            Check("[Header] lido", speed.Header == "Movimento", "Header=" + speed.Header);
            Check("[Tooltip] lido", speed.Tooltip == "Velocidade maxima", "Tooltip=" + speed.Tooltip);
            Check("[Range] lido", friction.Range != null && friction.Range.Min == 0f && friction.Range.Max == 1f,
                friction.Range == null ? "null" : friction.Range.Min + ".." + friction.Range.Max);
        }

        private static void TestReadWrite()
        {
            var d = new Dummy();
            var t = TypeCache.Get(typeof(Dummy));

            var speed = Find(t, "MaxSpeed");
            Check("Leitura de float", (float)speed.GetValue(d) == 90f, "MaxSpeed=" + speed.GetValue(d));
            speed.TrySetValue(d, 123f);
            Check("Escrita de float", d.MaxSpeed == 123f, "MaxSpeed=" + d.MaxSpeed);

            var priv = Find(t, "hiddenButSerialized");
            priv.TrySetValue(d, 55);
            Check("Escrita em campo privado [SerializeField]", d.PeekPrivate() == 55,
                "valor=" + d.PeekPrivate());

            var name = Find(t, "Name");
            name.TrySetValue(d, "editado");
            Check("Escrita de string", d.Name == "editado", "Name=" + d.Name);

            var facing = Find(t, "Facing");
            facing.TrySetValue(d, Facings.Left);
            Check("Escrita de enum", d.Facing == Facings.Left, "Facing=" + d.Facing);

            var ro = Find(t, "ReadOnlyValue");
            Check("Somente-leitura recusa escrita", !ro.TrySetValue(d, 7), "TrySetValue=false");

            // struct aninhada: escreve via copia boxed e devolve ao pai
            var posMember = Find(t, "Position");
            var vecType = TypeCache.Get(typeof(Vector2));
            object boxed = posMember.GetValue(d);
            var xMember = Find(vecType, "X");
            bool wroteX = xMember.TrySetValue(boxed, 33f);
            posMember.TrySetValue(d, boxed);
            Check("Struct aninhada: edita X via copia boxed",
                wroteX && d.Position.X == 33f, "Position=" + d.Position);
        }

        private static void TestCache()
        {
            TypeCache.Clear();
            var a = TypeCache.Get(typeof(Dummy));
            int afterFirst = TypeCache.CachedTypeCount;
            var b = TypeCache.Get(typeof(Dummy));
            Check("Cache devolve a mesma instancia (sem refletir de novo)",
                ReferenceEquals(a, b) && TypeCache.CachedTypeCount == afterFirst,
                "tipos em cache=" + TypeCache.CachedTypeCount);
            Check("Tipos atomicos nao sao expandidos por reflexao",
                TypeCache.IsAtomic(typeof(float)) && TypeCache.IsAtomic(typeof(Color))
                    && !TypeCache.IsAtomic(typeof(Dummy)), "float/Color atomicos");
        }

        private static void TestUndoRedo()
        {
            var d = new Dummy();
            var t = TypeCache.Get(typeof(Dummy));
            var speed = Find(t, "MaxSpeed");
            var undo = new UndoSystem();

            void Edit(float from, float to)
            {
                var cmd = new SetMemberCommand(d, speed, from, to);
                cmd.Apply();
                undo.Record(cmd);
            }

            Edit(90f, 100f);
            undo.BreakMerge();
            Edit(100f, 200f);
            Check("Duas edicoes separadas viram 2 entradas", undo.UndoCount == 2,
                "UndoCount=" + undo.UndoCount);

            undo.Undo();
            Check("Undo restaura o valor anterior", d.MaxSpeed == 100f, "MaxSpeed=" + d.MaxSpeed);
            undo.Undo();
            Check("Undo encadeado volta ao original", d.MaxSpeed == 90f, "MaxSpeed=" + d.MaxSpeed);
            Check("Pilha de undo esvaziou", !undo.CanUndo && undo.RedoCount == 2,
                "undo=" + undo.UndoCount + " redo=" + undo.RedoCount);

            undo.Redo();
            Check("Redo reaplica", d.MaxSpeed == 100f, "MaxSpeed=" + d.MaxSpeed);
            undo.Redo();
            Check("Redo encadeado chega ao ultimo valor", d.MaxSpeed == 200f, "MaxSpeed=" + d.MaxSpeed);

            // arraste: varias edicoes seguidas do mesmo campo viram uma entrada so
            var drag = new UndoSystem();
            float prev = 0f;
            for (int i = 1; i <= 10; i++)
            {
                var cmd = new SetMemberCommand(d, speed, prev, i);
                cmd.Apply();
                drag.Record(cmd);
                prev = i;
            }
            Check("Arraste funde em 1 entrada de undo", drag.UndoCount == 1,
                "UndoCount=" + drag.UndoCount);
            drag.Undo();
            Check("Undo do arraste volta ao valor pre-arraste", d.MaxSpeed == 0f,
                "MaxSpeed=" + d.MaxSpeed);

            // nova edicao limpa o redo
            undo.Record(new SetMemberCommand(d, speed, 200f, 5f));
            Check("Editar apos undo limpa a pilha de redo", undo.RedoCount == 0,
                "RedoCount=" + undo.RedoCount);
        }

        private static void TestSelectionBaseline()
        {
            var d = new Dummy();
            var sel = new Selection();
            bool fired = false;
            sel.Changed += _ => fired = true;
            sel.Select(d);

            Check("Evento de mudanca de selecao dispara", fired && sel.HasSelection, "fired=" + fired);
            Check("Baseline capturada na selecao",
                sel.GetBaseline("MaxSpeed") is float f && f == 90f, "baseline=" + sel.GetBaseline("MaxSpeed"));
            Check("Campo intacto nao marca como modificado",
                !sel.IsModified("MaxSpeed", 90f), "IsModified(90)=false");
            Check("Campo alterado marca como modificado",
                sel.IsModified("MaxSpeed", 120f), "IsModified(120)=true");

            sel.Select(d); // mesma referencia: nao redispara
            fired = false;
            sel.Select(new Dummy());
            Check("Selecionar outro objeto dispara o evento", fired, "fired=" + fired);
        }

        private static void TestLabels()
        {
            Check("Rotulo de camelCase", InspectedMember.Prettify("maxRunSpeed") == "Max Run Speed",
                InspectedMember.Prettify("maxRunSpeed"));
            Check("Rotulo remove prefixo _", InspectedMember.Prettify("_speed") == "Speed",
                InspectedMember.Prettify("_speed"));
            Check("Rotulo separa digitos", InspectedMember.Prettify("player2Hp") == "Player 2 Hp",
                InspectedMember.Prettify("player2Hp"));
            Check("Fonte mede/corta texto",
                GuiFont.Measure("abc") == 18 && GuiFont.Fit("abcdefgh", 24).Length <= 4,
                "medida=" + GuiFont.Measure("abc") + " corte=" + GuiFont.Fit("abcdefgh", 24));
        }
    }
}
