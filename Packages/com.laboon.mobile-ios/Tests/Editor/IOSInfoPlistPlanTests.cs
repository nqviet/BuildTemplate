using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Laboon.Mobile.IOS.Tests
{
    public sealed class IOSInfoPlistPlanTests
    {
        private IOSSceneLifecycleConfig m_Config;

        [SetUp]
        public void SetUp() => m_Config = ScriptableObject.CreateInstance<IOSSceneLifecycleConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Config);

        private static DictionaryPlist SceneConfiguration(DictionaryPlist root)
        {
            var manifest = (DictionaryPlist)root.Values[MobileSceneLifecycleKeys.SceneManifestKey];
            var configurations = (DictionaryPlist)manifest.Values[MobileSceneLifecycleKeys.SceneConfigurationsKey];
            var role = (List<object>)configurations.Values[MobileSceneLifecycleKeys.ApplicationRoleKey];
            return (DictionaryPlist)role[0];
        }

        [Test]
        public void WritesManifestMarkerAndEncryption()
        {
            var root = new DictionaryPlist();
            IOSInfoPlistPlan.Apply(root, m_Config, "Sample Game");

            Assert.AreEqual(true, root.Values[MobileSceneLifecycleKeys.MarkerKey]);
            Assert.AreEqual(false, root.Values[MobileSceneLifecycleKeys.EncryptionKey]);
            var manifest = (DictionaryPlist)root.Values[MobileSceneLifecycleKeys.SceneManifestKey];
            Assert.AreEqual(false, manifest.Values[MobileSceneLifecycleKeys.SupportsMultipleScenesKey]);
            var configuration = SceneConfiguration(root);
            Assert.AreEqual("Sample Game", configuration.Values[MobileSceneLifecycleKeys.ConfigurationNameKey]);
            Assert.AreEqual("UIWindowScene", configuration.Values[MobileSceneLifecycleKeys.SceneClassNameKey]);
            Assert.AreEqual("MobileSceneDelegate", configuration.Values[MobileSceneLifecycleKeys.DelegateClassNameKey]);
        }

        [Test]
        public void ConfiguredNamesWin()
        {
            m_Config.SceneConfigurationName = "Main";
            m_Config.DelegateClassName = "LegacySceneDelegate";
            var root = new DictionaryPlist();
            IOSInfoPlistPlan.Apply(root, m_Config, "Ignored");
            var configuration = SceneConfiguration(root);
            Assert.AreEqual("Main", configuration.Values[MobileSceneLifecycleKeys.ConfigurationNameKey]);
            Assert.AreEqual("LegacySceneDelegate", configuration.Values[MobileSceneLifecycleKeys.DelegateClassNameKey]);
        }

        [Test]
        public void ExistingManifestIsKeptWithoutMarker()
        {
            var root = new DictionaryPlist();
            var existing = root.CreateDict(MobileSceneLifecycleKeys.SceneManifestKey);
            IOSInfoPlistPlan.Apply(root, m_Config, "Game");
            Assert.AreSame(existing, root.Values[MobileSceneLifecycleKeys.SceneManifestKey]);
            Assert.IsFalse(root.Contains(MobileSceneLifecycleKeys.MarkerKey), "native code must fall back to Unity's own scene path");
        }

        [Test]
        public void OverwriteReplacesExistingManifest()
        {
            m_Config.OverwriteExistingSceneManifest = true;
            var root = new DictionaryPlist();
            var existing = root.CreateDict(MobileSceneLifecycleKeys.SceneManifestKey);
            IOSInfoPlistPlan.Apply(root, m_Config, "Game");
            Assert.AreNotSame(existing, root.Values[MobileSceneLifecycleKeys.SceneManifestKey]);
            Assert.AreEqual(true, root.Values[MobileSceneLifecycleKeys.MarkerKey]);
        }

        [Test]
        public void ExistingEncryptionDeclarationIsKept()
        {
            var root = new DictionaryPlist();
            root.SetBool(MobileSceneLifecycleKeys.EncryptionKey, true);
            IOSInfoPlistPlan.Apply(root, m_Config, "Game");
            Assert.AreEqual(true, root.Values[MobileSceneLifecycleKeys.EncryptionKey]);
        }

        [Test]
        public void DisabledConfigOnlyWritesEncryption()
        {
            m_Config.Enabled = false;
            var root = new DictionaryPlist();
            IOSInfoPlistPlan.Apply(root, m_Config, "Game");
            Assert.IsFalse(root.Contains(MobileSceneLifecycleKeys.SceneManifestKey));
            Assert.IsFalse(root.Contains(MobileSceneLifecycleKeys.MarkerKey));
            Assert.IsTrue(root.Contains(MobileSceneLifecycleKeys.EncryptionKey));
        }

        [Test]
        public void EmptyNamesFallBack()
        {
            m_Config.DelegateClassName = " ";
            Assert.AreEqual(MobileSceneLifecycleKeys.DefaultDelegateClassName, m_Config.ResolveDelegateClassName());
            Assert.AreEqual("Default Configuration", m_Config.ResolveConfigurationName(""));
        }
    }
}
