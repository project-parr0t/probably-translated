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

```powershell
dotnet build src/ProbablyStolenKo.Localization/ProbablyStolenKo.Localization.csproj --configuration Release -p:GameManagedDir="path/to/the/game/Managed"
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Finalize-ManagedBindings.ps1 `
  -InputDll src/ProbablyStolenKo.Localization/bin/Release/netstandard2.1/ProbablyStolenKo.Localization.dll `
  -OutputDll ProbablyStolenKo.Localization.dll `
  -WorkshopRoot "path/to/Steam/steamapps/workshop/content/4348910" `
  -GameManagedDir "path/to/the/game/Managed" `
  -Cecil "path/to/Mono.Cecil.dll"
```

The first command produces an intermediate assembly. The finalizer resolves every reviewed target against the installed, read-only mod assemblies and writes exact type/module metadata tokens into the distributable root DLL. Publish only that finalized DLL. Both steps preserve the `ProbablyStolenKo.Localization` assembly name and deterministic optimized output without debug symbols. Build output is ignored and no game or dependency assembly is bundled.

## Regenerate managed route bindings

The bridge does not scan assemblies, types, or members by name at runtime. `tools/Expand-ManagedRoutes.ps1` expands every reviewed runtime route into generated Harmony patch classes; `tools/Finalize-ManagedBindings.ps1` replaces their intermediate string metadata with 438 exact type/module metadata-token targets and does the same for all six refresh targets. Generated patch classes stay disabled during the loader's assembly-wide Harmony scan and are enabled only for their exact target. Runtime activation contains no `ModReflect.Target` calls, no per-target caller/ownership stack validation, and no patch queue running across gameplay frames.

Regenerate the bindings whenever a supported managed mod changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Expand-ManagedRoutes.ps1 `
  -ManagedRuntime managed-ko-runtime.json `
  -WorkshopRoot "path/to/Steam/steamapps/workshop/content/4348910" `
  -GameManagedDir "path/to/the/game/Managed" `
  -Cecil "path/to/Mono.Cecil.dll" `
  -Output src/ProbablyStolenKo.Localization/GeneratedManagedBindings.cs
```

The command must finish with `unsupported=0`. Treat any unsupported binding or changed route count as a held update, not as permission to omit translations.

## Resume an update

`parr0t-release-checkpoint.json` is the canonical r22 release baseline. Load it with Parr0t's release-checkpoint API and require patch ID `probably-stolen-ko-localization` and release ID `local-r22`. Before starting an update, call `canonical_checkpoint_bytes` and confirm that the returned bytes exactly match the file; `validate_release_checkpoint` provides schema and dependency validation.

Use the validated checkpoint to retain reviewed targets whose source, location, dependency, context, and review fingerprints are unchanged. Re-select and review targets whose recorded fingerprints no longer match the newly inventoried source. Preserve the checkpoint as the comparison baseline; write a new release checkpoint through Parr0t's canonical serialization API rather than editing JSON manually.

Keep all inspection and generated intermediates outside this repository. Only publish title-specific bridge source, approved runtime data, and canonical release metadata.
