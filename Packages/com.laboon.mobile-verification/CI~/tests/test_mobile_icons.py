import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest
import zlib


spec = importlib.util.spec_from_file_location("icons", Path(__file__).resolve().parents[1] / "verify_mobile_icons.py")
icons = importlib.util.module_from_spec(spec)
spec.loader.exec_module(icons)

GUIDS = {"ios": "a" * 32, "android": "b" * 32, "bg": "c" * 32, "fg": "d" * 32}


def png(width, height, color=b"\xff\x00\x00\xff"):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)
    raw = b"".join(b"\x00" + color * width for _ in range(height))
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def settings(ios_guid, android_guid, bg_guid, fg_guid):
    return f"""PlayerSettings:
  m_BuildTargetPlatformIcons:
  - m_BuildTarget: iPhone
    m_Icons:
    - m_Textures:
      - {{fileID: 2800000, guid: {ios_guid}, type: 3}}
      m_Width: 180
      m_Kind: 0
  - m_BuildTarget: Android
    m_Icons:
    - m_Textures:
      - {{fileID: 2800000, guid: {android_guid}, type: 3}}
      m_Width: 192
      m_Kind: 0
    - m_Textures:
      - {{fileID: 2800000, guid: {bg_guid}, type: 3}}
      - {{fileID: 2800000, guid: {fg_guid}, type: 3}}
      m_Width: 432
      m_Kind: 2
  m_BuildTargetBatching: []
"""


class MobileIconTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        art = self.root / "Assets/Icons"
        art.mkdir(parents=True)
        for name, guid in GUIDS.items():
            (art / f"{name}.png").write_bytes(png(4, 4))
            (art / f"{name}.png.meta").write_text(f"fileFormatVersion: 2\nguid: {guid}\n")
        (self.root / "ProjectSettings").mkdir()
        self.write_settings(GUIDS["ios"], GUIDS["android"], GUIDS["bg"], GUIDS["fg"])
        self.manifest = {
            "icons": [{"id": n, "path": f"Assets/Icons/{n}.png", "size": [4, 4],
                       "sha256": icons.sha256(art / f"{n}.png")} for n in GUIDS],
            "slots": [
                {"target": "iPhone", "kind": None, "icons": ["ios"]},
                {"target": "Android", "kind": 0, "icons": ["android"]},
                {"target": "Android", "kind": 2, "icons": ["bg", "fg"]},
            ],
            "ios_export_icon": "ios",
        }

    def tearDown(self):
        self.temp.cleanup()

    def write_settings(self, *guids):
        (self.root / "ProjectSettings/ProjectSettings.asset").write_text(settings(*guids))

    def write_manifest(self):
        path = self.root / "icons.json"
        path.write_text(json.dumps(self.manifest))
        return path

    def test_valid_project_passes(self):
        self.assertEqual(7, len(icons.verify_sources(self.root, self.manifest)))
        self.assertEqual(0, icons.main(["--manifest", str(self.write_manifest()), "--project-root", str(self.root)]))

    def test_changed_hash_fails(self):
        (self.root / "Assets/Icons/android.png").write_bytes(png(4, 4, b"\x00\xff\x00\xff"))
        with self.assertRaisesRegex(icons.IconError, "approved hash"):
            icons.verify_sources(self.root, self.manifest)

    def test_wrong_size_fails(self):
        self.manifest["icons"][0]["size"] = [1024, 1024]
        with self.assertRaisesRegex(icons.IconError, "1024x1024"):
            icons.verify_sources(self.root, self.manifest)

    def test_swapped_adaptive_layers_fail(self):
        self.write_settings(GUIDS["ios"], GUIDS["android"], GUIDS["fg"], GUIDS["bg"])
        with self.assertRaisesRegex(icons.IconError, "Android kind 2"):
            icons.verify_sources(self.root, self.manifest)

    def test_wrong_ios_slot_fails(self):
        self.write_settings(GUIDS["android"], GUIDS["android"], GUIDS["bg"], GUIDS["fg"])
        with self.assertRaisesRegex(icons.IconError, "iPhone"):
            icons.verify_sources(self.root, self.manifest)

    def test_missing_meta_fails(self):
        (self.root / "Assets/Icons/ios.png.meta").unlink()
        with self.assertRaisesRegex(icons.IconError, "meta"):
            icons.verify_sources(self.root, self.manifest)

    def test_source_artwork_check(self):
        source = self.root / "source.png"
        source.write_bytes(png(8, 8))
        self.manifest["source"] = {"path": "source.png", "sha256": icons.sha256(source)}
        icons.verify_sources(self.root, self.manifest)
        self.manifest["source"]["sha256"] = "0" * 64
        with self.assertRaisesRegex(icons.IconError, "canonical source"):
            icons.verify_sources(self.root, self.manifest)

    def test_manifest_rejects_unknown_slot_icon(self):
        self.manifest["slots"].append({"target": "iPhone", "icons": ["nope"]})
        with self.assertRaisesRegex(icons.IconError, "unknown icon id"):
            icons.load_manifest(self.write_manifest())

    def test_cli_reports_failure_with_exit_code(self):
        self.write_settings(GUIDS["android"], GUIDS["android"], GUIDS["bg"], GUIDS["fg"])
        self.assertEqual(1, icons.main(["--manifest", str(self.write_manifest()), "--project-root", str(self.root)]))

    def test_print_hashes(self):
        self.assertEqual(0, icons.main(["--manifest", str(self.write_manifest()), "--project-root", str(self.root), "--print-hashes"]))


if __name__ == "__main__":
    unittest.main()
