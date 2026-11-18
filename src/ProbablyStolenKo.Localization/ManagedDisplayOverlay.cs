using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;

namespace ProbablyStolenKo
{
    internal sealed class ManagedDisplayOverlay
    {
        private sealed class DisplayReplacement
        {
            internal DisplayReplacement(string source, string translation)
            {
                Source = source;
                Translation = translation;
            }

            internal string Source { get; }
            internal string Translation { get; }
        }

        private const string HarmonyId = "probably-stolen-ko-localization.managed-display";
        private static ManagedDisplayOverlay _active;
        private static bool _installing;

        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _conflicts = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _embeddedReplacements =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private IReadOnlyDictionary<string, string> _snapshot =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private DisplayReplacement[] _embeddedSnapshot = Array.Empty<DisplayReplacement>();
        private Harmony _harmony;

        internal void ApplyTarget(string modId, ManagedTarget target)
        {
            if (target == null || _appliedTargets.Contains(modId) ||
                (!target.DisplayLiteralOverlay && target.DisplayReplacements.Count == 0)) return;

            var entries = target.DisplayLiteralOverlay
                ? CollectUnambiguousEntries(target.Routes)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var replacement in target.DisplayReplacements)
                entries[replacement.Key] = replacement.Value;
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
                    _embeddedReplacements.Remove(entry.Key);
                    _conflicts.Add(entry.Key);
                    continue;
                }
                next[entry.Key] = entry.Value;
            }
            foreach (var replacement in target.DisplayReplacements)
            {
                if (!_conflicts.Contains(replacement.Key) &&
                    next.ContainsKey(replacement.Key))
                    _embeddedReplacements[replacement.Key] = replacement.Value;
            }
            _snapshot = next;
            RebuildEmbeddedSnapshot();
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
                if (!CanTranslateDisplayedValue(route.Mode)) continue;
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


        private void RebuildEmbeddedSnapshot()
        {
            var replacements = new List<DisplayReplacement>(_embeddedReplacements.Count);
            foreach (var replacement in _embeddedReplacements)
                replacements.Add(new DisplayReplacement(replacement.Key, replacement.Value));
            replacements.Sort((left, right) => right.Source.Length.CompareTo(left.Source.Length));
            _embeddedSnapshot = replacements.ToArray();
        }

        private static bool CanTranslateDisplayedValue(string mode)
        {
            return mode == "literal" ||
                   mode == "literal_occurrence" ||
                   mode == "field_store" ||
                   mode == "result_string" ||
                   mode == "result_string_array" ||
                   mode == "result_by_argument";
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
            if (active._snapshot.TryGetValue(source, out translation))
            {
                source = translation;
                return;
            }
            if (!ContainsHangul(source) && !ContainsCjk(source) && !ContainsClinicDynamicLiteral(source)) return;
            foreach (var replacement in active._embeddedSnapshot)
            {
                if (source.IndexOf(replacement.Source, StringComparison.Ordinal) < 0) continue;
                source = source.Replace(replacement.Source, replacement.Translation);
            }
        }

        private static bool ContainsCjk(string source)
        {
            foreach (var value in source)
                if (value >= '\u3400' && value <= '\u9FFF') return true;
            return false;
        }

        private static bool ContainsClinicDynamicLiteral(string source)
        {
            return source.IndexOf("The supplies are on the left", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("Double click on the manual", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("This time, both the hands and legs require", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("Treatment reward:", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("Possibility of postoperative complications:", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("Total deduction points:", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("Medical Reputation +", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("debridement suture successful", StringComparison.Ordinal) >= 0 ||
                   source.IndexOf("When the shelf fell down, I held it up", StringComparison.Ordinal) >= 0;
        }


        private static bool ContainsHangul(string source)
        {
            foreach (var value in source)
            {
                if ((value >= '\u1100' && value <= '\u11ff') ||
                    (value >= '\u3130' && value <= '\u318f') ||
                    (value >= '\uac00' && value <= '\ud7a3')) return true;
            }
            return false;
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
            _embeddedReplacements.Clear();
            _embeddedSnapshot = Array.Empty<DisplayReplacement>();
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
