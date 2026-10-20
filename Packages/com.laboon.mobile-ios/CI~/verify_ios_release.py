#!/usr/bin/env python3
"""Verify a built iOS .app or .xcarchive against a JSON release config.

Checks: scene manifest and LaboonSceneLifecycle marker, non-empty scene delegate class, export-compliance flag,
required embedded SDK privacy bundles, every PrivacyInfo.xcprivacy parses, optional bundle id and privacy URL.
Project-specific values (bundle id, SDK bundle names, privacy URL source) live in the config, not in this script.
"""
import argparse
import json
from pathlib import Path
import plistlib
import re
import sys

DEFAULT_CONFIG = {
    "expected_bundle_id": None,
    "require_scene_manifest": True,
    "require_scene_marker": True,
    "marker_key": "LaboonSceneLifecycle",
    "require_encryption_declaration": True,
    "framework_path": "Frameworks/UnityFramework.framework",
    # [{"name": "Firebase_FirebaseCore.bundle", "required_data_types": []}]
    "required_sdk_bundles": [],
    # {"file": "Assets/Scenes/Main.unity", "pattern": "^  _privacyUrl: (.+)$"} relative to --project-root
    "privacy_url": None,
}


def load_config(path=None):
    config = dict(DEFAULT_CONFIG)
    if path:
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)
        unknown = set(data) - set(DEFAULT_CONFIG) - {"$schema", "description"}
        if unknown:
            raise ValueError(f"Unknown config keys: {', '.join(sorted(unknown))}")
        config.update(data)
    return config


def _scene_configurations(info):
    manifest = info.get("UIApplicationSceneManifest")
    if not isinstance(manifest, dict):
        return []
    configurations = manifest.get("UISceneConfigurations", {})
    if not isinstance(configurations, dict):
        return []
    role = configurations.get("UIWindowSceneSessionRoleApplication")
    return role if isinstance(role, list) else []


def verify_app(app, config=None):
    """Return (info, errors) for an .app directory. Never modifies the app."""
    config = config or load_config()
    app = Path(app)
    errors = []
    try:
        info = plistlib.loads((app / "Info.plist").read_bytes())
    except (OSError, plistlib.InvalidFileException) as error:
        return {}, [f"Cannot read Info.plist: {error}"]

    scenes = _scene_configurations(info)
    if config["require_scene_manifest"]:
        if not scenes:
            errors.append("Missing UIApplicationSceneManifest scene configuration: SDK 27 apps cannot launch on iOS 27.")
        elif not all(isinstance(s, dict) and str(s.get("UISceneDelegateClassName", "")).strip() for s in scenes):
            errors.append("Scene configuration has an empty UISceneDelegateClassName.")
    if config["require_scene_marker"] and scenes and info.get(config["marker_key"]) is not True:
        errors.append(f"{config['marker_key']} is not true: the package scene lifecycle will not activate.")

    if config["require_encryption_declaration"] and not isinstance(info.get("ITSAppUsesNonExemptEncryption"), bool):
        errors.append("Encryption declaration is absent; complete export compliance before submission.")

    expected_id = config.get("expected_bundle_id")
    if expected_id and info.get("CFBundleIdentifier") != expected_id:
        errors.append(f"CFBundleIdentifier is {info.get('CFBundleIdentifier')!r}, expected {expected_id!r}.")

    framework = app / config["framework_path"]
    for bundle in config["required_sdk_bundles"]:
        name = bundle["name"] if isinstance(bundle, dict) else bundle
        required_types = set(bundle.get("required_data_types", [])) if isinstance(bundle, dict) else set()
        manifest = framework / name / "PrivacyInfo.xcprivacy"
        if not manifest.is_file():
            errors.append(f"Missing embedded SDK privacy manifest: {name}")
            continue
        try:
            data = plistlib.loads(manifest.read_bytes())
        except plistlib.InvalidFileException:
            continue  # Reported by the full manifest scan below.
        declared = {x.get("NSPrivacyCollectedDataType") for x in data.get("NSPrivacyCollectedDataTypes", [])}
        for missing in sorted(required_types - declared):
            errors.append(f"{name} privacy manifest does not declare {missing}.")

    for manifest in app.rglob("PrivacyInfo.xcprivacy"):
        try:
            plistlib.loads(manifest.read_bytes())
        except plistlib.InvalidFileException:
            errors.append(f"Invalid privacy manifest: {manifest.relative_to(app)}")
    return info, errors


def verify_privacy_url(project_root, privacy_config):
    """Return errors when the configured file has no HTTPS privacy URL."""
    if not privacy_config:
        return []
    path = Path(project_root) / privacy_config["file"]
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as error:
        return [f"Cannot read privacy URL source {privacy_config['file']}: {error}"]
    match = re.search(privacy_config["pattern"], text, re.MULTILINE)
    if not match or not match.group(1).strip().startswith("https://"):
        return ["Privacy Policy URL is empty or not HTTPS. Configure a real policy before App Review."]
    return []


def resolve_app(path):
    path = Path(path)
    if path.suffix == ".xcarchive":
        apps = list((path / "Products/Applications").glob("*.app"))
        if len(apps) != 1:
            raise ValueError("Archive must contain exactly one app.")
        return apps[0]
    return path


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("app", type=Path, help=".app directory or .xcarchive")
    parser.add_argument("--config", type=Path, help="JSON release config (see ios_release_config.example.json)")
    parser.add_argument("--project-root", type=Path, default=Path.cwd(), help="Unity project root for policy checks")
    parser.add_argument("--skip-policy", action="store_true", help="Native development verification only; not ready for review.")
    args = parser.parse_args(argv)
    config = load_config(args.config)
    try:
        app = resolve_app(args.app)
    except ValueError as error:
        parser.error(str(error))
    info, errors = verify_app(app, config)
    if not args.skip_policy:
        errors += verify_privacy_url(args.project_root, config.get("privacy_url"))
    print(f"{info.get('CFBundleIdentifier')} {info.get('CFBundleShortVersionString')} ({info.get('CFBundleVersion')})")
    for error in errors:
        print(f"ERROR: {error}")
    if not errors:
        print("PASS: scenes, encryption declaration and embedded SDK privacy resources verified.")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
