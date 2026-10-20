// Requires the Firebase Unity SDK (Firebase.App, Firebase.Crashlytics) in the project.
// Put this file in an asmdef that references Laboon.Mobile.Core and the Firebase assemblies.
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Crashlytics;
using Firebase.Extensions;
using Laboon.Mobile.CrashReporting;
using UnityEngine;

namespace Laboon.Mobile.Samples.Firebase
{
    /// <summary>Crashlytics adapter. Registers itself with <see cref="CrashReports"/> once dependencies check out.</summary>
    public sealed class FirebaseCrashReporter : ICrashReporter
    {
        private static Task<DependencyStatus> s_DependencyCheck;

        public bool IsReady { get; private set; }

        public static Task<DependencyStatus> CheckDependenciesAsync() =>
            s_DependencyCheck ??= FirebaseApp.CheckAndFixDependenciesAsync();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Keep the platform guard: Firebase native backends exist only in Android and iOS players.
            if (!CrashReports.IsMobilePlayer) return;

            CheckDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogWarning("Firebase Crashlytics skipped: Firebase dependencies are unavailable.");
                    return;
                }
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning($"Firebase Crashlytics skipped: {task.Result}.");
                    return;
                }
                try
                {
                    if (string.IsNullOrWhiteSpace(FirebaseApp.DefaultInstance.Options.AppId))
                    {
                        Debug.LogWarning("Firebase Crashlytics is not configured. Add the Firebase config files before building.");
                        return;
                    }
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    CrashReports.Register(new FirebaseCrashReporter { IsReady = true });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Firebase Crashlytics initialization failed ({exception.GetType().Name}).");
                }
            });
        }

        public void Log(string message) => Crashlytics.Log(message);
        public void RecordException(Exception exception) => Crashlytics.LogException(exception);
        public void SetCustomKey(string key, string value) => Crashlytics.SetCustomKey(key, value);
        public void SetUserId(string userId) => Crashlytics.SetUserId(userId);
    }
}
