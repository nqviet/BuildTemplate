using UnityEditor;
using UnityEngine;

namespace Laboon.Mobile.Build
{
    /// <summary>
    /// Helpers for project menu wrappers. <c>[MenuItem]</c> paths must be compile-time constants, so each project
    /// keeps its own menu class and calls these.
    /// </summary>
    public static class MobileBuildMenu
    {
        /// <summary>Batch mode builds immediately; interactive sessions ask for the version first.</summary>
        public static void BuildAndroid(MobileBuildProfile profile, bool autoRun, MobileBuildOptions options = null)
        {
            profile = MobileBuildPipeline.ResolveProfile(profile);
            options = options ?? new MobileBuildOptions();
            options.AutoRun = autoRun;
            if (autoRun && options.Release == null) options.Release = false;
            if (Application.isBatchMode)
            {
                MobileBuildPipeline.BuildAndroid(profile, options);
                return;
            }
            MobileBuildVersionWindow.Open($"{profile.ProductName} — Build APK", autoRun ? "Build & Run" : "Build APK", () =>
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    MobileBuildPipeline.SwitchPlatform(BuildTarget.Android);
                MobileBuildPipeline.BuildAndroid(profile, options);
            });
        }

        public static void ExportIOS(MobileBuildProfile profile, MobileBuildOptions options = null)
        {
            profile = MobileBuildPipeline.ResolveProfile(profile);
            options = options ?? new MobileBuildOptions();
            if (Application.isBatchMode)
            {
                MobileBuildPipeline.ExportIOS(profile, options);
                return;
            }
            MobileBuildVersionWindow.Open($"{profile.ProductName} — Export Xcode", "Export Xcode Project", () =>
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                    MobileBuildPipeline.SwitchPlatform(BuildTarget.iOS);
                MobileBuildPipeline.ExportIOS(profile, options);
            });
        }

        /// <summary>Selects the default profile in the Project window, or explains how to create one.</summary>
        public static void SelectProfile()
        {
            var profile = MobileBuildProfile.FindDefault();
            if (profile == null)
            {
                EditorUtility.DisplayDialog("Mobile Build Profile",
                    "No profile found. Use Assets > Create > Laboon > Mobile Build Profile.", "OK");
                return;
            }
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }
    }
}
