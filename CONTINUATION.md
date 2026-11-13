# Build and continuation

## Required inputs

Keep the current game installation and installed mod read-only. Do not copy game assemblies, decompiled output, logs, or private workspace data into this repository.

Continuation requires:

- Parr0t from branch `sdk-unity-2021.3` at commit `629b1c3e1f1c35c66243aea0a3f7bde2d05eada7`.
- The existing runtime JSON/CSV files from this repository. Treat user-owned translations as immutable inputs unless their owner explicitly updates them.
- A local `GameManagedDir` pointing to the current game's `Managed` directory. It must contain `Assembly-CSharp.dll`, `0Harmony.dll`, and `Newtonsoft.Json.dll`; these dependencies are referenced in place and must not be committed.
- The current installed mod as a read-only compatibility/reference input.

## Build

From the repository root, run:

```sh
dotnet build src/ProbablyStolenKo.Localization/ProbablyStolenKo.Localization.csproj --configuration Release -p:GameManagedDir="path/to/the/game/Managed"
```

The project preserves the `ProbablyStolenKo.Localization` assembly name and produces an optimized, deterministic build without debug symbols. Build output is ignored and is not a distributable game dependency bundle.

## Resume an update

`parr0t-release-checkpoint.json` is the canonical r10 release baseline. Load it with Parr0t's release-checkpoint API and require patch ID `probably-stolen-ko-localization` and release ID `local-r10`. Before starting an update, call `canonical_checkpoint_bytes` and confirm that the returned bytes exactly match the file; `validate_release_checkpoint` provides schema and dependency validation.

Use the validated checkpoint to retain reviewed targets whose source, location, dependency, context, and review fingerprints are unchanged. Re-select and review targets whose recorded fingerprints no longer match the newly inventoried source. Preserve the checkpoint as the comparison baseline; write a new release checkpoint through Parr0t's canonical serialization API rather than editing JSON manually.

Keep all inspection and generated intermediates outside this repository. Only publish title-specific bridge source, approved runtime data, and canonical release metadata.
