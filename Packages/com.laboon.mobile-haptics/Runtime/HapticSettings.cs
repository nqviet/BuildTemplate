using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Laboon.Mobile.Haptics
{
    /// <summary>
    /// Generic feedback cues. Games map their own events to these. The integer values match the original
    /// game-event order, so casts from a legacy enum stay valid during migration.
    /// </summary>
    public enum HapticCue
    {
        Preview = 0,
        Launch = 1,
        Complete = 2,
        Booster = 3,
        Success = 4,
        Failure = 5,
        /// <summary>Light tick on the 80 ms tick lane (landings).</summary>
        Tick = 6,
        /// <summary>Crisp impact on the tick lane (hits).</summary>
        Impact = 7
    }

    /// <summary>Native impact style. Values match the iOS bridge: 0 Light, 1 Medium, 2 Soft, 3 Heavy.</summary>
    public enum HapticStyle { Light, Medium, Soft, Heavy }

    [Serializable]
    public struct HapticPulse
    {
        public HapticStyle Style;
        [Range(0f, 1f)] public float Intensity;
        [Range(1, 30)] public int AndroidDurationMs;
        [Range(1, 2)] public int PulseCount;
        [Range(60, 200)] public int GapMs;

        public HapticPulse(HapticStyle style, float intensity, int duration, int count = 1, int gap = 100)
        {
            Style = style;
            Intensity = intensity;
            AndroidDurationMs = duration;
            PulseCount = count;
            GapMs = gap;
        }
    }

    /// <summary>
    /// Tactile feedback tuning. Never changes gameplay state. Default lookup: <c>MobileKitSettings.HapticSettings</c>,
    /// then <c>Resources/HapticSettings</c>, then built-in defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "Laboon/Haptic Settings", fileName = "HapticSettings")]
    public sealed class HapticSettings : ScriptableObject
    {
        public const string ResourcePath = "HapticSettings";
        public const string DefaultPreferenceKey = "Laboon_HapticsEnabled";

        [Tooltip("PlayerPrefs key for the on/off preference. Keep a project's old key to preserve user choices.")]
        public string PreferenceKey = DefaultPreferenceKey;

        [Range(.1f, 1f)] public float MinimumInterval = .3f;

        [Tooltip("Minimum seconds between tick-lane cues (Tick, Impact).")]
        [Range(.02f, .5f)] public float TickInterval = .08f;

        // UIImpactFeedbackGenerator Light below ~0.5 intensity is barely felt on iPhone.
        public HapticPulse Preview = new HapticPulse(HapticStyle.Medium, .8f, 12);
        public HapticPulse Launch = new HapticPulse(HapticStyle.Light, .7f, 10);
        [FormerlySerializedAs("BasketComplete")]
        public HapticPulse Complete = new HapticPulse(HapticStyle.Heavy, .9f, 20);
        public HapticPulse Booster = new HapticPulse(HapticStyle.Medium, .8f, 16);
        [FormerlySerializedAs("Win")]
        public HapticPulse Success = new HapticPulse(HapticStyle.Heavy, 1f, 24, 2, 110);
        [FormerlySerializedAs("Fail")]
        public HapticPulse Failure = new HapticPulse(HapticStyle.Soft, .8f, 16);
        [Tooltip("Light tick on the tick lane.")]
        [FormerlySerializedAs("TargetLand")]
        public HapticPulse Tick = new HapticPulse(HapticStyle.Light, .6f, 8);
        [Tooltip("Crisp medium tick on the tick lane.")]
        [FormerlySerializedAs("MarbleHit")]
        public HapticPulse Impact = new HapticPulse(HapticStyle.Medium, .75f, 10);

        public HapticPulse GetPulse(HapticCue cue)
        {
            switch (cue)
            {
                case HapticCue.Launch: return Launch;
                case HapticCue.Complete: return Complete;
                case HapticCue.Booster: return Booster;
                case HapticCue.Success: return Success;
                case HapticCue.Failure: return Failure;
                case HapticCue.Tick: return Tick;
                case HapticCue.Impact: return Impact;
                default: return Preview;
            }
        }

        public static bool IsTickLane(HapticCue cue) => cue == HapticCue.Tick || cue == HapticCue.Impact;
        public static bool IsResult(HapticCue cue) => cue == HapticCue.Success || cue == HapticCue.Failure;
    }
}
