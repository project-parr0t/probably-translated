using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class ClaimsLanguageOverlay
    {
        internal const string TargetModId = "yexiaoyang.InteriorExpansion";
        private const string PackModId = "probably-stolen-ko-localization";
        private const string RoomLocalization = "Yexiaoyang.InteriorExpansion.RoomLocalization";
        private const string ConfigureMethod = "ConfigureSources";
        private static readonly Type[] ConfigureSignature =
        {
            typeof(IEnumerable<string>), typeof(Func<string, string, bool>)
        };

        private readonly ModLog _log;
        private Harmony _harmony;
        private Type _localization;

        internal ClaimsLanguageOverlay(ModLog log)
        {
            _log = log;
        }

        internal void Apply()
        {
            if (_harmony != null) return;
            var configure = ModReflect.Target(RoomLocalization, ConfigureMethod, ConfigureSignature);
            var harmony = new Harmony(PackModId + ".claims-language");
            try
            {
                harmony.Patch(configure, prefix: new HarmonyMethod(typeof(ClaimsLanguageOverlay), nameof(IncludePack)));
                Reload(configure.DeclaringType, removing: false);
                _localization = configure.DeclaringType;
                _harmony = harmony;
            }
            catch
            {
                harmony.UnpatchAll(harmony.Id);
                throw;
            }
        }

        private static void IncludePack(ref IEnumerable<string> __0, Func<string, string, bool> __1)
        {
            // Claims rejects a framework package if any of its unrelated prerequisites are absent.
            // Keep its own dependency check, but include our language files even in that case.
            if (__0 == null || __1 == null || !__1(PackModId, TargetModId) ||
                !__0.Contains(TargetModId, StringComparer.Ordinal) || __0.Contains(PackModId, StringComparer.Ordinal)) return;
            __0 = __0.Append(PackModId);
        }

        private static void Reload(Type localization, bool removing)
        {
            if (!ModReflect.GetStatic<bool>(localization, "loaded")) return;
            var sources = ModReflect.GetStatic<string[]>(localization, "orderedSources");
            var canPatch = ModReflect.GetStatic<Func<string, string, bool>>(localization, "canPatchTarget");
            var included = sources.Contains(PackModId, StringComparer.Ordinal);
            if (removing ? !included : included || canPatch?.Invoke(PackModId, TargetModId) != true) return;
            ModReflect.CallStatic(localization, ConfigureMethod, ConfigureSignature,
                removing ? sources.Where(id => id != PackModId) : sources, canPatch);
        }

        internal void Restore()
        {
            if (_harmony == null) return;
            try
            {
                _harmony.UnpatchAll(_harmony.Id);
                if (ModLoader.Mods.Any(mod => mod?.Manifest?.ID == TargetModId && mod.IsEnabled))
                    Reload(_localization, removing: true);
            }
            catch (Exception error)
            {
                _log.Error("Claims language overlay removal failed: " + error.Message);
            }
            finally
            {
                _harmony = null;
                _localization = null;
            }
        }
    }
}
