using UnityEditor;
using UnityEditor.Build;

namespace Laboon.Mobile.Build
{
    /// <summary>Captures the Player Settings a profile apply can change, so a build can restore them afterwards.</summary>
    public sealed class PlayerSettingsSnapshot
    {
        private string m_ProductName;
        private string m_CompanyName;
        private string m_AndroidId;
        private string m_IosId;
        private UIOrientation m_Orientation;
        private AndroidSdkVersions m_MinSdk;
        private AndroidSdkVersions m_TargetSdk;
        private AndroidArchitecture m_Architectures;
        private ScriptingImplementation m_AndroidBackend;
        private bool m_BuildAppBundle;
        private string m_IosMinimumVersion;
        private string m_IosTeamId;
        private bool m_IosAutomaticSigning;

        public static PlayerSettingsSnapshot Capture()
        {
            return new PlayerSettingsSnapshot
            {
                m_ProductName = PlayerSettings.productName,
                m_CompanyName = PlayerSettings.companyName,
                m_AndroidId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                m_IosId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS),
                m_Orientation = PlayerSettings.defaultInterfaceOrientation,
                m_MinSdk = PlayerSettings.Android.minSdkVersion,
                m_TargetSdk = PlayerSettings.Android.targetSdkVersion,
                m_Architectures = PlayerSettings.Android.targetArchitectures,
                m_AndroidBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                m_BuildAppBundle = EditorUserBuildSettings.buildAppBundle,
                m_IosMinimumVersion = PlayerSettings.iOS.targetOSVersionString,
                m_IosTeamId = PlayerSettings.iOS.appleDeveloperTeamID,
                m_IosAutomaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning
            };
        }

        public void Restore()
        {
            PlayerSettings.productName = m_ProductName;
            PlayerSettings.companyName = m_CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, m_AndroidId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, m_IosId);
            PlayerSettings.defaultInterfaceOrientation = m_Orientation;
            PlayerSettings.Android.minSdkVersion = m_MinSdk;
            PlayerSettings.Android.targetSdkVersion = m_TargetSdk;
            PlayerSettings.Android.targetArchitectures = m_Architectures;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, m_AndroidBackend);
            EditorUserBuildSettings.buildAppBundle = m_BuildAppBundle;
            PlayerSettings.iOS.targetOSVersionString = m_IosMinimumVersion;
            PlayerSettings.iOS.appleDeveloperTeamID = m_IosTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = m_IosAutomaticSigning;
        }
    }
}
