# Changelog

## [0.1.0] - 2026-10-20
### Added
- `MobileBuildProfile` asset (app id, product, SDKs, architectures, scenes, outputs) with `Validate()`.
- `MobileBuildPipeline`: `BuildAndroid`, `ExportIOS`, `SwitchPlatform`, profile-driven PlayerSettings apply with snapshot/restore.
- `MobileBuildVersion` and `MobileBuildVersionWindow` (version + version code / build number before a build).
- `MobileBuildMenu` helpers for project `[MenuItem]` wrappers.
- `IMobileBuildHook` with automatic discovery; Android hooks after Gradle generation, iOS hooks after Xcode export.
- `GradleTemplateValidator`: validates project Gradle templates, never overwrites them.
- Generic `MOBILE_BUILD_*` environment variables.
