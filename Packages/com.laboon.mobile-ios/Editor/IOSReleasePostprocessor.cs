#if UNITY_IOS
using System.IO;
using Laboon.Mobile.Build;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Laboon.Mobile.IOS
{
    /// <summary>
    /// Generic iOS release postprocess: scene manifest + marker and the export-compliance flag.
    /// Project-specific work (SDK resources, Firebase) belongs in an <see cref="IMobileBuildHook"/>, which runs after this.
    /// </summary>
    public static class IOSReleasePostprocessor
    {
        public const int Order = 10000;

        [PostProcessBuild(Order)]
        public static void OnPostprocessBuild(BuildTarget target, string output)
        {
            if (target != BuildTarget.iOS) return;
            var profile = MobileBuildProfile.FindDefault();
            string productName = profile != null ? profile.ProductName : PlayerSettings.productName;
            ConfigureInfoPlist(output, IOSSceneLifecycleConfig.FindOrDefault(), productName);
        }

        public static void ConfigureInfoPlist(string xcodeProjectPath, IOSSceneLifecycleConfig config, string productName)
        {
            string plistPath = Path.Combine(xcodeProjectPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var changes = IOSInfoPlistPlan.Apply(new PlistDocumentDict(plist.root), config, productName);
            plist.WriteToFile(plistPath);
            Debug.Log("[MobileIOS] Info.plist: " + string.Join("; ", changes));
        }

        private sealed class PlistDocumentDict : IPlistDict
        {
            private readonly PlistElementDict m_Dict;
            public PlistDocumentDict(PlistElementDict dict) => m_Dict = dict;
            public bool Contains(string key) => m_Dict.values.ContainsKey(key);
            public void SetBool(string key, bool value) => m_Dict.SetBoolean(key, value);
            public void SetString(string key, string value) => m_Dict.SetString(key, value);
            public IPlistDict CreateDict(string key) => new PlistDocumentDict(m_Dict.CreateDict(key));
            public IPlistDict CreateArrayWithDict(string key) => new PlistDocumentDict(m_Dict.CreateArray(key).AddDict());
        }
    }
}
#endif
