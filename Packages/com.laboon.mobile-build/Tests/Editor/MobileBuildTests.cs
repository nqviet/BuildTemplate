using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Laboon.Mobile.Build.Tests
{
    public sealed class MobileBuildTests
    {
        private MobileBuildProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Profile = ScriptableObject.CreateInstance<MobileBuildProfile>();
            m_Profile.ApplicationId = "kts.laboon.sample";
            m_Profile.ProductName = "Sample";
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        [TestCase("1.0.0", true)]
        [TestCase("10.20.300", true)]
        [TestCase("1.0", false)]
        [TestCase("1.0.0.0", false)]
        [TestCase("1.a.0", false)]
        [TestCase("1..0", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void VersionValidation(string version, bool valid) => Assert.AreEqual(valid, MobileBuildVersion.IsValidVersion(version));

        [Test]
        public void VersionCodeMustBePositive()
        {
            Assert.IsFalse(MobileBuildVersion.IsValidVersionCode(0));
            Assert.IsFalse(MobileBuildVersion.IsValidVersionCode(-3));
            Assert.IsTrue(MobileBuildVersion.IsValidVersionCode(1));
        }

        [Test]
        public void DowngradeDetection()
        {
            Assert.IsTrue(MobileBuildVersion.IsNotDowngrade("1.2.0", "1.10.0"), "numeric, not lexical");
            Assert.IsFalse(MobileBuildVersion.IsNotDowngrade("1.2.0", "1.1.9"));
            Assert.IsTrue(MobileBuildVersion.IsNotDowngrade("garbage", "1.0.0"));
        }

        [TestCase("15.0", true)]
        [TestCase("15.0.1", true)]
        [TestCase("15", false)]
        [TestCase("v15.0", false)]
        public void OsVersionValidation(string version, bool valid) => Assert.AreEqual(valid, MobileBuildVersion.IsValidOsVersion(version));

        [TestCase("kts.laboon.pigbeam", true)]
        [TestCase("com.company.game_2", true)]
        [TestCase("game", false)]
        [TestCase("com..game", false)]
        [TestCase("com.1game", false)]
        [TestCase("com.game!", false)]
        public void ApplicationIdValidation(string id, bool valid) => Assert.AreEqual(valid, MobileBuildProfile.IsValidApplicationId(id));

        [Test]
        public void DefaultProfileWithIdIsValid() => CollectionAssert.IsEmpty(m_Profile.Validate());

        [Test]
        public void ProfileReportsEveryProblem()
        {
            m_Profile.ApplicationId = "bad";
            m_Profile.ProductName = " ";
            m_Profile.AndroidOutputPath = "";
            m_Profile.AndroidArchitectures = AndroidArchitecture.None;
            m_Profile.AndroidMinSdk = 25;
            m_Profile.AndroidTargetSdk = 23;
            m_Profile.IosMinimumVersion = "fifteen";
            m_Profile.UseEditorBuildSettingsScenes = false;
            Assert.AreEqual(7, m_Profile.Validate().Count, string.Join("\n", m_Profile.Validate()));
        }

        [Test]
        public void ReleaseOutputFallsBackToDevelopmentOutput()
        {
            m_Profile.AndroidOutputPath = "Builds/Android/Game.apk";
            Assert.AreEqual("Builds/Android/Game.apk", m_Profile.GetAndroidOutputPath(true));
            m_Profile.AndroidReleaseOutputPath = "Builds/Android/Game-release.apk";
            Assert.AreEqual("Builds/Android/Game-release.apk", m_Profile.GetAndroidOutputPath(true));
            Assert.AreEqual("Builds/Android/Game.apk", m_Profile.GetAndroidOutputPath(false));
        }

        [Test]
        public void GradleValidationNamesMissingTokens()
        {
            var errors = GradleTemplateValidator.ValidateText("mainTemplate.gradle", "**APPLY_PLUGINS**\n**DEPS**",
                GradleTemplateValidator.RequiredMainTokens);
            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors.Any(e => e.Contains("**VERSIONCODE**")));
            CollectionAssert.IsEmpty(GradleTemplateValidator.ValidateText("x",
                string.Join("\n", GradleTemplateValidator.RequiredMainTokens), GradleTemplateValidator.RequiredMainTokens));
        }

        [Test]
        public void GradleValidationIgnoresMissingTemplatesAndNeverWrites()
        {
            string root = Path.Combine(Path.GetTempPath(), "laboon-gradle-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                CollectionAssert.IsEmpty(GradleTemplateValidator.Validate(root));

                string settings = Path.Combine(root, GradleTemplateValidator.SettingsTemplatePath);
                Directory.CreateDirectory(Path.GetDirectoryName(settings));
                File.WriteAllText(settings, "include ':unityLibrary'");
                Assert.AreEqual(3, GradleTemplateValidator.Validate(root).Count);
                Assert.AreEqual("include ':unityLibrary'", File.ReadAllText(settings));
                Assert.Throws<System.InvalidOperationException>(() => GradleTemplateValidator.ThrowIfInvalid(root));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void SnapshotRestoresPlayerSettings()
        {
            var snapshot = PlayerSettingsSnapshot.Capture();
            string product = PlayerSettings.productName;
            string androidId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            var architectures = PlayerSettings.Android.targetArchitectures;
            try
            {
                m_Profile.ProductName = "Snapshot Test Product";
                m_Profile.ApplicationId = "com.laboon.snapshottest";
                MobileBuildPipeline.ApplyAndroidSettings(m_Profile);
                Assert.AreEqual("Snapshot Test Product", PlayerSettings.productName);
                Assert.AreEqual("com.laboon.snapshottest", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            }
            finally
            {
                snapshot.Restore();
            }
            Assert.AreEqual(product, PlayerSettings.productName);
            Assert.AreEqual(androidId, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual(architectures, PlayerSettings.Android.targetArchitectures);
        }

        [Test]
        public void HooksAreDiscoveredInOrder()
        {
            var hooks = MobileBuildHooks.Discover();
            int first = hooks.ToList().FindIndex(h => h is FirstTestHook);
            int second = hooks.ToList().FindIndex(h => h is SecondTestHook);
            Assert.GreaterOrEqual(first, 0);
            Assert.Greater(second, first);
        }

        [Test]
        public void HooksReceiveProfile()
        {
            var hook = new FirstTestHook();
            MobileBuildHooks.RunIOS("Builds/iOS/X", m_Profile, new IMobileBuildHook[] { hook });
            MobileBuildHooks.RunAndroid("Temp/gradle", new IMobileBuildHook[] { hook });
            Assert.AreSame(m_Profile, hook.Profile);
            Assert.AreEqual("Temp/gradle", hook.AndroidPath);
        }

        [MobileBuildHookOrder(-100)]
        private sealed class FirstTestHook : IMobileBuildHook
        {
            public MobileBuildProfile Profile;
            public string AndroidPath;
            public void OnAndroidProjectGenerated(string projectPath) => AndroidPath = projectPath;
            public void OnIOSPostprocess(string xcodeProjectPath, MobileBuildProfile profile) => Profile = profile;
        }

        [MobileBuildHookOrder(100)]
        private sealed class SecondTestHook : IMobileBuildHook
        {
            public void OnAndroidProjectGenerated(string projectPath) { }
            public void OnIOSPostprocess(string xcodeProjectPath, MobileBuildProfile profile) { }
        }
    }
}
