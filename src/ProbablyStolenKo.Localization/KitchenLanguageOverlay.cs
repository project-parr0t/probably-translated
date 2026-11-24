using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenKo
{
    internal sealed class KitchenLanguageOverlay
    {
        internal const string TargetModId = "xiaowo_kitchen_business";
        private const string TranslationType = "XIAOWOKitchenBusiness.KitchenRuntimeTranslation";
        private const string CardType = "XIAOWOKitchenBusiness.KitchenBusinessCard";
        private const string CardTitle = "后厨服务专线";
        private const string LocaleType = "XIAOWOKitchenBusiness.KitchenLocalization";
        private static readonly Type[] TextPair = { typeof(string), typeof(string) };
        private static GameObject _cardCaption;
        private static IReadOnlyDictionary<string, string> _korean;
        private Harmony _harmony;

        internal void Apply(IReadOnlyDictionary<string, string> korean)
        {
            if (_harmony != null) return;
            var openCard = ModReflect.Target(CardType, "Open", Type.EmptyTypes);
            var translate = ModReflect.Target(TranslationType, "Translate", TextPair);
            var isTranslation = ModReflect.Target(TranslationType, "IsTranslation", TextPair);
            var machineSummary = ModReflect.Target("XIAOWOKitchenBusiness.KitchenMachineEffects", "Summary", new[] { typeof(GameItem), typeof(string) });
            var windowText = ModReflect.Target("XIAOWOKitchenBusiness.KitchenFactory", "TranslateKnownWindowText", TextPair);
            var harmony = new Harmony("probably-stolen-ko-localization.kitchen-language");
            _korean = korean;
            try
            {
                harmony.Patch(openCard, postfix: new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(LocalizeCard)));
                harmony.Patch(translate, prefix: new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(TranslateKorean)));
                harmony.Patch(isTranslation, postfix: new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(RecognizeKorean)));
                harmony.Patch(machineSummary, transpiler: new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(LocalizeSummary)));
                // The original item-directory hook initializes the catalog first. JIT-compiling the window
                // routine would otherwise initialize KitchenProgression first and enter the recipe tables in reverse order.
                ModReflect.GetStatic<object>(ModReflect.FindType("XIAOWOKitchenBusiness.KitchenCatalog"), "Items");
                harmony.Patch(windowText, transpiler: new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(RecognizeRecipeLocales)));
                ResetLocaleCache();
                _harmony = harmony;
            }
            catch
            {
                _korean = null;
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
        }

        internal void Restore()
        {
            if (_harmony == null) return;
            if (_cardCaption != null)
            {
                _cardCaption.SetActive(false);
                UnityEngine.Object.Destroy(_cardCaption);
            }
            _cardCaption = null;
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
            _korean = null;
            if (ModReflect.FindType(LocaleType) != null) ResetLocaleCache();
        }

        private static void LocalizeCard()
        {
            var panel = ModReflect.GetStatic<GameObject>(ModReflect.FindType(CardType), "_panel");
            if (panel == null || !panel.activeInHierarchy) return;
            if (!IsKorean(LocHelper.GetCurrentLocaleCode()) || _korean == null || !_korean.TryGetValue(CardTitle, out var text))
            {
                if (_cardCaption != null) _cardCaption.SetActive(false);
                return;
            }
            if (_cardCaption != null)
            {
                _cardCaption.SetActive(true);
                return;
            }

            var font = ModReflect.GetStatic<TMP_FontAsset>(ModReflect.FindType(LocaleType), "Font");
            if (font == null) return;
            _cardCaption = new GameObject("ProbablyTranslatedKitchenCardCaption", typeof(RectTransform), typeof(Image));
            var plate = (RectTransform)_cardCaption.transform;
            plate.SetParent(panel.transform, false);
            plate.anchorMin = new Vector2(0.235f, 0.393f);
            plate.anchorMax = new Vector2(0.766f, 0.557f);
            plate.offsetMin = plate.offsetMax = Vector2.zero;
            var background = _cardCaption.GetComponent<Image>();
            background.color = new Color32(32, 57, 59, 255);
            background.raycastTarget = false;

            var caption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)caption.transform;
            rect.SetParent(plate, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = caption.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 16f;
            label.enableWordWrapping = false;
            label.fontSize = 16f;
            label.color = new Color32(249, 220, 144, 255);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private static bool IsKorean(string locale) =>
            locale != null && (locale.Equals("ko", StringComparison.OrdinalIgnoreCase) ||
                locale.StartsWith("ko-", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("ko_", StringComparison.OrdinalIgnoreCase));

        private static void ResetLocaleCache() => ModReflect.CallStatic(ModReflect.FindType(LocaleType), "Initialize", Type.EmptyTypes);

        // Native Format calls Translate before substituting values.
        private static bool TranslateKorean(string __0, string __1, ref string __result)
        {
            if (_korean == null || __0 == null || !IsKorean(__1) ||
                !_korean.TryGetValue(__0, out var korean)) return true;
            __result = korean;
            return false;
        }

        // Machine windows use this predicate to recognize text already displayed before a locale change.
        private static void RecognizeKorean(string __0, string __1, ref bool __result)
        {
            if (!__result && _korean != null && __1 != null && _korean.TryGetValue(__1, out var korean))
                __result = string.Equals(__0, korean, StringComparison.Ordinal);
        }

        private static string TranslateFragment(string source) =>
            _korean != null && IsKorean(LocHelper.GetCurrentLocaleCode()) && _korean.TryGetValue(source, out var translated)
                ? translated : source;

        private static IEnumerable<CodeInstruction> LocalizeSummary(IEnumerable<CodeInstruction> instructions)
        {
            var translate = new HarmonyMethod(typeof(KitchenLanguageOverlay), nameof(TranslateFragment)).method;
            var count = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string text &&
                    (text == "Power varies by recipe" || text == "Power per serving " ||
                        text == " · Quality " || text == "% · Saving "))
                {
                    yield return new CodeInstruction(OpCodes.Call, translate);
                    count++;
                }
            }
            if (count != 4) throw new InvalidOperationException("Kitchen machine summary layout changed.");
        }

        private static IEnumerable<CodeInstruction> RecognizeRecipeLocales(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            for (int i = 0; i <= code.Count - 14; i++)
            {
                if (code[i].opcode != OpCodes.Ldc_I4_3 || code[i + 1].opcode != OpCodes.Newarr ||
                    !Equals(code[i + 1].operand, typeof(string)) ||
                    !SetsLanguage(i + 2, OpCodes.Ldc_I4_0, "en") ||
                    !SetsLanguage(i + 6, OpCodes.Ldc_I4_1, "ru") ||
                    !SetsLanguage(i + 10, OpCodes.Ldc_I4_2, "ja")) continue;
                code[i].opcode = OpCodes.Ldc_I4_4;
                code.Insert(i + 14, new CodeInstruction(OpCodes.Dup));
                code.Insert(i + 15, new CodeInstruction(OpCodes.Ldc_I4_3));
                code.Insert(i + 16, new CodeInstruction(OpCodes.Ldstr, "ko"));
                code.Insert(i + 17, new CodeInstruction(OpCodes.Stelem_Ref));
                return code;
            }
            throw new InvalidOperationException("Kitchen recipe locale list changed.");

            bool SetsLanguage(int offset, OpCode index, string locale) =>
                code[offset].opcode == OpCodes.Dup && code[offset + 1].opcode == index &&
                code[offset + 2].opcode == OpCodes.Ldstr && Equals(code[offset + 2].operand, locale) &&
                code[offset + 3].opcode == OpCodes.Stelem_Ref;
        }
    }
}
