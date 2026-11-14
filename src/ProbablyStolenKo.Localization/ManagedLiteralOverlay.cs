using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace ProbablyStolenKo
{
    internal sealed class ManagedRoute
    {
        internal ManagedRoute(
            string typeName,
            string methodName,
            string mode,
            int argumentIndex,
            IReadOnlyDictionary<string, string> entries)
        {
            TypeName = typeName;
            MethodName = methodName;
            Mode = mode;
            ArgumentIndex = argumentIndex;
            Entries = entries;
        }

        internal string TypeName { get; }
        internal string MethodName { get; }
        internal string Mode { get; }
        internal int ArgumentIndex { get; }
        internal IReadOnlyDictionary<string, string> Entries { get; }
    }

    internal sealed class ManagedRefresh
    {
        internal ManagedRefresh(string typeName, string methodName)
        {
            TypeName = typeName;
            MethodName = methodName;
        }

        internal string TypeName { get; }
        internal string MethodName { get; }
    }

    internal sealed class ManagedTarget
    {
        internal ManagedTarget(
            string workshopItemId,
            string assemblyName,
            int entryCount,
            string manifestName,
            string manifestDescription,
            IReadOnlyList<ManagedRoute> routes,
            IReadOnlyList<ManagedRefresh> refresh)
        {
            WorkshopItemId = workshopItemId;
            AssemblyName = assemblyName;
            EntryCount = entryCount;
            ManifestName = manifestName;
            ManifestDescription = manifestDescription;
            Routes = routes;
            Refresh = refresh;
        }

        internal string WorkshopItemId { get; }
        internal string AssemblyName { get; }
        internal int EntryCount { get; }
        internal string ManifestName { get; }
        internal string ManifestDescription { get; }
        internal IReadOnlyList<ManagedRoute> Routes { get; }
        internal IReadOnlyList<ManagedRefresh> Refresh { get; }
    }

    internal sealed class ManagedLiteralCatalog
    {
        private const string ExpectedFingerprint = "7f4783cdb9f20aa37e099e7b96e6189da0bc9ba99f2f87b3ce163ac1ae0929a0";
        private const int ExpectedEntries = 21689;
        private static readonly IReadOnlyDictionary<string, string> ExpectedWorkshopItems =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["chiqingsan.AquaSense"] = "3828878417",
                ["korean_translation_rnin"] = "3828896687",
                ["ps-fishing-gameplay"] = "3829110153",
                ["smart_inventory_arrange"] = "3829206666",
                ["Pen_mod_pipette"] = "3829409725",
                ["brewing_expansion"] = "3829730904",
                ["disaster_day"] = "3829946713",
                ["fishing-cuisine-expansion"] = "3830031787",
                ["asteroid_crate_reveal"] = "3830643693",
                ["visual_probe_20261031_three"] = "3830798369",
                ["item_durability_bars"] = "3831464547",
                ["better_liquid_scanner"] = "3831472187",
                ["bottle.yeast.stay"] = "3831834469",
                ["agri_expansion"] = "3832249311",
                ["tobacco_expansion"] = "3832840208",
                ["ai.emporium"] = "3832941698",
                ["user.realtimeclock"] = "3833083602",
                ["local.station-events"] = "3833966506",
                ["cs_weapon_case"] = "3834110482",
                ["traits_probably_fun"] = "3834226532",
                ["asteroid_in_game_mod_settings"] = "3834556946",
                ["gunsmith_expansion"] = "3834619049",
                ["upper_levels"] = "3838944803",
                ["combat_contracts"] = "3834364038",
            };
        private const int ExpectedTargets = 24;

        private ManagedLiteralCatalog(IReadOnlyDictionary<string, ManagedTarget> targets)
        {
            Targets = targets;
        }

        internal IReadOnlyDictionary<string, ManagedTarget> Targets { get; }

        internal static ManagedLiteralCatalog Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if ((int?)root["schema_version"] != 2) throw new FormatException("Unsupported managed payload schema_version.");
            if ((string)root["inventory_fingerprint"] != ExpectedFingerprint) throw new FormatException("Managed payload fingerprint does not match this bridge.");
            if ((int?)root["entry_count"] != ExpectedEntries) throw new FormatException("Managed payload entry count is invalid.");
            var sourceTargets = root["targets"] as JObject;
            if (sourceTargets == null || sourceTargets.Count != ExpectedTargets) throw new FormatException("Managed target count is invalid.");

            var targets = new Dictionary<string, ManagedTarget>(StringComparer.Ordinal);
            var total = 0;
            foreach (var targetProperty in sourceTargets.Properties())
            {
                var target = targetProperty.Value as JObject;
                var workshopItemId = (string)target?["workshop_item_id"];
                var assemblyName = (string)target?["assembly_name"];
                var entryCount = (int?)target?["entry_count"];
                var manifest = target?["manifest"] as JObject;
                var manifestName = (string)manifest?["name"];
                var manifestDescription = (string)manifest?["description"];
                var sourceRoutes = target?["routes"] as JArray;
                var sourceRefresh = target?["refresh"] as JArray;
                if (string.IsNullOrWhiteSpace(targetProperty.Name) || string.IsNullOrWhiteSpace(workshopItemId) ||
                    !entryCount.HasValue || entryCount.Value < 1 || sourceRoutes == null || sourceRefresh == null ||
                    (sourceRoutes.Count != 0 && string.IsNullOrWhiteSpace(assemblyName)))
                    throw new FormatException("Incomplete managed target record.");
                string expectedWorkshopItem;
                if (!ExpectedWorkshopItems.TryGetValue(targetProperty.Name, out expectedWorkshopItem) ||
                    !StringComparer.Ordinal.Equals(workshopItemId, expectedWorkshopItem))
                    throw new FormatException("Managed target Workshop item identity is invalid.");
                var routes = new List<ManagedRoute>(sourceRoutes.Count);
                foreach (var routeToken in sourceRoutes)
                {
                    var route = routeToken as JObject;
                    var typeName = (string)route?["type"];
                    var methodName = (string)route?["method"];
                    var mode = (string)route?["mode"];
                    var argumentIndex = (int?)route?["argument_index"] ?? 0;
                    var sourceEntries = route?["entries"] as JObject;
                    if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(methodName) ||
                        (mode != "literal" && mode != "literal_occurrence" &&
                         mode != "result_string" && mode != "result_string_array" &&
                         mode != "result_by_argument" && mode != "result_by_arguments") ||
                        argumentIndex < 0 || sourceEntries == null)
                        throw new FormatException("Incomplete managed route record.");
                    var entries = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var entry in sourceEntries.Properties())
                    {
                        var translation = (string)entry.Value;
                        if (string.IsNullOrEmpty(entry.Name) || translation == null) throw new FormatException("Invalid managed route entry.");
                        entries.Add(entry.Name, translation);
                    }
                    routes.Add(new ManagedRoute(typeName, methodName, mode, argumentIndex, entries));
                }
                var refresh = new List<ManagedRefresh>(sourceRefresh.Count);
                foreach (var refreshToken in sourceRefresh)
                {
                    var action = refreshToken as JObject;
                    var typeName = (string)action?["type"];
                    var methodName = (string)action?["method"];
                    if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(methodName))
                        throw new FormatException("Incomplete managed refresh record.");
                    refresh.Add(new ManagedRefresh(typeName, methodName));
                }
                total += entryCount.Value;
                targets.Add(targetProperty.Name, new ManagedTarget(
                    workshopItemId, assemblyName, entryCount.Value, manifestName, manifestDescription, routes, refresh));
            }
            if (total != ExpectedEntries) throw new FormatException("Managed payload total is invalid.");
            return new ManagedLiteralCatalog(targets);
        }
    }

    internal sealed class ManagedLiteralOverlay
    {
        private const string HarmonyId = "probably-stolen-ko-localization.managed-r8";
        private static readonly object Gate = new object();
        private static readonly Dictionary<int, IReadOnlyDictionary<string, string>> LiteralBindingRoutes =
            new Dictionary<int, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<int, IReadOnlyDictionary<string, string>> ResultBindingRoutes =
            new Dictionary<int, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<int, Tuple<int, IReadOnlyDictionary<string, string>>> ArgumentBindingRoutes =
            new Dictionary<int, Tuple<int, IReadOnlyDictionary<string, string>>>();
        private static readonly HashSet<int> EnabledStaticBindings = new HashSet<int>();

        private readonly Dictionary<string, Harmony> _harmonies =
            new Dictionary<string, Harmony>(StringComparer.Ordinal);
        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _appliedRefresh = new List<string>();
        private readonly ModLog _log;

        internal ManagedLiteralOverlay(ModLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        internal void ApplyTarget(string modId, ManagedTarget target)
        {
            if (target == null || _appliedTargets.Contains(modId)) return;
            var bindings = GeneratedManagedBindings.Create(modId);
            if (target.Routes.Count != 0 && bindings.Count == 0)
                throw new InvalidOperationException("No generated exact bindings exist for managed target " + modId + ".");

            var harmony = new Harmony(HarmonyId + "." + modId);
            var staticBindings = new List<int>();
            var refreshPrepared = false;
            _harmonies.Add(modId, harmony);
            try
            {
                foreach (var binding in bindings)
                    ApplyBinding(harmony, binding, target, staticBindings);

                GeneratedManagedRefresh.Prepare(harmony, modId);
                refreshPrepared = true;
                GeneratedManagedRefresh.Invoke(modId);
                if (target.Refresh.Count != 0) _appliedRefresh.Add(modId);
                _appliedTargets.Add(modId);
                _log.Log("Applied managed Korean routes to " + modId + ".");
            }
            catch
            {
                RollbackTarget(modId, harmony, staticBindings, refreshPrepared);
                throw;
            }
        }

        private static void ApplyBinding(
            Harmony harmony,
            ManagedExactBinding binding,
            ManagedTarget target,
            ICollection<int> staticBindings)
        {
            if (binding.RouteIndex < 0 || binding.RouteIndex >= target.Routes.Count)
                throw new InvalidOperationException("Generated managed route index is outside the runtime catalog.");
            var route = target.Routes[binding.RouteIndex];
            if (binding.Kind == ManagedBindingKind.Unsupported)
                throw new InvalidOperationException("Unsupported generated managed binding: " + binding.Issue);

            if (binding.Kind != ManagedBindingKind.StaticPatch)
                throw new InvalidOperationException("Generated managed binding is not an exact static patch.");
            staticBindings.Add(binding.Id);
            lock (Gate)
            {
                if (route.Mode == "literal" || route.Mode == "literal_occurrence")
                    LiteralBindingRoutes[binding.Id] = route.Entries;
                else if (route.Mode == "result_by_argument")
                    ArgumentBindingRoutes[binding.Id] = Tuple.Create(route.ArgumentIndex, route.Entries);
                else ResultBindingRoutes[binding.Id] = route.Entries;
                EnabledStaticBindings.Add(binding.Id);
            }
            try
            {
                harmony.CreateClassProcessor(binding.PatchType).Patch();
            }
            finally
            {
                lock (Gate) EnabledStaticBindings.Remove(binding.Id);
            }
        }

        private void RollbackTarget(
            string modId,
            Harmony harmony,
            IEnumerable<int> staticBindings,
            bool refreshPrepared)
        {
            harmony.UnpatchAll(harmony.Id);
            lock (Gate)
            {
                foreach (var bindingId in staticBindings)
                {
                    LiteralBindingRoutes.Remove(bindingId);
                    ResultBindingRoutes.Remove(bindingId);
                    ArgumentBindingRoutes.Remove(bindingId);
                }
            }
            if (refreshPrepared)
            {
                try { GeneratedManagedRefresh.Invoke(modId); }
                catch (Exception error) { _log.Error("Managed rollback refresh failed after " + error.GetType().Name + "."); }
            }
            _harmonies.Remove(modId);
            _appliedTargets.Remove(modId);
        }

        internal static bool PrepareBinding(int bindingId)
        {
            lock (Gate) return EnabledStaticBindings.Contains(bindingId);
        }

        internal void Restore()
        {
            foreach (var harmony in _harmonies.Values) harmony.UnpatchAll(harmony.Id);
            lock (Gate)
            {
                LiteralBindingRoutes.Clear();
                ResultBindingRoutes.Clear();
                ArgumentBindingRoutes.Clear();
                EnabledStaticBindings.Clear();
            }
            foreach (var modId in _appliedRefresh)
            {
                try { GeneratedManagedRefresh.Invoke(modId); }
                catch (Exception error) { _log.Error("Managed restore refresh failed after " + error.GetType().Name + "."); }
            }
            _appliedRefresh.Clear();
            _harmonies.Clear();
            _appliedTargets.Clear();
        }


        internal static IEnumerable<CodeInstruction> TranspileBinding(
            IEnumerable<CodeInstruction> instructions,
            int bindingId)
        {
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) LiteralBindingRoutes.TryGetValue(bindingId, out entries);
            return TranslateInstructions(instructions, entries);
        }

        internal static void PostfixStringBinding(ref string result, int bindingId)
        {
            if (result == null) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultBindingRoutes.TryGetValue(bindingId, out entries);
            if (entries != null && entries.TryGetValue(result, out var translation)) result = translation;
        }

        internal static void PostfixByArgumentBinding(object[] args, ref string result, int bindingId)
        {
            Tuple<int, IReadOnlyDictionary<string, string>> route;
            lock (Gate) ArgumentBindingRoutes.TryGetValue(bindingId, out route);
            if (route == null || args == null || route.Item1 >= args.Length) return;
            var source = args[route.Item1] as string;
            if (source != null && route.Item2.TryGetValue(source, out var translation)) result = translation;
        }

        internal static void PostfixByArgumentsBinding(object[] args, ref string result, int bindingId)
        {
            if (args == null || args.Length < 2 || !(args[0] is string identity)) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultBindingRoutes.TryGetValue(bindingId, out entries);
            if (entries == null) return;
            var key = identity + "|" + Convert.ToString(args[1], System.Globalization.CultureInfo.InvariantCulture);
            if (entries.TryGetValue(key, out var translation)) result = translation;
        }

        internal static void PostfixStringArrayBinding(ref string[] result, int bindingId)
        {
            if (result == null) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultBindingRoutes.TryGetValue(bindingId, out entries);
            if (entries == null) return;
            string[] translatedResult = null;
            for (var index = 0; index < result.Length; index++)
            {
                if (result[index] == null || !entries.TryGetValue(result[index], out var translation)) continue;
                if (translatedResult == null) translatedResult = (string[])result.Clone();
                translatedResult[index] = translation;
            }
            if (translatedResult != null) result = translatedResult;
        }

        private static IEnumerable<CodeInstruction> TranslateInstructions(
            IEnumerable<CodeInstruction> instructions,
            IReadOnlyDictionary<string, string> entries)
        {
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var instruction in instructions)
            {
                if (entries != null && instruction.opcode == OpCodes.Ldstr && instruction.operand is string source)
                {
                    if (entries.TryGetValue(source, out var translation)) instruction.operand = translation;
                    else
                    {
                        occurrences.TryGetValue(source, out var index);
                        occurrences[source] = index + 1;
                        if (entries.TryGetValue(source + "\u001f" + index, out translation))
                            instruction.operand = translation;
                    }
                }
                yield return instruction;
            }
        }

    }
}
