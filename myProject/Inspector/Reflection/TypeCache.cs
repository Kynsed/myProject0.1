using System;
using System.Collections.Generic;
using System.Reflection;

namespace myProject.Inspector.Reflection
{
    // Um bloco tematico de membros dentro de um tipo.
    public sealed class MemberGroup
    {
        public readonly string Name;
        public readonly InspectedMember[] Members;
        internal MemberGroup(string name, InspectedMember[] members)
        {
            Name = name;
            Members = members;
        }
    }

    // Metadados refletidos de um tipo, montados uma unica vez.
    public sealed class InspectedType
    {
        public readonly Type Type;
        public readonly string DisplayName;
        public readonly InspectedMember[] Members; // ordem de declaracao (base -> derivada)
        public readonly MemberGroup[] Groups;      // mesma lista, particionada em blocos

        internal InspectedType(Type type, InspectedMember[] members, MemberGroup[] groups)
        {
            Type = type;
            DisplayName = Prettify(type);
            Members = members;
            Groups = groups;
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

            var arr = members.ToArray();
            var result = new InspectedType(type, arr, BuildGroups(arr));
            cache[type] = result;
            return result;
        }

        // Regra de atribuicao de bloco, em ordem de prioridade:
        //   1. [InspectorGroup("X")]      — intencao explicita, vale so p/ o membro
        //   2. [Header("X")]              — inicia um bloco que segue ate o proximo [Header]
        //   3. classificacao automatica   — heuristica por nome/tipo (codigo portado)
        private static MemberGroup[] BuildGroups(InspectedMember[] members)
        {
            var order = new List<string>();
            var buckets = new Dictionary<string, List<InspectedMember>>(StringComparer.Ordinal);
            var authored = new HashSet<string>(StringComparer.Ordinal);
            var explicitOrder = new Dictionary<string, int>(StringComparer.Ordinal);

            string runningHeader = null;
            foreach (var m in members)
            {
                string group;
                if (m.GroupAttribute != null)
                {
                    group = m.GroupAttribute.Name;
                    authored.Add(group);
                    if (!explicitOrder.ContainsKey(group))
                        explicitOrder[group] = m.GroupAttribute.Order;
                }
                else if (m.Header != null)
                {
                    runningHeader = m.Header;
                    group = runningHeader;
                    authored.Add(group);
                }
                else if (runningHeader != null)
                {
                    group = runningHeader;
                }
                else
                {
                    group = MemberClassifier.Classify(m.Name, m.ValueType);
                }

                m.Group = group;
                if (!buckets.TryGetValue(group, out var list))
                {
                    list = new List<InspectedMember>();
                    buckets[group] = list;
                    order.Add(group);
                }
                list.Add(m);
            }

            // blocos autorais primeiro (ordem declarada / [InspectorGroup(order)]),
            // depois os automaticos na ordem canonica do classificador
            var names = new List<string>(order);
            names.Sort((a, b) =>
            {
                bool aa = authored.Contains(a), ab = authored.Contains(b);
                if (aa != ab)
                    return aa ? -1 : 1;
                if (aa)
                {
                    int oa = explicitOrder.TryGetValue(a, out var x) ? x : 0;
                    int ob = explicitOrder.TryGetValue(b, out var y) ? y : 0;
                    if (oa != ob)
                        return oa.CompareTo(ob);
                    return order.IndexOf(a).CompareTo(order.IndexOf(b));
                }
                return MemberClassifier.SortKey(a).CompareTo(MemberClassifier.SortKey(b));
            });

            var groups = new MemberGroup[names.Count];
            for (int i = 0; i < names.Count; i++)
                groups[i] = new MemberGroup(names[i], buckets[names[i]].ToArray());
            return groups;
        }

        public static void Clear() => cache.Clear();
    }
}
