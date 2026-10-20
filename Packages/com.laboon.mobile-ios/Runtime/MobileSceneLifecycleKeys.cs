namespace Laboon.Mobile.IOS
{
    /// <summary>Info.plist keys and class names shared by the native plugin, the postprocessor and the verifier.</summary>
    public static class MobileSceneLifecycleKeys
    {
        /// <summary>Boolean Info.plist key that turns on the package scene lifecycle in native code.</summary>
        public const string MarkerKey = "LaboonSceneLifecycle";

        /// <summary>Scene delegate class implemented by MobileSceneLifecycle.mm.</summary>
        public const string DefaultDelegateClassName = "MobileSceneDelegate";

        public const string SceneManifestKey = "UIApplicationSceneManifest";
        public const string SupportsMultipleScenesKey = "UIApplicationSupportsMultipleScenes";
        public const string SceneConfigurationsKey = "UISceneConfigurations";
        public const string ApplicationRoleKey = "UIWindowSceneSessionRoleApplication";
        public const string ConfigurationNameKey = "UISceneConfigurationName";
        public const string SceneClassNameKey = "UISceneClassName";
        public const string DelegateClassNameKey = "UISceneDelegateClassName";
        public const string EncryptionKey = "ITSAppUsesNonExemptEncryption";
    }
}
