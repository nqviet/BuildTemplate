"""Compile the packaged native bridges with fake UIKit. Control-flow checks only; no device verification."""
import pathlib
import shutil
import subprocess
import sys
import tempfile
import unittest

CI = pathlib.Path(__file__).resolve().parents[1]
PACKAGES = CI.parents[1]


def package_dir(name):
    """Find a sibling package (embedded) or one passed via LABOON_PACKAGES_DIR."""
    import os
    base = pathlib.Path(os.environ.get("LABOON_PACKAGES_DIR", PACKAGES))
    return base / name


@unittest.skipUnless(sys.platform == "darwin" and shutil.which("xcrun"), "Requires macOS Foundation and clang")
class NativeProbeTests(unittest.TestCase):
    def compile(self, sources, include_dirs, frameworks):
        directory = tempfile.mkdtemp(prefix="laboon-probe-")
        self.addCleanup(shutil.rmtree, directory, True)
        executable = pathlib.Path(directory) / "probe"
        command = ["xcrun", "clang++", "-fobjc-arc", "-Wall", "-Wextra", "-Werror", "-Wno-unused-parameter"]
        command += [f"-I{d}" for d in include_dirs]
        for framework in frameworks:
            command += ["-framework", framework]
        command += [str(s) for s in sources] + ["-o", str(executable)]
        result = subprocess.run(command, capture_output=True, text=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return executable

    def test_haptics_preparation_reuse_cancellation_and_background(self):
        fixture = CI / "probes/haptics_probe"
        bridge = package_dir("com.laboon.mobile-haptics") / "Runtime/Plugins/iOS/MobileHaptics.mm"
        executable = self.compile([bridge, fixture / "main.mm"], [fixture], ["Foundation"])
        result = subprocess.run([str(executable)], capture_output=True, text=True, timeout=10)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Native control-flow probe passed", result.stdout)

    def test_scene_lifecycle_compiles_against_fake_trampoline(self):
        # Compile-only: the lifecycle probe is an app and must run on a simulator (see probes/README.md).
        fixture = CI / "probes/ios_lifecycle_probe"
        plugin = package_dir("com.laboon.mobile-ios") / "Runtime/Plugins/iOS"
        sdk = subprocess.run(["xcrun", "--sdk", "iphonesimulator", "--show-sdk-path"], capture_output=True, text=True)
        if sdk.returncode != 0:
            self.skipTest("iOS simulator SDK not installed")
        directory = tempfile.mkdtemp(prefix="laboon-lifecycle-")
        self.addCleanup(shutil.rmtree, directory, True)
        result = subprocess.run([
            "xcrun", "--sdk", "iphonesimulator", "clang++", "-fobjc-arc", "-fsyntax-only",
            "-target", "arm64-apple-ios15.0-simulator", f"-I{fixture}", f"-I{plugin}",
            str(plugin / "MobileSceneLifecycle.mm"),
        ], capture_output=True, text=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
