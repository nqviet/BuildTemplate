# Native probes

Both probes compile the **packaged** native code unchanged against test doubles.

| Probe | What it checks | How it runs |
|---|---|---|
| `haptics_probe` | `MobileHaptics.mm`: generator reuse, main-thread access, stop/replace cancellation, background rejection | `tests/test_native_probes.py` on macOS (fake UIKit, Foundation only) |
| `ios_lifecycle_probe` | `MobileSceneLifecycle.mm`: Unity initializes once, only after a `UIWindowScene` exists; active/background/foreground events | Compile check in `tests/test_native_probes.py`; full run is a simulator app (below) |

## Running the lifecycle probe on a simulator

1. Create an iOS app target in Xcode, add `ios_lifecycle_probe/main.mm`, `UnityAppController.h`
   and the package `Runtime/Plugins/iOS/MobileSceneLifecycle.{h,mm}`.
2. Info.plist: add `UIApplicationSceneManifest` with `UISceneDelegateClassName = MobileSceneDelegate`
   and `LaboonSceneLifecycle = YES`.
3. Run, background and foreground the app, then read `Documents/lifecycle-events.txt`:
   expect `initialized-with-scene`, `active`, `inactive`, `background`, `foreground`, `active`.
4. Remove `LaboonSceneLifecycle` and confirm the app still starts (fallback to Unity's own path).

These probes verify control flow only. Haptic strength and real Unity start-up still need a device check.
