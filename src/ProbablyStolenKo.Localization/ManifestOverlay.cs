using System.Collections.Generic;

namespace ProbablyStolenKo
{
    internal sealed class ManifestOverlay
    {
        private sealed class Original
        {
            internal Original(ModManifest target, string name, string description)
            {
                Target = target;
                Name = name;
                Description = description;
            }

            internal ModManifest Target { get; }
            internal string Name { get; }
            internal string Description { get; }
        }

        private readonly List<Original> _originals = new List<Original>();
        private readonly HashSet<ModManifest> _applied = new HashSet<ModManifest>();

        internal void Apply(ModManifest manifest, string name, string description)
        {
            if (manifest == null || _applied.Contains(manifest)) return;
            var original = new Original(manifest, manifest.Name, manifest.Description);
            try
            {
                if (name != null) manifest.Name = name;
                if (description != null) manifest.Description = description;
                _originals.Add(original);
                _applied.Add(manifest);
            }
            catch
            {
                Restore(original);
                throw;
            }
        }

        internal void Restore()
        {
            for (var index = _originals.Count - 1; index >= 0; index--) Restore(_originals[index]);
            _originals.Clear();
            _applied.Clear();
        }

        private static void Restore(Original original)
        {
            original.Target.Name = original.Name;
            original.Target.Description = original.Description;
        }
    }
}
