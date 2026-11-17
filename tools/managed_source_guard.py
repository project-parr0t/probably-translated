from __future__ import annotations

import argparse
import hashlib
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class SourceCheck:
    target: str
    workshop_item_id: str
    status: str
    relative_path: str | None = None


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _assembly_candidates(item_root: Path, assembly_name: str) -> list[Path]:
    dlls = sorted(
        item_root.rglob("*.dll"),
        key=lambda path: path.relative_to(item_root).as_posix().casefold(),
    )
    expected = f"{assembly_name}.dll".casefold()
    exact = [path for path in dlls if path.name.casefold() == expected]
    if exact:
        return exact
    return dlls if len(dlls) == 1 else []


def bind_installed_sources(managed: dict[str, Any], workshop_root: Path) -> list[SourceCheck]:
    checks: list[SourceCheck] = []
    for target_name, target in managed["targets"].items():
        item_id = str(target["workshop_item_id"])
        item_root = workshop_root / item_id
        if not item_root.is_dir():
            checks.append(SourceCheck(target_name, item_id, "absent_optional"))
            continue
        candidates = _assembly_candidates(item_root, str(target["assembly_name"]))
        if not candidates:
            raise ValueError(f"installed managed target assembly is missing: {target_name}")
        if len(candidates) != 1:
            raise ValueError(f"installed managed target assembly is ambiguous: {target_name}")
        assembly = candidates[0]
        relative = assembly.relative_to(item_root).as_posix()
        target["source_identity"] = {
            "assembly_relative_path": relative,
            "assembly_sha256": file_sha256(assembly),
        }
        checks.append(SourceCheck(target_name, item_id, "bound", relative))
    return checks


def verify_installed_sources(managed: dict[str, Any], workshop_root: Path) -> list[SourceCheck]:
    checks: list[SourceCheck] = []
    for target_name, target in managed["targets"].items():
        item_id = str(target["workshop_item_id"])
        item_root = workshop_root / item_id
        if not item_root.is_dir():
            checks.append(SourceCheck(target_name, item_id, "absent_optional"))
            continue
        identity = target.get("source_identity")
        if not isinstance(identity, dict):
            raise ValueError(f"installed managed target has no source identity: {target_name}")
        relative = identity.get("assembly_relative_path")
        expected = identity.get("assembly_sha256")
        if (
            not isinstance(relative, str)
            or not relative
            or not isinstance(expected, str)
            or re.fullmatch(r"[0-9a-fA-F]{64}", expected) is None
        ):
            raise ValueError(f"installed managed target source identity is malformed: {target_name}")
        item_root_resolved = item_root.resolve()
        assembly = (item_root_resolved / Path(relative)).resolve()
        try:
            assembly.relative_to(item_root_resolved)
        except ValueError as exc:
            raise ValueError(f"managed source path escapes target root: {target_name}") from exc
        if not assembly.is_file():
            raise ValueError(f"bound managed source assembly is missing: {target_name}")
        actual = file_sha256(assembly)
        if actual != expected.lower():
            raise ValueError(f"installed managed target source identity is stale: {target_name}")
        checks.append(SourceCheck(target_name, item_id, "verified", relative))
    return checks


def _summary(checks: list[SourceCheck]) -> str:
    verified = sum(check.status in {"bound", "verified"} for check in checks)
    absent = sum(check.status == "absent_optional" for check in checks)
    return f"managed-source-guard=ok installed={verified} absent_optional={absent}"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--managed-runtime", type=Path, required=True)
    parser.add_argument("--workshop-root", type=Path, required=True)
    parser.add_argument("--record", action="store_true")
    args = parser.parse_args()

    managed_path = args.managed_runtime.resolve()
    workshop_root = args.workshop_root.resolve()
    managed = json.loads(managed_path.read_text(encoding="utf-8"))
    if args.record:
        checks = bind_installed_sources(managed, workshop_root)
        managed_path.write_text(json.dumps(managed, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    else:
        checks = verify_installed_sources(managed, workshop_root)
    print(_summary(checks))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
