# Laboon Mobile Kit 0.1.0 — extracted from Pig Beam

| Pig Beam source | Package | New API |
|---|---|---|
| `UI/SafeAreaHudAnchor.cs` | mobile-core | `SafeAreaUtility`, `SafeAreaAnchor` (driven anchors, no scene dirtying) |
| `Environment/CameraResponsiveFitter.cs` | mobile-core | `ResponsiveCameraFitter` (`FormerlySerializedAs` old `_` fields), `CameraFitMath` |
| `ShooterController.HandleInput` pointer block | mobile-core | `MobilePointer.Read()`, `PointerState`, `PointerTracker` |
| `UI/SimulatedRewardedAdProvider.cs` (DOTween) | mobile-core | `IRewardedAdProvider`, `RewardedAds`, `SimulatedRewardedAdProvider` (no DOTween) |
| `CrashReporting/FirebaseCrashlyticsInitializer.cs` | mobile-core + firebase sample | `ICrashReporter`, `CrashReports`, sample `FirebaseCrashReporter` |
| `Effects/HapticFeedback.cs`, `HapticSettings.cs` | mobile-haptics | `Haptics`, `HapticCue`, `HapticSettings` (`FormerlySerializedAs` old field names) |
| `Plugins/iOS/PigBeamHaptics.mm`, `PigBeamHaptics.androidlib` | mobile-haptics | `MobileHaptics.mm` (`LaboonHaptics*`), `LaboonHaptics.androidlib` |
| `Editor/AndroidBuild.cs`, `IOSBuild.cs`, `AndroidBuildPipeline.cs` | mobile-build | `MobileBuildProfile`, `MobileBuildPipeline`, `MobileBuildMenu`, `MobileBuildVersionWindow`, `IMobileBuildHook`, `GradleTemplateValidator` |
| `Editor/IOSReleasePostprocess.cs`, `Plugins/iOS/PigBeamSceneLifecycle.mm` | mobile-ios | `IOSReleasePostprocessor`, `IOSSceneLifecycleConfig`, `MobileSceneLifecycle.mm` (`LaboonSceneLifecycle` marker) |
| `scripts/verify_ios_release.py` | mobile-ios `CI~` | config-driven verifier + `ios_release_config.example.json` |
| `scripts/verify_mobile_app_icons.py`, `scripts/tests/*_probe` | mobile-verification `CI~` | manifest-driven icon guard, native probes, `run_all.sh` |
| `scripts/copy_ios_sdk_resources.sh` | mobile-firebase sample | `FirebaseBuildHook` |

## Migration notes for Pig Beam
- `HapticEvent` → `HapticCue` keeps the same integer values. Set `PreferenceKey = PigBeam_HapticsEnabled` on the settings asset to keep players' choice.
- `HapticSettings.asset` and `CameraResponsiveFitter` scene data reload through `FormerlySerializedAs` once the script reference points to the package type (project wrapper subclass, or GUID remap).
- Wrappers must pass build flavor explicitly: `AndroidBuild.BuildApk` → `Release` from `PIGBEAM_ANDROID_RELEASE`, `StatusFilePath`; `IOSBuild.ExportXcodeProject` → `Release = true`, `OutputPath` from `PIGBEAM_IOS_EXPORT_PATH`.
- Keep `PigBeamSceneDelegate` via the mobile-ios "Legacy Scene Delegate Alias" sample for one release.
- Remove the project native plugins only after the package path is verified (duplicate plugins = double haptics).

## Verify
- Unity: Test Runner, EditMode, `Laboon.*` (79 tests).
- Python: `sh Packages/com.laboon.mobile-verification/CI~/run_all.sh [--icons scripts/mobile_icons.json]`.
