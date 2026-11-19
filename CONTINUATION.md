# Build and continuation

## Current continuation point

`parr0t-continuation-state.json` is the machine-readable continuation contract for the current tracked release and commit. It pins artifact and source-tree digests, payload counts, generated binding counts, the supported game version, the Parr0t SDK, and the current release-checkpoint inventory.

`parr0t-release-checkpoint.json` retains release ID `local-r30` for SDK compatibility, but its source inventory now includes the directly reviewed 2026-11-18 target identities. The tracked runtime JSON and CSV files remain authoritative for runtime composition; the checkpoint supplies deterministic source/target fingerprints and must be updated together with those payloads rather than used to reconstruct them.

## 2026-11-18 direct-model review inventory

The ten-target update wave is recorded under `localization_review` in `parr0t-continuation-state.json`. Its per-target `reviewed-direct.json` SHA-256 values, integrated counts, direct assigned-model provenance, and categorical source-absent exclusions are the deterministic resume boundary. Only those reviewed-direct artifacts were accepted; the deleted contaminated Agri artifact is not a source and must not be recreated.

Resume from the tracked runtime JSON, CSV overlays, generated bindings, Space Coffee artwork, and release-checkpoint identities. Do not reintroduce Agri's 647 source-absent route entries or dead dynamic helper, and do not route Gun Mod Tarkov's one source-absent row. Probably Helpful owns no concrete runtime wording: its reviewed package/preview identities remain inventory evidence rather than invented runtime localization keys, while the external Containers+ lookup text stays with the existing Containers+ overlay.

The managed source guard binds the current assemblies for all ten targets, including identity-only managed records for CSV-backed or helper-only mods. A changed assembly digest invalidates the corresponding reviewed inventory before any candidate reuse.

The post-wave deterministic repair is recorded under `localization_review.fresh_repairs`. The refreshed Perks: Probably Fun assembly retained the already reviewed localization identity set (`added=0`, `changed=0`, `removed=0`) and all 613 visible identities remain covered by its native Korean catalog. Upper Levels added `act1.dex.bye`; its directly reviewed Korean overlay entry and review fingerprint are pinned in the release checkpoint.

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

### Harmony compatibility check

Constructor routes resolve their targets in `HarmonyTargetMethod`, after the `HarmonyPrepare` gate. Do not put the declaring type and constructor kind in a named-type `HarmonyPatch` attribute: the loader reads attributes before `Prepare`, and the game's Harmony 2.4.2 and the BepInEx HarmonyX 2.9 in Hangul Patch Studio's 20261115 package expose different overloads.

Build the test with the same `GameManagedDir` as the mod, then run it once per `0Harmony.dll` using Windows x64 Python. For HarmonyX, point to the extracted `BepInEx/core/0Harmony.dll` with its sibling dependencies intact; do not install the external patch to run this check.

```powershell
dotnet build tests/HarmonyCompatibility/HarmonyCompatibility.csproj -c Release -p:GameManagedDir="$managed"
python tests/HarmonyCompatibility/run.py "$game" `
  tests/HarmonyCompatibility/bin/Release/netstandard2.1/HarmonyCompatibility.dll `
  src/ProbablyStolenKo.Localization/bin/Release/netstandard2.1/ProbablyStolenKo.Localization.dll `
  "$harmonyDll"
```

The harness uses the game's Mono to run the mod scanner and Harmony's assembly scan, then checks exact instance and static constructor resolution. It does not start the game or modify the installation.

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


## Installed managed-source identity gate

An old translation inventory is never evidence for a newly installed managed assembly. Before declaring that a managed mod has no added or changed text, verify the exact installed binary:

```powershell
python tools/managed_source_guard.py `
  --managed-runtime managed-ko-runtime.json `
  --workshop-root "path/to/Steam/steamapps/workshop/content/4348910"
```

The guard verifies the SHA-256 and relative path recorded under each installed target's `source_identity`. Missing optional mods remain valid. An installed target with no binding, a missing bound assembly, or a digest mismatch fails closed and invalidates the previous completeness claim.

On a mismatch, do not begin with another broad full-content scan. First compute the old/new source-identity delta, preserve reviewed candidates only where source and location identities are unchanged, then translate and review added or changed entries and remove stale routes. Regenerate and verify exact IL bindings before accepting the update.

Only after the current source inventory, translations, and routes pass review may the reviewed assembly identities be recorded:

```powershell
python tools/managed_source_guard.py `
  --managed-runtime managed-ko-runtime.json `
  --workshop-root "path/to/Steam/steamapps/workshop/content/4348910" `
  --record
```

`--record` acknowledges a reviewed source version; it is not a repair for stale translation data. Pass `-WorkshopRoot` to `Validate-Continuation.ps1` during every installed-mod update so this guard runs before build validation.

## Resume an update

Run the exact installed-key coverage audit before and after changing Workshop overlays:

```powershell
python tools/audit_workshop_coverage.py `
  --workshop-root "path/to/Steam/steamapps/workshop/content/4348910"
```

The final scan must report `workshop-coverage=ok`. It distinguishes native Korean coverage, private overlay coverage, stale keys, malformed source records, and mods that are not installed; never infer missing translations from locale averages.

1. Validate the fresh checkout before reading installed mod content.
2. Treat all installed game and Workshop files as read-only.
3. Run the installed managed-source identity gate before reusing any prior inventory or claiming no additions.
4. If a source identity changed, compute its source/location delta first. Preserve reviewed entries only when both identities are unchanged.
5. Inventory only installed supported mods and compare them with the tracked runtime JSON/CSV source state.
6. Preserve translation keys, placeholders, control codes, identifiers, and runtime behavior.
7. Update payload entry counts and matching C# expected constants.
8. When managed routes changed, regenerate bindings from a complete supported-mod snapshot. Build once, then run `Validate-Continuation.ps1`; add `-VerifyGeneratedBindings` when that complete snapshot is available.
9. After the signed release commit exists, refresh the continuation contract:

```powershell
python tools/update_continuation_state.py --release-commit <full-release-commit>
```

10. Run `Validate-Continuation.ps1` again and commit the refreshed continuation state separately. Never claim the older canonical checkpoint contains post-r30 corrections.

Keep inspection data and generated intermediates outside this repository. Publish only title-specific bridge source, approved runtime data and assets, canonical Parr0t release metadata, and the continuation contract.
