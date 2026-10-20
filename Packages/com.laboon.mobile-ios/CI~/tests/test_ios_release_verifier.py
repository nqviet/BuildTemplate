import importlib.util
import json
from pathlib import Path
import plistlib
import tempfile
import unittest


spec = importlib.util.spec_from_file_location("ios_release", Path(__file__).resolve().parents[1] / "verify_ios_release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)

CORE = "Firebase_FirebaseCore.bundle"
CRASH = "Firebase_FirebaseCrashlytics.bundle"
CRASH_TYPE = "NSPrivacyCollectedDataTypeCrashData"


class IOSReleaseVerifierTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.app = self.root / "Game.app"
        self.app.mkdir()
        self.config = release.load_config()
        self.config["required_sdk_bundles"] = [
            {"name": CORE, "required_data_types": []},
            {"name": CRASH, "required_data_types": [CRASH_TYPE]},
        ]

    def tearDown(self):
        self.temp.cleanup()

    def write_info(self, scenes=True, marker=True, delegate="MobileSceneDelegate", bundle_id="com.company.game"):
        info = {"ITSAppUsesNonExemptEncryption": False, "CFBundleIdentifier": bundle_id}
        if scenes:
            info["UIApplicationSceneManifest"] = {"UISceneConfigurations": {
                "UIWindowSceneSessionRoleApplication": [{"UISceneDelegateClassName": delegate}]}}
        if marker:
            info["LaboonSceneLifecycle"] = True
        (self.app / "Info.plist").write_bytes(plistlib.dumps(info))

    def write_resources(self, crash_types=(CRASH_TYPE,)):
        for name in [CORE, CRASH]:
            path = self.app / "Frameworks/UnityFramework.framework" / name
            path.mkdir(parents=True)
            data = {"NSPrivacyCollectedDataTypes": [{"NSPrivacyCollectedDataType": t} for t in crash_types]}
            (path / "PrivacyInfo.xcprivacy").write_bytes(plistlib.dumps(data))

    def test_rejects_legacy_lifecycle_even_when_sdk_resources_exist(self):
        self.write_info(scenes=False)
        self.write_resources()
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any("cannot launch" in e for e in errors), errors)

    def test_rejects_missing_marker(self):
        self.write_info(marker=False)
        self.write_resources()
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any("LaboonSceneLifecycle" in e for e in errors), errors)

    def test_rejects_empty_delegate(self):
        self.write_info(delegate=" ")
        self.write_resources()
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any("empty UISceneDelegateClassName" in e for e in errors), errors)

    def test_accepts_any_delegate_name(self):
        self.write_info(delegate="PigBeamSceneDelegate")
        self.write_resources()
        _, errors = release.verify_app(self.app, self.config)
        self.assertEqual([], errors)

    def test_rejects_missing_sdk_resources(self):
        self.write_info()
        _, errors = release.verify_app(self.app, self.config)
        self.assertEqual(2, len(errors), errors)

    def test_rejects_undeclared_data_type(self):
        self.write_info()
        self.write_resources(crash_types=())
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any(CRASH_TYPE in e for e in errors), errors)

    def test_rejects_corrupt_sdk_manifest(self):
        self.write_info()
        self.write_resources()
        manifest = self.app / "Frameworks/UnityFramework.framework" / CRASH / "PrivacyInfo.xcprivacy"
        manifest.write_text("invalid plist")
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any("Invalid privacy manifest" in e for e in errors), errors)

    def test_bundle_id_check(self):
        self.write_info(bundle_id="com.other")
        self.write_resources()
        self.config["expected_bundle_id"] = "com.company.game"
        _, errors = release.verify_app(self.app, self.config)
        self.assertTrue(any("CFBundleIdentifier" in e for e in errors), errors)

    def test_default_config_needs_no_sdk_bundles(self):
        self.write_info()
        _, errors = release.verify_app(self.app)
        self.assertEqual([], errors)

    def test_accepts_configuration_without_mutating_app(self):
        self.write_info()
        self.write_resources()
        before = (self.app / "Info.plist").read_bytes()
        _, errors = release.verify_app(self.app, self.config)
        self.assertEqual([], errors)
        self.assertEqual(before, (self.app / "Info.plist").read_bytes())

    def test_privacy_url_policy(self):
        scene = self.root / "Main.unity"
        rule = {"file": "Main.unity", "pattern": r"^  _privacyUrl: (.+)$"}
        scene.write_text("  _privacyUrl: http://insecure\n")
        self.assertEqual(1, len(release.verify_privacy_url(self.root, rule)))
        scene.write_text("  _privacyUrl: https://example.com/privacy\n")
        self.assertEqual([], release.verify_privacy_url(self.root, rule))
        self.assertEqual([], release.verify_privacy_url(self.root, None))

    def test_archive_resolution_and_cli_exit_code(self):
        archive = self.root / "Game.xcarchive"
        app = archive / "Products/Applications/Game.app"
        app.mkdir(parents=True)
        self.app = app
        self.write_info()
        self.assertEqual(app, release.resolve_app(archive))
        self.assertEqual(0, release.main([str(archive), "--skip-policy"]))
        config = self.root / "config.json"
        config.write_text(json.dumps({"expected_bundle_id": "com.mismatch"}))
        self.assertEqual(1, release.main([str(archive), "--skip-policy", "--config", str(config)]))

    def test_unknown_config_key_is_rejected(self):
        config = self.root / "bad.json"
        config.write_text(json.dumps({"required_bundles": []}))
        with self.assertRaises(ValueError):
            release.load_config(config)


if __name__ == "__main__":
    unittest.main()
