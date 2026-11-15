from __future__ import annotations

import argparse
import hashlib
import json
import re
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
        digest.update(path.relative_to(root).as_posix().encode("utf-8"))
        digest.update(b"\0")
        digest.update(sha256(path).encode("ascii"))
        digest.update(b"\n")
    return len(files), digest.hexdigest()


def constant(source: str, name: str) -> int:
    match = re.search(rf"\b{name}\s*=\s*(\d+)\s*;", source)
    if not match:
        raise ValueError(f"missing generated/source constant: {name}")
    return int(match.group(1))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--release-commit", required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"[0-9a-f]{40}", args.release_commit):
        raise ValueError("--release-commit must be a full lowercase Git commit ID")

    root = args.root.resolve()
    state_path = root / "parr0t-continuation-state.json"
    state = json.loads(state_path.read_text(encoding="utf-8"))
    manifest = ET.parse(root / "manifest.xml").getroot()
    managed = json.loads((root / "managed-ko-runtime.json").read_text(encoding="utf-8"))
    workshop = json.loads((root / "workshop-ko-runtime.json").read_text(encoding="utf-8"))
    checkpoint = json.loads((root / "parr0t-release-checkpoint.json").read_text(encoding="utf-8"))
    generated = (root / "src/ProbablyStolenKo.Localization/GeneratedManagedBindings.cs").read_text(encoding="utf-8")

    state["release"] = {
        "version": manifest.findtext("ModVersion"),
        "commit": args.release_commit,
        "game_version": manifest.findtext("./GameVersions/Version"),
    }
    state["parr0t"]["branch"] = checkpoint["sdk"]["branch"]
    state["parr0t"]["commit"] = checkpoint["sdk"]["commit"]
    state["parr0t"]["checkpoint_release_id"] = checkpoint["release_id"]
    state["parr0t"]["checkpoint_target_count"] = checkpoint["source_inventory"]["target_count"]
    state["contracts"] = {
        "managed_entries": managed["entry_count"],
        "managed_targets": len(managed["targets"]),
        "workshop_entries": workshop["entry_count"],
        "workshop_mods": len(workshop["mods"]),
        "generated_routes": constant(generated, "RouteCount"),
        "generated_bindings": constant(generated, "BindingCount"),
        "generated_static_patches": constant(generated, "StaticPatchBindingCount"),
        "generated_unsupported": constant(generated, "UnsupportedBindingCount"),
        "generated_refresh": constant(generated, "RefreshBindingCount"),
    }
    for relative in state["artifacts"]:
        state["artifacts"][relative] = sha256(root / relative)
    for relative in state["trees"]:
        files, digest = tree_digest(root, relative)
        state["trees"][relative] = {"files": files, "sha256": digest}

    state_path.write_text(json.dumps(state, ensure_ascii=False, sort_keys=True, indent=2) + "\n", encoding="utf-8")
    print(f"continuation-state=updated release={state['release']['version']} commit={args.release_commit}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
