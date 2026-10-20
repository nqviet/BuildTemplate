using System;
using System.Linq;
using UnityEditor;

namespace Laboon.Mobile.Build
{
    /// <summary>App version parsing and Player Settings writes.</summary>
    public static class MobileBuildVersion
    {
        /// <summary>Semantic app version: exactly three numeric parts, such as 1.2.0.</summary>
        public static bool IsValidVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string[] parts = value.Split('.');
            return parts.Length == 3 && parts.All(IsDigits);
        }

        /// <summary>Android versionCode / iOS build number: a positive integer.</summary>
        public static bool IsValidVersionCode(int value) => value > 0;

        /// <summary>OS version such as 15.0 or 15.0.1.</summary>
        public static bool IsValidOsVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string[] parts = value.Split('.');
            return parts.Length >= 2 && parts.Length <= 3 && parts.All(IsDigits);
        }

        public static bool TryParse(string value, out Version version)
        {
            version = null;
            return IsValidVersion(value) && Version.TryParse(value, out version);
        }

        /// <summary>True when <paramref name="next"/> is greater than or equal to <paramref name="current"/>.</summary>
        public static bool IsNotDowngrade(string current, string next)
        {
            if (!TryParse(next, out var n)) return false;
            return !TryParse(current, out var c) || n >= c;
        }

        /// <summary>Writes the version to Player Settings. The code is used for Android versionCode and the iOS build number.</summary>
        public static void Save(string version, int versionCode)
        {
            if (!IsValidVersion(version) || !IsValidVersionCode(versionCode))
                throw new ArgumentException("Invalid app version or version code.");
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.iOS.buildNumber = versionCode.ToString();
            AssetDatabase.SaveAssets();
        }

        private static bool IsDigits(string part) => part.Length > 0 && part.All(c => c >= '0' && c <= '9');
    }
}
