using System;
using UnityEditor;
using UnityEngine;

namespace Laboon.Mobile.Build
{
    /// <summary>Asks for the version and version code before a build. Cancel leaves Player Settings unchanged.</summary>
    public sealed class MobileBuildVersionWindow : EditorWindow
    {
        [SerializeField] private string m_Version;
        [SerializeField] private int m_VersionCode;
        [SerializeField] private string m_ActionLabel;

        private Action m_OnConfirmed;

        /// <summary>Opens the window. <paramref name="onConfirmed"/> runs on the next editor tick after the version is saved.</summary>
        public static MobileBuildVersionWindow Open(string title, string actionLabel, Action onConfirmed)
        {
            var window = GetWindow<MobileBuildVersionWindow>(true, title, true);
            window.m_ActionLabel = actionLabel;
            window.m_Version = PlayerSettings.bundleVersion;
            window.m_VersionCode = PlayerSettings.Android.bundleVersionCode;
            window.m_OnConfirmed = onConfirmed;
            window.minSize = new Vector2(480, 320);
            window.ShowUtility();
            return window;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins))
            {
                GUILayout.Space(12);
                EditorGUILayout.LabelField(m_ActionLabel, EditorStyles.boldLabel);
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Current app version", PlayerSettings.bundleVersion);
                EditorGUILayout.LabelField("Current version code", PlayerSettings.Android.bundleVersionCode.ToString());
                GUILayout.Space(8);
                m_Version = EditorGUILayout.TextField("Version to build", m_Version);
                m_VersionCode = EditorGUILayout.IntField("Version code to build", m_VersionCode);
                bool valid = MobileBuildVersion.IsValidVersion(m_Version) && MobileBuildVersion.IsValidVersionCode(m_VersionCode);
                EditorGUILayout.HelpBox(valid
                        ? "This version is saved to Player Settings (Android version code and iOS build number) when you build."
                        : "Enter a version such as 1.1.0 and a positive integer version code.",
                    valid ? MessageType.Info : MessageType.Warning);
                if (valid && !MobileBuildVersion.IsNotDowngrade(PlayerSettings.bundleVersion, m_Version))
                    EditorGUILayout.HelpBox("This version is lower than the current one.", MessageType.Warning);
                EditorGUILayout.HelpBox("For a store update, use a version code higher than the previous release. Nothing is incremented automatically.", MessageType.Info);
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cancel", GUILayout.Height(32))) Close();
                    using (new EditorGUI.DisabledScope(!valid || m_OnConfirmed == null || EditorApplication.isCompiling
                        || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
                    {
                        if (GUILayout.Button(m_ActionLabel, GUILayout.Height(32)))
                        {
                            MobileBuildVersion.Save(m_Version, m_VersionCode);
                            var confirmed = m_OnConfirmed;
                            Close();
                            EditorApplication.delayCall += () => confirmed();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                GUILayout.Space(12);
            }
        }
    }
}
