# Build and continuation

## Current continuation point

`parr0t-continuation-state.json` is the machine-readable continuation contract for release 1.0.32 at commit `c354bc5626c794ef28628fd4e66e0743e3c53d86`. It pins artifact and source-tree digests, payload counts, generated binding counts, the supported game version, the Parr0t SDK, and the canonical release-checkpoint baseline.

`parr0t-release-checkpoint.json` remains the canonical Parr0t `local-r30` fingerprint baseline. Releases after r30 contain reviewed title-specific corrections and additions that are authoritative in the tracked runtime JSON and CSV files. Do not reconstruct or overwrite those tracked translations from the older checkpoint. Use the checkpoint only to reuse unchanged source fingerprints; use `parr0t-continuation-state.json` and the tracked payloads as the current released state.

## Required inputs

Keep the current game installation and installed mods read-only. Do not copy game assemblies, decompiled output, logs, credentials, or private workspace data into this repository.

Continuation requires:

- Parr0t branch `sdk-unity-2021.3` pinned to commit `5cc62680c02156b8487de6b306714b0064a260b4`.
- A local `GameManagedDir` containing `Assembly-CSharp.dll`, `0Harmony.dll`, `Newtonsoft.Json.dll`, and the referenced Unity assemblies.
- A read-only Workshop root containing the currently installed supported mods for translation inventory.
- PowerShell, Python 3.11 or newer, Git, and a .NET SDK capable of building `netstandard2.1`.
- Mono.Cecil 0.11.6 only when managed bindings must be regenerated. `tools/Validate-Continuation.ps1 -VerifyGeneratedBindings` downloads the pinned NuGet package when `-Cecil` is omitted and rejects an unexpected package digest.

## Verify a fresh checkout

Before inspecting or changing installed mod content, initialize the external Parr0t workspace and enter its text-free control plane. Then run this from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Validate-Continuation.ps1 `
  -GameManagedDir "path/to/Probably Stolen_Data/Managed" `
  -Parr0tRoot "path/to/parr0t" `
  -DotNet "path/to/dotnet" `
  -Python "path/to/python-3.11-or-newer"
```

The verifier performs all repository contracts in one pass:

- canonical Parr0t checkpoint validation and pinned SDK verification;
- artifact and translation-tree digest verification;
- manifest, payload count, CSV shape, and generated constant checks;
- a clean release build against the supplied read-only game assemblies.

A clean checkout is resumable only when it prints both `continuation=ok` and `continuation-build=ok`.

Managed binding regeneration additionally requires a read-only snapshot containing every managed mod named by `managed-ko-runtime.json`; absent optional mods cannot be reconstructed from copyrighted assemblies that are intentionally not committed. With that complete snapshot, add:

```powershell
  -VerifyGeneratedBindings `
  -WorkshopRoot "path/to/complete-supported-mod-snapshot"
```

This additionally regenerates bindings to a temporary file, requires `unsupported=0`, and byte-compares the result with the tracked generated source. Normal translation-only continuation does not require absent optional mods: the reviewed generated source remains tracked and the runtime skips missing prerequisites.

## Build

```powershell
dotnet build src/ProbablyStolenKo.Localization/ProbablyStolenKo.Localization.csproj `
  --configuration Release `
  -p:GameManagedDir="path/to/Probably Stolen_Data/Managed"
Copy-Item src/ProbablyStolenKo.Localization/bin/Release/netstandard2.1/ProbablyStolenKo.Localization.dll `
  ProbablyStolenKo.Localization.dll -Force
```

The tracked root DLL is the immutable released artifact and its digest is enforced by the continuation contract. A fresh build may differ byte-for-byte when the .NET compiler or referenced game assemblies differ, so validation proves that the tracked source still compiles rather than replacing the released DLL. The DLL contains no assembly reference to optional target mods. Generated patch classes remain disabled during the loader's assembly-wide Harmony scan; the bridge applies only reviewed target bindings for enabled prerequisite mods. Runtime preparation consolidates compatible bindings that target the same method so Harmony never rebuilds one method twice for equivalent translation routes. The display overlay also indexes exact values from result and argument routes, not only literal routes, because mod initialization may cache source-language names before the managed Harmony binding is installed. A target's optional `display_replacements` map supplies reviewed exact display values and replaces those terms only inside already-Korean mixed strings; it must never contain identifiers or broad prose fragments. Use `force_exact_patch` for direct literal builders such as custom document pages that combine text before assigning it to a display control.

## Required local test gate

Every release must install the exact distributable package from the repository root into `Mods/ProbablyTranslated-Test` before Workshop staging. Close the game before `Install` or `Remove`. The gate never modifies game assemblies or Workshop content directly.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Install `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen"
```

The gate never launches the game. Keep the exact local test package installed while staging and uploading. After SteamCMD reports a successful Workshop upload, remove the local test copy. Unsubscribe and resubscribe to the Workshop item before post-release verification so Steam installs the uploaded manifest instead of retaining an older subscribed copy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Stage `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen" `
  -WorkshopRoot "path/to/Steam/steamapps/workshop/content/4348910" `
  -StageDir "path/to/Workshop/staging/content"
# Upload the staged directory through SteamCMD.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Remove `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen"
```

`Stage` requires the exact local Mods test copy to remain installed and rejects packages changed after installation. The local copy is removed immediately after a successful upload; post-release checks use a freshly resubscribed Workshop copy. Rebuild or translation changes invalidate the receipt and require a new install cycle.

## Required Workshop publication path

`workshop/description.bbcode.txt` is the canonical Workshop description. `workshop/localized-metadata.json` binds the public item, title, description file, and every managed Steam locale.

Workshop content and default-locale metadata MUST use SteamCMD `workshop_build_item` with the reviewed VDF and the exact directory produced by `Release-TestGate.ps1 -Action Stage`. A successful content release requires `Committing update...Success`.

```powershell
steamcmd.exe +login <account> `
  +workshop_build_item path/to/probably-translated.vdf `
  +quit
```

Localized title and description synchronization MAY use Parr0t's standard-library-only `scripts/sync_steam_workshop_metadata.py` with Valve's native Steam API. The manifest binds the expected owner; apply requires an online authenticated Steam client on that exact account, the exact published-file ID, and the reviewed Steam API SHA-256. The selected DLL MUST be a first-party Valve-signed binary or an exact locked dependency already approved by Parr0t's dependency contract; never acquire or execute a third-party Workshop publisher. A Steam API error holds the localized update instead of falling back to an unapproved tool.

```powershell
python path/to/parr0t/scripts/sync_steam_workshop_metadata.py `
  --manifest workshop/localized-metadata.json `
  --steam-api path/to/steam_api64.dll `
  --apply `
  --approve-item 3838981739 `
  --approve-steam-api-sha256 <reviewed-lowercase-sha256>
```

After either path, query the public Workshop details and every managed locale. Require the expected nonempty title and canonical description; a successful command alone is not publication proof. Remove the local test installation only after the applicable upload and public verification succeed.

## Regenerate managed route bindings

Run this whenever a supported managed mod or `managed-ko-runtime.json` changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Expand-ManagedRoutes.ps1 `
  -ManagedRuntime managed-ko-runtime.json `
  -WorkshopRoot "path/to/Steam/steamapps/workshop/content/4348910" `
  -GameManagedDir "path/to/Probably Stolen_Data/Managed" `
  -Cecil "path/to/Mono.Cecil.dll" `
  -Output src/ProbablyStolenKo.Localization/GeneratedManagedBindings.cs
```

The command requires a read-only snapshot containing every managed target in the runtime and must finish with `unsupported=0`. A changed route or binding count requires matching runtime constants and continuation-state regeneration; an unsupported binding holds the update.

## Resume an update

Run the exact installed-key coverage audit before and after changing Workshop overlays:

```powershell
python tools/audit_workshop_coverage.py `
  --workshop-root "path/to/Steam/steamapps/workshop/content/4348910"
```

The final scan must report `workshop-coverage=ok`. It distinguishes native Korean coverage, private overlay coverage, stale keys, malformed source records, and mods that are not installed; never infer missing translations from locale averages.

1. Validate the fresh checkout before reading installed mod content.
2. Treat all installed game and Workshop files as read-only.
3. Inventory only installed supported mods and compare them with the tracked runtime JSON/CSV source state.
4. Preserve reviewed entries whose source and location fingerprints are unchanged. Re-review changed or new entries only.
5. Preserve translation keys, placeholders, control codes, identifiers, and runtime behavior.
6. Update payload entry counts and matching C# expected constants.
7. When managed routes changed, regenerate bindings from a complete supported-mod snapshot. Build once, then run `Validate-Continuation.ps1`; add `-VerifyGeneratedBindings` when that complete snapshot is available.
8. After the signed release commit exists, refresh the continuation contract:

```powershell
python tools/update_continuation_state.py --release-commit <full-release-commit>
```

9. Run `Validate-Continuation.ps1` again and commit the refreshed continuation state separately. Never claim the older canonical checkpoint contains post-r30 corrections.

Keep inspection data and generated intermediates outside this repository. Publish only title-specific bridge source, approved runtime data and assets, canonical Parr0t release metadata, and the continuation contract.
