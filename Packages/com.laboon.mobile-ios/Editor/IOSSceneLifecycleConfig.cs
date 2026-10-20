using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Laboon.Mobile.IOS
{
    /// <summary>
    /// Project values for the iOS scene lifecycle and release plist. Optional: without an asset the defaults apply
    /// and the configuration name comes from the build profile's product name.
    /// </summary>
    [CreateAssetMenu(menuName = "Laboon/iOS Scene Lifecycle Config", fileName = "IOSSceneLifecycleConfig")]
    public sealed class IOSSceneLifecycleConfig : ScriptableObject
    {
        [Tooltip("Write the scene manifest and the LaboonSceneLifecycle marker.")]
        [SerializeField] private bool m_Enabled = true;
        [Tooltip("Empty = product name.")]
        [SerializeField] private string m_SceneConfigurationName = "";
        [Tooltip("MobileSceneDelegate, or a subclass kept for compatibility.")]
        [SerializeField] private string m_DelegateClassName = MobileSceneLifecycleKeys.DefaultDelegateClassName;
        [Tooltip("Replace a scene manifest that is already present (otherwise it is kept and the marker is not set).")]
        [SerializeField] private bool m_OverwriteExistingSceneManifest = false;

        [Header("Export compliance")]
        [Tooltip("Add ITSAppUsesNonExemptEncryption when it is absent.")]
        [SerializeField] private bool m_SetEncryptionDeclaration = true;
        [Tooltip("Value written when absent. False for apps using only HTTPS/TLS and OS crypto.")]
        [SerializeField] private bool m_UsesNonExemptEncryption = false;

        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }
        public string SceneConfigurationName { get => m_SceneConfigurationName; set => m_SceneConfigurationName = value; }
        public string DelegateClassName { get => m_DelegateClassName; set => m_DelegateClassName = value; }
        public bool OverwriteExistingSceneManifest { get => m_OverwriteExistingSceneManifest; set => m_OverwriteExistingSceneManifest = value; }
        public bool SetEncryptionDeclaration { get => m_SetEncryptionDeclaration; set => m_SetEncryptionDeclaration = value; }
        public bool UsesNonExemptEncryption { get => m_UsesNonExemptEncryption; set => m_UsesNonExemptEncryption = value; }

        public string ResolveConfigurationName(string productName)
        {
            if (!string.IsNullOrWhiteSpace(m_SceneConfigurationName)) return m_SceneConfigurationName;
            return string.IsNullOrWhiteSpace(productName) ? "Default Configuration" : productName;
        }

        public string ResolveDelegateClassName() =>
            string.IsNullOrWhiteSpace(m_DelegateClassName) ? MobileSceneLifecycleKeys.DefaultDelegateClassName : m_DelegateClassName;

        /// <summary>The first config asset in the project, or a transient default.</summary>
        public static IOSSceneLifecycleConfig FindOrDefault()
        {
            string guid = AssetDatabase.FindAssets("t:" + nameof(IOSSceneLifecycleConfig))
                .OrderBy(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            var config = guid == null ? null : AssetDatabase.LoadAssetAtPath<IOSSceneLifecycleConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (config != null) return config;
            config = CreateInstance<IOSSceneLifecycleConfig>();
            config.hideFlags = HideFlags.HideAndDontSave;
            return config;
        }
    }
}
