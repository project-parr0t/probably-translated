using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string fieldType,
            string fieldName,
            IReadOnlyDictionary<string, string> entries)
        {
            TypeName = typeName;
            MethodName = methodName;
            Mode = mode;
            ArgumentIndex = argumentIndex;
            FieldType = fieldType;
            FieldName = fieldName;
            Entries = entries;
        }

        internal string TypeName { get; }
        internal string MethodName { get; }
        internal string Mode { get; }
        internal int ArgumentIndex { get; }
        internal string FieldType { get; }
        internal string FieldName { get; }
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
            bool displayLiteralOverlay,
            IReadOnlyDictionary<string, string> displayReplacements,
            IReadOnlyList<ManagedRoute> routes,
            IReadOnlyList<ManagedRefresh> refresh)
        {
            WorkshopItemId = workshopItemId;
            AssemblyName = assemblyName;
            EntryCount = entryCount;
            ManifestName = manifestName;
            ManifestDescription = manifestDescription;
            DisplayLiteralOverlay = displayLiteralOverlay;
            DisplayReplacements = displayReplacements;
            Routes = routes;
            Refresh = refresh;
        }

        internal string WorkshopItemId { get; }
        internal string AssemblyName { get; }
        internal int EntryCount { get; }
        internal string ManifestName { get; }
        internal string ManifestDescription { get; }
        internal bool DisplayLiteralOverlay { get; }
        internal IReadOnlyDictionary<string, string> DisplayReplacements { get; }
        internal IReadOnlyList<ManagedRoute> Routes { get; }
        internal IReadOnlyList<ManagedRefresh> Refresh { get; }
    }

    internal sealed class ManagedLiteralCatalog
    {
        private const string ExpectedFingerprint = "a84c14b34b0c618aa3ea1673bbdc3016ce7f9efb2e03e188828d14c3b020d342";
        private const int ExpectedEntries = 26213;
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
                ["space_coffee"] = "3834667583",
                ["upper_levels"] = "3838944803",
                ["combat_contracts"] = "3834364038",
                ["gunmod_tarkov"] = "3831650864",
                ["probably_flyers"] = "3833522385",
                ["eleanor_rescue"] = "3834076808",
                ["charles_chemistry_items"] = "3838151650",
                ["probably_helpful"] = "3838230589",
                ["va11halla"] = "3831732004",
                ["DeepSpaceClinic_InventoryTest"] = "3832819040",
                ["local.better-box-selection"] = "3836727677",
                ["duoji_hotkey_rebind"] = "3829508405",
                ["yexiaoyang.InteriorExpansion"] = "3841509159",
            };
        private const int ExpectedTargets = 35;

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
                var displayLiteralOverlay = (bool?)target?["display_literal_overlay"] ?? false;
                var sourceDisplayReplacements = target?["display_replacements"] as JObject;
                var sourceRoutes = target?["routes"] as JArray;
                var sourceRefresh = target?["refresh"] as JArray;
                if (string.IsNullOrWhiteSpace(targetProperty.Name) || string.IsNullOrWhiteSpace(workshopItemId) ||
                    !entryCount.HasValue || entryCount.Value < 0 || sourceRoutes == null || sourceRefresh == null ||
                    (sourceRoutes.Count != 0 && string.IsNullOrWhiteSpace(assemblyName)))
                    throw new FormatException("Incomplete managed target record.");
                string expectedWorkshopItem;
                if (!ExpectedWorkshopItems.TryGetValue(targetProperty.Name, out expectedWorkshopItem) ||
                    !StringComparer.Ordinal.Equals(workshopItemId, expectedWorkshopItem))
                    throw new FormatException("Managed target Workshop item identity is invalid.");
                var displayReplacements = new Dictionary<string, string>(StringComparer.Ordinal);
                if (sourceDisplayReplacements != null)
                {
                    foreach (var replacement in sourceDisplayReplacements.Properties())
                    {
                        var translation = (string)replacement.Value;
                        if (string.IsNullOrEmpty(replacement.Name) || translation == null)
                            throw new FormatException("Invalid managed display replacement.");
                        displayReplacements.Add(replacement.Name, translation);
                    }
                }
                var routes = new List<ManagedRoute>(sourceRoutes.Count);
                foreach (var routeToken in sourceRoutes)
                {
                    var route = routeToken as JObject;
                    var typeName = (string)route?["type"];
                    var methodName = (string)route?["method"];
                    var mode = (string)route?["mode"];
                    var argumentIndex = (int?)route?["argument_index"] ?? 0;
                    var fieldType = (string)route?["field_type"];
                    var fieldName = (string)route?["field_name"];
                    var sourceEntries = route?["entries"] as JObject;
                    if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(methodName) ||
                        (mode != "literal" && mode != "literal_occurrence" && mode != "field_store" &&
                         mode != "result_string" && mode != "result_string_array" &&
                         mode != "result_by_argument" && mode != "result_by_arguments") ||
                        (mode == "field_store" && (string.IsNullOrWhiteSpace(fieldType) || string.IsNullOrWhiteSpace(fieldName))) ||
                        argumentIndex < 0 || sourceEntries == null)
                        throw new FormatException("Incomplete managed route record.");
                    var entries = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var entry in sourceEntries.Properties())
                    {
                        var translation = (string)entry.Value;
                        if (string.IsNullOrEmpty(entry.Name) || translation == null) throw new FormatException("Invalid managed route entry.");
                        entries.Add(entry.Name, translation);
                    }
                    routes.Add(new ManagedRoute(typeName, methodName, mode, argumentIndex, fieldType, fieldName, entries));
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
                    workshopItemId, assemblyName, entryCount.Value, manifestName, manifestDescription,
                    displayLiteralOverlay, displayReplacements, routes, refresh));
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
        private static readonly Dictionary<int, Tuple<string, string, IReadOnlyDictionary<string, string>>> FieldStoreBindingRoutes =
            new Dictionary<int, Tuple<string, string, IReadOnlyDictionary<string, string>>>();
        private static readonly HashSet<int> EnabledStaticBindings = new HashSet<int>();

        private sealed class PendingTarget
        {
            private Dictionary<string, Type> _typeIndex;

            internal PendingTarget(
                string modId,
                ManagedTarget target,
                IReadOnlyList<ManagedExactBinding> bindings,
                Harmony harmony)
            {
                ModId = modId;
                Target = target;
                Bindings = bindings;
                Harmony = harmony;
                StaticBindings = new List<int>();
                Started = Stopwatch.GetTimestamp();
            }

            internal string ModId { get; }
            internal ManagedTarget Target { get; }
            internal IReadOnlyList<ManagedExactBinding> Bindings { get; }
            internal Harmony Harmony { get; }
            internal List<int> StaticBindings { get; }
            internal long Started { get; }
            internal long FirstWorkStarted;
            internal long LastSliceEnded;
            internal long BetweenSlicesTicks;
            internal long BindingTicks;
            internal long ResolveTicks;
            internal long TypeIndexTicks;
            internal int IndexedTypes;
            internal long HarmonyTicks;
            internal long ConstructorHarmonyTicks;
            internal long SlowestBindingTicks;
            internal ManagedExactBinding SlowestBinding;
            internal int Index { get; set; }
            internal int Applied { get; set; }
            internal int Skipped { get; set; }
            internal bool RefreshPrepared { get; set; }

            internal MethodBase ResolveMethod(ManagedExactBinding binding)
            {
                var types = _typeIndex ?? BuildTypeIndex();
                if (!types.TryGetValue(binding.TypeName, out var type) || type == null)
                    throw new ModReflectAccessException($"No unique patch type '{binding.TypeName}' in '{ModId}'.");

                // Keep overload selection and the declaring type's access check in the game API.
                return ModReflect.Target(type, binding.MethodName, binding.ParameterTypes ?? Type.EmptyTypes);
            }

            private Dictionary<string, Type> BuildTypeIndex()
            {
                var started = Stopwatch.GetTimestamp();
                try
                {
                    var types = new Dictionary<string, Type>(StringComparer.Ordinal);
                    foreach (var binding in Bindings)
                        if (!binding.UsesPatchAttributes) types.TryAdd(binding.TypeName, null);

                    var count = 0;
                    foreach (var type in ModReflect.Types(ModId))
                    {
                        var name = type.FullName;
                        if (name == null || !types.TryGetValue(name, out var previous)) continue;
                        if (previous == null)
                        {
                            types[name] = type;
                            count++;
                        }
                        else if (previous != type)
                        {
                            // Leave an ambiguous name unavailable even if it appears again.
                            types.Remove(name);
                            count--;
                        }
                    }

                    IndexedTypes = count;
                    return _typeIndex = types;
                }
                finally
                {
                    TypeIndexTicks += Stopwatch.GetTimestamp() - started;
                }
            }
        }

        private readonly Dictionary<string, Harmony> _harmonies =
            new Dictionary<string, Harmony>(StringComparer.Ordinal);
        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _attemptedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _appliedRefresh = new List<string>();
        private readonly ManagedDisplayOverlay _displayOverlay = new ManagedDisplayOverlay();
        private readonly ModLog _log;
        private readonly Queue<PendingTarget> _pendingTargets = new Queue<PendingTarget>();
        private PendingTarget _currentTarget;
        private int _completedWork;
        private int _totalWork;
        private bool _queueDrainLogged = true;

        internal ManagedLiteralOverlay(ModLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        internal bool HasPendingWork => _currentTarget != null || _pendingTargets.Count != 0;
        internal int CompletedWork => _completedWork;
        internal int TotalWork => _totalWork;
        internal string CurrentModId =>
            _currentTarget != null
                ? _currentTarget.ModId
                : _pendingTargets.Count != 0 ? _pendingTargets.Peek().ModId : null;
        internal int CurrentBindingCompleted => _currentTarget != null ? _currentTarget.Index : 0;
        internal int CurrentBindingTotal =>
            _currentTarget != null
                ? _currentTarget.Bindings.Count
                : _pendingTargets.Count != 0 ? _pendingTargets.Peek().Bindings.Count : 0;

        internal void QueueTarget(string modId, ManagedTarget target)
        {
            if (target == null || _appliedTargets.Contains(modId) || !_attemptedTargets.Add(modId)) return;
            var bindings = GeneratedManagedBindings.Create(modId);
            if (target.Routes.Count != 0 && bindings.Count == 0)
                throw new InvalidOperationException("No generated exact bindings exist for managed target " + modId + ".");

            int consolidated;
            bindings = ConsolidateBindings(target, bindings, out target, out consolidated);
            if (consolidated != 0)
                _log.Log("Consolidated " + consolidated + " duplicate managed bindings for " + modId + ".");

            var harmony = new Harmony(HarmonyId + "." + modId);
            _harmonies.Add(modId, harmony);
            _pendingTargets.Enqueue(new PendingTarget(modId, target, bindings, harmony));
            _queueDrainLogged = false;
            _totalWork += bindings.Count + 1;
        }

        private static IReadOnlyList<ManagedExactBinding> ConsolidateBindings(
            ManagedTarget source,
            IReadOnlyList<ManagedExactBinding> bindings,
            out ManagedTarget target,
            out int consolidated)
        {
            var routes = new List<ManagedRoute>(source.Routes);
            var output = new List<ManagedExactBinding>(bindings.Count);
            var candidates = new Dictionary<string, ManagedExactBinding>(StringComparer.Ordinal);
            consolidated = 0;

            foreach (var binding in bindings)
            {
                var key = binding.Kind + "\u001f" + binding.PatchMethodName + "\u001f" +
                          binding.TypeName + "\u001f" + binding.MethodName + "\u001f" + binding.Signature;
                ManagedExactBinding existing;
                if (binding.Kind == ManagedBindingKind.StaticPatch &&
                    candidates.TryGetValue(key, out existing) &&
                    TryMergeRoutes(routes, existing.RouteIndex, binding.RouteIndex))
                {
                    consolidated++;
                    continue;
                }
                output.Add(binding);
                if (binding.Kind == ManagedBindingKind.StaticPatch) candidates[key] = binding;
            }

            if (consolidated == 0)
            {
                target = source;
                return bindings;
            }
            target = new ManagedTarget(
                source.WorkshopItemId,
                source.AssemblyName,
                source.EntryCount,
                source.ManifestName,
                source.ManifestDescription,
                source.DisplayLiteralOverlay,
                source.DisplayReplacements,
                routes,
                source.Refresh);
            return output;
        }

        private static bool TryMergeRoutes(List<ManagedRoute> routes, int destinationIndex, int sourceIndex)
        {
            var destination = routes[destinationIndex];
            var source = routes[sourceIndex];
            var destinationLiteral = destination.Mode == "literal" || destination.Mode == "literal_occurrence";
            var sourceLiteral = source.Mode == "literal" || source.Mode == "literal_occurrence";
            if ((!destinationLiteral || !sourceLiteral) && destination.Mode != source.Mode) return false;
            if (destination.ArgumentIndex != source.ArgumentIndex ||
                !StringComparer.Ordinal.Equals(destination.FieldType, source.FieldType) ||
                !StringComparer.Ordinal.Equals(destination.FieldName, source.FieldName))
                return false;

            foreach (var entry in source.Entries)
            {
                string translation;
                if (destination.Entries.TryGetValue(entry.Key, out translation) &&
                    !StringComparer.Ordinal.Equals(translation, entry.Value))
                    return false;
            }

            var entries = new Dictionary<string, string>(destination.Entries, StringComparer.Ordinal);
            foreach (var entry in source.Entries) entries[entry.Key] = entry.Value;
            routes[destinationIndex] = new ManagedRoute(
                destination.TypeName,
                destination.MethodName,
                destinationLiteral ? "literal" : destination.Mode,
                destination.ArgumentIndex,
                destination.FieldType,
                destination.FieldName,
                entries);
            return true;
        }

        internal void ProcessPending(int millisecondBudget)
        {
            if (millisecondBudget <= 0) throw new ArgumentOutOfRangeException(nameof(millisecondBudget));
            var frameStarted = Stopwatch.GetTimestamp();
            if (_currentTarget != null)
                _currentTarget.BetweenSlicesTicks += frameStarted - _currentTarget.LastSliceEnded;
            long sliceEnded;
            do
            {
                if (_currentTarget == null)
                {
                    if (_pendingTargets.Count == 0) return;
                    _currentTarget = _pendingTargets.Dequeue();
                    _currentTarget.FirstWorkStarted = Stopwatch.GetTimestamp();
                }

                var work = _currentTarget;
                if (work.Index < work.Bindings.Count)
                {
                    var binding = work.Bindings[work.Index++];
                    var bindingStarted = Stopwatch.GetTimestamp();
                    try
                    {
                        ApplyBinding(work, binding);
                        work.Applied++;
                    }
                    catch (Exception error)
                    {
                        work.Skipped++;
                        work.StaticBindings.Remove(binding.Id);
                        RemoveBindingState(binding.Id);
                        var cause = error.GetBaseException();
                        _log.Error(
                            "Skipped managed binding " + binding.Id + " (" + binding.TypeName + "." +
                            binding.MethodName + "). " + cause.GetType().Name + ": " + cause.Message);
                    }
                    var bindingTicks = Stopwatch.GetTimestamp() - bindingStarted;
                    work.BindingTicks += bindingTicks;
                    if (work.SlowestBinding == null || bindingTicks > work.SlowestBindingTicks)
                    {
                        work.SlowestBindingTicks = bindingTicks;
                        work.SlowestBinding = binding;
                    }
                    _completedWork++;
                }
                else
                {
                    var finalizationStarted = Stopwatch.GetTimestamp();
                    CompleteCurrentTarget();
                    LogTargetTiming(work, Stopwatch.GetTimestamp() - finalizationStarted);
                    _completedWork++;
                    _currentTarget = null;
                    if (!HasPendingWork && !_queueDrainLogged)
                    {
                        _queueDrainLogged = true;
                        _log.Log(
                            "Managed Korean patch queue completed: work=" +
                            _completedWork + "/" + _totalWork + ".");
                    }
                }
                sliceEnded = Stopwatch.GetTimestamp();
            }
            while (HasPendingWork &&
                   (sliceEnded - frameStarted) * 1000L / Stopwatch.Frequency < millisecondBudget);
            if (_currentTarget != null) _currentTarget.LastSliceEnded = sliceEnded;
        }

        private void LogTargetTiming(PendingTarget work, long finalizationTicks)
        {
            var slowest = work.SlowestBinding;
            _log.Log(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Managed patch timing: mod={0} queueMs={1:F1} betweenSlicesMs={2:F1} bindingMs={3:F1} " +
                "resolveMs={4:F1} harmonyMs={5:F1} constructorHarmonyMs={6:F1} finalizeMs={7:F1} " +
                "slowestBindingMs={8:F1} slowestBinding={9} typeIndexMs={10:F1} indexedTypes={11}",
                work.ModId,
                ToMilliseconds(work.FirstWorkStarted - work.Started),
                ToMilliseconds(work.BetweenSlicesTicks),
                ToMilliseconds(work.BindingTicks),
                ToMilliseconds(work.ResolveTicks),
                ToMilliseconds(work.HarmonyTicks),
                ToMilliseconds(work.ConstructorHarmonyTicks),
                ToMilliseconds(finalizationTicks),
                ToMilliseconds(work.SlowestBindingTicks),
                slowest == null ? "-" : $"{slowest.Id}:{slowest.TypeName}.{slowest.MethodName}",
                ToMilliseconds(work.TypeIndexTicks),
                work.IndexedTypes));
        }

        private static double ToMilliseconds(long ticks) => ticks * (1000.0 / Stopwatch.Frequency);

        private void CompleteCurrentTarget()
        {
            var work = _currentTarget;
            try
            {
                work.RefreshPrepared = true;
                GeneratedManagedRefresh.Prepare(work.Harmony, work.ModId);
                GeneratedManagedRefresh.Invoke(work.ModId);
                _displayOverlay.ApplyTarget(work.ModId, work.Target);
                if (work.Target.Refresh.Count != 0) _appliedRefresh.Add(work.ModId);
                _appliedTargets.Add(work.ModId);
                var elapsedMs = (Stopwatch.GetTimestamp() - work.Started) * 1000L / Stopwatch.Frequency;
                _log.Log(
                    "Applied " + work.Applied + " of " + work.Bindings.Count +
                    " managed Korean bindings to " + work.ModId + " in " + elapsedMs +
                    " ms; skipped=" + work.Skipped + ".");
            }
            catch (Exception error)
            {
                RollbackTarget(
                    work.ModId,
                    work.Harmony,
                    work.StaticBindings,
                    work.RefreshPrepared);
                var cause = error.GetBaseException();
                _log.Error(
                    "Managed target finalization failed for " + work.ModId + ". " +
                    cause.GetType().Name + ": " + cause.Message);
            }
        }

        private static void ApplyBinding(PendingTarget work, ManagedExactBinding binding)
        {
            var target = work.Target;
            if (binding.RouteIndex < 0 || binding.RouteIndex >= target.Routes.Count)
                throw new InvalidOperationException("Generated managed route index is outside the runtime catalog.");
            var route = target.Routes[binding.RouteIndex];
            if (binding.Kind == ManagedBindingKind.Unsupported)
                throw new InvalidOperationException("Unsupported generated managed binding: " + binding.Issue);

            if (binding.Kind != ManagedBindingKind.StaticPatch)
                throw new InvalidOperationException("Generated managed binding is not an exact static patch.");
            work.StaticBindings.Add(binding.Id);
            lock (Gate)
            {
                if (route.Mode == "literal" || route.Mode == "literal_occurrence")
                    LiteralBindingRoutes[binding.Id] = route.Entries;
                else if (route.Mode == "field_store")
                    FieldStoreBindingRoutes[binding.Id] = Tuple.Create(route.FieldType, route.FieldName, route.Entries);
                else if (route.Mode == "result_by_argument")
                    ArgumentBindingRoutes[binding.Id] = Tuple.Create(route.ArgumentIndex, route.Entries);
                else ResultBindingRoutes[binding.Id] = route.Entries;
                EnabledStaticBindings.Add(binding.Id);
            }
            try
            {
                if (binding.UsesPatchAttributes)
                {
                    var constructorStarted = Stopwatch.GetTimestamp();
                    try
                    {
                        work.Harmony.CreateClassProcessor(binding.PatchType).Patch();
                    }
                    finally
                    {
                        work.ConstructorHarmonyTicks += Stopwatch.GetTimestamp() - constructorStarted;
                    }
                    return;
                }

                MethodBase original;
                var resolveStarted = Stopwatch.GetTimestamp();
                try
                {
                    original = work.ResolveMethod(binding);
                }
                finally
                {
                    work.ResolveTicks += Stopwatch.GetTimestamp() - resolveStarted;
                }

                var harmonyStarted = Stopwatch.GetTimestamp();
                try
                {
                    var patch = new HarmonyMethod(binding.PatchType, binding.PatchMethodName);
                    if (binding.PatchMethodName == "Transpiler")
                        work.Harmony.Patch(original, transpiler: patch);
                    else
                        work.Harmony.Patch(original, postfix: patch);
                }
                finally
                {
                    work.HarmonyTicks += Stopwatch.GetTimestamp() - harmonyStarted;
                }
            }
            finally
            {
                lock (Gate) EnabledStaticBindings.Remove(binding.Id);
            }
        }

        private static void RemoveBindingState(int bindingId)
        {
            lock (Gate)
            {
                LiteralBindingRoutes.Remove(bindingId);
                ResultBindingRoutes.Remove(bindingId);
                ArgumentBindingRoutes.Remove(bindingId);
                FieldStoreBindingRoutes.Remove(bindingId);
                EnabledStaticBindings.Remove(bindingId);
            }
        }

        private void RollbackTarget(
            string modId,
            Harmony harmony,
            IEnumerable<int> staticBindings,
            bool refreshPrepared)
        {
            harmony.UnpatchAll(harmony.Id);
            foreach (var bindingId in staticBindings) RemoveBindingState(bindingId);
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
            _displayOverlay.Restore();
            foreach (var harmony in _harmonies.Values) harmony.UnpatchAll(harmony.Id);
            lock (Gate)
            {
                LiteralBindingRoutes.Clear();
                ResultBindingRoutes.Clear();
                ArgumentBindingRoutes.Clear();
                EnabledStaticBindings.Clear();
                FieldStoreBindingRoutes.Clear();
            }
            foreach (var modId in _appliedRefresh)
            {
                try { GeneratedManagedRefresh.Invoke(modId); }
                catch (Exception error) { _log.Error("Managed restore refresh failed after " + error.GetType().Name + "."); }
            }
            _appliedRefresh.Clear();
            _harmonies.Clear();
            _appliedTargets.Clear();
            _attemptedTargets.Clear();
            _pendingTargets.Clear();
            _currentTarget = null;
            _completedWork = 0;
            _totalWork = 0;
            _queueDrainLogged = true;
        }


        internal static IEnumerable<CodeInstruction> TranspileBinding(
            IEnumerable<CodeInstruction> instructions,
            int bindingId)
        {
            IReadOnlyDictionary<string, string> entries;
            lock (Gate) LiteralBindingRoutes.TryGetValue(bindingId, out entries);
            return TranslateInstructions(instructions, entries);
        }

        internal static IEnumerable<CodeInstruction> TranspileFieldStoreBinding(
            IEnumerable<CodeInstruction> instructions,
            int bindingId)
        {
            Tuple<string, string, IReadOnlyDictionary<string, string>> route;
            lock (Gate) FieldStoreBindingRoutes.TryGetValue(bindingId, out route);
            if (route == null) throw new InvalidOperationException("Field-store route was not prepared.");

            var source = new List<CodeInstruction>(instructions);
            var output = new List<CodeInstruction>(source.Count + 2);
            var matches = 0;
            foreach (var instruction in source)
            {
                if (instruction.opcode == OpCodes.Stfld &&
                    instruction.operand != null &&
                    StringComparer.Ordinal.Equals(instruction.operand.ToString(), "System.String " + route.Item2))
                {
                    output.Add(new CodeInstruction(OpCodes.Ldc_I4, bindingId));
                    output.Add(CodeInstruction.Call(typeof(ManagedLiteralOverlay), nameof(TranslateFieldStoreValue)));
                    matches++;
                }
                output.Add(instruction);
            }
            if (matches != 1)
                throw new InvalidOperationException("Expected exactly one reviewed field store, found " + matches + ".");
            return output;
        }

        internal static string TranslateFieldStoreValue(string source, int bindingId)
        {
            if (source == null) return null;
            Tuple<string, string, IReadOnlyDictionary<string, string>> route;
            lock (Gate) FieldStoreBindingRoutes.TryGetValue(bindingId, out route);
            return route != null && route.Item3.TryGetValue(source, out var translation) ? translation : source;
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
