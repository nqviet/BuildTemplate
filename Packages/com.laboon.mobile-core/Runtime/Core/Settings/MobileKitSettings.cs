using UnityEngine;

namespace Laboon.Mobile
{
    public enum RewardedAdMode
    {
        /// <summary>No provider is installed automatically.</summary>
        None,
        /// <summary>Install the simulated provider on startup (review builds, editor).</summary>
        Simulated,
        /// <summary>The project installs a real SDK adapter itself.</summary>
        External
    }

    /// <summary>
    /// Runtime settings for the mobile kit. Place at <c>Resources/MobileKitSettings.asset</c>.
    /// Every field is optional; missing settings fall back to package defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "Laboon/Mobile Kit Settings", fileName = "MobileKitSettings")]
    public sealed class MobileKitSettings : ScriptableObject
    {
        public const string ResourcePath = "MobileKitSettings";

        private static MobileKitSettings s_Instance;
        private static bool s_Loaded;

        [Tooltip("Laboon.Mobile.Haptics.HapticSettings asset. Empty = Resources/HapticSettings.")]
        [SerializeField] private ScriptableObject m_HapticSettings;

        [SerializeField] private RewardedAdMode m_RewardedAdMode = RewardedAdMode.Simulated;

        [Tooltip("Seconds the simulated rewarded ad plays (unscaled time).")]
        [SerializeField, Min(0f)] private float m_SimulatedAdSeconds = 1.5f;

        [Tooltip("Watched duration the simulated ad reports.")]
        [SerializeField, Min(0f)] private float m_SimulatedReportedSeconds = 30f;

        public ScriptableObject HapticSettings => m_HapticSettings;
        public RewardedAdMode RewardedAdMode => m_RewardedAdMode;
        public float SimulatedAdSeconds => m_SimulatedAdSeconds;
        public float SimulatedReportedSeconds => m_SimulatedReportedSeconds;

        /// <summary>The Resources asset, or null when the project has none.</summary>
        public static MobileKitSettings Instance
        {
            get
            {
                if (!s_Loaded)
                {
                    s_Instance = Resources.Load<MobileKitSettings>(ResourcePath);
                    s_Loaded = true;
                }
                return s_Instance;
            }
        }

        /// <summary>Overrides the loaded settings (tests, bootstrap code). Null restores Resources lookup.</summary>
        public static void SetInstance(MobileKitSettings settings)
        {
            s_Instance = settings;
            s_Loaded = settings != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Instance = null;
            s_Loaded = false;
        }
    }
}
