using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Laboon.Mobile.Haptics.Tests
{
    public sealed class HapticsTests
    {
        private const string k_Key = "Laboon_HapticsTests_Enabled";

        private HapticSettings m_Settings;
        private readonly List<HapticPulse> m_Played = new List<HapticPulse>();
        private int m_Stops;
        private int m_Prepares;
        private float m_Now;
        private float m_TimeScale;

        [SetUp]
        public void SetUp()
        {
            Haptics.ResetState();
            m_Settings = ScriptableObject.CreateInstance<HapticSettings>();
            m_Settings.PreferenceKey = k_Key;
            Haptics.Settings = m_Settings;
            m_Played.Clear();
            m_Stops = m_Prepares = 0;
            m_Now = 10f;
            m_TimeScale = 1f;
            Haptics.SetTestHooks(p => m_Played.Add(p), _ => m_Stops++, _ => m_Prepares++, () => m_Now, () => m_TimeScale);
            Haptics.SetEnabled(true);
            m_Stops = 0;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(k_Key);
            PlayerPrefs.Save();
            Haptics.ResetState();
            UnityEngine.Object.DestroyImmediate(m_Settings);
        }

        [Test]
        public void PlaysConfiguredPulse()
        {
            Assert.IsTrue(Haptics.Play(HapticCue.Complete));
            Assert.AreEqual(1, m_Played.Count);
            Assert.AreEqual(m_Settings.Complete.Style, m_Played[0].Style);
        }

        [Test]
        public void SharedBudgetDropsAccentsInsideMinimumInterval()
        {
            Assert.IsTrue(Haptics.Play(HapticCue.Launch));
            m_Now += m_Settings.MinimumInterval * .5f;
            Assert.IsFalse(Haptics.Play(HapticCue.Booster));
            m_Now += m_Settings.MinimumInterval;
            Assert.IsTrue(Haptics.Play(HapticCue.Booster));
        }

        [Test]
        public void ResultReplacesAccentButCannotStack()
        {
            Assert.IsTrue(Haptics.Play(HapticCue.Complete));
            Assert.IsTrue(Haptics.Play(HapticCue.Success), "result pulse has priority over the budget");
            Assert.IsFalse(Haptics.Play(HapticCue.Failure), "duplicate result callbacks cannot stack");
            m_Now += 1.01f;
            Assert.IsTrue(Haptics.Play(HapticCue.Failure));
        }

        [Test]
        public void TickLaneNeverSpendsSharedBudget()
        {
            m_Settings.MinimumInterval = .3f;
            Assert.IsTrue(Haptics.Play(HapticCue.Tick));
            Assert.IsTrue(Haptics.Play(HapticCue.Complete), "a tick cannot swallow the following accent");
            m_Now += .1f;
            Assert.IsFalse(Haptics.Play(HapticCue.Impact), "ticks wait for the shared budget");
            m_Now += .3f;
            Assert.IsTrue(Haptics.Play(HapticCue.Impact));
            m_Now += .05f;
            Assert.IsFalse(Haptics.Play(HapticCue.Tick), "tick lane interval");
        }

        [Test]
        public void DisabledPreferenceSilencesAndStops()
        {
            Haptics.SetEnabled(false);
            Assert.AreEqual(1, m_Stops);
            Assert.IsFalse(Haptics.Enabled);
            Assert.IsFalse(Haptics.Play(HapticCue.Preview));
            Haptics.Prepare(HapticCue.Launch);
            Assert.AreEqual(0, m_Played.Count);
            Assert.AreEqual(0, m_Prepares);
            Assert.AreEqual(0, PlayerPrefs.GetInt(k_Key, 1));
        }

        [Test]
        public void PausedGameOnlyAllowsPreview()
        {
            m_TimeScale = 0f;
            Assert.IsFalse(Haptics.Play(HapticCue.Launch));
            Assert.IsTrue(Haptics.Play(HapticCue.Preview));
        }

        [Test]
        public void ZeroIntensityIsSkipped()
        {
            m_Settings.Launch.Intensity = 0f;
            Assert.IsFalse(Haptics.Play(HapticCue.Launch));
            Haptics.Prepare(HapticCue.Launch);
            Assert.AreEqual(0, m_Prepares);
        }

        [Test]
        public void NativeFailureIsIsolatedAndPreservesPreference()
        {
            Haptics.SetTestHooks(_ => throw new EntryPointNotFoundException("LaboonHapticsPlay"), _ => { }, _ => { }, () => m_Now, () => m_TimeScale);
            Assert.DoesNotThrow(() => Haptics.Play(HapticCue.Launch));
            Assert.IsTrue(Haptics.BackendFailed);
            Assert.IsTrue(Haptics.Enabled, "the user's preference is not changed by a device failure");
            m_Now += 5f;
            Assert.IsFalse(Haptics.Play(HapticCue.Launch), "no repeated native calls after a failure");
        }

        [Test]
        public void UnrelatedExceptionsStillPropagate()
        {
            Haptics.SetTestHooks(_ => throw new InvalidOperationException(), _ => { }, _ => { }, () => m_Now, () => m_TimeScale);
            Assert.Throws<InvalidOperationException>(() => Haptics.Play(HapticCue.Launch));
        }

        [Test]
        public void StopResetsBudget()
        {
            Assert.IsTrue(Haptics.Play(HapticCue.Launch));
            Haptics.Stop();
            Assert.IsTrue(Haptics.Play(HapticCue.Launch));
        }

        [Test]
        public void CueValuesMatchLegacyEventOrder()
        {
            // Preview, Launch, BasketComplete, Booster, Win, Fail, TargetLand, MarbleHit
            Assert.AreEqual(2, (int)HapticCue.Complete);
            Assert.AreEqual(4, (int)HapticCue.Success);
            Assert.AreEqual(5, (int)HapticCue.Failure);
            Assert.AreEqual(6, (int)HapticCue.Tick);
            Assert.AreEqual(7, (int)HapticCue.Impact);
        }

        [Test]
        public void DefaultSettingsExistWithoutAnAsset()
        {
            Haptics.Settings = null;
            Assert.IsNotNull(Haptics.Settings);
        }
    }
}
