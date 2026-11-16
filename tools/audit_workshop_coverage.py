from __future__ import annotations

import argparse
import csv
import json
from dataclasses import dataclass
from pathlib import Path


@dataclass
class CsvKeys:
    keys: set[str]
    visible_keys: set[str]
    malformed: int
    duplicate_occurrences: int
    empty_text_records: int


def read_keys(
    path: Path,
    strict: bool,
    allow_duplicates: bool = False,
) -> CsvKeys:
    keys: set[str] = set()
    visible_keys: set[str] = set()
    seen_keys: set[str] = set()
    malformed = 0
    duplicate_occurrences = 0
    empty_text_records = 0
    with path.open(encoding="utf-8-sig", newline="") as stream:
        for record in csv.reader(stream):
            if record and record[0].lstrip().startswith("#"):
                continue
            if len(record) != 2 or not record[0].strip():
                malformed += 1
                if strict:
                    raise ValueError(f"{path}: expected exactly key and text columns")
                if not record or not record[0].strip():
                    continue
            key = record[0].strip()
            keys.add(key)
            if len(record) > 1 and record[1].strip():
                visible_keys.add(key)
            elif len(record) > 1:
                empty_text_records += 1
            if key in seen_keys:
                if allow_duplicates:
                    duplicate_occurrences += 1
                    continue
                raise ValueError(f"{path}: duplicate key {key}")
            seen_keys.add(key)
    return CsvKeys(keys, visible_keys, malformed, duplicate_occurrences, empty_text_records)






def is_korean_locale(path: Path) -> bool:
    name = path.stem.lower().replace("_", "-")
    return name == "ko" or name.startswith("ko-") or "korean" in name


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--workshop-root", required=True, type=Path)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--json-output", type=Path)
    args = parser.parse_args()

    root = args.root.resolve()
    workshop_root = args.workshop_root.resolve()
    catalog = json.loads((root / "workshop-ko-runtime.json").read_text(encoding="utf-8"))
    report: dict[str, object] = {"schema": "probably-translated.workshop-coverage/v1", "mods": {}}
    missing_total = 0

    for mod_id, item in sorted(catalog["mods"].items()):
        item_root = workshop_root / item["workshop_item_id"]
        overlay = read_keys(root / item["csv_path"], strict=True).keys
        if not item_root.is_dir():
            report["mods"][mod_id] = {"status": "not-installed", "overlay": len(overlay)}
            continue

        source_keys: set[str] = set()
        source_visible: set[str] = set()
        native_korean: set[str] = set()
        malformed = 0
        duplicate_occurrences = 0
        empty_text_records = 0
        locale_files = sorted(
            path
            for path in item_root.rglob("*.csv")
            if any(part.lower() == "localization" for part in path.parts)
        )
        for path in locale_files:
            parsed = read_keys(
                path,
                strict=False,
                allow_duplicates=True,
            )
            malformed += parsed.malformed
            duplicate_occurrences += parsed.duplicate_occurrences
            empty_text_records += parsed.empty_text_records
            if is_korean_locale(path):
                native_korean.update(parsed.visible_keys)
            else:
                source_keys.update(parsed.keys)
                source_visible.update(parsed.visible_keys)

        # Exclude malformed one-character extraction fragments, not visible strings.
        if mod_id == "traits_probably_fun":
            source_visible.difference_update({"N", "И", "名", "이"})
        missing = sorted(source_visible - overlay - native_korean)
        stale = sorted(overlay - source_keys)
        missing_total += len(missing)
        report["mods"][mod_id] = {
            "status": "installed",
            "source": len(source_visible),
            "source_keys": len(source_keys),
            "overlay": len(overlay),
            "native_ko": len(native_korean),
            "missing": len(missing),
            "stale": len(stale),
            "malformed_source_records": malformed,
            "empty_source_records": empty_text_records,
            "duplicate_source_occurrences": duplicate_occurrences,
            "missing_keys": missing,
            "stale_keys": stale,
        }
        print(
            f"{mod_id}: source={len(source_visible)} overlay={len(overlay)} native_ko={len(native_korean)} "
            f"missing={len(missing)} stale={len(stale)} malformed={malformed} "
            f"duplicate_occurrences={duplicate_occurrences} empty_source={empty_text_records}"
        )

    report["missing_total"] = missing_total
    if args.json_output:
        args.json_output.parent.mkdir(parents=True, exist_ok=True)
        args.json_output.write_text(
            json.dumps(report, ensure_ascii=False, sort_keys=True, indent=2) + "\n",
            encoding="utf-8",
        )
    print(f"workshop-coverage={'ok' if missing_total == 0 else 'missing'} missing={missing_total}")
    return 0 if missing_total == 0 else 2


if __name__ == "__main__":
    raise SystemExit(main())
