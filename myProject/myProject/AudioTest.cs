using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;
using myProject;

// Contrato do audio. Le os nomes de evento direto dos .cs do jogo (fonte, nao lista
// hardcoded) e exige que cada um resolva no banco — evento novo no codigo quebra a
// bateria ate ganhar alias. Precisa de Engine (ContentDirectory + subsistema de audio),
// entao abre janela por um instante.
namespace MonocleSmoke
{
    public static class AudioTest
    {
        private static int fails;

        public static int Run()
        {
            Console.WriteLine("== audio-test (banco de sons) ==");
            fails = 0;
            using (AudioTestGame game = new AudioTestGame())
                game.Run();
            Console.WriteLine(fails == 0 ? "== AUDIO OK ==" : ("== " + fails + " FALHA(S) =="));
            return fails == 0 ? 0 : 1;
        }

        internal static void Check(string name, bool ok, string detail)
        {
            Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + name + "  [" + detail + "]");
            if (!ok)
                fails++;
        }

        // Sobe da pasta de build ate achar os .cs do projeto.
        internal static string FindSourceDir()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "Player.cs")))
                    return dir;
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
            return null;
        }

        internal static List<string> EventsInSource(string dir)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var found = new List<string>();
            foreach (string file in Directory.GetFiles(dir, "*.cs"))
            {
                string name = Path.GetFileName(file);
                if (name == "Audio.cs" || name == "AudioTest.cs")
                    continue;   // a fachada e o proprio teste nao contam
                foreach (Match m in Regex.Matches(File.ReadAllText(file), @"""(event:/[^""]*)"""))
                    if (seen.Add(m.Groups[1].Value))
                        found.Add(m.Groups[1].Value);
            }
            found.Sort(StringComparer.Ordinal);
            return found;
        }
    }

    public class AudioTestGame : Engine
    {
        public AudioTestGame() : base(320, 180, 320, 180, "audio-test", false, false) { }

        protected override void LoadContent()
        {
            base.LoadContent();
            try
            {
                RunChecks();
            }
            catch (Exception e)
            {
                AudioTest.Check("rodar sem excecao", false, e.GetType().Name + ": " + e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Exit();
        }

        private static void RunChecks()
        {
            AudioTest.Check("antes do Load o audio esta indisponivel", !Audio.Available,
                "Available=" + Audio.Available);

            Audio.Load();

            AudioTest.Check("banco carregado (8 sons, 49 eventos)",
                Audio.SoundCount == 8 && Audio.EventCount == 49,
                "sons=" + Audio.SoundCount + " eventos=" + Audio.EventCount);

            // ---- contrato: todo evento emitido pelo codigo tem alias no banco ----
            string dir = AudioTest.FindSourceDir();
            AudioTest.Check("contrato: pasta de fontes localizada", dir != null, dir ?? "NAO ENCONTRADA");
            if (dir != null)
            {
                List<string> events = AudioTest.EventsInSource(dir);
                List<string> missing = new List<string>();
                foreach (string ev in events)
                    if (!Audio.HasEvent(ev))
                        missing.Add(ev);
                AudioTest.Check("contrato: os " + events.Count + " eventos do codigo estao no banco",
                    missing.Count == 0,
                    missing.Count == 0 ? "nenhum faltando" : ("faltam: " + string.Join(", ", missing)));
            }

            // ---- tolerancia: nada pode lancar, e nenhum handle pode ser null ----
            EventInstanceChecks();

            // ---- SoundSource ----
            SoundSourceChecks();

            Audio.Unload();
            AudioTest.Check("Unload zera o banco",
                Audio.SoundCount == 0 && Audio.EventCount == 0 && !Audio.Available,
                "sons=" + Audio.SoundCount + " eventos=" + Audio.EventCount);
        }

        private static void EventInstanceChecks()
        {
            var known = Audio.Play("event:/char/madeline/jump");
            AudioTest.Check("Play de evento conhecido devolve handle",
                known != null, "handle != null");

            var unknown = Audio.Play("event:/nao/existe/nada");
            AudioTest.Check("Play de evento desconhecido devolve handle MUDO, sem lancar",
                unknown != null && unknown.IsSilent,
                "null=" + (unknown == null) + " mudo=" + (unknown != null && unknown.IsSilent));

            var nullPath = Audio.Play(null);
            AudioTest.Check("Play(null) nao lanca", nullPath != null && nullPath.IsSilent,
                "mudo=" + (nullPath != null && nullPath.IsSilent));

            AudioTest.Check("HasEvent distingue conhecido de desconhecido",
                Audio.HasEvent("event:/char/madeline/jump") && !Audio.HasEvent("event:/nada"),
                "jump=true nada=false");

            // Pan pela camera: clampa em [-1, 1] mesmo muito fora da tela.
            Camera camera = new Camera(320, 180);
            camera.Position = Vector2.Zero;
            Audio.SetCamera(camera);
            var far = Audio.Play("event:/char/madeline/jump", new Vector2(99999f, 0f));
            bool panOk = far.IsSilent || (far.Instance.Pan >= -1f && far.Instance.Pan <= 1f);
            AudioTest.Check("Position clampa o pan em [-1,1]", panOk,
                far.IsSilent ? "mudo" : ("pan=" + far.Instance.Pan));

            Audio.Stop(known);
            Audio.Stop(far);
            AudioTest.Check("Stop libera o handle", !known.Playing, "Playing=" + known.Playing);

            Audio.Stop(null);
            AudioTest.Check("Stop(null) nao lanca", true, "sem excecao");
        }

        private static void SoundSourceChecks()
        {
            // Engine.Initialize chama base.Initialize() — que dispara o LoadContent — ANTES
            // de Tracker.Initialize()/Pooler. Entao aqui dentro eles ainda nao existem, e
            // montar uma Scene sem eles quebra. Os outros harnesses graficos nao esbarram
            // nisso porque nao criam Scene.
            Tracker.Initialize();
            if (Engine.Pooler == null)
                typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler());

            Scene scene = new Scene();
            Entity entity = new Entity(new Vector2(40f, 20f));
            SoundSource source = new SoundSource();
            entity.Add(source);
            scene.Add(entity);
            scene.Entities.UpdateLists();

            source.Play("event:/char/madeline/landing");
            AudioTest.Check("SoundSource.Play registra o evento e marca Playing",
                source.Playing && source.EventName == "event:/char/madeline/landing",
                "Playing=" + source.Playing + " EventName=" + source.EventName);

            source.Param("volume", 0.5f);
            AudioTest.Check("SoundSource.Param nao lanca", true, "sem excecao");

            // Pause/Resume existem no Celeste e sao suportados pelo SoundEffectInstance —
            // nao dependem de FMOD.
            source.Pause();
            source.Resume();
            AudioTest.Check("SoundSource.Pause/Resume nao lancam e mantem o Playing",
                source.Playing, "Playing=" + source.Playing);

            source.Stop(false);
            AudioTest.Check("SoundSource.Stop limpa o Playing", !source.Playing,
                "Playing=" + source.Playing);

            source.Play("event:/char/madeline/jump");
            scene.Remove(entity);
            scene.Entities.UpdateLists();
            AudioTest.Check("SoundSource para quando a entidade sai da cena",
                !source.Playing, "Playing=" + source.Playing);
        }
    }
}
