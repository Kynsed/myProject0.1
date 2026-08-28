using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace myProject
{
    // Audio do jogo. A API tem a FORMA do Audio do Celeste — os mesmos Play/Loop/Position/
    // Stop com nome de evento em string ("event:/char/madeline/jump") — para o codigo
    // portado (Player, SoundSource) continuar intocado.
    //
    // NOTE: a implementacao NAO e port. O Audio do Celeste sao 771 linhas envolvendo FMOD
    // Studio (banks, buses, VCAs, snapshots, listener 3D) e o celeste_source nao traz os
    // banks — so o C# que os carrega. Aqui e SoundEffect do MonoGame, com um banco em
    // texto (Content/Audio/sounds.txt) mapeando evento -> som.
    //
    // Tudo tolera nao haver dispositivo de audio: Available fica false, os handles saem
    // mudos e nada lanca. Maquina sem placa (ou CI) roda igual.
    public static class Audio
    {
        public const string BankPath = "Audio/sounds.txt";

        public static bool Available { get; private set; }
        public static bool MusicUnderwater;

        // Camera usada p/ o pan estereo (o Celeste faz isso pelo listener 3D do FMOD).
        public static Camera Camera;

        private static readonly Dictionary<string, SoundDef> sounds =
            new Dictionary<string, SoundDef>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> events =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<EventInstance> live = new List<EventInstance>();

        private class SoundDef
        {
            public SoundEffect Effect;
            public float Volume = 1f;
            public string File;
        }

        public static int SoundCount { get { return sounds.Count; } }
        public static int EventCount { get { return events.Count; } }

        public static void Load()
        {
            Unload();

            string bank = Path.Combine(Engine.ContentDirectory, BankPath);
            if (!File.Exists(bank))
            {
                Console.WriteLine("Audio: " + bank + " nao existe. Jogo mudo.");
                return;
            }

            string dir = Path.GetDirectoryName(bank);
            List<string> problems = new List<string>();

            foreach (string raw in File.ReadAllLines(bank))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//"))
                    continue;

                string[] p = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (p[0] == "sound")
                {
                    // sound <nome> <arquivo> [volume]
                    SoundDef def = new SoundDef();
                    def.File = p[2];
                    if (p.Length > 3)
                        def.Volume = float.Parse(p[3], CultureInfo.InvariantCulture);

                    string file = Path.Combine(dir, p[2]);
                    if (!File.Exists(file))
                    {
                        problems.Add("som " + p[1] + " -> " + p[2] + " (arquivo nao existe)");
                        continue;
                    }

                    try
                    {
                        using (FileStream stream = File.OpenRead(file))
                            def.Effect = SoundEffect.FromStream(stream);
                    }
                    catch (Exception e)
                    {
                        // Sem placa de som o MonoGame lanca aqui. Segue mudo.
                        problems.Add("som " + p[1] + " nao carregou (" + e.GetType().Name + ")");
                        continue;
                    }

                    sounds[p[1]] = def;
                }
                else if (p[0] == "alias")
                {
                    // alias <evento> <nome do som>
                    events[p[1]] = p[2];
                }
                else
                {
                    problems.Add("diretiva desconhecida: " + p[0]);
                }
            }

            foreach (KeyValuePair<string, string> pair in events)
                if (!sounds.ContainsKey(pair.Value))
                    problems.Add("evento " + pair.Key + " -> som " + pair.Value + " (nao declarado)");

            Available = sounds.Count > 0;
            Console.WriteLine("Audio: " + sounds.Count + " sons, " + events.Count + " eventos"
                + (Available ? "." : " — sem dispositivo, jogo mudo."));
            foreach (string problem in problems)
                Console.WriteLine("Audio: " + BankPath + " - " + problem);
        }

        public static void Unload()
        {
            foreach (EventInstance instance in live)
                instance.release();
            live.Clear();

            foreach (SoundDef def in sounds.Values)
                if (def.Effect != null)
                    def.Effect.Dispose();
            sounds.Clear();
            events.Clear();
            Available = false;
        }

        public static bool HasEvent(string path)
        {
            return path != null && events.ContainsKey(path);
        }

        #region API com a forma do Celeste

        public static EventInstance Play(string path)
        {
            return Start(path, null, false);
        }

        public static EventInstance Play(string path, Vector2 position)
        {
            return Start(path, position, false);
        }

        public static EventInstance Play(string path, string param, float value)
        {
            EventInstance instance = Start(path, null, false);
            instance.setParameterValue(param, value);
            return instance;
        }

        public static EventInstance Play(string path, Vector2 position, string param, float value)
        {
            EventInstance instance = Start(path, position, false);
            instance.setParameterValue(param, value);
            return instance;
        }

        public static EventInstance Play(string path, Vector2 position, string param, float value,
            string param2, float value2)
        {
            EventInstance instance = Start(path, position, false);
            instance.setParameterValue(param, value);
            instance.setParameterValue(param2, value2);
            return instance;
        }

        public static EventInstance Loop(string path)
        {
            return Start(path, null, true);
        }

        public static EventInstance Position(EventInstance instance, Vector2 position)
        {
            if (instance != null && instance.Instance != null)
                instance.Instance.Pan = PanFor(position);
            return instance;
        }

        public static void Stop(EventInstance instance, bool allowFadeOut = true)
        {
            if (instance == null)
                return;
            // NOTE: fade-out e coisa do FMOD; aqui para direto. O parametro fica na
            // assinatura porque o codigo portado passa ele.
            instance.release();
            live.Remove(instance);
        }

        public static void Apply(bool immediate)
        {
        }

        public static void SetCamera(Camera camera)
        {
            Camera = camera;
        }

        #endregion

        private static EventInstance Start(string path, Vector2? position, bool loop)
        {
            EventInstance handle = new EventInstance();
            handle.EventPath = path;

            string soundName;
            SoundDef def;
            if (!Available || path == null
                || !events.TryGetValue(path, out soundName)
                || !sounds.TryGetValue(soundName, out def)
                || def.Effect == null)
            {
                return handle;   // handle mudo, mas valido
            }

            try
            {
                SoundEffectInstance instance = def.Effect.CreateInstance();
                instance.Volume = def.Volume;
                instance.IsLooped = loop;
                if (position != null)
                    instance.Pan = PanFor(position.Value);
                instance.Play();

                handle.Instance = instance;
                live.Add(handle);
                Prune();
            }
            catch (Exception)
            {
                // dispositivo sumiu no meio do jogo: segue mudo
            }

            return handle;
        }

        // Pan pela posicao na tela: -1 na borda esquerda, +1 na direita.
        private static float PanFor(Vector2 position)
        {
            if (Camera == null)
                return 0f;
            float half = 160f;   // metade da largura de 320
            float pan = (position.X - (Camera.Left + half)) / half;
            return (pan < -1f) ? -1f : ((pan > 1f) ? 1f : pan);
        }

        // Solta os oneshots que ja terminaram, p/ nao acumular SoundEffectInstance.
        private static void Prune()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                EventInstance handle = live[i];
                if (handle.Instance != null && handle.Instance.State == SoundState.Stopped
                    && !handle.Instance.IsLooped)
                {
                    handle.release();
                    live.RemoveAt(i);
                }
            }
        }
    }
}
