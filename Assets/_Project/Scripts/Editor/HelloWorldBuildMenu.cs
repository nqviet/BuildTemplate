using Laboon.Mobile.Build;
using UnityEditor;

namespace HelloWorld.Editor
{
    /// <summary>Project-owned menu wrappers; the package owns the pipeline.</summary>
    public static class HelloWorldBuildMenu
    {
        private const string k_ProfilePath = "Assets/_Project/Config/MobileBuildProfile.asset";

        [MenuItem("Hello World/Build Android APK", priority = 100)]
        public static void BuildAndroidApk() => MobileBuildMenu.BuildAndroid(MobileBuildProfile.Load(k_ProfilePath), autoRun: false);

        [MenuItem("Hello World/Build and Run Android", priority = 101)]
        public static void BuildAndroidAndRun() => MobileBuildMenu.BuildAndroid(MobileBuildProfile.Load(k_ProfilePath), autoRun: true);

        [MenuItem("Hello World/Export iOS Xcode Project", priority = 102)]
        public static void ExportIOS() => MobileBuildMenu.ExportIOS(MobileBuildProfile.Load(k_ProfilePath));
    }
}
