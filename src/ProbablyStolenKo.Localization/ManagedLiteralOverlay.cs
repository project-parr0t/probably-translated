using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string HarmonyId = "probably-stolen-ko-localization.managed-r8";
        private static readonly object Gate = new object();
        private static readonly Dictionary<MethodBase, IReadOnlyDictionary<string, string>> LiteralRoutes =
            new Dictionary<MethodBase, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<MethodBase, IReadOnlyDictionary<string, string>> ResultRoutes =
            new Dictionary<MethodBase, IReadOnlyDictionary<string, string>>();
        private static readonly Dictionary<MethodBase, Tuple<int, IReadOnlyDictionary<string, string>>> ArgumentRoutes =
            new Dictionary<MethodBase, Tuple<int, IReadOnlyDictionary<string, string>>>();

        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly HashSet<string> _appliedTargets = new HashSet<string>(StringComparer.Ordinal);
        private readonly ModLog _log;
        private readonly List<Tuple<Assembly, ManagedTarget>> _appliedRefresh =
            new List<Tuple<Assembly, ManagedTarget>>();

        internal ManagedLiteralOverlay(ModLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        internal void ApplyTarget(string modId, ManagedTarget target)
        {
            if (target == null || _appliedTargets.Contains(modId)) return;
            Assembly assembly = null;
            if (target.Routes.Count != 0 || target.Refresh.Count != 0)
            {
                var candidates = AppDomain.CurrentDomain.GetAssemblies().Where(candidate =>
                        StringComparer.OrdinalIgnoreCase.Equals(candidate.GetName().Name, target.AssemblyName) &&
                        RoutesBelongToAssembly(candidate, target))
                    .ToArray();
                var owned = candidates.Where(candidate =>
                    AssemblyPathMatchesWorkshop(candidate, target.WorkshopItemId)).ToArray();
                RequireSingleOwnerPath(owned.Select(candidate => candidate.Location), target.WorkshopItemId);
                assembly = owned[0];
            }

            var patched = new List<MethodBase>();
            try
            {
                foreach (var route in target.Routes)
                {
                    var routeTypes = route.TypeName == "*"
                        ? assembly.GetTypes()
                        : route.TypeName.EndsWith("*", StringComparison.Ordinal)
                            ? assembly.GetTypes().Where(candidate =>
                                candidate.FullName != null &&
                                candidate.FullName.StartsWith(
                                    route.TypeName.Substring(0, route.TypeName.Length - 1),
                                    StringComparison.Ordinal)).ToArray()
                            : new[] { assembly.GetType(route.TypeName, false, false) };
                    if (routeTypes.Length == 0 || routeTypes.Any(candidate => candidate == null))
                        throw new MissingMemberException(route.TypeName);
                    var methods = routeTypes.SelectMany(type =>
                            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                .Cast<MethodBase>()
                                .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                                .Concat(type.TypeInitializer == null
                                    ? Array.Empty<MethodBase>()
                                    : new MethodBase[] { type.TypeInitializer }))
                        .Where(method => route.MethodName == "*" ||
                            StringComparer.Ordinal.Equals(method.Name, route.MethodName))
                        .Where(method => route.Mode == "literal" || route.Mode == "literal_occurrence" ||
                            (method is MethodInfo info && (
                                ((route.Mode == "result_string" || route.Mode == "result_by_argument" ||
                                  route.Mode == "result_by_arguments") &&
                                 info.ReturnType == typeof(string)) ||
                                (route.Mode == "result_string_array" && info.ReturnType == typeof(string[])))))
                        .ToArray();
                    if (methods.Length == 0) throw new MissingMethodException(route.TypeName, route.MethodName);
                    foreach (var method in methods)
                    {
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
                            transpiler = new HarmonyMethod(typeof(ManagedLiteralOverlay).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.NonPublic));
                        else if (route.Mode == "result_string")
                            postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay).GetMethod(nameof(PostfixString), BindingFlags.Static | BindingFlags.NonPublic));
                        else if (route.Mode == "result_by_argument")
                            postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay).GetMethod(nameof(PostfixByArgument), BindingFlags.Static | BindingFlags.NonPublic));
                        else if (route.Mode == "result_by_arguments")
                            postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay).GetMethod(nameof(PostfixByArguments), BindingFlags.Static | BindingFlags.NonPublic));
                        else
                            postfix = new HarmonyMethod(typeof(ManagedLiteralOverlay).GetMethod(nameof(PostfixStringArray), BindingFlags.Static | BindingFlags.NonPublic));
                        _harmony.Patch(method, postfix: postfix, transpiler: transpiler);
                        patched.Add(method);
                    }
                }
                if (assembly != null)
                {
                    InvokeRefresh(assembly, target);
                    _appliedRefresh.Add(Tuple.Create(assembly, target));
                }
                _appliedTargets.Add(modId);
                _log.Log("Applied managed Korean routes to " + modId + ".");
            }
            catch
            {
                foreach (var method in patched) _harmony.Unpatch(method, HarmonyPatchType.All, HarmonyId);
                lock (Gate)
                {
                    foreach (var method in patched)
                    {
                        LiteralRoutes.Remove(method);
                        ResultRoutes.Remove(method);
                        ArgumentRoutes.Remove(method);
                    }
                }
                if (assembly != null)
                {
                    try { InvokeRefresh(assembly, target); }
                    catch (Exception error) { _log.Error("Managed rollback refresh failed after " + error.GetType().Name + "."); }
                }
                throw;
            }
        }

        internal void Restore()
        {
            _harmony.UnpatchAll(HarmonyId);
            lock (Gate)
            {
                LiteralRoutes.Clear();
                ResultRoutes.Clear();
                ArgumentRoutes.Clear();
            }
            foreach (var applied in _appliedRefresh)
            {
                try { InvokeRefresh(applied.Item1, applied.Item2); }
                catch (Exception error) { _log.Error("Managed restore refresh failed after " + error.GetType().Name + "."); }
            }
            _appliedRefresh.Clear();
            _appliedTargets.Clear();
        }

        private static bool RoutesBelongToAssembly(Assembly assembly, ManagedTarget target)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error) { types = error.Types.Where(type => type != null).ToArray(); }
            foreach (var route in target.Routes)
            {
                if (route.TypeName == "*") continue;
                if (route.TypeName.EndsWith("*", StringComparison.Ordinal))
                {
                    var prefix = route.TypeName.Substring(0, route.TypeName.Length - 1);
                    if (!types.Any(type => type.FullName != null &&
                        type.FullName.StartsWith(prefix, StringComparison.Ordinal))) return false;
                }
                else if (!types.Any(type => StringComparer.Ordinal.Equals(type.FullName, route.TypeName)))
                    return false;
            }
            foreach (var refresh in target.Refresh)
                if (!types.Any(type => StringComparer.Ordinal.Equals(type.FullName, refresh.TypeName))) return false;
            return true;
        }

        internal static void RequireSingleOwnerPath(IEnumerable<string> locations, string workshopItemId)
        {
            if (locations == null || locations.Count(location =>
                    !string.IsNullOrEmpty(location) &&
                    location.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(segment => StringComparer.Ordinal.Equals(segment, workshopItemId))) != 1)
                throw new InvalidOperationException(
                    "Target managed assembly path does not match one unambiguous Workshop owner identity.");
        }

        private static bool AssemblyPathMatchesWorkshop(Assembly assembly, string workshopItemId)
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location)) return false;
            return location.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => StringComparer.Ordinal.Equals(segment, workshopItemId));
        }


        private static void InvokeRefresh(Assembly assembly, ManagedTarget target)
        {
            foreach (var action in target.Refresh)
            {
                var type = assembly.GetType(action.TypeName, false, false);
                var method = type?.GetMethod(
                    action.MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
                if (method == null) throw new MissingMethodException(action.TypeName, action.MethodName);
                method.Invoke(null, null);
            }
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
