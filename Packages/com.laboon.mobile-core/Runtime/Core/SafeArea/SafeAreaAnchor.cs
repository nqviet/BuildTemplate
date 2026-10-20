using UnityEngine;

namespace Laboon.Mobile.SafeArea
{
    /// <summary>
    /// Anchors a RectTransform to the device safe area. Anchors are driven, not serialized, so edit-mode
    /// repaints never mark the scene dirty.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAnchor : MonoBehaviour
    {
        public enum AnchorMode
        {
            /// <summary>Point anchor on the top safe edge (HUD controls under a notch).</summary>
            TopEdge,
            /// <summary>Stretch anchors over the whole safe area.</summary>
            FullRect
        }

        [SerializeField] private AnchorMode m_Mode = AnchorMode.TopEdge;
        [SerializeField, Range(0, 1)] private float m_HorizontalAnchor = 0.5f;

        private RectTransform m_Rect;
        private DrivenRectTransformTracker m_Tracker;

        public AnchorMode Mode
        {
            get => m_Mode;
            set { m_Mode = value; ApplyCurrentScreen(); }
        }

        public float HorizontalAnchor
        {
            get => m_HorizontalAnchor;
            set { m_HorizontalAnchor = Mathf.Clamp01(value); ApplyCurrentScreen(); }
        }

        protected virtual void OnEnable()
        {
            ApplyCurrentScreen();
            Canvas.preWillRenderCanvases += ApplyCurrentScreen;
        }

        protected virtual void OnDisable()
        {
            Canvas.preWillRenderCanvases -= ApplyCurrentScreen;
            m_Tracker.Clear();
        }

        public void ApplyCurrentScreen() => Apply(SafeAreaUtility.CurrentSafeArea, SafeAreaUtility.CurrentScreenSize);

        /// <summary>Applies anchors for the given safe area. Returns false and leaves anchors untouched when invalid.</summary>
        public bool Apply(Rect safeArea, Vector2 screenSize)
        {
            Vector2 min, max;
            if (m_Mode == AnchorMode.TopEdge)
            {
                if (!SafeAreaUtility.TryGetTopAnchor(safeArea, screenSize, m_HorizontalAnchor, out min)) return false;
                max = min;
            }
            else if (!SafeAreaUtility.TryGetNormalizedRect(safeArea, screenSize, out min, out max))
            {
                return false;
            }

            if (m_Rect == null) m_Rect = GetComponent<RectTransform>();
            m_Tracker.Clear();
            m_Tracker.Add(this, m_Rect, DrivenTransformProperties.Anchors);
            if (m_Rect.anchorMin != min) m_Rect.anchorMin = min;
            if (m_Rect.anchorMax != max) m_Rect.anchorMax = max;
            return true;
        }
    }
}
