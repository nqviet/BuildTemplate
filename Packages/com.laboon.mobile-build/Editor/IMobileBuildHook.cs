using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Laboon.Mobile.Build
{
    /// <summary>
    /// Project-specific build step (Firebase, Gradle checks, SwiftPM resources). Implementations are found
    /// automatically; they need a public parameterless constructor.
    /// </summary>
    public interface IMobileBuildHook
    {
        /// <summary>Called after Unity generates the Gradle project. <paramref name="projectPath"/> is the unityLibrary folder.</summary>
        void OnAndroidProjectGenerated(string projectPath);

        /// <summary>Called after the Xcode project is exported and the package plist/postprocess steps ran.</summary>
        void OnIOSPostprocess(string xcodeProjectPath, MobileBuildProfile profile);
    }

    /// <summary>Sets hook execution order; lower runs first. Default 0.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MobileBuildHookOrderAttribute : Attribute
    {
        public MobileBuildHookOrderAttribute(int order) => Order = order;
        public int Order { get; }
    }

    /// <summary>Discovers and runs <see cref="IMobileBuildHook"/> implementations.</summary>
    public static class MobileBuildHooks
    {
        /// <summary>Order of the iOS hook dispatch; package plist postprocessing runs at 10000, before it.</summary>
        public const int IOSPostprocessOrder = 10050;

        public static IReadOnlyList<IMobileBuildHook> Discover()
        {
            return TypeCache.GetTypesDerivedFrom<IMobileBuildHook>()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => (t.GetCustomAttributes(typeof(MobileBuildHookOrderAttribute), false).FirstOrDefault()
                    as MobileBuildHookOrderAttribute)?.Order ?? 0)
                .ThenBy(t => t.FullName, StringComparer.Ordinal)
                .Select(t => (IMobileBuildHook)Activator.CreateInstance(t))
                .ToList();
        }

        public static void RunAndroid(string projectPath, IEnumerable<IMobileBuildHook> hooks = null)
        {
            foreach (var hook in hooks ?? Discover())
            {
                Debug.Log($"[MobileBuild] Android hook {hook.GetType().Name}");
                hook.OnAndroidProjectGenerated(projectPath);
            }
        }

        public static void RunIOS(string xcodeProjectPath, MobileBuildProfile profile, IEnumerable<IMobileBuildHook> hooks = null)
        {
            foreach (var hook in hooks ?? Discover())
            {
                Debug.Log($"[MobileBuild] iOS hook {hook.GetType().Name}");
                hook.OnIOSPostprocess(xcodeProjectPath, profile);
            }
        }

        [PostProcessBuild(IOSPostprocessOrder)]
        private static void OnPostprocessBuild(BuildTarget target, string output)
        {
            if (target == BuildTarget.iOS) RunIOS(output, MobileBuildProfile.FindDefault());
        }
    }

    internal sealed class MobileAndroidGradleHookDispatcher : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var profile = MobileBuildProfile.FindDefault();
            if (profile == null || profile.ValidateGradleTemplates) GradleTemplateValidator.ThrowIfInvalid();
            MobileBuildHooks.RunAndroid(path);
        }
    }
}
