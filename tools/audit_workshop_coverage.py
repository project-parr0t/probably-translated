from __future__ import annotations

import argparse
import csv
import json
from dataclasses import dataclass
from pathlib import Path


@dataclass
class CsvKeys:
    keys: set[str]
    malformed: int


def read_keys(path: Path, strict: bool) -> CsvKeys:
    keys: set[str] = set()
    malformed = 0
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
            if key in keys:
                raise ValueError(f"{path}: duplicate key {key}")
            keys.add(key)
    return CsvKeys(keys, malformed)


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

        source: set[str] = set()
        native_korean: set[str] = set()
        malformed = 0
        locale_files = sorted(
            path
            for path in item_root.rglob("*.csv")
            if any(part.lower() == "localization" for part in path.parts)
        )
        for path in locale_files:
            parsed = read_keys(path, strict=False)
            malformed += parsed.malformed
            if is_korean_locale(path):
                native_korean.update(parsed.keys)
            else:
                source.update(parsed.keys)

        missing = sorted(source - overlay - native_korean)
        stale = sorted(overlay - source)
        missing_total += len(missing)
        report["mods"][mod_id] = {
            "status": "installed",
            "source": len(source),
            "overlay": len(overlay),
            "native_ko": len(native_korean),
            "missing": len(missing),
            "stale": len(stale),
            "malformed_source_records": malformed,
            "missing_keys": missing,
            "stale_keys": stale,
        }
        print(
            f"{mod_id}: source={len(source)} overlay={len(overlay)} native_ko={len(native_korean)} "
            f"missing={len(missing)} stale={len(stale)} malformed={malformed}"
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
