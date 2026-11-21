using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 3 && args.Length != 4)
            throw new ArgumentException("Expected: mod DLL, game Managed directory, runtime 0Harmony.dll, optional BetterLiquidScanner.dll");

        if (!StringComparer.OrdinalIgnoreCase.Equals(typeof(Harmony).Assembly.Location, args[2]))
            throw new InvalidOperationException($"Unexpected Harmony: {typeof(Harmony).Assembly.Location}");
        var mod = Assembly.LoadFrom(args[0]);
        Verify(mod, args[1]);
        if (args.Length == 4) CheckScannerFormatting(mod, Assembly.LoadFrom(args[3]));
    }

    private static void Verify(Assembly mod, string game)
    {
        var scan = ModScanner.Scan(new[] { mod.Location },
            System.IO.Directory.GetFiles(game, "*.dll").Select(Path.GetFileNameWithoutExtension),
            Path.GetDirectoryName(mod.Location), false);
        if (scan.Refused || scan.Error != null)
            throw new InvalidOperationException($"Mod scan failed: {scan.Error}\n{scan.FormatFindings(20)}");

        var harmony = new Harmony("probably-translated.compatibility-test");
        harmony.PatchAll(mod);
        if (harmony.GetPatchedMethods().Any())
            throw new InvalidOperationException("Assembly scan patched targets before the bridge enabled them.");

        CheckTarget(mod, typeof(SmartInventoryArrange.UnifiedSettingsGui).GetConstructor(new[] { typeof(ModConfig) }));
        CheckTarget(mod, typeof(SmartInventoryArrange.RussianText).TypeInitializer);
        Console.WriteLine($"constructor-compatibility=ok harmony={typeof(Harmony).Assembly.GetName().Version}");
    }

    private static void CheckScannerFormatting(Assembly mod, Assembly scanner)
    {
        const BindingFlags staticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        const BindingFlags instancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        Type BridgeType(string name) => mod.GetType("ProbablyStolenKo." + name, throwOnError: true);
        var original = AccessTools.Method(scanner.GetType("BetterLiquidScanner.L10n", throwOnError: true), "T",
            new[] { typeof(string), typeof(object[]) });
        var text = (Func<string, object[], string>)original.CreateDelegate(typeof(Func<string, object[], string>));
        var originalUnknown = text("no_such_scanner_key", Array.Empty<object>());

        var create = BridgeType("GeneratedManagedBindings").GetMethod("Create", staticPrivate);
        var binding = ((IEnumerable)create.Invoke(null, new object[] { "better_liquid_scanner" })).Cast<object>().Single();
        object Property(object obj, string name) => obj.GetType().GetProperty(name, instancePrivate).GetValue(obj);
        string payload = File.ReadAllText(Path.Combine(Path.GetDirectoryName(mod.Location), "managed-ko-runtime.json"));
        var catalog = BridgeType("ManagedLiteralCatalog").GetMethod("Parse", staticPrivate).Invoke(null, new object[] { payload });
        var target = ((IDictionary)Property(catalog, "Targets"))["better_liquid_scanner"];
        var route = ((IList)Property(target, "Routes"))[(int)Property(binding, "RouteIndex")];
        var routes = (IDictionary)BridgeType("ManagedLiteralOverlay").GetField("ResultBindingRoutes", staticPrivate).GetValue(null);
        var id = (int)Property(binding, "Id");
        var patch = ((Type)Property(binding, "PatchType")).GetMethod((string)Property(binding, "PatchMethodName"), staticPrivate);
        var harmony = new Harmony("probably-translated.scanner-format-test");
        routes.Add(id, Property(route, "Entries"));
        try
        {
            harmony.Patch(original, prefix: new HarmonyMethod(patch));
            void Expect(string expected, string key, params object[] args)
            {
                var actual = text(key, args);
                if (!StringComparer.Ordinal.Equals(expected, actual))
                    throw new InvalidOperationException($"Scanner {key}: expected [{expected}], got [{actual}].");
            }
            Expect("총 부피: 100 mL", "total_volume", "100 mL");
            Expect(": 30 mL", "inline_volume", "30 mL");
            Expect("물\n30 mL (30.00%)", "component_tooltip", "물", "30 mL", "30.00%");
            Expect("30000 uL", "amount_exact", 30000L);
            Expect("성분 비율", "composition_title");
            Expect(originalUnknown, "no_such_scanner_key");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            routes.Remove(id);
        }
        Console.WriteLine("scanner-formatting=ok volume, inline amount, tooltip, exact amount, static label, unknown key");
    }

    private static void CheckTarget(Assembly mod, ConstructorInfo expected)
    {
        var readInstructions = typeof(PatchProcessor).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(method => method.Name == "GetOriginalInstructions" &&
                method.GetParameters().Length == 2 && !method.GetParameters()[1].ParameterType.IsByRef);
        var resolver = mod.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            .Where(method => method.IsDefined(typeof(HarmonyTargetMethod), inherit: false))
            .Single(method =>
            {
                var instructions = (IEnumerable<CodeInstruction>)readInstructions.Invoke(null, new object[] { method, null });
                return instructions.Any(instruction => instruction.opcode == OpCodes.Ldstr &&
                           Equals(instruction.operand, expected.DeclaringType.FullName)) &&
                       instructions.Any(instruction => instruction.operand is MethodInfo call &&
                           call.DeclaringType == typeof(AccessTools) && call.Name == nameof(AccessTools.DeclaredConstructor));
            });
        if (!Equals(resolver.Invoke(null, null), expected))
            throw new InvalidOperationException($"{resolver.DeclaringType.Name} did not resolve {expected}.");
    }
}

namespace SmartInventoryArrange
{
    internal sealed class UnifiedSettingsGui
    {
        public UnifiedSettingsGui() { }
        public UnifiedSettingsGui(ModConfig config) { }
    }

    internal static class RussianText
    {
        static RussianText() { }
    }
}
