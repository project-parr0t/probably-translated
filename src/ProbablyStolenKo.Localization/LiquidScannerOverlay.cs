using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class LiquidScannerOverlay
    {
        private const string ErrorPrefix = "Translation Error '";
        private const string LiquidErrorPrefix = ErrorPrefix + "liquid_";
        private const string ErrorSuffix = "' in Mechanic";
        private readonly Harmony _harmony = new Harmony("probably-stolen-ko-localization.liquid-scanners");

        internal void Apply()
        {
            var transpiler = CreateTranspiler();
            _harmony.Patch(
                ModReflect.Target(typeof(ChemScannerUIManager), nameof(ChemScannerUIManager.RefreshUI), Type.EmptyTypes),
                transpiler: transpiler);
            _harmony.Patch(
                ModReflect.Target(typeof(WaterScannerUIManager), nameof(WaterScannerUIManager.RefreshUI), Type.EmptyTypes),
                transpiler: transpiler);
        }

        internal void Restore() => _harmony.UnpatchAll(_harmony.Id);

        internal static HarmonyMethod CreateTranspiler() =>
            new HarmonyMethod(typeof(LiquidScannerOverlay), nameof(RefreshComponentNames));

        private static IEnumerable<CodeInstruction> RefreshComponentNames(IEnumerable<CodeInstruction> instructions)
        {
            var resolve = new HarmonyMethod(typeof(LiquidScannerOverlay), nameof(ResolveName)).method;
            var count = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field &&
                    field.DeclaringType == typeof(Liquid) && field.Name == nameof(Liquid.displayName))
                {
                    yield return new CodeInstruction(OpCodes.Call, resolve);
                    count++;
                }
            }
            if (count != 1) throw new InvalidOperationException("Expected one liquid component name read.");
        }

        // Re-read only cached lookup failures. Do not change the shared Liquid registry.
        private static string ResolveName(string display)
        {
            if (display == null || display.Length <= LiquidErrorPrefix.Length + ErrorSuffix.Length ||
                !display.StartsWith(LiquidErrorPrefix, StringComparison.Ordinal) ||
                !display.EndsWith(ErrorSuffix, StringComparison.Ordinal)) return display;

            var key = display.Substring(ErrorPrefix.Length, display.Length - ErrorPrefix.Length - ErrorSuffix.Length);
            var current = LocHelper.GetLocalizedMechanic(key);
            return string.IsNullOrEmpty(current) || current == key || current.StartsWith(ErrorPrefix, StringComparison.Ordinal)
                ? display : current;
        }
    }
}
