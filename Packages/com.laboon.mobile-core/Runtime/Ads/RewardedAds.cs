using System;
using UnityEngine;

namespace Laboon.Mobile.Ads
{
    /// <summary>Process-wide rewarded-ad provider registry.</summary>
    public static class RewardedAds
    {
        private static IRewardedAdProvider s_Provider;

        public static IRewardedAdProvider Provider => s_Provider;

        /// <summary>A provider is installed and ready.</summary>
        public static bool IsAvailable => s_Provider != null && s_Provider.IsReady;

        /// <summary>The installed provider is the simulation.</summary>
        public static bool IsSimulated => s_Provider is SimulatedRewardedAdProvider;

        /// <summary>Raised when the provider is installed or removed.</summary>
        public static event Action<IRewardedAdProvider> OnProviderChanged;

        public static void Install(IRewardedAdProvider provider)
        {
            if (ReferenceEquals(s_Provider, provider)) return;
            if (s_Provider is IDisposable disposable) disposable.Dispose();
            s_Provider = provider;
            OnProviderChanged?.Invoke(s_Provider);
        }

        public static void Uninstall() => Install(null);

        /// <summary>Installs the simulated provider unless a provider is already installed. Returns the active simulation, or null.</summary>
        public static SimulatedRewardedAdProvider InstallSimulation()
        {
            if (s_Provider == null)
            {
                var settings = MobileKitSettings.Instance;
                Install(settings != null
                    ? new SimulatedRewardedAdProvider(settings.SimulatedAdSeconds, settings.SimulatedReportedSeconds)
                    : new SimulatedRewardedAdProvider());
            }
            return s_Provider as SimulatedRewardedAdProvider;
        }

        /// <summary>Shows an ad through the provider; reports <see cref="RewardedAdStatus.Unavailable"/> when there is none.</summary>
        public static void Show(Action<RewardedAdResult> completed)
        {
            if (!IsAvailable)
            {
                completed?.Invoke(RewardedAdResult.Unavailable());
                return;
            }
            bool reported = false;
            try
            {
                s_Provider.Show(result =>
                {
                    if (reported) return;
                    reported = true;
                    completed?.Invoke(result);
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded ad provider failed ({exception.GetType().Name}).");
                if (!reported)
                {
                    reported = true;
                    completed?.Invoke(RewardedAdResult.Failed(exception.Message));
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (s_Provider is IDisposable disposable) disposable.Dispose();
            s_Provider = null;
            OnProviderChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInstall()
        {
            var settings = MobileKitSettings.Instance;
            if (settings != null && settings.RewardedAdMode == RewardedAdMode.Simulated) InstallSimulation();
        }
    }
}
