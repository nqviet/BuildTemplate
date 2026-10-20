using System;
using UnityEngine;

namespace Laboon.Mobile.Ads
{
    /// <summary>
    /// Review-build stand-in for a real rewarded ad: "plays" for <see cref="AdSeconds"/> of unscaled time, then
    /// reports the configured outcome. Lets the whole reward flow be reviewed without an SDK.
    /// </summary>
    public sealed class SimulatedRewardedAdProvider : IRewardedAdProvider, IDisposable
    {
        public const float DefaultAdSeconds = 1.5f;
        public const float DefaultReportedSeconds = 30f;

        private Action<RewardedAdResult> m_Completed;
        private float m_Deadline;
        private SimulatedAdRunner m_Runner;

        public SimulatedRewardedAdProvider(float adSeconds = DefaultAdSeconds, float reportedSeconds = DefaultReportedSeconds)
        {
            AdSeconds = Mathf.Max(0f, adSeconds);
            ReportedSeconds = Mathf.Max(0f, reportedSeconds);
        }

        /// <summary>Real time the test ad plays before it reports.</summary>
        public float AdSeconds { get; set; }

        /// <summary>Watched duration reported on completion.</summary>
        public float ReportedSeconds { get; set; }

        /// <summary>Outcome reported when the timer elapses (QA can force cancel/failure paths).</summary>
        public RewardedAdStatus Outcome { get; set; } = RewardedAdStatus.Completed;

        public bool IsReady => true;

        /// <summary>A test ad is playing.</summary>
        public bool IsPlaying => m_Completed != null;

        public void Show(Action<RewardedAdResult> completed)
        {
            // A new request replaces an unfinished one; the old caller is told it was canceled.
            Finish(RewardedAdResult.Canceled());
            m_Completed = completed ?? (_ => { });
            m_Deadline = Time.realtimeSinceStartup + AdSeconds;
            if (Application.isPlaying) EnsureRunner();
        }

        /// <summary>Ends the playing test ad now with <see cref="Outcome"/>. No effect when none is playing.</summary>
        public void Complete() => Finish(BuildOutcome());

        /// <summary>Ends the playing test ad as canceled.</summary>
        public void Cancel() => Finish(RewardedAdResult.Canceled());

        /// <summary>Called by the runner each frame; completes once the unscaled deadline passes.</summary>
        internal void Tick(float realtime)
        {
            if (m_Completed != null && realtime >= m_Deadline) Complete();
        }

        public void Dispose()
        {
            m_Completed = null;
            if (m_Runner != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(m_Runner.gameObject);
                else UnityEngine.Object.DestroyImmediate(m_Runner.gameObject);
            }
            m_Runner = null;
        }

        private RewardedAdResult BuildOutcome()
        {
            switch (Outcome)
            {
                case RewardedAdStatus.Completed: return RewardedAdResult.Completed(ReportedSeconds);
                case RewardedAdStatus.Canceled: return RewardedAdResult.Canceled();
                case RewardedAdStatus.Failed: return RewardedAdResult.Failed("Simulated failure");
                default: return RewardedAdResult.Unavailable();
            }
        }

        private void Finish(RewardedAdResult result)
        {
            var completed = m_Completed;
            m_Completed = null;
            completed?.Invoke(result);
        }

        private void EnsureRunner()
        {
            if (m_Runner != null) return;
            var go = new GameObject("[SimulatedRewardedAd]") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            m_Runner = go.AddComponent<SimulatedAdRunner>();
            m_Runner.Provider = this;
        }
    }

    /// <summary>Drives the simulated ad timer on unscaled time; the game may be paused under the ad.</summary>
    [AddComponentMenu("")]
    internal sealed class SimulatedAdRunner : MonoBehaviour
    {
        public SimulatedRewardedAdProvider Provider;

        private void Update() => Provider?.Tick(Time.realtimeSinceStartup);
    }
}
