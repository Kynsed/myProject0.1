using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using myProject;

// Sistema de audio (jogo proprio): WAVs soltos em Content/Audio + banco em texto.
// Headless nao ha dispositivo de audio, entao o que se mede aqui e o CONTRATO: todo
// nome de som que o codigo pede resolve, os arquivos existem, e tocar sem dispositivo
// (ou som nao mapeado) nao quebra nada.
namespace MonocleSmoke
{
    public static class AudioTest
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok) fails++;
        }

        public static int Run()
        {
            Console.WriteLine("== audio (headless) ==");
            Tracker.Initialize();
            MInput.Initialize();
            Input.Initialize();
            PlayGame.InitTags();
            typeof(Engine).GetProperty("RawDeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("DeltaTime").SetValue(null, 1f / 60f);
            typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());
            Audio.Load();

            Console.WriteLine("-- banco --");
            TestBanco();
            TestContratoDoCodigo();

            Console.WriteLine("-- arquivos --");
            TestArquivos();

            Console.WriteLine("-- robustez --");
            TestSemDispositivo();
            TestSoundSource();

            Console.WriteLine(fails == 0 ? "== AUDIO OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails;
        }

        private static string ContentDir => AppContext.BaseDirectory + "Content";

        private static void TestBanco()
        {
            Check("Banco: sons do jogo carregam",
                Audio.Resolve("jump") == "jump" && Audio.Resolve("attack") == "attack"
                    && Audio.Resolve("hit") == "hit",
                "jump/attack/hit ok");
            Check("Banco: evento herdado do port cai no alias",
                Audio.Resolve("event:/char/madeline/jump") == "jump"
                    && Audio.Resolve("event:/char/madeline/landing") == "land",
                "jump=" + Audio.Resolve("event:/char/madeline/jump"));
            Check("Banco: som continuo marcado como loop",
                Audio.IsLoop("slide") && Audio.IsLoop("event:/char/madeline/wallslide")
                    && !Audio.IsLoop("jump"),
                "slide=loop, jump=one-shot");
            Check("Banco: nome desconhecido nao resolve (nao inventa som)",
                Audio.Resolve("event:/nao/existe") == null, "null ok");
        }

        // Todo "event:/..." citado no codigo precisa de destino no banco.
        private static void TestContratoDoCodigo()
        {
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            if (!Directory.Exists(root) || !File.Exists(Path.Combine(root, "Player.cs")))
            {
                Console.WriteLine("  SKIP  contrato: fontes nao estao ao lado do binario");
                return;
            }

            var events = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(root, "*.cs"))
            {
                if (file.EndsWith("Test.cs", StringComparison.Ordinal))
                    continue;   // os proprios harness citam eventos de exemplo
                foreach (Match m in Regex.Matches(File.ReadAllText(file), @"""(event:/[^""]+)"""))
                    events.Add(m.Groups[1].Value);
            }

            var orfaos = new List<string>();
            foreach (string e in events)
                if (Audio.Resolve(e) == null)
                    orfaos.Add(e);

            Check("Contrato: todo evento citado no codigo tem destino no banco",
                orfaos.Count == 0,
                "eventos=" + events.Count + " sem destino=" + string.Join(",", orfaos));
        }

        private static void TestArquivos()
        {
            string dir = Path.Combine(ContentDir, Audio.AudioDir);
            if (!Directory.Exists(dir))
            {
                Check("Arquivos: Content/Audio existe", false, dir);
                return;
            }
            string[] wavs = Directory.GetFiles(dir, "*.wav");
            Check("Arquivos: WAVs copiados p/ o output", wavs.Length > 0, "arquivos=" + wavs.Length);

            // todo "sound" do banco precisa do arquivo
            var faltando = new List<string>();
            foreach (string line in File.ReadAllLines(Path.Combine(dir, "sounds.txt")))
            {
                string t = line.Trim();
                if (!t.StartsWith("sound "))
                    continue;
                string[] p = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (!File.Exists(Path.Combine(dir, p[2])))
                    faltando.Add(p[1]);
            }
            Check("Arquivos: todo som do banco aponta p/ um arquivo que existe",
                faltando.Count == 0, "sem arquivo=" + string.Join(",", faltando));
        }

        // Sem dispositivo de audio (headless) tudo tem que continuar rodando.
        private static void TestSemDispositivo()
        {
            bool crashou = false;
            SoundHandle h = null;
            try
            {
                h = Audio.Play("jump");
                Audio.Play("event:/char/madeline/landing", new Vector2(10f, 20f));
                Audio.Play("nao/existe/nada");
                Audio.Position(h, new Vector2(1f, 2f));
                Audio.Stop(h);
                Audio.Loop("slide").Stop();
            }
            catch (Exception e)
            {
                crashou = true;
                Console.WriteLine("      excecao: " + e.GetType().Name + " " + e.Message);
            }
            Check("Robustez: tocar/parar sem dispositivo nao quebra", !crashou,
                "Available=" + Audio.Available);
            Check("Robustez: handle de som nao mapeado e valido e mudo",
                h != null && !Audio.Play("nao/existe/nada").Playing, "handle mudo ok");
        }

        private static void TestSoundSource()
        {
            Level lvl = new Level { Bounds = new Rectangle(0, 0, 320, 180) };
            lvl.Add(new Solid(new Vector2(0f, 160f), 320f, 20f, false));
            Player p = new Player(new Vector2(60f, 150f), PlayerSpriteMode.Madeline);
            lvl.Add(p);
            lvl.Begin(); lvl.BeforeUpdate();

            var src = new SoundSource();
            p.Add(src);
            bool crashou = false;
            try
            {
                src.Play("event:/char/madeline/wallslide");
                for (int i = 0; i < 10; i++) { lvl.BeforeUpdate(); lvl.Update(); lvl.AfterUpdate(); }
                src.Stop();
            }
            catch (Exception e)
            {
                crashou = true;
                Console.WriteLine("      excecao: " + e.GetType().Name);
            }
            Check("SoundSource: ciclo play/update/stop numa entidade nao quebra", !crashou,
                "EventName=" + src.EventName);
            Check("SoundSource: parar deixa de tocar", !src.Playing, "Playing=" + src.Playing);
        }
    }
}
