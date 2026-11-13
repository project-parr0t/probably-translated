using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProbablyStolenKo
{
    internal sealed class ManifestOverlay
    {
        private sealed class Original
        {
            internal Original(object target, MemberInfo member, string value)
            {
                Target = target;
                Member = member;
                Value = value;
            }

            internal object Target { get; }
            internal MemberInfo Member { get; }
            internal string Value { get; }
        }

        private readonly List<Original> _originals = new List<Original>();
        private readonly HashSet<object> _applied = new HashSet<object>();

        internal void Apply(object manifest, string name, string description)
        {
            if (manifest == null || _applied.Contains(manifest)) return;
            var changed = new List<Original>();
            try
            {
                if (name != null) Set(manifest, "Name", name, changed);
                if (description != null) Set(manifest, "Description", description, changed);
                _originals.AddRange(changed);
                _applied.Add(manifest);
            }
            catch
            {
                for (var index = changed.Count - 1; index >= 0; index--) Restore(changed[index]);
                throw;
            }
        }

        internal void Restore()
        {
            for (var index = _originals.Count - 1; index >= 0; index--) Restore(_originals[index]);
            _originals.Clear();
            _applied.Clear();
        }

        private static void Set(object target, string name, string value, ICollection<Original> originals)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = target.GetType();
            var field = type.GetField(name, flags) ?? type.GetField("<" + name + ">k__BackingField", flags);
            if (field != null && field.FieldType == typeof(string))
            {
                originals.Add(new Original(target, field, (string)field.GetValue(target)));
                field.SetValue(target, value);
                return;
            }
            var property = type.GetProperty(name, flags);
            if (property != null && property.PropertyType == typeof(string) && property.CanRead && property.CanWrite)
            {
                originals.Add(new Original(target, property, (string)property.GetValue(target, null)));
                property.SetValue(target, value, null);
                return;
            }
            throw new MissingMemberException(type.FullName, name);
        }

        private static void Restore(Original original)
        {
            if (original.Member is FieldInfo field) field.SetValue(original.Target, original.Value);
            else if (original.Member is PropertyInfo property) property.SetValue(original.Target, original.Value, null);
        }
    }
}
