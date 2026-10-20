using Laboon.Mobile.Ads;
using Laboon.Mobile.CameraFit;
using Laboon.Mobile.CrashReporting;
using Laboon.Mobile.Haptics;
using Laboon.Mobile.SafeArea;
using Laboon.Mobile.TouchInput;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HelloWorld
{
    /// <summary>
    /// Hello World smoke test for the Laboon mobile kit. Builds its own UI at runtime and
    /// touches every runtime module: safe area, camera fit, touch pointer, haptics, rewarded ads, crash facade.
    /// </summary>
    public class HelloWorldGame : MonoBehaviour
    {
        private Text m_Status;
        private Text m_HapticsLabel;
        private int m_Taps;
        private int m_Rewards;

        private void Start()
        {
            CrashReports.Log("HelloWorld started");
            SetupCamera();
            BuildUi();
            Refresh("Tap anywhere");
        }

        private void Update()
        {
            var pointer = MobilePointer.Read();
            if (pointer.OverUI || !pointer.Began) return;
            m_Taps++;
            Haptics.Play(HapticCue.Tick);
            Refresh($"Tap at {pointer.Position:0}");
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.24f);
            if (cam.GetComponent<ResponsiveCameraFitter>() == null) cam.gameObject.AddComponent<ResponsiveCameraFitter>();
        }

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasGo.transform, false);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaAnchor>().Mode = SafeAreaAnchor.AnchorMode.FullRect;

            CreateText(safe, "Hello World", 110, new Vector2(0.5f, 0.75f));
            m_Status = CreateText(safe, "", 48, new Vector2(0.5f, 0.6f));
            CreateButton(safe, "Watch Ad", new Vector2(0.5f, 0.35f), OnWatchAd);
            m_HapticsLabel = CreateButton(safe, "", new Vector2(0.5f, 0.22f), OnToggleHaptics);
        }

        private void OnWatchAd()
        {
            Haptics.Play(HapticCue.Launch);
            Refresh("Ad playing...");
            RewardedAds.Show(result =>
            {
                if (result.IsCompleted) { m_Rewards++; Haptics.Play(HapticCue.Success); }
                else Haptics.Play(HapticCue.Failure);
                Refresh($"Ad: {result}");
            });
        }

        private void OnToggleHaptics()
        {
            Haptics.SetEnabled(!Haptics.Enabled);
            Refresh("Haptics toggled");
        }

        private void Refresh(string message)
        {
            string provider = RewardedAds.Provider == null ? "none" : RewardedAds.IsSimulated ? "simulated" : "external";
            m_Status.text = $"{message}\nTaps: {m_Taps}   Rewards: {m_Rewards}\nAd provider: {provider} (ready: {RewardedAds.IsAvailable})";
            m_HapticsLabel.text = Haptics.Enabled ? "Haptics: ON" : "Haptics: OFF";
        }

        private static Text CreateText(Transform parent, string value, int size, Vector2 anchor)
        {
            var rt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(1000, 400);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static Text CreateButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
        {
            var rt = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(600, 150);
            rt.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.95f);
            rt.GetComponent<Button>().onClick.AddListener(onClick);
            var text = CreateText(rt, label, 56, new Vector2(0.5f, 0.5f));
            text.rectTransform.sizeDelta = rt.sizeDelta;
            return text;
        }
    }
}
