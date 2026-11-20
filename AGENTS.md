# Repository Instructions

## Commit messages

When a commit changes the release version, its subject MUST use exactly this format:

```text
Release <version> <Title>
```

- `<version>` MUST be the new release version represented by the commit.
- `<Title>` MUST be a concise, non-empty description.
- A commit that changes the release version MUST NOT use prefixes, scopes, or alternative subject formats.
- Commits that do not change the release version have no subject-format restriction from this rule.

Example:

```text
Release 1.0.25 Translation corrections
```

This rule applies only to the `probably-translated` repository.

## README maintenance

This rule applies to all of `README.md`, not just one section. Unless the user explicitly requests a different README edit, agents may change only the supported-mod lists and existing version values. Leave all other content unchanged; do not add release notes, session commentary, findings, or opinions.

## Steam Workshop verification

Follow [Workshop verification](CONTINUATION.md#workshop-verification). Verify via the native Steam client API only; SteamCMD is for uploads. Never fall back to web requests. Verify English and Korean only for a localized release, and skip locale readback for content-only updates. Serialize network-bearing queries, leave at least one second between them, and stop on rate limiting, access denial, offline status, or timeout without automatically retrying.

## Installed managed-mod inventory

- Before claiming that an installed managed mod has no new or changed text, run `tools/managed_source_guard.py` against the current read-only Workshop root.
- A missing or mismatched bound assembly fingerprint invalidates every prior completeness claim for that target. Compare the current assembly with the bound version first, then inventory and review only its added, changed, and removed source identities.
- `--record` is permitted only after the current assembly's source inventory, translation coverage, and runtime routes have been reviewed. It MUST NOT be used to silence a stale-source failure.
- An optional supported mod that is not installed is skipped. An installed managed mod without a bound source identity fails closed.

## Localization authority and isolation

- Translation and linguistic review MUST use only the exact assigned model and bound local source/context. External machine translation, translation APIs, remote dictionaries, web search, and unapproved network content are prohibited.
- Follow the original `[Korean Translation (한국어 번역)]` mod's translation rules. It is the authority for Korean terminology, UI register and phrasing, names, and proper nouns. Use its established wording consistently for equivalent UI functions and concepts; document an exception only when the current source requires one.
- Use [KOREAN-LOCALIZATION.md](KOREAN-LOCALIZATION.md) for verified terminology, speaker evidence, and translation review rules. Check the cited original Korean row before changing an established term or character register.
- For personal names, reuse the established Korean transliteration or transliterate phonetically when practical. Retain the exact original spelling when the name is a parody, carries wordplay or stylization lost in transliteration, or must be preserved as written. For example, keep `FR1ZE` exactly as written because the spelling is part of the joke. Examples are contextual rules, not global replacement tables.
- Translation keys, identifiers, placeholders, control codes, markup, and route identities MUST remain unchanged.
