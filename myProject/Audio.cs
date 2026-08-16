using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace myProject
{
    // Audio do jogo (nao e port): SoundEffect nativo do MonoGame, sem FMOD e sem
    // dependencia nova. WAVs soltos em Content/Audio + um banco em texto que da nome
    // aos sons e mapeia os eventos herdados do port (ver Content/Audio/sounds.txt).
    //
    // Tudo aqui tolera nao ter dispositivo de audio: harness headless e maquina sem
    // placa continuam rodando, so nao sai som (Available == false).
    public static class Audio
    {
        public const string AudioDir = "Audio";

        public static float MasterVolume = 1f;
        public static bool MusicUnderwater;      // lido pelo codigo herdado (Water)
        public static bool Available { get; private set; }

        private static readonly Dictionary<string, Entry> sounds = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> loops = new HashSet<string>(StringComparer.Ordinal);
        private static readonly List<SoundHandle> playing = new List<SoundHandle>();

        private class Entry
        {
            public string Name;
            public string File;
            public float Volume = 1f;
            public SoundEffect Effect;
        }

        // Engine.ContentDirectory depende do Engine existir; nos harness headless nao ha.
        public static string ContentRoot =>
            Engine.Instance != null ? Engine.ContentDirectory
                                    : Path.Combine(AppContext.BaseDirectory, "Content");

        // Le o banco (texto) e, se houver dispositivo, carrega os WAVs.
        public static void Load()
        {
            string dir = Path.Combine(ContentRoot, AudioDir);
            string bank = Path.Combine(dir, "sounds.txt");
            if (!File.Exists(bank))
                return;

            sounds.Clear();
            aliases.Clear();
            loops.Clear();

            foreach (string raw in File.ReadAllLines(bank))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//"))
                    continue;
                string[] p = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                switch (p[0])
                {
                    case "sound" when p.Length >= 3:
                        sounds[p[1]] = new Entry
                        {
                            Name = p[1],
                            File = p[2],
                            Volume = p.Length >= 4 ? float.Parse(p[3], CultureInfo.InvariantCulture) : 1f
                        };
                        break;
                    case "alias" when p.Length >= 3:
                        aliases[p[1]] = p[2];
                        break;
                    case "loop" when p.Length >= 2:
                        loops.Add(p[1]);
                        break;
                }
            }

            foreach (Entry e in sounds.Values)
            {
                string file = Path.Combine(dir, e.File);
                if (!File.Exists(file))
                    continue;
                try
                {
                    using (FileStream fs = File.OpenRead(file))
                        e.Effect = SoundEffect.FromStream(fs);
                    Available = true;
                }
                catch (Exception)
                {
                    e.Effect = null;   // sem dispositivo de audio: segue sem som
                }
            }
        }

        public static void Unload()
        {
            foreach (SoundHandle h in playing.ToArray())
                h.Stop();
            playing.Clear();
            foreach (Entry e in sounds.Values)
            {
                e.Effect?.Dispose();
                e.Effect = null;
            }
            Available = false;
        }

        // nome pedido -> som que existe de fato (segue a cadeia de alias)
        public static string Resolve(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            string cur = path;
            for (int hops = 0; hops < 8; hops++)
            {
                if (sounds.ContainsKey(cur))
                    return cur;
                if (!aliases.TryGetValue(cur, out string next))
                    return null;
                cur = next;
            }
            return null;
        }

        public static bool IsLoop(string path)
        {
            string name = Resolve(path);
            return name != null && loops.Contains(name);
        }

        public static SoundHandle Play(string path)
        {
            return Start(path, false, null);
        }

        public static SoundHandle Play(string path, Vector2 position)
        {
            return Start(path, false, position);
        }

        public static SoundHandle Play(string path, string param, float value)
        {
            return Start(path, false, null);
        }

        public static SoundHandle Play(string path, Vector2 position, string param, float value)
        {
            return Start(path, false, position);
        }

        public static SoundHandle Play(string path, Vector2 position, string param, float value, string param2, float value2)
        {
            return Start(path, false, position);
        }

        public static SoundHandle Loop(string path)
        {
            return Start(path, true, null);
        }

        public static SoundHandle Loop(string path, Vector2 position)
        {
            return Start(path, true, position);
        }

        private static SoundHandle Start(string path, bool loop, Vector2? position)
        {
            var handle = new SoundHandle(path);
            string name = Resolve(path);
            if (name == null || !sounds.TryGetValue(name, out Entry e) || e.Effect == null)
                return handle;   // som nao mapeado ou sem dispositivo: handle mudo

            SoundEffectInstance inst = e.Effect.CreateInstance();
            inst.IsLooped = loop || loops.Contains(name);
            inst.Volume = MathHelper.Clamp(e.Volume * MasterVolume, 0f, 1f);
            handle.Attach(inst, e.Volume);
            if (position != null)
                handle.SetPosition(position.Value);
            inst.Play();

            playing.Add(handle);
            if (playing.Count > 64)
                Cleanup();
            return handle;
        }

        private static void Cleanup()
        {
            for (int i = playing.Count - 1; i >= 0; i--)
                if (!playing[i].Playing)
                    playing.RemoveAt(i);
        }

        public static SoundHandle Position(SoundHandle handle, Vector2 position)
        {
            handle?.SetPosition(position);
            return handle;
        }

        public static void Stop(SoundHandle handle, bool allowFadeOut = true)
        {
            handle?.Stop();
        }

        public static void SetMusic(string path, bool startPlaying = true, bool allowFadeOut = true) { }
        public static void SetAmbience(string path, bool startPlaying = true) { }
        public static void Apply(bool immediate) { }
        public static void PauseMusic() { }
        public static void ResumeMusic() { }
        public static void SetMusicParam(string path, float value) { }
        public static void SetParameter(SoundHandle handle, string param, float value) { }
        public static void BusPaused(string path, bool paused) { }
        public static void SetAltMusic(string path) { }
        public static void EndSnapshot(object snapshot) { }
    }
}
