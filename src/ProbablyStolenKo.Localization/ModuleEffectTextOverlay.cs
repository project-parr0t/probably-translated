using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class ModuleEffectTextOverlay
    {
        private const string ErrorPrefix = "Translation Error '";
        private const string ErrorSuffix = "' in Mechanic";
        private const string EffectErrorPrefix = ErrorPrefix + "mech_module_effect_";
        private readonly Harmony _harmony = new Harmony("probably-stolen-ko-localization.module-effect-text");
        private readonly ModLog _log;
        private bool _patchAttempted;

        internal ModuleEffectTextOverlay(ModLog log) => _log = log;

        internal void Apply() => ModHook.OnCreateTooltipEarly += OnTooltip;

        internal void Restore()
        {
            ModHook.OnCreateTooltipEarly -= OnTooltip;
            if (!_patchAttempted) return;
            _harmony.UnpatchAll(_harmony.Id);
            _patchAttempted = false;
        }

        private void OnTooltip(RichTextBuilder builder, GameItem item)
        {
            if (!item.IsTag(ModuleEffectHelper.MODULE_EFFECT1_TAG) &&
                !item.IsTag(ModuleEffectHelper.MODULE_EFFECT2_TAG)) return;

            // Patching a static method may run its type initializer. Wait until the first effect tooltip.
            ModHook.OnCreateTooltipEarly -= OnTooltip;
            _patchAttempted = true;
            try
            {
                _harmony.Patch(
                    ModReflect.Target(typeof(ModuleEffectHelper), nameof(ModuleEffectHelper.CreateModuleEffectTooltip),
                        new[] { typeof(RichTextBuilder), typeof(GameItem) }),
                    transpiler: new HarmonyMethod(typeof(ModuleEffectTextOverlay), nameof(RefreshEffectText)));
            }
            catch (Exception error)
            {
                _log.Error("Module effect text overlay failed: " + error.Message);
            }
        }

        private static IEnumerable<CodeInstruction> RefreshEffectText(IEnumerable<CodeInstruction> instructions)
        {
            var resolve = new HarmonyMethod(typeof(ModuleEffectTextOverlay), nameof(Resolve)).method;
            var names = 0;
            var descriptions = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode != OpCodes.Ldfld || !(instruction.operand is FieldInfo field) ||
                    field.DeclaringType != typeof(ModuleEffectHelper.ModuleEffect)) continue;
                if (field.Name == nameof(ModuleEffectHelper.ModuleEffect.displayName)) names++;
                else if (field.Name == nameof(ModuleEffectHelper.ModuleEffect.description)) descriptions++;
                else continue;
                yield return new CodeInstruction(OpCodes.Call, resolve);
            }
            if (names != 2 || descriptions != 2)
                throw new InvalidOperationException("Expected one name and description read for each of the two module effect slots.");
        }

        private static string Resolve(string display)
        {
            if (display == null || !display.StartsWith(EffectErrorPrefix, StringComparison.Ordinal) ||
                !display.EndsWith(ErrorSuffix, StringComparison.Ordinal)) return display;

            var key = display.Substring(ErrorPrefix.Length, display.Length - ErrorPrefix.Length - ErrorSuffix.Length);
            var arguments = key.EndsWith("_name", StringComparison.Ordinal) ? Array.Empty<object>() : DescriptionArguments.Get(key);
            if (arguments == null) return display;

            var current = LocHelper.GetLocalizedMechanic(key, arguments);
            // LocHelper can return the unformatted table entry when formatting fails.
            if (string.IsNullOrEmpty(current) || current == key || current.StartsWith(ErrorPrefix, StringComparison.Ordinal) ||
                (arguments.Length != 0 && current.IndexOf("{0}", StringComparison.Ordinal) >= 0) ||
                (arguments.Length == 2 && current.IndexOf("{1}", StringComparison.Ordinal) >= 0)) return display;
            return current;
        }

        private static class DescriptionArguments
        {
            // These are the boxed arguments passed by the stock ModuleEffectHelper initializer (0.46D).
            private static readonly object[] Twenty = { 20 };
            private static readonly object[] Nineteen = { 19 };
            private static readonly object[] Ten = { 10 };
            private static readonly object[] One = { 1 };
            private static readonly object[] Overvolted = { 200, 10 };
            private static readonly object[] NegativeFeedback = { 25, 25 };

            internal static object[] Get(string key)
            {
                switch (key)
                {
                    case "mech_module_effect_premium_desc":
                    case "mech_module_effect_top_mount_desc":
                    case "mech_module_effect_side_mount_desc":
                    case "mech_module_effect_support_node_desc": return Twenty;
                    case "mech_module_effect_inferior_desc": return Nineteen;
                    case "mech_module_effect_receiver_desc": return Ten;
                    case "mech_module_effect_degrading_desc":
                    case "mech_module_effect_learning_algorithm_desc": return One;
                    case "mech_module_effect_overvolted_desc": return Overvolted;
                    case "mech_module_effect_negative_feedback_desc": return NegativeFeedback;
                    default: return null;
                }
            }
        }
    }
}
