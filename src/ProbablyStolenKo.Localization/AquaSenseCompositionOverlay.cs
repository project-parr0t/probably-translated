using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class AquaSenseCompositionOverlay
    {
        internal const string TargetModId = "chiqingsan.AquaSense";
        private const string ErrorPrefix = "Translation Error '";
        private const string LiquidErrorPrefix = ErrorPrefix + "liquid_";
        private const string ErrorSuffix = "' in Mechanic";
        private readonly Harmony _harmony = new Harmony("probably-stolen-ko-localization.aquasense-composition");
        private bool _applied;

        internal void Apply()
        {
            if (_applied) return;
            var target = ModReflect.Target("AquaSense.Hooks", "AddLiquidEntry", new[]
            {
                typeof(List<(string, int)>), typeof(GameItem), typeof(Liquid), typeof(long).MakeByRefType()
            });
            _harmony.Patch(target,
                transpiler: new HarmonyMethod(typeof(AquaSenseCompositionOverlay), nameof(RefreshComponentNames)));
            _applied = true;
        }

        internal void Restore()
        {
            if (!_applied) return;
            _harmony.UnpatchAll(_harmony.Id);
            _applied = false;
        }

        private static IEnumerable<CodeInstruction> RefreshComponentNames(IEnumerable<CodeInstruction> instructions)
        {
            var resolve = new HarmonyMethod(typeof(AquaSenseCompositionOverlay), nameof(ResolveName)).method;
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
            if (count != 1) throw new InvalidOperationException("Expected one AquaSense component name read.");
        }

        // Resolve only cached lookup failures. Never write back to the shared Liquid registry.
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
