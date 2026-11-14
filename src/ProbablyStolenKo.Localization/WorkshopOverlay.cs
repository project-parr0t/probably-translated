using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenKo
{
    internal sealed class WorkshopModPayload
    {
        internal WorkshopModPayload(string workshopItemId, IReadOnlyDictionary<string, string> entries)
        {
            WorkshopItemId = workshopItemId;
            Entries = entries;
        }

        internal string WorkshopItemId { get; }
        internal IReadOnlyDictionary<string, string> Entries { get; }
    }

    internal sealed class WorkshopPayloadCatalog
    {
        private const string ExpectedFingerprint = "3a1b33e3d687bdde0541d2be4cfbb90eec86191f615d092ca83f2466c3b9fea6";
        private const int ExpectedEntries = 9928;
        private const int ExpectedMods = 13;
        private static readonly IReadOnlyDictionary<string, string> ExpectedWorkshopItems =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ps-fishing-gameplay"] = "3829110153",
                ["Pen_mod_pipette"] = "3829409725",
                ["brewing_expansion"] = "3829730904",
                ["disaster_day"] = "3829946713",
                ["fishing-cuisine-expansion"] = "3830031787",
                ["visual_probe_20261031_three"] = "3830798369",
                ["tobacco_expansion"] = "3832840208",
                ["ai.emporium"] = "3832941698",
                ["gunsmith_expansion"] = "3834619049",
                ["containers_plus"] = "3837147782",
                ["sfi.storageappearance"] = "3837983447",
                ["upper_levels"] = "3838944803",
                ["traits_probably_fun"] = "3834226532",
            };

        private WorkshopPayloadCatalog(string locale, IReadOnlyDictionary<string, WorkshopModPayload> mods, int count)
        {
            Locale = locale;
            Mods = mods;
            Count = count;
        }

        internal string Locale { get; }
        internal IReadOnlyDictionary<string, WorkshopModPayload> Mods { get; }
        internal int Count { get; }

        internal bool TryGetMod(string modId, out WorkshopModPayload payload)
        {
            return Mods.TryGetValue(modId, out payload);
        }

        internal static WorkshopPayloadCatalog Parse(string json, Func<string, string> readFile)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));
            var root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if ((int?)root["schema_version"] != 1) throw new FormatException("Unsupported workshop payload schema_version.");
            if ((string)root["inventory_fingerprint"] != ExpectedFingerprint) throw new FormatException("Workshop payload fingerprint does not match this bridge.");
            var locale = (string)root["locale"];
            if (locale != "ko") throw new FormatException("Workshop payload locale is not ko.");
            var sourceMods = root["mods"] as JObject;
            if (sourceMods == null || sourceMods.Count != ExpectedMods) throw new FormatException("Workshop payload target-mod count is invalid.");

            var mods = new Dictionary<string, WorkshopModPayload>(StringComparer.Ordinal);
            var total = 0;
            foreach (var modProperty in sourceMods.Properties())
            {
                if (string.IsNullOrWhiteSpace(modProperty.Name) || !(modProperty.Value is JObject sourceMod))
                    throw new FormatException("Invalid workshop target-mod record.");
                var workshopItemId = (string)sourceMod["workshop_item_id"];
                var csvPath = (string)sourceMod["csv_path"];
                var entryCount = (int?)sourceMod["entry_count"];
                if (string.IsNullOrWhiteSpace(workshopItemId) || string.IsNullOrWhiteSpace(csvPath) ||
                    !csvPath.StartsWith("workshop-overlays/", StringComparison.Ordinal) || csvPath.Contains("..") ||
                    csvPath.Contains("\\") || !entryCount.HasValue || entryCount.Value < 1)
                    throw new FormatException("Incomplete workshop target-mod record.");
                string expectedWorkshopItem;
                if (!ExpectedWorkshopItems.TryGetValue(modProperty.Name, out expectedWorkshopItem) ||
                    !StringComparer.Ordinal.Equals(workshopItemId, expectedWorkshopItem))
                    throw new FormatException("Workshop target item identity is invalid.");
                var entries = ParseCsv(readFile(csvPath));
                if (entries.Count != entryCount.Value) throw new FormatException("Workshop CSV entry count is invalid.");
                total += entries.Count;
                mods.Add(modProperty.Name, new WorkshopModPayload(workshopItemId, entries));
            }
            if (total != ExpectedEntries) throw new FormatException("Workshop payload entry count is not 9928.");
            return new WorkshopPayloadCatalog(locale, mods, total);
        }

        private static IReadOnlyDictionary<string, string> ParseCsv(string csv)
        {
            if (csv == null) throw new ArgumentNullException(nameof(csv));
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            var fields = new List<string>(2);
            var field = new StringBuilder();
            var quoted = false;
            var atLineStart = true;
            var skippingLine = false;
            for (var index = 0; index <= csv.Length; index++)
            {
                var atEnd = index == csv.Length;
                var value = atEnd ? '\n' : csv[index];
                if (skippingLine)
                {
                    if (value != '\r' && value != '\n') continue;
                    if (value == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n') index++;
                    skippingLine = false;
                    atLineStart = true;
                    fields.Clear();
                    field.Length = 0;
                    continue;
                }
                if (quoted)
                {
                    if (atEnd) throw new FormatException("Unterminated quoted CSV field.");
                    if (value == '"')
                    {
                        if (index + 1 < csv.Length && csv[index + 1] == '"') { field.Append('"'); index++; }
                        else quoted = false;
                    }
                    else field.Append(value);
                    continue;
                }
                if (index == 0 && value == '\uFEFF') continue;
                if (atLineStart)
                {
                    if (value == '#')
                    {
                        skippingLine = true;
                        fields.Clear();
                        field.Length = 0;
                        continue;
                    }
                    if (!char.IsWhiteSpace(value)) atLineStart = false;
                }
                if (value == '"' && field.Length == 0) { quoted = true; continue; }
                if (value == ',') { fields.Add(field.ToString()); field.Length = 0; continue; }
                if (value != '\r' && value != '\n') { field.Append(value); continue; }
                fields.Add(field.ToString());
                field.Length = 0;
                atLineStart = true;
                if (value == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n') index++;
                if (fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0])) { fields.Clear(); continue; }
                if (fields.Count != 2 || string.IsNullOrWhiteSpace(fields[0]))
                    throw new FormatException("Workshop CSV must contain exactly key and text columns.");
                entries.Add(fields[0].Trim(), fields[1]);
                fields.Clear();
            }
            return entries;
        }
    }

    internal readonly struct WorkshopEntryKey : IEquatable<WorkshopEntryKey>
    {
        internal WorkshopEntryKey(LoadedMod owner, Dictionary<string, string> locale, string modId, string key)
        {
            Owner = owner;
            Locale = locale;
            ModId = modId;
            Key = key;
        }
        internal LoadedMod Owner { get; }
        internal Dictionary<string, string> Locale { get; }
        internal string ModId { get; }
        internal string Key { get; }
        public bool Equals(WorkshopEntryKey other) =>
            ReferenceEquals(Owner, other.Owner) && ReferenceEquals(Locale, other.Locale) &&
            StringComparer.Ordinal.Equals(ModId, other.ModId) && StringComparer.Ordinal.Equals(Key, other.Key);
        public override bool Equals(object obj) => obj is WorkshopEntryKey other && Equals(other);
        public override int GetHashCode() => unchecked(
            (((RuntimeHelpers.GetHashCode(Owner) * 397) ^ RuntimeHelpers.GetHashCode(Locale)) * 397 ^
              StringComparer.Ordinal.GetHashCode(ModId)) * 397 ^ StringComparer.Ordinal.GetHashCode(Key));
    }

    internal sealed class WorkshopOriginal
    {
        internal WorkshopOriginal(
            LoadedMod owner,
            Dictionary<string, string> locale,
            bool localeCreated,
            bool existed,
            string value,
            string applied)
        {
            Owner = owner;
            Locale = locale;
            LocaleCreated = localeCreated;
            Existed = existed;
            Value = value;
            Applied = applied;
        }

        internal LoadedMod Owner { get; }
        internal Dictionary<string, string> Locale { get; }
        internal bool LocaleCreated { get; }
        internal bool Existed { get; }
        internal string Value { get; }
        internal string Applied { get; }
    }

    internal readonly struct WorkshopApplyResult
    {
        internal WorkshopApplyResult(int appliedCount, int missingCount)
        {
            AppliedCount = appliedCount;
            MissingCount = missingCount;
        }

        internal int AppliedCount { get; }
        internal int MissingCount { get; }
    }

    internal static class WorkshopOverlayEngine
    {
        internal static WorkshopApplyResult Apply(
            WorkshopPayloadCatalog catalog,
            LoadedMod loaded,
            IDictionary<WorkshopEntryKey, WorkshopOriginal> originals)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (loaded == null || loaded.Manifest == null) throw new ArgumentNullException(nameof(loaded));
            if (originals == null) throw new ArgumentNullException(nameof(originals));
            WorkshopModPayload payload;
            if (!catalog.TryGetMod(loaded.Manifest.ID, out payload)) return new WorkshopApplyResult(0, 0);
            if (loaded.Localization == null) throw new InvalidOperationException("Target mod localization namespace is unavailable.");

            var staged = new List<KeyValuePair<string, string>>(payload.Entries.Count);
            var missingCount = 0;
            foreach (var translation in payload.Entries)
            {
                var sourceExists = false;
                foreach (var locale in loaded.Localization.Values)
                {
                    if (locale != null && locale.ContainsKey(translation.Key)) { sourceExists = true; break; }
                }
                if (sourceExists) staged.Add(translation);
                else missingCount++;
            }
            if (staged.Count == 0) return new WorkshopApplyResult(0, missingCount);

            Dictionary<string, string> korean;
            var localeCreated = !loaded.Localization.TryGetValue(catalog.Locale, out korean);
            if (localeCreated)
            {
                korean = new Dictionary<string, string>(StringComparer.Ordinal);
                loaded.Localization.Add(catalog.Locale, korean);
            }

            var changed = new List<Tuple<string, bool, string>>(staged.Count);
            var addedOriginals = new List<WorkshopEntryKey>(staged.Count);
            try
            {
                foreach (var translation in staged)
                {
                    var entryKey = new WorkshopEntryKey(loaded, korean, loaded.Manifest.ID, translation.Key);
                    string previous;
                    var existed = korean.TryGetValue(translation.Key, out previous);
                    if (!originals.ContainsKey(entryKey))
                    {
                        originals.Add(entryKey, new WorkshopOriginal(
                            loaded, korean, localeCreated, existed, previous, translation.Value));
                        addedOriginals.Add(entryKey);
                    }
                    changed.Add(Tuple.Create(translation.Key, existed, previous));
                    korean[translation.Key] = translation.Value;
                }
            }
            catch
            {
                for (var index = changed.Count - 1; index >= 0; index--)
                {
                    var item = changed[index];
                    if (item.Item2) korean[item.Item1] = item.Item3;
                    else korean.Remove(item.Item1);
                }
                foreach (var entryKey in addedOriginals) originals.Remove(entryKey);
                if (localeCreated && korean.Count == 0) loaded.Localization.Remove(catalog.Locale);
                throw;
            }
            return new WorkshopApplyResult(staged.Count, missingCount);
        }

        internal static void Restore(WorkshopPayloadCatalog catalog, IDictionary<WorkshopEntryKey, WorkshopOriginal> originals)
        {
            if (catalog == null || originals == null) return;
            foreach (var item in originals)
            {
                var key = item.Key;
                var original = item.Value;
                string current;
                if (!original.Locale.TryGetValue(key.Key, out current) || current != original.Applied) continue;
                if (original.Existed) original.Locale[key.Key] = original.Value;
                else original.Locale.Remove(key.Key);
            }
            foreach (var original in originals.Values)
            {
                if (original.LocaleCreated && original.Locale.Count == 0 && original.Owner.Localization != null &&
                    original.Owner.Localization.TryGetValue(catalog.Locale, out var locale) &&
                    ReferenceEquals(locale, original.Locale))
                    original.Owner.Localization.Remove(catalog.Locale);
            }
            originals.Clear();
        }
    }
}
