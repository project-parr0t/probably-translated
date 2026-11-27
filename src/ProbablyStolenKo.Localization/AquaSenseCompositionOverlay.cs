using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyStolenKo
{
    internal sealed class AquaSenseCompositionOverlay
    {
        internal const string TargetModId = "chiqingsan.AquaSense";
        private readonly Harmony _harmony = new Harmony("probably-stolen-ko-localization.aquasense-composition");
        private bool _applied;

        internal void Apply()
        {
            if (_applied) return;
            var target = ModReflect.Target("AquaSense.Hooks", "AddLiquidEntry", new[]
            {
                typeof(List<(string, int)>), typeof(GameItem), typeof(Liquid), typeof(long).MakeByRefType()
            });
            _harmony.Patch(target, transpiler: LiquidScannerOverlay.CreateTranspiler());
            _applied = true;
        }

        internal void Restore()
        {
            if (!_applied) return;
            _harmony.UnpatchAll(_harmony.Id);
            _applied = false;
        }
    }
}
