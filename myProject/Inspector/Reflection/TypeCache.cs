using System;
using System.Collections.Generic;
using System.Reflection;

namespace myProject.Inspector.Reflection
{
    // Metadados refletidos de um tipo, montados uma unica vez.
    public sealed class InspectedType
    {
        public readonly Type Type;
        public readonly string DisplayName;
        public readonly InspectedMember[] Members;

        internal InspectedType(Type type, InspectedMember[] members)
        {
            Type = type;
            DisplayName = Prettify(type);
            Members = members;
        }

        private static string Prettify(Type t)
        {
            if (!t.IsGenericType)
                return t.Name;
            string name = t.Name;
            int tick = name.IndexOf('`');
            if (tick > 0)
                name = name.Substring(0, tick);
            var args = t.GetGenericArguments();
            var parts = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                parts[i] = Prettify(args[i]);
            return name + "<" + string.Join(", ", parts) + ">";
        }
    }

    // Cache global de reflexao. Chave = Type; nunca reflete o mesmo tipo duas vezes.
    public static class TypeCache
    {
        private static readonly Dictionary<Type, InspectedType> cache = new Dictionary<Type, InspectedType>();

        // Tipos que sao editados por drawer proprio e nao devem ser abertos por reflexao.
        private static readonly HashSet<Type> atomic = new HashSet<Type>
        {
            typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal), typeof(char),
            typeof(Microsoft.Xna.Framework.Vector2), typeof(Microsoft.Xna.Framework.Vector3),
            typeof(Microsoft.Xna.Framework.Color), typeof(Microsoft.Xna.Framework.Rectangle),
            typeof(Microsoft.Xna.Framework.Point)
        };

        public static int CachedTypeCount => cache.Count;

        public static bool IsAtomic(Type t) => t == null || t.IsEnum || atomic.Contains(t);

        public static InspectedType Get(Type type)
        {
            if (cache.TryGetValue(type, out var cached))
                return cached;

            var members = new List<InspectedMember>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // da classe base para a derivada: campos herdados aparecem primeiro, como na Unity
            var chain = new List<Type>();
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                chain.Add(t);
            chain.Reverse();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (Type t in chain)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (!seen.Add(f.Name))
                        continue;
                    var m = InspectedMember.TryCreate(f);
                    if (m != null)
                        members.Add(m);
                }
                foreach (var p in t.GetProperties(flags))
                {
                    if (!seen.Add(p.Name))
                        continue;
                    var m = InspectedMember.TryCreate(p);
                    if (m != null)
                        members.Add(m);
                }
            }

            var result = new InspectedType(type, members.ToArray());
            cache[type] = result;
            return result;
        }

        public static void Clear() => cache.Clear();
    }
}
