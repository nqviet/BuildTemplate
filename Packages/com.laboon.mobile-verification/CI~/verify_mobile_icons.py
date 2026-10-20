#!/usr/bin/env python3
"""Guard a Unity project's iOS and Android launcher icons against a JSON manifest.

The manifest names each icon source (path, expected SHA-256, size) and which Unity icon slots must reference
which icons (by the GUID in each icon's .meta file). See mobile_icons.schema.json.

  verify_mobile_icons.py --manifest scripts/mobile_icons.json
  verify_mobile_icons.py --manifest scripts/mobile_icons.json --ios-export Builds/iOS/Game
  verify_mobile_icons.py --manifest scripts/mobile_icons.json --print-hashes   # bootstrap / update hashes
"""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path


class IconError(Exception):
    pass


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def png_dimensions(path):
    data = Path(path).read_bytes()
    if len(data) < 24 or data[:8] != b"\x89PNG\r\n\x1a\n":
        raise IconError(f"{path} is not a PNG file")
    return int.from_bytes(data[16:20], "big"), int.from_bytes(data[20:24], "big")


def read_guid(asset_path):
    meta_path = Path(str(asset_path) + ".meta")
    try:
        text = meta_path.read_text(encoding="utf-8")
    except OSError:
        raise IconError(f"missing meta file {meta_path}")
    match = re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$", text, re.M)
    if not match:
        raise IconError(f"could not read a valid Unity GUID from {meta_path}")
    return match.group(1).lower()


def platform_icon_section(settings, target):
    marker = "  m_BuildTargetPlatformIcons:\n"
    start = settings.find(marker)
    if start < 0:
        raise IconError("ProjectSettings has no m_BuildTargetPlatformIcons section")
    tail = settings[start + len(marker):]
    next_top_level = re.search(r"^  [A-Za-z_][A-Za-z0-9_]*:", tail, re.M)
    section = tail[:next_top_level.start()] if next_top_level else tail
    match = re.search(
        rf"^  - m_BuildTarget: {re.escape(target)}\s*$\n(?P<body>.*?)(?=^  - m_BuildTarget:|\Z)",
        section,
        re.M | re.S,
    )
    if not match:
        raise IconError(f"ProjectSettings has no {target} platform icon configuration")
    return match.group("body")


def parse_icon_entries(section):
    entries = []
    current = None
    for line in section.splitlines():
        if line.startswith("    - m_Textures:"):
            if current is not None:
                entries.append(current)
            current = {"guids": [], "kind": None}
            continue
        if current is None:
            continue
        guid_match = re.search(r"guid:\s*([0-9a-fA-F]{32})", line)
        if guid_match:
            current["guids"].append(guid_match.group(1).lower())
        kind_match = re.match(r"\s*m_Kind:\s*(\d+)\s*$", line)
        if kind_match:
            current["kind"] = int(kind_match.group(1))
    if current is not None:
        entries.append(current)
    return entries


def load_manifest(path):
    manifest = json.loads(Path(path).read_text(encoding="utf-8"))
    icons = manifest.get("icons")
    if not isinstance(icons, list) or not icons:
        raise IconError("manifest needs a non-empty 'icons' list")
    ids = [icon.get("id") for icon in icons]
    if len(set(ids)) != len(ids) or not all(ids):
        raise IconError("every icon needs a unique 'id'")
    for slot in manifest.get("slots", []):
        for icon_id in slot.get("icons", []):
            if icon_id not in ids:
                raise IconError(f"slot references unknown icon id '{icon_id}'")
    return manifest


def verify_sources(root, manifest):
    """Check hashes, sizes and slot GUIDs. Returns a list of messages for passing checks."""
    root = Path(root)
    passed = []
    source = manifest.get("source")
    if source:
        path = root / source["path"]
        if not path.is_file() or sha256(path) != source["sha256"].lower():
            raise IconError(f"canonical source {source['path']} is missing or changed")
        passed.append(f"source {source['path']}")

    guids = {}
    for icon in manifest["icons"]:
        path = root / icon["path"]
        if not path.is_file():
            raise IconError(f"missing {icon['id']} icon {icon['path']}")
        size = tuple(icon.get("size", [1024, 1024]))
        dimensions = png_dimensions(path)
        if dimensions != size:
            raise IconError(f"{icon['id']} must be {size[0]}x{size[1]}, found {dimensions[0]}x{dimensions[1]}")
        if icon.get("sha256") and sha256(path) != icon["sha256"].lower():
            raise IconError(f"{icon['id']} ({icon['path']}) no longer matches its approved hash")
        guids[icon["id"]] = read_guid(path)
        passed.append(f"icon {icon['id']}")

    slots = manifest.get("slots", [])
    if slots:
        settings = (root / manifest.get("settings_path", "ProjectSettings/ProjectSettings.asset")).read_text(encoding="utf-8")
        for slot in slots:
            entries = parse_icon_entries(platform_icon_section(settings, slot["target"]))
            kind = slot.get("kind")
            selected = [e for e in entries if kind is None or e["kind"] == kind]
            expected = [guids[i] for i in slot["icons"]]
            label = slot.get("label") or f"{slot['target']} kind {kind}"
            if not selected or any(e["guids"] != expected for e in selected):
                raise IconError(f"{label} slots do not reference {', '.join(slot['icons'])}")
            passed.append(f"slots {label}")
    return passed


def verify_ios_export(root, manifest, project_path):
    icon_id = manifest.get("ios_export_icon")
    if not icon_id:
        raise IconError("manifest has no 'ios_export_icon' for --ios-export")
    icon = next(i for i in manifest["icons"] if i["id"] == icon_id)
    source = Path(root) / icon["path"]
    generated = Path(project_path) / "Unity-iPhone/Images.xcassets/AppIcon.appiconset/Icon-Store-1024.png"
    if not generated.is_file():
        raise IconError(f"Xcode App Store icon not found at {generated}")
    try:
        from PIL import Image
    except ImportError:
        raise IconError("Pillow is required to compare exported icon pixels")
    with Image.open(source) as source_image, Image.open(generated) as generated_image:
        a = source_image.convert("RGBA")
        b = generated_image.convert("RGBA")
        if a.size != b.size or a.tobytes() != b.tobytes():
            raise IconError("the exported iOS App Store icon differs from the source")


def print_hashes(root, manifest):
    for icon in manifest["icons"]:
        path = Path(root) / icon["path"]
        status = sha256(path) if path.is_file() else "MISSING"
        print(f"{icon['id']}: {status}")
    if manifest.get("source"):
        path = Path(root) / manifest["source"]["path"]
        print(f"source: {sha256(path) if path.is_file() else 'MISSING'}")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--manifest", required=True, type=Path, help="icon manifest JSON")
    parser.add_argument("--project-root", type=Path, default=Path.cwd(), help="Unity project root (default: cwd)")
    parser.add_argument("--ios-export", help="also pixel-compare a freshly exported Xcode project")
    parser.add_argument("--print-hashes", action="store_true", help="print current hashes and exit")
    args = parser.parse_args(argv)
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(errors="replace")
    try:
        manifest = load_manifest(args.manifest)
        if args.print_hashes:
            print_hashes(args.project_root, manifest)
            return 0
        verify_sources(args.project_root, manifest)
        print("✅ iOS and Android launcher icons match the manifest.")
        if args.ios_export:
            verify_ios_export(args.project_root, manifest, args.ios_export)
            print("✅ Fresh Unity iOS export uses the source App Store icon pixel-for-pixel.")
    except IconError as error:
        print(f"❌ Mobile app icon verification failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
