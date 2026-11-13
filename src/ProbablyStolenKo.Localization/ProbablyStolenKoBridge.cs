using System;
using System.Collections.Generic;

namespace ProbablyStolenKo
{
    public sealed class ProbablyStolenKoBridge : IMod
    {
        private ModManifest _manifest;
        private WorkshopPayloadCatalog _workshopCatalog;
        private ManagedLiteralCatalog _managedCatalog;
        private ManagedLiteralOverlay _managedOverlay;
        private ManifestOverlay _manifestOverlay;
        private ModLog _log;
        private bool _enabled;
        private bool _assetsSubscribed;
        private readonly Dictionary<WorkshopEntryKey, WorkshopOriginal> _workshopOriginals =
            new Dictionary<WorkshopEntryKey, WorkshopOriginal>();

        public void Init(ModManifest manifest)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            _log = new ModLog(_manifest);
            _managedOverlay = new ManagedLiteralOverlay(_log);
            _manifestOverlay = new ManifestOverlay();
        }

        public void OnEnable()
        {
            if (_enabled) return;
            _enabled = true;
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
            ActivateWhenRegistered();
            if (_workshopCatalog != null && IsUsable(loaded)) ApplyTarget(loaded);
        }

        private void ActivateWhenRegistered()
        {
            if (!IsRegistered()) return;
            if (_workshopCatalog == null && !TryLoadCatalog()) return;
            TryLoadManagedCatalog();
            foreach (var loaded in ModLoader.Mods)
            {
                if (IsUsable(loaded)) ApplyTarget(loaded);
            }
        }

        private bool IsRegistered()
        {
            foreach (var loaded in ModLoader.Mods)
                if (loaded != null && loaded.Manifest != null &&
                    StringComparer.Ordinal.Equals(loaded.Manifest.ID, _manifest.ID)) return true;
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
                _log.Error("Managed payload load deferred after " + error.GetType().Name + ".");
            }
        }

        private void ApplyTarget(LoadedMod loaded)
        {
            if (_workshopCatalog.TryGetMod(loaded.Manifest.ID, out _))
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
                if (_managedOverlay.QueueTarget(loaded.Manifest.ID, managedTarget))
                    ModHelper.StartCoroutine(_manifest.ID, _managedOverlay.ProcessQueue(() => _enabled));
            }
            catch (Exception error)
            {
                _log.Error("Managed target overlay failed for " + loaded.Manifest.ID + ". " + error.GetType().Name);
            }
        }

        public void OnDisable()
        {
            _enabled = false;
            if (_assetsSubscribed)
            {
                ModHook.OnModAssetsLoaded -= OnModAssetsLoaded;
                _assetsSubscribed = false;
            }
            _managedOverlay?.Restore();
            _manifestOverlay?.Restore();
            WorkshopOverlayEngine.Restore(_workshopCatalog, _workshopOriginals);
        }
    }
}
