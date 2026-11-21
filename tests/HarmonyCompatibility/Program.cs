using System;
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
        if (args.Length != 3)
            throw new ArgumentException("Expected: mod DLL, game Managed directory, runtime 0Harmony.dll");

        if (!StringComparer.OrdinalIgnoreCase.Equals(typeof(Harmony).Assembly.Location, args[2]))
            throw new InvalidOperationException($"Unexpected Harmony: {typeof(Harmony).Assembly.Location}");
        Verify(Assembly.LoadFrom(args[0]), args[1]);
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
