# Legacy Scene Delegate Alias

Use this when a project already shipped with its own `UISceneDelegateClassName` (for example `PigBeamSceneDelegate`).

1. Import the sample, rename `LegacySceneDelegate` in the `.mm` to the old class name.
2. In the `.mm` import settings, enable iOS only.
3. Create `IOSSceneLifecycleConfig` and set **Delegate Class Name** to the old name.
4. The native plugin accepts any subclass of `MobileSceneDelegate`, and the `LaboonSceneLifecycle` marker turns it on.
5. Remove the alias after one release and switch the config back to `MobileSceneDelegate`.
