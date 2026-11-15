from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


TEXT_SUFFIXES = {".cs", ".csv", ".json", ".md", ".ps1", ".py", ".txt", ".xml"}


def canonical_bytes(path: Path) -> bytes:
    data = path.read_bytes()
    return data.replace(b"\r\n", b"\n") if path.suffix.lower() in TEXT_SUFFIXES else data


def sha256(path: Path) -> str:
    return hashlib.sha256(canonical_bytes(path)).hexdigest()


def tree_digest(root: Path, relative: str) -> tuple[int, str]:
    files = sorted((path for path in (root / relative).rglob("*") if path.is_file()), key=lambda path: path.relative_to(root).as_posix())
    digest = hashlib.sha256()
    for path in files:
        name = path.relative_to(root).as_posix()
        digest.update(name.encode("utf-8"))
        digest.update(b"\0")
        digest.update(sha256(path).encode("ascii"))
        digest.update(b"\n")
    return len(files), digest.hexdigest()


def csv_rows(path: Path) -> int:
    count = 0
    keys: set[str] = set()
    with path.open(encoding="utf-8-sig", newline="") as stream:
        for row in csv.reader(stream):
            if len(row) != 2 or not row[0].strip():
                raise ValueError(f"{path}: expected exactly key and text columns")
            key = row[0].strip()
            if key in keys:
                raise ValueError(f"{path}: duplicate key {key}")
            keys.add(key)
            count += 1
    return count


def require_constant(source: str, name: str, value: int) -> None:
    if not re.search(rf"\b{name}\s*=\s*{value}\s*;", source):
        raise ValueError(f"generated/source constant {name} does not equal {value}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--parr0t-root", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    state = json.loads((root / "parr0t-continuation-state.json").read_text(encoding="utf-8"))
    if state.get("schema") != "probably-translated.continuation/v1":
        raise ValueError("unsupported continuation state schema")

    for relative, expected in state["artifacts"].items():
        actual = sha256(root / relative)
        if actual != expected:
            raise ValueError(f"artifact digest mismatch: {relative}")
    for relative, expected in state["trees"].items():
        count, digest = tree_digest(root, relative)
        if count != expected["files"] or digest != expected["sha256"]:
            raise ValueError(f"tree digest mismatch: {relative}")

    manifest = ET.parse(root / "manifest.xml").getroot()
    if manifest.findtext("ModVersion") != state["release"]["version"]:
        raise ValueError("manifest version does not match continuation state")
    versions = [node.text for node in manifest.findall("./GameVersions/Version")]
    if versions != [state["release"]["game_version"]]:
        raise ValueError("manifest game version does not match continuation state")

    managed = json.loads((root / "managed-ko-runtime.json").read_text(encoding="utf-8"))
    workshop = json.loads((root / "workshop-ko-runtime.json").read_text(encoding="utf-8"))
    contracts = state["contracts"]
    if managed["entry_count"] != contracts["managed_entries"] or sum(item["entry_count"] for item in managed["targets"].values()) != contracts["managed_entries"]:
        raise ValueError("managed entry contract mismatch")
    if len(managed["targets"]) != contracts["managed_targets"]:
        raise ValueError("managed target contract mismatch")
    clinic_routes = managed["targets"]["DeepSpaceClinic_InventoryTest"]["routes"]
    clinic_text = next(
        route
        for route in clinic_routes
        if route["type"] == "Clinic.InventoryTest.ClinicText"
        and route["method"] == "T"
        and route["mode"] == "result_by_argument"
    )
    if clinic_text["entries"].get("导线") != "전선":
        raise ValueError("Clinic must preserve the Korean patch's canonical Wire item name")
    if workshop["entry_count"] != contracts["workshop_entries"] or sum(item["entry_count"] for item in workshop["mods"].values()) != contracts["workshop_entries"]:
        raise ValueError("Workshop entry contract mismatch")
    if len(workshop["mods"]) != contracts["workshop_mods"]:
        raise ValueError("Workshop mod contract mismatch")
    for mod_id, item in workshop["mods"].items():
        path = root / item["csv_path"]
        actual = csv_rows(path)
        if actual != item["entry_count"]:
            raise ValueError(f"Workshop CSV count mismatch: {mod_id}")

    generated = (root / "src/ProbablyStolenKo.Localization/GeneratedManagedBindings.cs").read_text(encoding="utf-8")
    managed_source = (root / "src/ProbablyStolenKo.Localization/ManagedLiteralOverlay.cs").read_text(encoding="utf-8")
    workshop_source = (root / "src/ProbablyStolenKo.Localization/WorkshopOverlay.cs").read_text(encoding="utf-8")
    require_constant(generated, "RouteCount", contracts["generated_routes"])
    require_constant(generated, "BindingCount", contracts["generated_bindings"])
    require_constant(generated, "StaticPatchBindingCount", contracts["generated_static_patches"])
    require_constant(generated, "UnsupportedBindingCount", contracts["generated_unsupported"])
    require_constant(generated, "RefreshBindingCount", contracts["generated_refresh"])
    require_constant(managed_source, "ExpectedEntries", contracts["managed_entries"])
    require_constant(workshop_source, "ExpectedEntries", contracts["workshop_entries"])

    if args.parr0t_root:
        sys.path.insert(0, str((args.parr0t_root.resolve() / "src")))
        from game_l10n.release_checkpoint import canonical_checkpoint_bytes

        checkpoint_path = root / "parr0t-release-checkpoint.json"
        checkpoint, canonical = canonical_checkpoint_bytes(
            checkpoint_path,
            expected_patch_id="probably-stolen-ko-localization",
            expected_release_id=state["parr0t"]["checkpoint_release_id"],
        )
        if canonical_bytes(checkpoint_path) != canonical:
            raise ValueError("release checkpoint is not canonically serialized")
        if checkpoint["sdk"]["branch"] != state["parr0t"]["branch"] or checkpoint["sdk"]["commit"] != state["parr0t"]["commit"]:
            raise ValueError("release checkpoint SDK binding mismatch")
        if checkpoint["source_inventory"]["target_count"] != state["parr0t"]["checkpoint_target_count"]:
            raise ValueError("release checkpoint target count mismatch")

    print(
        "continuation=ok "
        f"release={state['release']['version']} managed={contracts['managed_entries']} "
        f"workshop={contracts['workshop_entries']} routes={contracts['generated_routes']} "
        f"bindings={contracts['generated_bindings']} unsupported={contracts['generated_unsupported']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
