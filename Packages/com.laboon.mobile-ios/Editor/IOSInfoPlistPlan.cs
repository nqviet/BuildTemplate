using System.Collections.Generic;

namespace Laboon.Mobile.IOS
{
    /// <summary>Minimal plist dictionary surface, so the plan can be tested without the iOS build module.</summary>
    public interface IPlistDict
    {
        bool Contains(string key);
        void SetBool(string key, bool value);
        void SetString(string key, string value);
        IPlistDict CreateDict(string key);
        /// <summary>Creates an array under <paramref name="key"/> and returns a new dictionary appended to it.</summary>
        IPlistDict CreateArrayWithDict(string key);
    }

    /// <summary>Info.plist changes for the scene lifecycle and export compliance.</summary>
    public static class IOSInfoPlistPlan
    {
        /// <summary>Applies the plan to <paramref name="root"/>. Returns a human-readable list of changes.</summary>
        public static List<string> Apply(IPlistDict root, IOSSceneLifecycleConfig config, string productName)
        {
            var changes = new List<string>();
            if (config.Enabled)
            {
                bool hasManifest = root.Contains(MobileSceneLifecycleKeys.SceneManifestKey);
                if (!hasManifest || config.OverwriteExistingSceneManifest)
                {
                    var manifest = root.CreateDict(MobileSceneLifecycleKeys.SceneManifestKey);
                    manifest.SetBool(MobileSceneLifecycleKeys.SupportsMultipleScenesKey, false);
                    var configuration = manifest.CreateDict(MobileSceneLifecycleKeys.SceneConfigurationsKey)
                        .CreateArrayWithDict(MobileSceneLifecycleKeys.ApplicationRoleKey);
                    configuration.SetString(MobileSceneLifecycleKeys.ConfigurationNameKey, config.ResolveConfigurationName(productName));
                    configuration.SetString(MobileSceneLifecycleKeys.SceneClassNameKey, "UIWindowScene");
                    configuration.SetString(MobileSceneLifecycleKeys.DelegateClassNameKey, config.ResolveDelegateClassName());
                    root.SetBool(MobileSceneLifecycleKeys.MarkerKey, true);
                    changes.Add($"{MobileSceneLifecycleKeys.SceneManifestKey} -> {config.ResolveDelegateClassName()}");
                    changes.Add($"{MobileSceneLifecycleKeys.MarkerKey} = true");
                }
                else
                {
                    // Keep native scene support from newer Unity versions or user configuration.
                    changes.Add($"{MobileSceneLifecycleKeys.SceneManifestKey} kept (already present)");
                }
            }

            if (config.SetEncryptionDeclaration && !root.Contains(MobileSceneLifecycleKeys.EncryptionKey))
            {
                root.SetBool(MobileSceneLifecycleKeys.EncryptionKey, config.UsesNonExemptEncryption);
                changes.Add($"{MobileSceneLifecycleKeys.EncryptionKey} = {config.UsesNonExemptEncryption}");
            }
            return changes;
        }
    }

    /// <summary>In-memory plist dictionary for tests and dry runs.</summary>
    public sealed class DictionaryPlist : IPlistDict
    {
        public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

        public bool Contains(string key) => Values.ContainsKey(key);
        public void SetBool(string key, bool value) => Values[key] = value;
        public void SetString(string key, string value) => Values[key] = value;

        public IPlistDict CreateDict(string key)
        {
            var dict = new DictionaryPlist();
            Values[key] = dict;
            return dict;
        }

        public IPlistDict CreateArrayWithDict(string key)
        {
            var dict = new DictionaryPlist();
            Values[key] = new List<object> { dict };
            return dict;
        }
    }
}
