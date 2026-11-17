from __future__ import annotations

import hashlib
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))

from managed_source_guard import bind_installed_sources, verify_installed_sources


class ManagedSourceGuardTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.workshop = self.root / "workshop"
        self.workshop.mkdir()
        self.managed = {
            "targets": {
                "installed": {
                    "workshop_item_id": "100",
                    "assembly_name": "Example",
                },
                "optional": {
                    "workshop_item_id": "200",
                    "assembly_name": "Optional",
                },
            }
        }
        assembly = self.workshop / "100" / "bin" / "Example.dll"
        assembly.parent.mkdir(parents=True)
        assembly.write_bytes(b"current")
        self.assembly = assembly

    def tearDown(self) -> None:
        self.temp.cleanup()

    def test_current_installed_source_verifies_and_absent_optional_is_allowed(self) -> None:
        bound = bind_installed_sources(self.managed, self.workshop)
        checked = verify_installed_sources(self.managed, self.workshop)
        self.assertEqual([item.status for item in bound], ["bound", "absent_optional"])
        self.assertEqual([item.status for item in checked], ["verified", "absent_optional"])
        self.assertEqual(
            self.managed["targets"]["installed"]["source_identity"]["assembly_sha256"],
            hashlib.sha256(b"current").hexdigest(),
        )

    def test_stale_installed_source_fails_closed(self) -> None:
        bind_installed_sources(self.managed, self.workshop)
        self.assembly.write_bytes(b"updated")
        with self.assertRaisesRegex(ValueError, "source identity is stale"):
            verify_installed_sources(self.managed, self.workshop)

    def test_unbound_installed_source_fails_closed(self) -> None:
        with self.assertRaisesRegex(ValueError, "has no source identity"):
            verify_installed_sources(self.managed, self.workshop)


if __name__ == "__main__":
    unittest.main()
