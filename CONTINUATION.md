# Build and continuation

## Current continuation point

`parr0t-continuation-state.json` is the machine-readable continuation contract for the current tracked release and commit. It pins artifact and source-tree digests, payload counts, generated binding counts, the supported game version, the Parr0t SDK, and the current release-checkpoint inventory.

`parr0t-release-checkpoint.json` retains release ID `local-r30` for SDK compatibility, but its source inventory now includes the directly reviewed 2026-11-18 target identities. The tracked runtime JSON and CSV files remain authoritative for runtime composition; the checkpoint supplies deterministic source/target fingerprints and must be updated together with those payloads rather than used to reconstruct them.

## 2026-11-18 direct-model review inventory

The ten-target update wave is recorded under `localization_review` in `parr0t-continuation-state.json`. Its per-target `reviewed-direct.json` SHA-256 values, integrated counts, direct assigned-model provenance, and categorical source-absent exclusions are the deterministic resume boundary. Only those reviewed-direct artifacts were accepted; the deleted contaminated Agri artifact is not a source and must not be recreated.

Resume from the tracked runtime JSON, CSV overlays, generated bindings, Space Coffee artwork, and release-checkpoint identities. Do not reintroduce Agri's 647 source-absent route entries or dead dynamic helper, and do not route Gun Mod Tarkov's one source-absent row. Probably Helpful owns no concrete runtime wording: its reviewed package/preview identities remain inventory evidence rather than invented runtime localization keys, while the external Containers+ lookup text stays with the existing Containers+ overlay.

The managed source guard binds the current assemblies for all ten targets, including identity-only managed records for CSV-backed or helper-only mods. A changed assembly digest invalidates the corresponding reviewed inventory before any candidate reuse.

The post-wave deterministic repair is recorded under `localization_review.fresh_repairs`. The refreshed Perks: Probably Fun assembly retained the already reviewed localization identity set (`added=0`, `changed=0`, `removed=0`) and all 613 visible identities remain covered by its native Korean catalog. Upper Levels added `act1.dex.bye`; its directly reviewed Korean overlay entry and review fingerprint are pinned in the release checkpoint.

The 2026-11-20 AI Emporium update adds five Korean CSV entries for upgrade cost sharing. The English source, Korean wording, and format tokens were checked directly; this CSV update does not re-attest the changed managed assembly.

The managed follow-up adds 16 AquaSense settings and tooltip entries, 228 Agri Expansion entries across exact one- and two-argument lookups, and 17 Deep Space Mine entries for furnace and diagnostic panels. Gun Mod Tarkov's current callback signatures restore existing Korean settings and button text. Only AquaSense, Korean Translation, and Visible Item Durability have exact pre-update binary comparisons for this wave; the other changed source identities remain unbound pending complete source evidence.

The current-DLL pass adds 25 Deep Space Mine locale keys, 86 CS Weapon Expansion UI and dialogue entries, and 13 Gun Mod Tarkov stat labels. The Agri Expansion review uses the original Korean Translation's Eleanor dialogue as its voice authority: she addresses the shopkeeper in 해요체, with the established spellings 엘리너, 파스텔 사, 뉴트리프루트, and 데이터 드라이브. Contract, investigation, and ending dialogue retain their branch conditions and sequence; active, fallback, and saved-text routes share the revised wording.

The next pass re-inventories the installed Agri Expansion, AI Emporium, Local Station Events, CS Weapon Expansion, Perks: Probably Fun, Upper Levels, Charles Chemistry, Space Coffee, and Gun Mod Tarkov DLLs. It traces constructed keys and indirect book, settings, equipment, event, and dialogue display paths to the native locale tables or tracked overlays. The old matching DLL binaries were not available for a binary-to-binary comparison. The updated SHA-256 values identify the newly reviewed installed sources, not a comparison against those unavailable binaries.

Agri Expansion adds Korean for composed crop and livestock labels, water-source summaries, tailoring steps and headings, the final farm terminal repair list, and uniform effect summaries assembled from numbers and text. Perks: Probably Fun adds Korean for the companion's ending choice and the preset numbering tooltip. Key IDs, saved values, placeholders, and numeric calculations are unchanged. See [KOREAN-LOCALIZATION.md](KOREAN-LOCALIZATION.md) for the original patch glossary and Eleanor's dialogue register.

The signed farm record uses the completed-delivery wording after claiming the reward, not the earlier contract's future tense. The evidence handoff keeps Eleanor's polite register, while the private research note retains memo style and uses the original patch's 뉴트리프루트 spelling.

The 2.0.11 farm update revises the cooking preview and the tailoring manual's nine profession pages. Ingredient size changes the double-yield chance, while the chef uniform can grant one additional serving. The tailoring material label `Pure water` is `순수한 물`; the separate game water-grade display keeps the original Korean patch's `정제수`. Perks 0.9.678 moves the landlord dispute choice and adds a not-enough-cash message already covered by the mod's native Korean CSV.

The bridge also covers DuoJi's hotkey panel, key settings and user-facing feedback using exact IL routes, without changing configuration keys or save values. The installed manifest audit preserves English titles, translates Chinese-only titles into English, and localizes descriptions from the current source manifests.

Claims & Uprisings uses its own indexed XML language loader. Probably Translated ships the index and translations in its root `Languages` tree; there is no second mod or install step. The game manifest prerequisite declares the relationship used by the game and the Claims language loader. The game warns but does not disable Probably Translated if Claims is absent; the bridge only applies the Claims integration to the enabled source mod.

The integrated tree covers all 1,910 keys in Claims & Uprisings 1.1.4's 66 English XML files, including the four empty-jug withdrawal strings. Japanese and Traditional Chinese have the same key set. If Claims excludes Probably Translated from its content framework because another source mod is absent, the bridge adds this translation pack to its language sources only when Claims' own dependency check permits it. The original installations are untouched.

The next installed-source review updates four fingerprints. AI Emporium 1.3.69 splits the aquaculture log into water/fish counts and an optional roe count. CS Weapon Expansion 0.5.48 adds four Cyberpunk weapon names and their opening-view descriptions; its native Korean registration already covers tooltips and the renamed CF agents. In Perks: Probably Fun 0.9.680, the newly added non-key strings belong to diagnostic logging; the changed dialogue and preset UI still resolve through existing native Korean keys and perk localization. The bound prior IL inventories and the installed Claims baseline supply the comparison; the upstream DLLs are not distributed here.

Vanilla Chemistry Expansion 1.9.0 and Holographic Trading Panel 1.3.2 use the Workshop CSV overlay, with 486 and 75 keys respectively. Their current DLL fingerprints also cover non-CSV display paths: the chemistry settings groups and cartel phone label, and the trading panel's item appraisal, customer indicators, and formatted screen text. The original Korean Translation's terms and UI labels apply to shared concepts; `드림 톡신` is a mod-only liquid. The upstream installations are not modified.

Upper Levels: Probably DLC 0.9.29.1 adds 185 CSV keys, including crate keycards, trading calls, and Act 1 scenes. The installed source also changes prior instructions for crate opening, printed-organ value, and repeat-customer reputation. Unbox Moment 1.5.2 changes font selection; Perks: Probably Fun 0.9.681 changes buyer filtering and logs. Their existing visible text coverage is unchanged. The three current DLL fingerprints are recorded alongside the runtime catalogs.

## Cro pack coexistence

The [Cro pack's maintained support list](https://gall.dcinside.com/mgallery/board/view/?id=probablystolen&no=1248) supersedes its Steam description for coverage. Match source mods by Workshop item ID, not the display name. Enable both packs and all seven overlaps when running a coexistence check; GoFishing and Fishing Cuisine Expansion list each other as prerequisites. Include Perks: Probably Fun as a non-overlapping control.

| Expected owner when both packs are enabled | Manifest ID | Workshop item |
| --- | --- | --- |
| Cro | `ps-fishing-gameplay` | `3829110153` |
| Cro | `brewing_expansion` | `3829730904` |
| Cro | `tobacco_expansion` | `3832840208` |
| Cro | `ai.emporium` | `3832941698` |
| Cro | `gunsmith_expansion` | `3834619049` |
| Cro | `fishing-cuisine-expansion` | `3830031787` |
| Cro | `upper_levels` | `3838944803` |

Run with Korean selected and restart after each enable-state change. All seven overlapping mods must be skipped only while Cro is enabled; Perks: Probably Fun must retain its overlay in every state. Check both registration orders and an enabled local copy with Cro's shadowed Workshop ID. The bridge checks the live loaded mod list once per successful activation. The public listing establishes mod-level ownership, not key-by-key coverage of Cro's package.

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

### Startup patch timing

`Player.log` records one `Managed patch timing:` line per completed managed target, including targets with no bindings. `queueMs` is enqueue to first work; `betweenSlicesMs` is time between patch slices after the target starts. These are wall-clock waits and can include other game work. `bindingMs` includes binding setup and cleanup. `resolveMs` includes the one-time type index (`typeIndexMs`) and per-method `ModReflect.Target(Type, ...)` calls. `indexedTypes` counts the distinct, unambiguous types needed by the queued bindings. Harmony uses the same overloads and order as before. `harmonyMs` measures direct application including patch method setup; constructor resolution and application remain in the Harmony class processor (`constructorHarmonyMs`). `finalizeMs` covers refresh and display overlay work. The `slowestBinding` ID links the slowest binding to `GeneratedManagedBindings.cs`; when the first binding is slowest, its time includes index construction.

The type index is scoped to one queued target and uses the public `ModReflect.Types(modId)` API. Missing or ambiguous type names are skipped with a binding error, never searched in another mod. Overload selection and declaring-type access checks remain in `ModReflect.Target`.

A missing binding stays local to that binding. A target that fails to finalize is not marked applied; a separate rollback failure is logged, and the startup queue still advances. If Harmony cannot unpatch the failed target, its owner is retained for shutdown cleanup.

Do not add `resolveMs`, `harmonyMs`, or `constructorHarmonyMs` to `bindingMs`; they are already included. Compare `bindingMs` and `betweenSlicesMs` before treating the old enqueue-to-completion line as patch execution time.

## Required local test gate

Every release must install the exact distributable package from the repository root into `Mods/ProbablyTranslated-Test` before Workshop staging. Close the game before `Install` or `Remove`. The gate never modifies game assemblies or Workshop content directly.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Install `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen"
```

The gate never launches the game. Keep the exact local test package installed while staging, uploading, and verifying the client-managed Workshop copy. Remove it only when verification succeeds:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Stage `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen" `
  -WorkshopRoot "path/to/Steam/steamapps/workshop/content/4348910" `
  -StageDir "path/to/Workshop/staging/content"
# Upload through SteamCMD, then verify through the Steam client API as described below.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Release-TestGate.ps1 `
  -Action Remove `
  -GameRoot "path/to/Steam/steamapps/common/Probably Stolen"
```

`Stage` requires the exact local Mods test copy to remain installed and rejects packages changed after installation. Rebuild or translation changes invalidate the receipt and require a new install cycle.

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

### Workshop verification

Verify only through Valve's native Steam client API (`ISteamUGC`), after SteamCMD exits. Use the reviewed Valve library and the authenticated client on the expected account. If SteamCMD disconnected the desktop client, leave verification pending until the user reconnects. Never use SteamCMD, the Steam Web API, `steamcommunity.com`, browser automation, or HTTP requests as a verification fallback.

- Bind the exact item ID, owner, app, visibility, ban state, and required tags. For content, verify that the client-managed install is current and compare its package files against the staged package. Do not treat an upload acknowledgement or a stale install as proof.
- For a localized release, verify English (`english`) and Korean (`koreana`) once each. If both match and all requested submissions succeeded, locale verification is complete; do not read back the other languages. Skip locale readback for content-only uploads. For fields unavailable through the client API, report the verification limit.
- Keep at most one network-bearing UGC request in flight, then wait at least one second after completion before sending another. Stop immediately on rate limiting, access denial, offline status, or timeout. Do not retry automatically or switch clients, accounts, IPs, or endpoints. Respect any reported cooldown and resume only outstanding checks on later user authorization.

### Workshop 패치노트 규칙

SteamCMD VDF의 `changenote`는 첫 줄에 `Probably Translated <VERSION>`을 쓰고, `<VERSION>`에는 배포 패키지의 `manifest.xml`에 있는 `ModVersion` 값을 넣는다. 이후 실제로 배포된 수정사항을 한국어로 작성하고, 각 항목을 별도 줄에 `- `로 시작한다. 아래 두 항목은 형식 예시이며, 최소·최대 개수 제한은 없다.

```text
Probably Translated <VERSION>
- 수정사항 1
- 수정사항 2
```

세션이나 작업자의 의견·추측, 누락·미번역 데이터와 그 수치, 아직 반영되지 않은 작업은 패치노트에 넣지 않는다. 필요한 누락 정보는 내부 검증 기록에만 남긴다.

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
