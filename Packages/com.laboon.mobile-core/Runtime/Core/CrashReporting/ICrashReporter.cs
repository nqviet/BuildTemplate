using System;

namespace Laboon.Mobile.CrashReporting
{
    /// <summary>Crash-reporting backend contract. Implementations live in the project (for example a Firebase adapter).</summary>
    public interface ICrashReporter
    {
        /// <summary>Backend initialized and accepting reports.</summary>
        bool IsReady { get; }
        void Log(string message);
        void RecordException(Exception exception);
        void SetCustomKey(string key, string value);
        void SetUserId(string userId);
    }

    /// <summary>Default reporter: accepts every call and does nothing.</summary>
    public sealed class NullCrashReporter : ICrashReporter
    {
        public static readonly NullCrashReporter Instance = new NullCrashReporter();
        public bool IsReady => false;
        public void Log(string message) { }
        public void RecordException(Exception exception) { }
        public void SetCustomKey(string key, string value) { }
        public void SetUserId(string userId) { }
    }
}
