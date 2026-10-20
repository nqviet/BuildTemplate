# Changelog

## [0.1.0] - 2026-10-20
### Added
- `MobileSceneLifecycle.mm`: `MobileAppController` defers Unity start-up until UIKit connects the window scene; `MobileSceneDelegate` forwards scene events, URLs and user activities. Activated by the `LaboonSceneLifecycle` Info.plist key, otherwise falls back to Unity's normal path.
- `IOSSceneLifecycleConfig` asset and `IOSReleasePostprocessor` (`[PostProcessBuild(10000)]`): scene manifest, marker key, export-compliance flag.
- `CI~/verify_ios_release.py`: config-driven checks (scene manifest + marker, delegate class, encryption flag, required SDK privacy bundles, privacy manifests, bundle id, privacy URL).
- Sample: legacy scene-delegate alias for migrating projects.
