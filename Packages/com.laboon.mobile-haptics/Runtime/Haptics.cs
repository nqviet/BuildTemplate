using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Laboon.Mobile.Haptics
{
    /// <summary>
    /// Cue-based mobile haptics with a shared pulse budget and an independent saved preference.
    /// Native failures disable haptics for the rest of the run and never propagate to callers.
    /// </summary>
    public static class Haptics
    {
        private const float k_ResultLockSeconds = 1f;

        private static float s_NextPulseTime;
        private static float s_NextResultTime;
        private static float s_NextTickTime;
        private static HapticSettings s_Settings;
        private static HapticSettings s_DefaultSettings;
        private static bool s_BackendFailed;
        private static Action<HapticPulse> s_PlayBackend = PlayNative;
        private static Action<HapticPulse> s_StopBackend = StopNative;
        private static Action<HapticPulse> s_PrepareBackend = PrepareNative;
        private static Func<float> s_Clock = () => Time.unscaledTime;
        private static Func<float> s_TimeScale = () => Time.timeScale;

#if !UNITY_EDITOR && UNITY_IOS
        [DllImport("__Internal")] private static extern void LaboonHapticsPlay(int style, float intensity, int count, int gapMs);
        [DllImport("__Internal")] private static extern void LaboonHapticsStop();
        [DllImport("__Internal")] private static extern void LaboonHapticsPrepare(int style);
#endif

        /// <summary>Raised after the preference changes.</summary>
        public static event Action<bool> OnEnabledChanged;

        /// <summary>Active settings. Assign to override the default lookup; null restores it.</summary>
        public static HapticSettings Settings
        {
            get
            {
                if (s_Settings != null) return s_Settings;
                var kit = MobileKitSettings.Instance;
                s_Settings = kit != null ? kit.HapticSettings as HapticSettings : null;
                if (s_Settings == null) s_Settings = Resources.Load<HapticSettings>(HapticSettings.ResourcePath);
                if (s_Settings == null)
                {
                    if (s_DefaultSettings == null)
                    {
                        s_DefaultSettings = ScriptableObject.CreateInstance<HapticSettings>();
                        s_DefaultSettings.hideFlags = HideFlags.HideAndDontSave;
                    }
                    s_Settings = s_DefaultSettings;
                }
                return s_Settings;
            }
            set => s_Settings = value;
        }

        public static string PreferenceKey
        {
            get
            {
                string key = Settings.PreferenceKey;
                return string.IsNullOrEmpty(key) ? HapticSettings.DefaultPreferenceKey : key;
            }
        }

        /// <summary>The player's saved preference (default on).</summary>
        public static bool Enabled => PlayerPrefs.GetInt(PreferenceKey, 1) == 1;

        /// <summary>The native backend failed this run; haptics are off until restart.</summary>
        public static bool BackendFailed => s_BackendFailed;

        public static void SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) Stop();
            OnEnabledChanged?.Invoke(enabled);
        }

        /// <summary>Plays a cue if the preference, budget and lanes allow it. Returns true when a pulse was sent.</summary>
        public static bool Play(HapticCue cue)
        {
            if (cue != HapticCue.Preview && s_TimeScale() <= 0f) return false;
            if (!TryAcceptPulse(cue, s_Clock())) return false;
            return InvokeBackend(s_PlayBackend, Settings.GetPulse(cue));
        }

        /// <summary>Warms the native generator for a cue that is about to play. Silent.</summary>
        public static void Prepare(HapticCue cue)
        {
            if (s_BackendFailed || !Enabled || s_TimeScale() <= 0f) return;
            var pulse = Settings.GetPulse(cue);
            if (pulse.Intensity > 0f) InvokeBackend(s_PrepareBackend, pulse);
        }

        /// <summary>Cancels pending pulses and resets the budget.</summary>
        public static void Stop()
        {
            s_NextPulseTime = s_NextResultTime = s_NextTickTime = 0f;
            InvokeBackend(s_StopBackend, default);
        }

        private static bool TryAcceptPulse(HapticCue cue, float now)
        {
            if (s_BackendFailed || !Enabled) return false;
            var settings = Settings;
            if (settings.GetPulse(cue).Intensity <= 0f) return false;

            if (HapticSettings.IsTickLane(cue))
            {
                // Ticks use their own lane and never spend the shared budget, so a tick cannot swallow
                // the completion or result pulse that follows it.
                if (now < s_NextPulseTime || now < s_NextTickTime) return false;
                s_NextTickTime = now + Mathf.Clamp(settings.TickInterval, .02f, .5f);
                return true;
            }

            bool result = HapticSettings.IsResult(cue);
            // Results may replace an accent, but duplicate result callbacks cannot stack.
            if (result ? now < s_NextResultTime : now < s_NextPulseTime) return false;
            s_NextPulseTime = now + Mathf.Clamp(settings.MinimumInterval, .1f, 1f);
            if (result) s_NextResultTime = now + k_ResultLockSeconds;
            return true;
        }

        private static bool InvokeBackend(Action<HapticPulse> operation, HapticPulse pulse)
        {
            try
            {
                operation(pulse);
                return true;
            }
            catch (Exception exception) when (IsNativeFailure(exception))
            {
                // Optional feedback must not abort gameplay or UI callbacks. Keep the user's preference and
                // stop repeated failures until the next app run.
                s_BackendFailed = true;
                return false;
            }
        }

        private static bool IsNativeFailure(Exception exception) =>
            exception is EntryPointNotFoundException || exception is DllNotFoundException
            || exception is AndroidJavaException || exception is MarshalDirectiveException;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetState()
        {
            s_NextPulseTime = s_NextResultTime = s_NextTickTime = 0f;
            s_Settings = null;
            s_BackendFailed = false;
            s_PlayBackend = PlayNative;
            s_StopBackend = StopNative;
            s_PrepareBackend = PrepareNative;
            s_Clock = () => Time.unscaledTime;
            s_TimeScale = () => Time.timeScale;
            OnEnabledChanged = null;
        }

        /// <summary>Test seam: replaces the native backends and time sources.</summary>
        internal static void SetTestHooks(Action<HapticPulse> play, Action<HapticPulse> stop, Action<HapticPulse> prepare,
            Func<float> clock = null, Func<float> timeScale = null)
        {
            s_PlayBackend = play ?? (_ => { });
            s_StopBackend = stop ?? (_ => { });
            s_PrepareBackend = prepare ?? (_ => { });
            if (clock != null) s_Clock = clock;
            if (timeScale != null) s_TimeScale = timeScale;
        }

        private static void StopNative(HapticPulse pulse)
        {
#if !UNITY_EDITOR && UNITY_IOS
            LaboonHapticsStop();
#elif !UNITY_EDITOR && UNITY_ANDROID
            using (var vibrator = GetAndroidVibrator())
                if (vibrator != null) vibrator.Call("cancel");
#endif
        }

        private static void PrepareNative(HapticPulse pulse)
        {
#if !UNITY_EDITOR && UNITY_IOS
            LaboonHapticsPrepare((int)pulse.Style);
#endif
        }

        private static void PlayNative(HapticPulse pulse)
        {
#if !UNITY_EDITOR && UNITY_IOS
            LaboonHapticsPlay((int)pulse.Style, Mathf.Clamp01(pulse.Intensity),
                Mathf.Clamp(pulse.PulseCount, 1, 2), Mathf.Clamp(pulse.GapMs, 60, 200));
#elif !UNITY_EDITOR && UNITY_ANDROID
            PlayAndroid(pulse);
#endif
        }

#if !UNITY_EDITOR && UNITY_ANDROID
        private static AndroidJavaObject GetAndroidVibrator()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                return activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        private static void PlayAndroid(HapticPulse pulse)
        {
            using (var vibrator = GetAndroidVibrator())
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                vibrator.Call("cancel");
                long duration = Mathf.Clamp(pulse.AndroidDurationMs, 1, 30);
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    bool amplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
                    int amplitude = amplitudeControl ? Mathf.Clamp(Mathf.RoundToInt(pulse.Intensity * 255f), 1, 255) : -1;
                    if (!amplitudeControl) duration = Math.Min(duration, 8);
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = pulse.PulseCount > 1
                        ? effects.CallStatic<AndroidJavaObject>("createWaveform",
                            new long[] { 0, duration, Mathf.Clamp(pulse.GapMs, 60, 200), duration },
                            new int[] { 0, amplitude, 0, amplitude }, -1)
                        : effects.CallStatic<AndroidJavaObject>("createOneShot", duration, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", Math.Min(duration, 8));
            }
        }
#endif
    }
}
