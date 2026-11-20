using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class LabelerTextOverlay
    {
        private const string ErrorPrefix = "Translation Error '";
        private const string ErrorSuffix = "' in Mechanic";
        private const string CategoryPrefix = "item_feature_category_";
        private const string ConditionPrefix = "mech_condition_";
        private readonly Harmony _harmony = new Harmony("probably-stolen-ko-localization.labeler-text");

        internal void Apply()
        {
            _harmony.Patch(
                ModReflect.Target(typeof(LocHelper), "Get", new[] { typeof(string), typeof(string), typeof(object[]) }),
                prefix: new HarmonyMethod(typeof(LabelerTextOverlay), nameof(RepairConditionFormat)));
            _harmony.Patch(
                ModReflect.Target(typeof(LabelerUIManager), nameof(LabelerUIManager.OpenUI), new[] { typeof(GameItem) }),
                transpiler: new HarmonyMethod(typeof(LabelerTextOverlay), nameof(PatchOpen)));
            _harmony.Patch(
                ModReflect.Target(typeof(LabelerHelper), nameof(LabelerHelper.HandleLine), new[] { typeof(RichTextBuilder), typeof(ItemFeature) }),
                transpiler: new HarmonyMethod(typeof(LabelerTextOverlay), nameof(PatchTooltip)));
        }

        internal void Restore() => _harmony.UnpatchAll(_harmony.Id);

        // Both labeler value display sites pass a fresh formatting array. Leave the condition and modifier untouched.
        private static void RepairConditionFormat(string __0, string __1, object[] __2)
        {
            if (__1 == "mech_label_condition_value" && __0 == "Mechanic" &&
                __2?.Length == 2 && __2[0] is string display)
                __2[0] = Resolve(display, ConditionPrefix);
        }

        private static IEnumerable<CodeInstruction> PatchOpen(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceCall(instructions,
                ModReflect.Target(typeof(LabelerElement), nameof(LabelerElement.InitElement), new[] { typeof(ItemFeature) }),
                new HarmonyMethod(typeof(LabelerTextOverlay), nameof(InitRow)).method);
        }

        private static IEnumerable<CodeInstruction> PatchTooltip(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceCall(instructions,
                ModReflect.Target(typeof(ItemFeature), nameof(ItemFeature.GetBaseCurrentDisplay), Type.EmptyTypes),
                new HarmonyMethod(typeof(LabelerTextOverlay), nameof(GetTooltipDisplay)).method);
        }

        // Wrap the calls at the display sites instead of patching the methods that read the static caches.
        private static IEnumerable<CodeInstruction> ReplaceCall(IEnumerable<CodeInstruction> instructions, object target, object replacement)
        {
            var count = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, target))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    count++;
                }
                yield return instruction;
            }
            if (count != 1) throw new InvalidOperationException("Expected one vanilla labeler display call.");
        }

        private static void InitRow(LabelerElement element, ItemFeature feature)
        {
            element.InitElement(feature);
            var text = element.featureName.text;
            var resolved = Resolve(text, CategoryPrefix);
            if (!ReferenceEquals(text, resolved)) element.featureName.text = resolved;
        }

        private static string GetTooltipDisplay(ItemFeature feature) =>
            Resolve(feature.GetBaseCurrentDisplay(), ConditionPrefix);

        private static string Resolve(string display, string keyPrefix)
        {
            if (display == null || display.Length <= ErrorPrefix.Length + keyPrefix.Length + ErrorSuffix.Length ||
                !display.StartsWith(ErrorPrefix, StringComparison.Ordinal) ||
                !display.EndsWith(ErrorSuffix, StringComparison.Ordinal) ||
                string.CompareOrdinal(display, ErrorPrefix.Length, keyPrefix, 0, keyPrefix.Length) != 0)
                return display;

            var key = display.Substring(ErrorPrefix.Length, display.Length - ErrorPrefix.Length - ErrorSuffix.Length);
            var resolved = LocHelper.GetLocalizedMechanic(key);
            return string.IsNullOrEmpty(resolved) || resolved == key || resolved.StartsWith(ErrorPrefix, StringComparison.Ordinal)
                ? display : resolved;
        }
    }
}
