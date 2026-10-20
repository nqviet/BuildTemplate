using System;
using UnityEngine;

namespace Laboon.Mobile.CrashReporting
{
    /// <summary>Process-wide crash reporter. Calls never throw; a failing backend is replaced by <see cref="NullCrashReporter"/>.</summary>
    public static class CrashReports
    {
        private static ICrashReporter s_Reporter = NullCrashReporter.Instance;

        public static ICrashReporter Reporter => s_Reporter;

        /// <summary>Raised after <see cref="Register"/> replaces the active reporter.</summary>
        public static event Action<ICrashReporter> OnReporterChanged;

        /// <summary>True in Android and iOS players, where native crash backends normally run.</summary>
        public static bool IsMobilePlayer =>
            !Application.isEditor &&
            (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);

        public static void Register(ICrashReporter reporter)
        {
            s_Reporter = reporter ?? NullCrashReporter.Instance;
            OnReporterChanged?.Invoke(s_Reporter);
        }

        public static void Log(string message) => Safe(r => r.Log(message));
        public static void RecordException(Exception exception) => Safe(r => r.RecordException(exception));
        public static void SetCustomKey(string key, string value) => Safe(r => r.SetCustomKey(key, value));
        public static void SetUserId(string userId) => Safe(r => r.SetUserId(userId));

        private static void Safe(Action<ICrashReporter> call)
        {
            try { call(s_Reporter); }
            catch (Exception exception)
            {
                Debug.LogWarning($"Crash reporter failed ({exception.GetType().Name}); reporting disabled for this run.");
                s_Reporter = NullCrashReporter.Instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Reporter = NullCrashReporter.Instance;
            OnReporterChanged = null;
        }
    }
}
