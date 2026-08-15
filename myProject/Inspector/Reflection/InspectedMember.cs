using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace myProject.Inspector.Reflection
{
    // Um campo/propriedade exposto no inspector. Getter e setter sao delegates compilados
    // uma unica vez (via expression tree) — depois disso nao ha mais reflexao por frame.
    public sealed class InspectedMember
    {
        public readonly string Name;
        public readonly string Label;        // rotulo amigavel ("maxRunSpeed" -> "Max Run Speed")
        public readonly Type ValueType;
        public readonly string Header;       // [Header] — null se ausente
        public readonly string Tooltip;      // [Tooltip] — null se ausente
        public readonly RangeAttribute Range; // [Range] — null se ausente
        public readonly bool CanWrite;

        private readonly Func<object, object> getter;
        private readonly Action<object, object> setter;
        // Fallback p/ membros de struct: delegates compilados nao mutam value types,
        // mas reflexao sobre a copia BOXED funciona (usado ao expandir structs aninhadas).
        private readonly MemberInfo boxedInfo;

        private InspectedMember(string name, Type valueType, MemberInfo info, bool canWrite,
            Func<object, object> getter, Action<object, object> setter)
        {
            boxedInfo = info;
            Name = name;
            Label = Prettify(name);
            ValueType = valueType;
            CanWrite = canWrite;
            this.getter = getter;
            this.setter = setter;

            Header = info.GetCustomAttribute<HeaderAttribute>()?.Text;
            Tooltip = info.GetCustomAttribute<TooltipAttribute>()?.Text;
            Range = info.GetCustomAttribute<RangeAttribute>();
            if (info.GetCustomAttribute<ReadOnlyAttribute>() != null)
                CanWrite = false;
        }

        public object GetValue(object target)
        {
            try { return getter(target); }
            catch (Exception e) { return "<erro: " + e.GetType().Name + ">"; }
        }

        public bool TrySetValue(object target, object value)
        {
            if (!CanWrite)
                return false;
            try
            {
                if (setter != null)
                {
                    setter(target, value);
                    return true;
                }
                // struct boxed: escreve por reflexao na copia e devolve pelo chamador
                if (boxedInfo is FieldInfo f)
                {
                    f.SetValue(target, value);
                    return true;
                }
                if (boxedInfo is PropertyInfo p && p.CanWrite)
                {
                    p.SetValue(target, value);
                    return true;
                }
            }
            catch { }
            return false;
        }

        // ---- construcao ----

        public static InspectedMember TryCreate(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral)
                return null;
            if (field.GetCustomAttribute<HideInInspectorAttribute>() != null)
                return null;
            // publico, ou privado marcado com [SerializeField]
            if (!field.IsPublic && field.GetCustomAttribute<SerializeFieldAttribute>() == null)
                return null;
            if (field.Name.IndexOf('<') >= 0) // backing field gerado pelo compilador
                return null;

            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var typedTarget = Expression.Convert(target, field.DeclaringType);
            var access = Expression.Field(typedTarget, field);

            var getter = Expression.Lambda<Func<object, object>>(
                Expression.Convert(access, typeof(object)), target).Compile();

            // struct: delegate compilado nao muta value type — cai no fallback boxed
            Action<object, object> setter = null;
            if (!field.DeclaringType.IsValueType)
            {
                setter = Expression.Lambda<Action<object, object>>(
                    Expression.Assign(access, Expression.Convert(value, field.FieldType)),
                    target, value).Compile();
            }

            return new InspectedMember(field.Name, field.FieldType, field, true, getter, setter);
        }

        public static InspectedMember TryCreate(PropertyInfo prop)
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                return null;
            var get = prop.GetGetMethod(true);
            if (get == null || get.IsStatic || !get.IsPublic)
                return null;
            if (prop.GetCustomAttribute<HideInInspectorAttribute>() != null)
                return null;

            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var typedTarget = Expression.Convert(target, prop.DeclaringType);
            var access = Expression.Property(typedTarget, prop);

            var getter = Expression.Lambda<Func<object, object>>(
                Expression.Convert(access, typeof(object)), target).Compile();

            Action<object, object> setter = null;
            var set = prop.GetSetMethod(true);
            bool writable = set != null && set.IsPublic;
            if (writable && !prop.DeclaringType.IsValueType)
            {
                setter = Expression.Lambda<Action<object, object>>(
                    Expression.Assign(access, Expression.Convert(value, prop.PropertyType)),
                    target, value).Compile();
            }

            return new InspectedMember(prop.Name, prop.PropertyType, prop, writable, getter, setter);
        }

        // "maxRunSpeed" / "MaxRunSpeed" / "_speed" -> "Max Run Speed" / "Speed"
        public static string Prettify(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            int start = 0;
            while (start < name.Length && (name[start] == '_' || name[start] == 'm' && start + 1 < name.Length && name[start + 1] == '_'))
                start++;
            var sb = new StringBuilder(name.Length + 6);
            bool first = true;
            for (int i = start; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_')
                    continue;
                if (first)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    first = false;
                    continue;
                }
                bool boundary = char.IsUpper(c) && !char.IsUpper(name[i - 1]);
                bool digitBoundary = char.IsDigit(c) && !char.IsDigit(name[i - 1]);
                if (boundary || digitBoundary)
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
