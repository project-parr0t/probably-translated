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

## Installed managed-mod inventory

- Before claiming that an installed managed mod has no new or changed text, run `tools/managed_source_guard.py` against the current read-only Workshop root.
- A missing or mismatched bound assembly fingerprint invalidates every prior completeness claim for that target. Compare the current assembly with the bound version first, then inventory and review only its added, changed, and removed source identities.
- `--record` is permitted only after the current assembly's source inventory, translation coverage, and runtime routes have been reviewed. It MUST NOT be used to silence a stale-source failure.
- An optional supported mod that is not installed is skipped. An installed managed mod without a bound source identity fails closed.

## Localization authority and isolation

- Translation and linguistic review MUST use only the exact assigned model and bound local source/context. External machine translation, translation APIs, remote dictionaries, web search, and unapproved network content are prohibited.
- `[Korean Translation (한국어 번역)]` is the authoritative source for established Korean names, proper nouns, terminology, register, and UI style. Preserve its exact established rendering unless current source evidence requires a documented exception.
- Personal names SHOULD use their established Korean rendering unless Latin spelling is intrinsically required or no defensible Korean rendering exists. Examples illustrate the rule and MUST NOT become global replacement tables.
- Translation keys, identifiers, placeholders, control codes, markup, and route identities MUST remain unchanged.
