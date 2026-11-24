using System;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class PerksFriendlyTalkOverlay
    {
        internal const string TargetModId = "traits_probably_fun";
        private const string StartingPerk = "dsp_new_world";
        private static readonly Type[] LookupSignature = { typeof(string), typeof(string), typeof(object[]) };
        private Harmony _harmony;

        internal void Apply()
        {
            if (_harmony != null) return;
            var gameLookup = ModReflect.Target(typeof(LocHelper), "Get", LookupSignature);
            var modLookup = ModReflect.Target(typeof(ModHelper), nameof(ModHelper.GetLocalized), LookupSignature);
            var harmony = new Harmony("probably-stolen-ko-localization.perks-friendly-talk");
            try
            {
                harmony.Patch(gameLookup, postfix: new HarmonyMethod(typeof(PerksFriendlyTalkOverlay), nameof(AfterGameLookup)));
                harmony.Patch(modLookup, postfix: new HarmonyMethod(typeof(PerksFriendlyTalkOverlay), nameof(AfterModLookup)));
                _harmony = harmony;
            }
            catch
            {
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
        }

        internal void Restore()
        {
            if (_harmony == null) return;
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        private static void AfterGameLookup(string __1, ref string __result) =>
            Translate(ref __result, __1);

        private static void AfterModLookup(string __0, string __1, ref string __result)
        {
            if (__0 != TargetModId || string.IsNullOrEmpty(__1) || __result == __1 ||
                __1.StartsWith(StartingPerk, StringComparison.Ordinal)) return;
            Translate(ref __result, inspectionKey: null);
        }

        private static void Translate(ref string text, string inspectionKey)
        {
            if (string.IsNullOrEmpty(text)) return;
            var renameFaction = text.Contains("보안대") || text.Contains("보안부");
            var renameInspection = (text.Contains("검문") || text.Contains("검사관")) && IsInspectionKey(inspectionKey);
            if ((!renameFaction && !renameInspection) || LocHelper.GetCurrentLocaleCode() != "ko" ||
                !ModHelper.IsStartingPerkActive(StartingPerk)) return;

            if (renameFaction) text = text.Replace("보안대", "애정부").Replace("보안부", "애정부");
            if (renameInspection)
                text = text.Replace("불시 검문", "불시 면담").Replace("정기 검문", "정기 면담")
                    .Replace("보안 검사관", "애정부 면담관").Replace("검사관", "면담관")
                    .Replace("검문", "우호 면담");
        }

        // Same inspection-key scope as Perks' FriendlyTalkRenamePatch, using only the native locale and perk APIs.
        private static bool IsInspectionKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return key.StartsWith("dialog_inspection_", StringComparison.Ordinal) ||
                key.StartsWith("dialog_sec_inspect_", StringComparison.Ordinal) ||
                key.StartsWith("dialog_lun_duty", StringComparison.Ordinal) ||
                key.StartsWith("dialog_morning_sec", StringComparison.Ordinal) ||
                key.StartsWith("rep_perk_SEC_", StringComparison.Ordinal) ||
                key.StartsWith("sec_inspection_", StringComparison.Ordinal) ||
                key == "network_SEC_GRACE_desc" || key == "network_INSPECTION_INFORMANT_desc" ||
                key == "intel_mentor_note_inspection_body" || key == "mech_end_sec_reason" || key == "mech_end_sec_desc";
        }
    }
}
