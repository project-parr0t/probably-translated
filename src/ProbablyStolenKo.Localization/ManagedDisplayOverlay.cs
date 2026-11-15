using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;

namespace ProbablyStolenKo
{
    internal sealed class ManagedDisplayOverlay
    {
        private const string HarmonyId = "probably-stolen-ko-localization.managed-display";
        private static ManagedDisplayOverlay _active;
        private static bool _installing;

        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _conflicts = new HashSet<string>(StringComparer.Ordinal);
        private IReadOnlyDictionary<string, string> _snapshot =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private Harmony _harmony;

        internal void ApplyTarget(string modId, ManagedTarget target)
        {
            if (target == null || !target.DisplayLiteralOverlay || _appliedTargets.Contains(modId)) return;

            var entries = CollectUnambiguousEntries(target.Routes);
            if (entries.Count == 0)
            {
                _appliedTargets.Add(modId);
                return;
            }

            EnsurePatched();
            var next = new Dictionary<string, string>(_snapshot, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (_conflicts.Contains(entry.Key)) continue;
                string existing;
                if (next.TryGetValue(entry.Key, out existing) &&
                    !StringComparer.Ordinal.Equals(existing, entry.Value))
                {
                    next.Remove(entry.Key);
                    _conflicts.Add(entry.Key);
                    continue;
                }
                next[entry.Key] = entry.Value;
            }
            _snapshot = next;
            _appliedTargets.Add(modId);
        }

        private void EnsurePatched()
        {
            if (_harmony != null) return;
            var harmony = new Harmony(HarmonyId);
            _active = this;
            _installing = true;
            try
            {
                harmony.CreateClassProcessor(typeof(TmpTextSetterPatch)).Patch();
                harmony.CreateClassProcessor(typeof(TmpSetTextPatch)).Patch();
                _harmony = harmony;
            }
            catch
            {
                if (ReferenceEquals(_active, this)) _active = null;
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
            finally
            {
                _installing = false;
            }
        }

        private static Dictionary<string, string> CollectUnambiguousEntries(IReadOnlyList<ManagedRoute> routes)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            var conflicts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in routes)
            {
                if (route.Mode != "literal" && route.Mode != "literal_occurrence") continue;
                foreach (var entry in route.Entries)
                {
                    var source = NormalizeSource(entry.Key, route.Mode);
                    if (conflicts.Contains(source)) continue;
                    string existing;
                    if (entries.TryGetValue(source, out existing) &&
                        !StringComparer.Ordinal.Equals(existing, entry.Value))
                    {
                        entries.Remove(source);
                        conflicts.Add(source);
                        continue;
                    }
                    entries[source] = entry.Value;
                }
            }
            return entries;
        }

        private static string NormalizeSource(string source, string mode)
        {
            if (mode != "literal_occurrence") return source;
            var separator = source.LastIndexOf('\u001f');
            if (separator < 0) return source;
            int occurrence;
            return int.TryParse(source.Substring(separator + 1), out occurrence)
                ? source.Substring(0, separator)
                : source;
        }

        private static void TranslateArgument(ref string source)
        {
            var active = _active;
            if (active == null || source == null ||
                !StringComparer.Ordinal.Equals(LocHelper.GetCurrentLocaleCode(), "ko")) return;
            string translation;
            if (active._snapshot.TryGetValue(source, out translation)) source = translation;
        }

        internal void Restore()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(_harmony.Id);
                _harmony = null;
            }
            if (ReferenceEquals(_active, this)) _active = null;
            _snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
            _conflicts.Clear();
            _appliedTargets.Clear();
        }

        [HarmonyPatch(typeof(TMP_Text), "set_text")]
        private static class TmpTextSetterPatch
        {
            [HarmonyPrepare]
            private static bool Prepare() => _installing;

            [HarmonyPrefix]
            private static void Prefix(ref string __0) => TranslateArgument(ref __0);
        }

        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.SetText), new[] { typeof(string), typeof(bool) })]
        private static class TmpSetTextPatch
        {
            [HarmonyPrepare]
            private static bool Prepare() => _installing;

            [HarmonyPrefix]
            private static void Prefix(ref string __0) => TranslateArgument(ref __0);
        }
    }
}
