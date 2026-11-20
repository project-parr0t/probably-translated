using System;
using HarmonyLib;
using UnityEngine;

namespace ProbablyStolenKo
{
    internal sealed class HologramPreferenceLayout
    {
        internal const string TargetModId = "local.hologram_trade";
        private const string PreferenceSection = "TE_Preferences";
        private const float Gap = 8f;
        private Harmony _harmony;

        internal void Apply()
        {
            if (_harmony != null) return;
            var target = ModReflect.Target("HologramTrade.HologramTradeView", "LayoutInfo", Type.EmptyTypes);
            var harmony = new Harmony("probably-stolen-ko-localization.hologram-preferences");
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(HologramPreferenceLayout), nameof(ReserveSpace)));
            _harmony = harmony;
        }

        internal void Restore()
        {
            if (_harmony == null) return;
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        private static void ReserveSpace(NegotiationTabletFrame ___frame, ref float __result)
        {
            var host = ___frame == null ? null : ___frame.infoHost;
            if (host == null || !(host.Find(PreferenceSection) is RectTransform section) || !section.gameObject.activeSelf)
                return;

            if (!TryMeasureSection(__result, host.rect.width, section.rect.size, out var top, out var scale, out var end))
                return;

            // LayoutInfo already saves the original rect. Its caller places the wholesale rows after this height.
            section.localScale = new Vector3(scale, scale, 1f);
            section.anchoredPosition = new Vector2(0f, -top);
            __result = end;
        }

        private static bool TryMeasureSection(float occupied, float width, Vector2 size, out float top, out float scale, out float end)
        {
            top = scale = end = 0f;
            if (!(width > 0f && size.x > 0f && size.y > 0f)) return false;
            scale = Math.Min(1f, width / size.x);
            top = occupied > 0f ? occupied + Gap : 0f;
            end = top + size.y * scale + Gap;
            return true;
        }
    }
}
