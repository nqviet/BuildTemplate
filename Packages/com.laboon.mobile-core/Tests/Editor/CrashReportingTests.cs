using System;
using Laboon.Mobile.CrashReporting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Laboon.Mobile.Tests
{
    public sealed class CrashReportingTests
    {
        [TearDown]
        public void TearDown() => CrashReports.Register(null);

        [Test]
        public void DefaultsToNullReporter()
        {
            CrashReports.Register(null);
            Assert.AreSame(NullCrashReporter.Instance, CrashReports.Reporter);
            Assert.DoesNotThrow(() => CrashReports.RecordException(new Exception("x")));
        }

        [Test]
        public void ForwardsToRegisteredReporter()
        {
            var reporter = new RecordingReporter();
            CrashReports.Register(reporter);
            CrashReports.Log("hello");
            CrashReports.SetCustomKey("level", "3");
            Assert.AreEqual("hello|level=3", reporter.Calls);
        }

        [Test]
        public void FailingReporterIsReplaced()
        {
            CrashReports.Register(new ThrowingReporter());
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Crash reporter failed"));
            Assert.DoesNotThrow(() => CrashReports.Log("x"));
            Assert.AreSame(NullCrashReporter.Instance, CrashReports.Reporter);
        }

        private sealed class RecordingReporter : ICrashReporter
        {
            public string Calls = "";
            public bool IsReady => true;
            public void Log(string message) => Calls += message;
            public void RecordException(Exception exception) { }
            public void SetCustomKey(string key, string value) => Calls += $"|{key}={value}";
            public void SetUserId(string userId) { }
        }

        private sealed class ThrowingReporter : ICrashReporter
        {
            public bool IsReady => true;
            public void Log(string message) => throw new InvalidOperationException();
            public void RecordException(Exception exception) => throw new InvalidOperationException();
            public void SetCustomKey(string key, string value) => throw new InvalidOperationException();
            public void SetUserId(string userId) => throw new InvalidOperationException();
        }
    }
}
