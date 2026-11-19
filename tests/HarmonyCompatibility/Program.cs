using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

        CheckTarget(mod, 56, typeof(SmartInventoryArrange.UnifiedSettingsGui).GetConstructor(new[] { typeof(ModConfig) }));
        CheckTarget(mod, 80, typeof(SmartInventoryArrange.RussianText).TypeInitializer);
        Console.WriteLine($"constructor-compatibility=ok harmony={typeof(Harmony).Assembly.GetName().Version}");
    }

    private static void CheckTarget(Assembly mod, int bindingId, ConstructorInfo expected)
    {
        var patch = mod.GetType($"ProbablyStolenKo.GeneratedManagedPatch_{bindingId}", throwOnError: true);
        var resolver = patch.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.IsDefined(typeof(HarmonyTargetMethod), inherit: false));
        if (!Equals(resolver.Invoke(null, null), expected))
            throw new InvalidOperationException($"{patch.Name} did not resolve {expected}.");
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
