# Changelog

## [0.1.0] - 2026-10-20
### Added
- `Haptics` facade: `Play(HapticCue)`, `Prepare`, `Stop`, `Enabled`, `SetEnabled`.
- `HapticCue` (Preview, Launch, Complete, Booster, Success, Failure, Tick, Impact), `HapticPulse`, `HapticStyle`.
- `HapticSettings` asset with `FormerlySerializedAs` mappings for assets migrated from game-specific event names.
- Native iOS bridge `MobileHaptics.mm` (`LaboonHaptics*` symbols) and the `LaboonHaptics.androidlib` VIBRATE permission library.
