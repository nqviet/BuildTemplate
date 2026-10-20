using System;

namespace Laboon.Mobile.Ads
{
    /// <summary>Rewarded-ad backend. A real SDK adapter (for example PlayWill) lives in the project and implements this.</summary>
    public interface IRewardedAdProvider
    {
        /// <summary>An ad can be shown now.</summary>
        bool IsReady { get; }

        /// <summary>Shows an ad. <paramref name="completed"/> is called exactly once, on the main thread.</summary>
        void Show(Action<RewardedAdResult> completed);
    }

    public enum RewardedAdStatus
    {
        /// <summary>The ad was watched to the reward point.</summary>
        Completed,
        /// <summary>The player closed the ad early.</summary>
        Canceled,
        /// <summary>The ad failed to load or play.</summary>
        Failed,
        /// <summary>No provider is installed or it is not ready.</summary>
        Unavailable
    }

    /// <summary>Outcome of a rewarded ad. The game decides what to grant; the package never grants rewards.</summary>
    public readonly struct RewardedAdResult
    {
        public readonly RewardedAdStatus Status;
        /// <summary>Watched duration reported by the provider, in seconds.</summary>
        public readonly float WatchedSeconds;
        public readonly string Message;

        public RewardedAdResult(RewardedAdStatus status, float watchedSeconds = 0f, string message = null)
        {
            Status = status;
            WatchedSeconds = watchedSeconds;
            Message = message;
        }

        public bool IsCompleted => Status == RewardedAdStatus.Completed;

        public static RewardedAdResult Completed(float watchedSeconds) => new RewardedAdResult(RewardedAdStatus.Completed, watchedSeconds);
        public static RewardedAdResult Canceled(float watchedSeconds = 0f) => new RewardedAdResult(RewardedAdStatus.Canceled, watchedSeconds);
        public static RewardedAdResult Failed(string message = null) => new RewardedAdResult(RewardedAdStatus.Failed, 0f, message);
        public static RewardedAdResult Unavailable() => new RewardedAdResult(RewardedAdStatus.Unavailable);

        public override string ToString() => $"{Status} ({WatchedSeconds:0.##}s){(Message != null ? " " + Message : "")}";
    }
}
