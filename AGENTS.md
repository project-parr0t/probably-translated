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
