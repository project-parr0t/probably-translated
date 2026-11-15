# Build and continuation

## Required inputs

Keep the current game installation and installed mod read-only. Do not copy game assemblies, decompiled output, logs, or private workspace data into this repository.

Continuation requires:

- Parr0t from branch `sdk-unity-2021.3` at commit `5cc62680c02156b8487de6b306714b0064a260b4`, as recorded by the canonical checkpoint.
- The existing runtime JSON/CSV files from this repository. Treat user-owned translations as immutable inputs unless their owner explicitly updates them.
- A local `GameManagedDir` pointing to the current game's `Managed` directory. It must contain `Assembly-CSharp.dll`, `0Harmony.dll`, and `Newtonsoft.Json.dll`; these dependencies are referenced in place and must not be committed.
- The current installed mod as a read-only compatibility/reference input.

## Build

From the repository root, run:

```powershell
dotnet build src/ProbablyStolenKo.Localization/ProbablyStolenKo.Localization.csproj --configuration Release -p:GameManagedDir="path/to/the/game/Managed"
Copy-Item src/ProbablyStolenKo.Localization/bin/Release/netstandard2.1/ProbablyStolenKo.Localization.dll ProbablyStolenKo.Localization.dll -Force
```

The first command builds the runtime; the second promotes that exact output to the distributable root DLL. Generated patch classes stay disabled during the loader's assembly-wide Harmony scan. When a supported prerequisite is running, the bridge resolves each reviewed target once through the game's capability-free `ModReflect.Target` API and applies only that target's patches. The DLL therefore contains no assembly reference to an optional target mod, while retaining exact overload selection and avoiding assembly-wide runtime scans.

## Regenerate managed route bindings

`tools/Expand-ManagedRoutes.ps1` expands every reviewed runtime route into generated Harmony patch classes. Normal methods use literal `ModReflect.Target` calls with exact parameter signatures. Constructor patches retain statically auditable Harmony metadata because `ModReflect.Target` resolves methods only. All six refresh targets use the same checked API.

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

`parr0t-release-checkpoint.json` is the canonical r30 release baseline. Load it with Parr0t's release-checkpoint API and require patch ID `probably-stolen-ko-localization` and release ID `local-r30`. Before starting an update, call `canonical_checkpoint_bytes` and confirm that the returned bytes exactly match the file; `validate_release_checkpoint` provides schema and dependency validation.

Use the validated checkpoint to retain reviewed targets whose source, location, dependency, context, and review fingerprints are unchanged. Re-select and review targets whose recorded fingerprints no longer match the newly inventoried source. Preserve the checkpoint as the comparison baseline; write a new release checkpoint through Parr0t's canonical serialization API rather than editing JSON manually.

Keep all inspection and generated intermediates outside this repository. Only publish title-specific bridge source, approved runtime data, and canonical release metadata.
