// Editor-only. Put this file in an Editor asmdef that references Laboon.Mobile.Build.Editor
// (and UnityEditor.iOS.Extensions.Xcode when the iOS module is installed).
using System;
using System.IO;
using Laboon.Mobile.Build;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Laboon.Mobile.Samples.Firebase.Editor
{
    /// <summary>
    /// Firebase-specific build steps kept out of the shared packages.
    /// Android: fails early when Firebase's Gradle inputs are missing. iOS: embeds SwiftPM SDK resources.
    /// </summary>
    [MobileBuildHookOrder(100)]
    public sealed class FirebaseBuildHook : IMobileBuildHook
    {
        /// <summary>Project-relative copy script; adjust if the project keeps it elsewhere.</summary>
        public const string CopyScriptPath = "scripts/copy_ios_sdk_resources.sh";
        public const string BuildPhaseName = "Embed SDK Resources";

        public void OnAndroidProjectGenerated(string projectPath)
        {
            // projectPath is .../unityLibrary. Unity places .androidlib modules inside or beside it depending on version.
            string inside = Path.Combine(projectPath, "FirebaseApp.androidlib");
            string beside = Path.Combine(Path.GetDirectoryName(projectPath) ?? projectPath, "FirebaseApp.androidlib");
            if (!Directory.Exists(inside) && !Directory.Exists(beside))
                throw new InvalidOperationException(
                    "FirebaseApp.androidlib missing from the Gradle project. Run Assets > External Dependency Manager > " +
                    "Android Resolver > Force Resolve and check google-services.json.");
        }

        public void OnIOSPostprocess(string xcodeProjectPath, MobileBuildProfile profile)
        {
#if UNITY_IOS
            if (!File.Exists(CopyScriptPath))
                throw new FileNotFoundException($"Missing {CopyScriptPath}; Firebase privacy bundles would not be embedded.");
            string projectPath = PBXProject.GetPBXProjectPath(xcodeProjectPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            string framework = project.GetUnityFrameworkTargetGuid();
            string script = File.ReadAllText(CopyScriptPath);
            if (project.GetShellScriptBuildPhaseForTarget(framework, BuildPhaseName, "/bin/sh", script) == null)
                project.AddShellScriptBuildPhase(framework, BuildPhaseName, "/bin/sh", script);
            project.WriteToFile(projectPath);
#endif
        }
    }
}
