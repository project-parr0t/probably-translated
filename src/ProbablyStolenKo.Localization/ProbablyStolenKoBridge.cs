using System;
using System.Collections.Generic;

namespace ProbablyStolenKo
{
    public sealed class ProbablyStolenKoBridge : IMod
    {
        private const ulong CroTranslationPackWorkshopId = 3834117650UL;
        // Match the source Workshop IDs in the Cro support list linked in CONTINUATION.md.
        private static readonly HashSet<string> CroOverlapManifestIds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "ps-fishing-gameplay",
                "brewing_expansion",
                "fishing-cuisine-expansion",
                "tobacco_expansion",
                "ai.emporium",
                "gunsmith_expansion",
                "upper_levels"
            };

        private ModManifest _manifest;
        private WorkshopPayloadCatalog _workshopCatalog;
        private ManagedLiteralCatalog _managedCatalog;
        private ManagedLiteralOverlay _managedOverlay;
        private ManagedPatchLoadingOverlay _patchLoadingOverlay;
        private ManifestOverlay _manifestOverlay;
        private Va11RecipeOverlay _va11RecipeOverlay;
        private SpaceCoffeeSceneOverlay _spaceCoffeeSceneOverlay;
        private ClaimsLanguageOverlay _claimsLanguageOverlay;
        private LabelerTextOverlay _labelerTextOverlay;
        private readonly HologramPreferenceLayout _hologramPreferenceLayout = new HologramPreferenceLayout();
        private readonly PerksFriendlyTalkOverlay _perksFriendlyTalkOverlay = new PerksFriendlyTalkOverlay();
        private ModLog _log;
        private bool _enabled;
        private bool _activated;
        private bool _assetsSubscribed;
        private bool _croTranslationPackActive;
        private readonly Dictionary<WorkshopEntryKey, WorkshopOriginal> _workshopOriginals =
            new Dictionary<WorkshopEntryKey, WorkshopOriginal>();

        public void Init(ModManifest manifest)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _log = new ModLog(_manifest);
            _managedOverlay = new ManagedLiteralOverlay(_log);
            _manifestOverlay = new ManifestOverlay();
            _va11RecipeOverlay = new Va11RecipeOverlay(_manifest);
            _spaceCoffeeSceneOverlay = new SpaceCoffeeSceneOverlay(_manifest);
            _claimsLanguageOverlay = new ClaimsLanguageOverlay(_log);
        }

        public void OnEnable()
        {
            if (_enabled) return;
            _enabled = true;
            try
            {
                _labelerTextOverlay = new LabelerTextOverlay();
                _labelerTextOverlay.Apply();
            }
            catch (Exception error)
            {
                _log.Error("Labeler text overlay failed: " + error.Message);
                RestoreLabelerText();
            }
            _patchLoadingOverlay = ManagedPatchLoadingOverlay.Create(_managedOverlay, _manifest.ID, _log);
            if (!_assetsSubscribed)
            {
                ModHook.OnModAssetsLoaded += OnModAssetsLoaded;
                _assetsSubscribed = true;
            }
            ActivateWhenRegistered();
        }

        private void OnModAssetsLoaded(LoadedMod loaded)
        {
            if (!_enabled) return;
            var activatedNow = ActivateWhenRegistered();
            if (!activatedNow && _activated && _workshopCatalog != null && IsUsable(loaded)) ApplyTarget(loaded);
        }

        private bool ActivateWhenRegistered()
        {
            if (_activated || !IsRegistered()) return false;
            if (_workshopCatalog == null && !TryLoadCatalog()) return false;
            TryLoadManagedCatalog();
            _croTranslationPackActive = IsCroTranslationPackActive();
            _activated = true;
            foreach (var loaded in ModLoader.Mods)
            {
                if (IsUsable(loaded)) ApplyTarget(loaded);
            }
            _patchLoadingOverlay?.AllowCompletion();
            return true;
        }

        private bool IsRegistered()
        {
            foreach (var loaded in ModLoader.Mods)
                if (loaded != null && loaded.Manifest != null &&
                    StringComparer.Ordinal.Equals(loaded.Manifest.ID, _manifest.ID)) return true;
            return false;
        }
        private static bool IsCroTranslationPackActive()
        {
            foreach (var loaded in ModLoader.Mods)
            {
                if (!IsUsable(loaded)) continue;
                if (loaded.WorkshopId == CroTranslationPackWorkshopId ||
                    loaded.ShadowedWorkshopId == CroTranslationPackWorkshopId) return true;
            }
            return false;
        }


        private static bool IsUsable(LoadedMod loaded)
        {
            return loaded != null && loaded.Manifest != null && loaded.IsEnabled;
        }

        private bool TryLoadCatalog()
        {
            try
            {
                _workshopCatalog = WorkshopPayloadCatalog.Parse(
                    ModHelper.ReadModFileText(_manifest.ID, "workshop-ko-runtime.json"),
                    path => ModHelper.ReadModFileText(_manifest.ID, path));
                return true;
            }
            catch (Exception error)
            {
                _log.Error("Payload load deferred after " + error.GetType().Name + ".");
                return false;
            }
        }

        private void TryLoadManagedCatalog()
        {
            if (_managedCatalog != null) return;
            try
            {
                _managedCatalog = ManagedLiteralCatalog.Parse(
                    ModHelper.ReadModFileText(_manifest.ID, "managed-ko-runtime.json"));
            }
            catch (Exception error)
            {
                _log.Error("Managed payload load deferred after " + error.GetType().Name + ": " + error.Message);
            }
        }

        private void ApplyTarget(LoadedMod loaded)
        {
            if (_croTranslationPackActive && CroOverlapManifestIds.Contains(loaded.Manifest.ID)) return;
            WorkshopModPayload workshopPayload;
            if (_workshopCatalog.TryGetMod(loaded.Manifest.ID, out workshopPayload))
            {
                try
                {
                    var result = WorkshopOverlayEngine.Apply(_workshopCatalog, loaded, _workshopOriginals);
                    if (result.AppliedCount != 0)
                        _log.Log("Applied " + result.AppliedCount + " entries to " + loaded.Manifest.ID + ".");
                    if (result.MissingCount != 0)
                        _log.Error("Skipped " + result.MissingCount + " missing localization keys for " + loaded.Manifest.ID + ".");
                }
                catch (Exception error)
                {
                    _log.Error("Target overlay failed for " + loaded.Manifest.ID + ". " + error.GetType().Name);
                }
            }
            if (workshopPayload != null &&
                (workshopPayload.ManifestName != null || workshopPayload.ManifestDescription != null))
            {
                try
                {
                    _manifestOverlay.Apply(
                        loaded.Manifest,
                        workshopPayload.ManifestName,
                        workshopPayload.ManifestDescription);
                }
                catch (Exception error)
                {
                    _log.Error("Workshop manifest overlay failed for " + loaded.Manifest.ID + ". " + error.GetType().Name);
                }
            }
            if (StringComparer.Ordinal.Equals(loaded.Manifest.ID, "va11halla"))
            {
                try
                {
                    _va11RecipeOverlay.Apply(loaded.Manifest.ID);
                }
                catch (Exception error)
                {
                    _log.Error("VA-11 recipe overlay failed. " + error.GetType().Name);
                }
            }
            if (StringComparer.Ordinal.Equals(loaded.Manifest.ID, "space_coffee"))
            {
                try
                {
                    _spaceCoffeeSceneOverlay.Apply(loaded.Manifest.ID);
                }
                catch (Exception error)
                {
                    _log.Error("Space Coffee scene overlay failed. " + error.GetType().Name);
                }
            }
            if (StringComparer.Ordinal.Equals(loaded.Manifest.ID, ClaimsLanguageOverlay.TargetModId))
            {
                try
                {
                    _claimsLanguageOverlay.Apply();
                }
                catch (Exception error)
                {
                    _log.Error("Claims language overlay failed: " + error.Message);
                }
            }
            if (StringComparer.Ordinal.Equals(loaded.Manifest.ID, PerksFriendlyTalkOverlay.TargetModId))
            {
                try
                {
                    _perksFriendlyTalkOverlay.Apply();
                }
                catch (Exception error)
                {
                    _log.Error("Perks friendly talk overlay failed: " + error.Message);
                }
            }
            if (StringComparer.Ordinal.Equals(loaded.Manifest.ID, HologramPreferenceLayout.TargetModId))
            {
                try
                {
                    _hologramPreferenceLayout.Apply();
                }
                catch (Exception error)
                {
                    _log.Error("Hologram preference layout failed: " + error.Message);
                }
            }



            ManagedTarget manifestTarget;
            if (_managedCatalog != null && _managedCatalog.Targets.TryGetValue(loaded.Manifest.ID, out manifestTarget) &&
                (manifestTarget.ManifestName != null || manifestTarget.ManifestDescription != null))
            {
                try
                {
                    _manifestOverlay.Apply(
                        loaded.Manifest,
                        manifestTarget.ManifestName,
                        manifestTarget.ManifestDescription);
                }
                catch (Exception error)
                {
                    _log.Error("Manifest overlay failed for " + loaded.Manifest.ID + ". " + error.GetType().Name);
                }
            }

            ManagedTarget managedTarget;
            if (_managedCatalog == null || !_managedCatalog.Targets.TryGetValue(loaded.Manifest.ID, out managedTarget)) return;
            try
            {
                _managedOverlay.QueueTarget(loaded.Manifest.ID, managedTarget);
            }
            catch (Exception error)
            {
                _log.Error(
                    "Managed target overlay failed for " + loaded.Manifest.ID + ". " +
                    error.GetType().Name + ": " + error.Message);
            }
        }

        public void OnDisable()
        {
            _activated = false;
            _enabled = false;
            RestoreLabelerText();
            if (_assetsSubscribed)
            {
                ModHook.OnModAssetsLoaded -= OnModAssetsLoaded;
                _assetsSubscribed = false;
            }
            _patchLoadingOverlay?.Dispose();
            _patchLoadingOverlay = null;
            _managedOverlay?.Restore();
            _va11RecipeOverlay?.Restore();
            _spaceCoffeeSceneOverlay?.Restore();
            _claimsLanguageOverlay?.Restore();
            _hologramPreferenceLayout.Restore();
            _perksFriendlyTalkOverlay.Restore();
            _manifestOverlay?.Restore();
            WorkshopOverlayEngine.Restore(_workshopCatalog, _workshopOriginals);
        }

        private void RestoreLabelerText()
        {
            if (_labelerTextOverlay == null) return;
            try
            {
                _labelerTextOverlay.Restore();
                _labelerTextOverlay = null;
            }
            catch (Exception error)
            {
                _log.Error("Labeler text overlay removal failed: " + error.Message);
            }
        }
    }
}
