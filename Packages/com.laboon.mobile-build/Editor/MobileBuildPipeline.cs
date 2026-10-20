using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Laboon.Mobile.Build
{
    /// <summary>Per-build options. Null fields fall back to the profile and the generic environment variables.</summary>
    public sealed class MobileBuildOptions
    {
        /// <summary>Release build (no Development flag). Null = <c>MOBILE_BUILD_RELEASE=1</c>.</summary>
        public bool? Release;
        /// <summary>Install and launch after the build (Android).</summary>
        public bool AutoRun;
        /// <summary>Output override. Null = <c>MOBILE_BUILD_OUTPUT</c>, then the profile.</summary>
        public string OutputPath;
        /// <summary>Optional file that receives STARTED / SUCCEEDED path / FAILED message, for external automation.</summary>
        public string StatusFilePath;
        /// <summary>Show dialogs and reveal output in interactive sessions.</summary>
        public bool Interactive = !Application.isBatchMode;
    }

    /// <summary>Profile-driven Android/iOS builds. Project menus and CI entry points call into this.</summary>
    public static class MobileBuildPipeline
    {
        public const string ReleaseVariable = "MOBILE_BUILD_RELEASE";
        public const string OutputVariable = "MOBILE_BUILD_OUTPUT";
        public const string ProfileVariable = "MOBILE_BUILD_PROFILE";

        /// <summary>Profile of the build in progress; null outside a pipeline build.</summary>
        public static MobileBuildProfile ActiveProfile { get; private set; }

        public static void SwitchPlatform(BuildTarget target)
        {
            EditorSceneManager.SaveOpenScenes();
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                throw new InvalidOperationException($"Unable to switch Unity to {target}.");
        }

        /// <summary>Resolves the profile from <c>MOBILE_BUILD_PROFILE</c> or the first profile asset.</summary>
        public static MobileBuildProfile ResolveProfile(MobileBuildProfile profile = null)
        {
            if (profile != null) return profile;
            string path = Environment.GetEnvironmentVariable(ProfileVariable);
            if (!string.IsNullOrEmpty(path)) return MobileBuildProfile.Load(path);
            profile = MobileBuildProfile.FindDefault();
            if (profile == null)
                throw new InvalidOperationException(
                    "No MobileBuildProfile found. Create one with Assets > Create > Laboon > Mobile Build Profile, " +
                    $"or set {ProfileVariable} to its asset path.");
            return profile;
        }

        public static bool IsReleaseFromEnvironment() => Environment.GetEnvironmentVariable(ReleaseVariable) == "1";

        public static BuildReport BuildAndroid(MobileBuildProfile profile, MobileBuildOptions options = null)
        {
            options = options ?? new MobileBuildOptions();
            profile = ResolveProfile(profile);
            bool release = options.Release ?? IsReleaseFromEnvironment();
            string output = options.OutputPath ?? Environment.GetEnvironmentVariable(OutputVariable) ?? profile.GetAndroidOutputPath(release);
            if (profile.BuildAppBundle && output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                output = Path.ChangeExtension(output, ".aab");

            var buildOptions = release ? BuildOptions.None : BuildOptions.Development;
            if (options.AutoRun) buildOptions |= BuildOptions.AutoRunPlayer;

            return Run(profile, options, BuildTarget.Android, output, buildOptions, () =>
            {
                if (profile.ValidateGradleTemplates) GradleTemplateValidator.ThrowIfInvalid();
                if (profile.ApplyPlayerSettings) ApplyAndroidSettings(profile);
                EditorUserBuildSettings.buildAppBundle = profile.BuildAppBundle;
            });
        }

        public static BuildReport ExportIOS(MobileBuildProfile profile, MobileBuildOptions options = null)
        {
            options = options ?? new MobileBuildOptions();
            profile = ResolveProfile(profile);
            bool release = options.Release ?? IsReleaseFromEnvironment();
            string output = options.OutputPath ?? Environment.GetEnvironmentVariable(OutputVariable) ?? profile.IosOutputPath;
            return Run(profile, options, BuildTarget.iOS, output, release ? BuildOptions.None : BuildOptions.Development, () =>
            {
                if (profile.ApplyPlayerSettings) ApplyIOSSettings(profile);
            });
        }

        public static void ApplyCommonSettings(MobileBuildProfile profile)
        {
            PlayerSettings.productName = profile.ProductName;
            if (!string.IsNullOrWhiteSpace(profile.CompanyName)) PlayerSettings.companyName = profile.CompanyName;
            PlayerSettings.defaultInterfaceOrientation = profile.Orientation;
        }

        public static void ApplyAndroidSettings(MobileBuildProfile profile)
        {
            ApplyCommonSettings(profile);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, profile.ApplicationId);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)profile.AndroidMinSdk;
            PlayerSettings.Android.targetSdkVersion = profile.AndroidTargetSdk <= 0
                ? AndroidSdkVersions.AndroidApiLevelAuto
                : (AndroidSdkVersions)profile.AndroidTargetSdk;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, profile.ScriptingBackend);
            PlayerSettings.Android.targetArchitectures = profile.AndroidArchitectures;
        }

        public static void ApplyIOSSettings(MobileBuildProfile profile)
        {
            ApplyCommonSettings(profile);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, profile.ApplicationId);
            PlayerSettings.iOS.targetOSVersionString = profile.IosMinimumVersion;
            if (!string.IsNullOrWhiteSpace(profile.IosTeamId)) PlayerSettings.iOS.appleDeveloperTeamID = profile.IosTeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = profile.IosAutomaticSigning;
        }

        private static BuildReport Run(MobileBuildProfile profile, MobileBuildOptions options, BuildTarget target,
            string output, BuildOptions buildOptions, Action prepare)
        {
            WriteStatus(options, "STARTED");
            PlayerSettingsSnapshot snapshot = profile.RestorePlayerSettings ? PlayerSettingsSnapshot.Capture() : null;
            ActiveProfile = profile;
            try
            {
                var errors = profile.Validate();
                if (errors.Count > 0)
                    throw new InvalidOperationException($"{profile.name} is invalid:\n- " + string.Join("\n- ", errors));
                if (EditorUserBuildSettings.activeBuildTarget != target)
                    throw new InvalidOperationException($"Switch Unity to {target} before building (MobileBuildPipeline.SwitchPlatform).");

                string[] scenes = profile.ResolveScenePaths();
                if (scenes.Length == 0) throw new InvalidOperationException("No scenes to build.");

                prepare();
                string directory = Path.GetDirectoryName(Path.GetFullPath(output));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                Debug.Log($"[MobileBuild] {target} {PlayerSettings.bundleVersion} -> {output}\n{string.Join("\n", scenes)}");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = target,
                    options = buildOptions
                });

                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors).");

                string full = Path.GetFullPath(output);
                WriteStatus(options, $"SUCCEEDED {full}");
                Debug.Log($"[MobileBuild] {target} succeeded: {full} ({report.summary.totalTime.TotalSeconds:F1}s)");
                if (options.Interactive)
                {
                    EditorUtility.RevealInFinder(full);
                    EditorUtility.DisplayDialog($"{profile.ProductName} build succeeded", full, "OK");
                }
                return report;
            }
            catch (Exception exception)
            {
                WriteStatus(options, $"FAILED {exception.Message}");
                if (options.Interactive)
                    EditorUtility.DisplayDialog($"{profile.ProductName} build failed", exception.Message, "OK");
                throw;
            }
            finally
            {
                ActiveProfile = null;
                snapshot?.Restore();
            }
        }

        private static void WriteStatus(MobileBuildOptions options, string status)
        {
            if (string.IsNullOrEmpty(options.StatusFilePath)) return;
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(options.StatusFilePath));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(options.StatusFilePath, status);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[MobileBuild] Could not write status file: {exception.Message}");
            }
        }
    }
}
