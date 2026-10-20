using System;
using Laboon.Mobile.Ads;
using NUnit.Framework;

namespace Laboon.Mobile.Tests
{
    public sealed class RewardedAdsTests
    {
        [SetUp]
        public void SetUp() => RewardedAds.Uninstall();

        [TearDown]
        public void TearDown() => RewardedAds.Uninstall();

        [Test]
        public void NoProviderReportsUnavailable()
        {
            RewardedAdResult? result = null;
            RewardedAds.Show(r => result = r);
            Assert.IsFalse(RewardedAds.IsAvailable);
            Assert.AreEqual(RewardedAdStatus.Unavailable, result?.Status);
        }

        [Test]
        public void SimulationCompletesWithReportedDuration()
        {
            var simulation = RewardedAds.InstallSimulation();
            Assert.IsTrue(RewardedAds.IsSimulated && RewardedAds.IsAvailable);
            Assert.AreSame(simulation, RewardedAds.InstallSimulation(), "install is idempotent");

            RewardedAdResult? result = null;
            RewardedAds.Show(r => result = r);
            Assert.IsTrue(simulation.IsPlaying);
            Assert.IsNull(result);

            simulation.Tick(float.MaxValue);
            Assert.IsTrue(result.Value.IsCompleted);
            Assert.AreEqual(simulation.ReportedSeconds, result.Value.WatchedSeconds);
            Assert.IsFalse(simulation.IsPlaying);
        }

        [Test]
        public void SimulationDoesNotCompleteBeforeDeadline()
        {
            var simulation = new SimulatedRewardedAdProvider(adSeconds: 100f);
            RewardedAdResult? result = null;
            simulation.Show(r => result = r);
            simulation.Tick(0f);
            Assert.IsNull(result);
            simulation.Dispose();
        }

        [TestCase(RewardedAdStatus.Canceled)]
        [TestCase(RewardedAdStatus.Failed)]
        public void SimulationCanForceOutcome(RewardedAdStatus outcome)
        {
            var simulation = new SimulatedRewardedAdProvider { Outcome = outcome };
            RewardedAdResult? result = null;
            simulation.Show(r => result = r);
            simulation.Complete();
            Assert.AreEqual(outcome, result?.Status);
        }

        [Test]
        public void NewRequestCancelsPendingOne()
        {
            var simulation = new SimulatedRewardedAdProvider();
            RewardedAdResult? first = null;
            simulation.Show(r => first = r);
            simulation.Show(_ => { });
            Assert.AreEqual(RewardedAdStatus.Canceled, first?.Status);
        }

        [Test]
        public void ThrowingProviderReportsFailureOnce()
        {
            RewardedAds.Install(new ThrowingProvider());
            int calls = 0;
            RewardedAdResult? result = null;
            RewardedAds.Show(r => { calls++; result = r; });
            Assert.AreEqual(1, calls);
            Assert.AreEqual(RewardedAdStatus.Failed, result?.Status);
        }

        [Test]
        public void DuplicateProviderCallbacksAreIgnored()
        {
            RewardedAds.Install(new DoubleReportProvider());
            int calls = 0;
            RewardedAds.Show(_ => calls++);
            Assert.AreEqual(1, calls);
        }

        private sealed class ThrowingProvider : IRewardedAdProvider
        {
            public bool IsReady => true;
            public void Show(Action<RewardedAdResult> completed) => throw new InvalidOperationException("sdk");
        }

        private sealed class DoubleReportProvider : IRewardedAdProvider
        {
            public bool IsReady => true;
            public void Show(Action<RewardedAdResult> completed)
            {
                completed(RewardedAdResult.Completed(30));
                completed(RewardedAdResult.Completed(30));
            }
        }
    }
}
