using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        private const string ExpectedFingerprint = "744738487893dc760b4913e7a2091666419386c1f55691f935e57922e50f7d11";
        private const int ExpectedEntries = 21685;
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
            };
        private const int ExpectedTargets = 22;

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
            if (total != ExpectedEntries) throw new FormatException("Managed payload total is not 21685.");
            return new ManagedLiteralCatalog(targets);
        }
    }

    internal sealed class ManagedLiteralOverlay
    {
        private sealed class PendingTarget
        {
            internal PendingTarget(string modId, ManagedTarget target)
            {
                ModId = modId;
                Target = target;
            }

            internal string ModId { get; }
            internal ManagedTarget Target { get; }
        }

        private const string HarmonyId = "probably-stolen-ko-localization.managed-r8";
        private static readonly object Gate = new object();
        private static readonly Dictionary<MethodBase, IReadOnlyDictionary<string, string>> LiteralRoutes =
            new Dictionary<MethodBase, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<int, IReadOnlyDictionary<string, string>> LiteralBindingRoutes =
            new Dictionary<int, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<MethodBase, IReadOnlyDictionary<string, string>> ResultRoutes =
            new Dictionary<MethodBase, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<MethodBase, Tuple<int, IReadOnlyDictionary<string, string>>> ArgumentRoutes =
            new Dictionary<MethodBase, Tuple<int, IReadOnlyDictionary<string, string>>>();
        private static readonly HashSet<int> EnabledStaticBindings = new HashSet<int>();

        private readonly Dictionary<string, Harmony> _harmonies =
            new Dictionary<string, Harmony>(StringComparer.Ordinal);
        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _queuedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<PendingTarget> _pendingTargets = new Queue<PendingTarget>();
        private readonly List<string> _appliedRefresh = new List<string>();
        private readonly ModLog _log;
        private bool _workerScheduled;
        private int _generation;

        internal ManagedLiteralOverlay(ModLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        internal bool QueueTarget(string modId, ManagedTarget target)
        {
            if (target == null || _appliedTargets.Contains(modId) || _queuedTargets.Contains(modId)) return false;
            _pendingTargets.Enqueue(new PendingTarget(modId, target));
            _queuedTargets.Add(modId);
            if (_workerScheduled) return false;
            _workerScheduled = true;
            return true;
        }

        internal IEnumerator ProcessQueue(Func<bool> isActive)
        {
            if (isActive == null) throw new ArgumentNullException(nameof(isActive));
            var generation = _generation;
            yield return null;
            try
            {
                while (generation == _generation && isActive() && _pendingTargets.Count != 0)
                {
                    var pending = _pendingTargets.Dequeue();
                    var routine = ApplyTargetBatched(pending.ModId, pending.Target, generation, isActive);
                    while (generation == _generation && isActive())
                    {
                        bool advanced;
                        try
                        {
                            advanced = routine.MoveNext();
                        }
                        catch (Exception error)
                        {
                            _queuedTargets.Remove(pending.ModId);
                            _log.Error("Managed target overlay failed for " + pending.ModId + ". " + error.GetType().Name);
                            break;
                        }
                        if (!advanced) break;
                        yield return routine.Current;
                    }
                }
            }
            finally
            {
                if (generation == _generation) _workerScheduled = false;
            }
        }

        private IEnumerator ApplyTargetBatched(
            string modId,
            ManagedTarget target,
            int generation,
            Func<bool> isActive)
        {
            var bindings = GeneratedManagedBindings.Create(modId);
            if (target.Routes.Count != 0 && bindings.Count == 0)
                throw new InvalidOperationException("No generated exact bindings exist for managed target " + modId + ".");

            var harmony = new Harmony(HarmonyId + "." + modId);
            var patched = new List<MethodBase>();
            var staticBindings = new List<int>();
            var refreshPrepared = false;
            _harmonies.Add(modId, harmony);
            foreach (var binding in bindings)
            {
                if (generation != _generation || !isActive()) yield break;
                try
                {
                    ApplyBinding(harmony, binding, target, patched, staticBindings);
                }
                catch
                {
                    RollbackTarget(modId, harmony, patched, staticBindings, refreshPrepared);
                    throw;
                }
                yield return null;
            }

            if (generation != _generation || !isActive()) yield break;
            try
            {
                GeneratedManagedRefresh.Prepare(harmony, modId);
                refreshPrepared = true;
                GeneratedManagedRefresh.Invoke(modId);
                if (target.Refresh.Count != 0) _appliedRefresh.Add(modId);
                _appliedTargets.Add(modId);
                _queuedTargets.Remove(modId);
                _log.Log("Applied managed Korean routes to " + modId + ".");
            }
            catch
            {
                RollbackTarget(modId, harmony, patched, staticBindings, refreshPrepared);
                throw;
            }
        }

        private static void ApplyBinding(
            Harmony harmony,
            ManagedExactBinding binding,
            ManagedTarget target,
            ICollection<MethodBase> patched,
            ICollection<int> staticBindings)
        {
            if (binding.RouteIndex < 0 || binding.RouteIndex >= target.Routes.Count)
                throw new InvalidOperationException("Generated managed route index is outside the runtime catalog.");
            var route = target.Routes[binding.RouteIndex];
            if (binding.Kind == ManagedBindingKind.Unsupported)
                throw new InvalidOperationException("Unsupported generated managed binding: " + binding.Issue);

            if (binding.Kind == ManagedBindingKind.StaticPatch)
            {
                if (route.Mode != "literal" && route.Mode != "literal_occurrence")
                    throw new InvalidOperationException("Constructor bindings only support literal transpilers.");
                staticBindings.Add(binding.Id);
                lock (Gate)
                {
                    LiteralBindingRoutes[binding.Id] = route.Entries;
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
                return;
            }

            var method = ModReflect.Target(binding.TypeName, binding.MethodName, binding.ParameterTypes);
            patched.Add(method);
            HarmonyMethod transpiler = null;
            HarmonyMethod postfix = null;
            lock (Gate)
            {
                if (route.Mode == "literal" || route.Mode == "literal_occurrence")
                    MergeLiteralRoute(method, route.Entries);
                else if (route.Mode == "result_by_argument")
                    ArgumentRoutes[method] = Tuple.Create(route.ArgumentIndex, route.Entries);
                else ResultRoutes[method] = route.Entries;
            }
            if (route.Mode == "literal" || route.Mode == "literal_occurrence")
                transpiler = new HarmonyMethod(typeof(ManagedLiteralOverlay), nameof(Transpile));
            else if (route.Mode == "result_string")
                postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay), nameof(PostfixString));
            else if (route.Mode == "result_by_argument")
                postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay), nameof(PostfixByArgument));
            else if (route.Mode == "result_by_arguments")
                postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay), nameof(PostfixByArguments));
            else
                postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay), nameof(PostfixStringArray));
            harmony.Patch(method, postfix: postfix, transpiler: transpiler);
        }

        private void RollbackTarget(
            string modId,
            Harmony harmony,
            IEnumerable<MethodBase> patched,
            IEnumerable<int> staticBindings,
            bool refreshPrepared)
        {
            harmony.UnpatchAll(harmony.Id);
            lock (Gate)
            {
                foreach (var method in patched)
                {
                    LiteralRoutes.Remove(method);
                    ResultRoutes.Remove(method);
                    ArgumentRoutes.Remove(method);
                }
                foreach (var bindingId in staticBindings) LiteralBindingRoutes.Remove(bindingId);
            }
            if (refreshPrepared)
            {
                try { GeneratedManagedRefresh.Invoke(modId); }
                catch (Exception error) { _log.Error("Managed rollback refresh failed after " + error.GetType().Name + "."); }
            }
            _harmonies.Remove(modId);
            _appliedTargets.Remove(modId);
            _queuedTargets.Remove(modId);
        }

        internal static bool PrepareBinding(int bindingId)
        {
            lock (Gate) return EnabledStaticBindings.Contains(bindingId);
        }

        internal void Restore()
        {
            _generation++;
            foreach (var harmony in _harmonies.Values) harmony.UnpatchAll(harmony.Id);
            lock (Gate)
            {
                LiteralRoutes.Clear();
                LiteralBindingRoutes.Clear();
                ResultRoutes.Clear();
                ArgumentRoutes.Clear();
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
            _queuedTargets.Clear();
            _pendingTargets.Clear();
            _workerScheduled = false;
        }

        private static void MergeLiteralRoute(
            MethodBase method,
            IReadOnlyDictionary<string, string> additions)
        {
            LiteralRoutes.TryGetValue(method, out var existing);
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            if (existing != null)
                foreach (var entry in existing) merged.Add(entry.Key, entry.Value);
            foreach (var entry in additions)
            {
                if (merged.TryGetValue(entry.Key, out var prior) &&
                    !StringComparer.Ordinal.Equals(prior, entry.Value))
                    throw new InvalidOperationException(
                        "Conflicting identity-bound literal route for " +
                        method.DeclaringType?.FullName + "." + method.Name + ": " + entry.Key);
                merged[entry.Key] = entry.Value;
            }
            LiteralRoutes[method] = merged;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) LiteralRoutes.TryGetValue(original, out entries);
            return TranslateInstructions(instructions, entries);
        }

        internal static IEnumerable<CodeInstruction> TranspileBinding(
            IEnumerable<CodeInstruction> instructions,
            int bindingId)
        {
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) LiteralBindingRoutes.TryGetValue(bindingId, out entries);
            return TranslateInstructions(instructions, entries);
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

        private static void PostfixString(MethodBase __originalMethod, ref string __result)
        {
            if (__result == null) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultRoutes.TryGetValue(__originalMethod, out entries);
            if (entries != null && entries.TryGetValue(__result, out var translation)) __result = translation;
        }

        private static void PostfixByArgument(MethodBase __originalMethod, object[] __args, ref string __result)
        {
            Tuple<int, IReadOnlyDictionary<string, string>> route;
            lock (Gate) ArgumentRoutes.TryGetValue(__originalMethod, out route);
            if (route == null || __args == null || route.Item1 >= __args.Length) return;
            var source = __args[route.Item1] as string;
            if (source != null && route.Item2.TryGetValue(source, out var translation)) __result = translation;
        }

        private static void PostfixByArguments(MethodBase __originalMethod, object[] __args, ref string __result)
        {
            if (__args == null || __args.Length < 2 || !(__args[0] is string identity)) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultRoutes.TryGetValue(__originalMethod, out entries);
            if (entries == null) return;
            var key = identity + "|" + Convert.ToString(__args[1], System.Globalization.CultureInfo.InvariantCulture);
            if (entries.TryGetValue(key, out var translation)) __result = translation;
        }

        private static void PostfixStringArray(MethodBase __originalMethod, ref string[] __result)
        {
            if (__result == null) return;
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) ResultRoutes.TryGetValue(__originalMethod, out entries);
            if (entries == null) return;
            string[] translatedResult = null;
            for (var index = 0; index < __result.Length; index++)
            {
                if (__result[index] == null || !entries.TryGetValue(__result[index], out var translation)) continue;
                if (translatedResult == null) translatedResult = (string[])__result.Clone();
                translatedResult[index] = translation;
            }
            if (translatedResult != null) __result = translatedResult;
        }
    }
}
