using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Laboon.Mobile.Build
{
    /// <summary>
    /// Project build values. The pipeline reads this asset instead of hardcoding app ids, names, outputs or scenes.
    /// </summary>
    [CreateAssetMenu(menuName = "Laboon/Mobile Build Profile", fileName = "MobileBuildProfile")]
    public sealed class MobileBuildProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_ApplicationId = "com.company.game";
        [SerializeField] private string m_ProductName = "Game";
        [Tooltip("Empty keeps the current Player Settings value.")]
        [SerializeField] private string m_CompanyName = "";
        [SerializeField] private UIOrientation m_Orientation = UIOrientation.Portrait;
        [Tooltip("Write identity/platform values to Player Settings before each build.")]
        [SerializeField] private bool m_ApplyPlayerSettings = true;
        [Tooltip("Restore the previous Player Settings after the build (prevents setting drift).")]
        [SerializeField] private bool m_RestorePlayerSettings = false;

        [Header("Android")]
        [SerializeField, Range(19, 36)] private int m_AndroidMinSdk = 25;
        [Tooltip("0 = highest installed.")]
        [SerializeField] private int m_AndroidTargetSdk = 0;
        [SerializeField] private AndroidArchitecture m_AndroidArchitectures = AndroidArchitecture.ARM64;
        [SerializeField] private ScriptingImplementation m_ScriptingBackend = ScriptingImplementation.IL2CPP;
        [SerializeField] private bool m_BuildAppBundle = false;
        [SerializeField] private string m_AndroidOutputPath = "Builds/Android/Game.apk";
        [Tooltip("Empty = same as the development output.")]
        [SerializeField] private string m_AndroidReleaseOutputPath = "";
        [SerializeField] private bool m_ValidateGradleTemplates = true;

        [Header("iOS")]
        [SerializeField] private string m_IosMinimumVersion = "15.0";
        [SerializeField] private string m_IosTeamId = "";
        [SerializeField] private bool m_IosAutomaticSigning = true;
        [SerializeField] private string m_IosOutputPath = "Builds/iOS/Game";

        [Header("Scenes")]
        [Tooltip("Use the enabled scenes from Build Settings. Off = use the list below.")]
        [SerializeField] private bool m_UseEditorBuildSettingsScenes = true;
        [SerializeField] private SceneAsset[] m_Scenes = Array.Empty<SceneAsset>();

        public string ApplicationId { get => m_ApplicationId; set => m_ApplicationId = value; }
        public string ProductName { get => m_ProductName; set => m_ProductName = value; }
        public string CompanyName { get => m_CompanyName; set => m_CompanyName = value; }
        public UIOrientation Orientation { get => m_Orientation; set => m_Orientation = value; }
        public bool ApplyPlayerSettings { get => m_ApplyPlayerSettings; set => m_ApplyPlayerSettings = value; }
        public bool RestorePlayerSettings { get => m_RestorePlayerSettings; set => m_RestorePlayerSettings = value; }

        public int AndroidMinSdk { get => m_AndroidMinSdk; set => m_AndroidMinSdk = value; }
        public int AndroidTargetSdk { get => m_AndroidTargetSdk; set => m_AndroidTargetSdk = value; }
        public AndroidArchitecture AndroidArchitectures { get => m_AndroidArchitectures; set => m_AndroidArchitectures = value; }
        public ScriptingImplementation ScriptingBackend { get => m_ScriptingBackend; set => m_ScriptingBackend = value; }
        public bool BuildAppBundle { get => m_BuildAppBundle; set => m_BuildAppBundle = value; }
        public string AndroidOutputPath { get => m_AndroidOutputPath; set => m_AndroidOutputPath = value; }
        public string AndroidReleaseOutputPath { get => m_AndroidReleaseOutputPath; set => m_AndroidReleaseOutputPath = value; }
        public bool ValidateGradleTemplates { get => m_ValidateGradleTemplates; set => m_ValidateGradleTemplates = value; }

        public string IosMinimumVersion { get => m_IosMinimumVersion; set => m_IosMinimumVersion = value; }
        public string IosTeamId { get => m_IosTeamId; set => m_IosTeamId = value; }
        public bool IosAutomaticSigning { get => m_IosAutomaticSigning; set => m_IosAutomaticSigning = value; }
        public string IosOutputPath { get => m_IosOutputPath; set => m_IosOutputPath = value; }

        public bool UseEditorBuildSettingsScenes { get => m_UseEditorBuildSettingsScenes; set => m_UseEditorBuildSettingsScenes = value; }
        public SceneAsset[] Scenes { get => m_Scenes; set => m_Scenes = value ?? Array.Empty<SceneAsset>(); }

        /// <summary>Android output for the requested build flavor.</summary>
        public string GetAndroidOutputPath(bool release) =>
            release && !string.IsNullOrWhiteSpace(m_AndroidReleaseOutputPath) ? m_AndroidReleaseOutputPath : m_AndroidOutputPath;

        /// <summary>Scene paths to build.</summary>
        public string[] ResolveScenePaths()
        {
            if (m_UseEditorBuildSettingsScenes)
            {
                return EditorBuildSettings.scenes
                    .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                    .Select(s => s.path)
                    .ToArray();
            }
            return m_Scenes.Where(s => s != null).Select(AssetDatabase.GetAssetPath)
                .Where(p => !string.IsNullOrEmpty(p)).ToArray();
        }

        /// <summary>Configuration errors; empty when the profile can build.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (!IsValidApplicationId(m_ApplicationId))
                errors.Add($"Application ID '{m_ApplicationId}' must be a reverse-domain id such as com.company.game.");
            if (string.IsNullOrWhiteSpace(m_ProductName)) errors.Add("Product name is empty.");
            if (string.IsNullOrWhiteSpace(m_AndroidOutputPath)) errors.Add("Android output path is empty.");
            if (string.IsNullOrWhiteSpace(m_IosOutputPath)) errors.Add("iOS output path is empty.");
            if (m_AndroidArchitectures == AndroidArchitecture.None) errors.Add("Select at least one Android architecture.");
            if (m_AndroidTargetSdk != 0 && m_AndroidTargetSdk < m_AndroidMinSdk)
                errors.Add($"Android target SDK {m_AndroidTargetSdk} is below min SDK {m_AndroidMinSdk}.");
            if (!MobileBuildVersion.IsValidOsVersion(m_IosMinimumVersion))
                errors.Add($"iOS minimum version '{m_IosMinimumVersion}' must look like 15.0.");
            if (!m_UseEditorBuildSettingsScenes && m_Scenes.All(s => s == null))
                errors.Add("Scene list is empty; add scenes or use the Build Settings scenes.");
            return errors;
        }

        public static bool IsValidApplicationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            string[] parts = id.Split('.');
            if (parts.Length < 2) return false;
            foreach (string part in parts)
            {
                if (part.Length == 0 || !char.IsLetter(part[0])) return false;
                foreach (char c in part)
                    if (!(char.IsLetterOrDigit(c) && c < 128) && c != '_' && c != '-') return false;
            }
            return true;
        }

        /// <summary>
        /// The profile in use: the one set for the running build, otherwise the first asset in the project.
        /// </summary>
        public static MobileBuildProfile FindDefault()
        {
            if (MobileBuildPipeline.ActiveProfile != null) return MobileBuildPipeline.ActiveProfile;
            string guid = AssetDatabase.FindAssets("t:" + nameof(MobileBuildProfile)).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<MobileBuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
        }

        /// <summary>Loads a profile by path or fails with an actionable message.</summary>
        public static MobileBuildProfile Load(string assetPath)
        {
            var profile = AssetDatabase.LoadAssetAtPath<MobileBuildProfile>(assetPath);
            if (profile == null)
                throw new InvalidOperationException(
                    $"MobileBuildProfile not found at '{assetPath}'. Create one with Assets > Create > Laboon > Mobile Build Profile.");
            return profile;
        }
    }
}
