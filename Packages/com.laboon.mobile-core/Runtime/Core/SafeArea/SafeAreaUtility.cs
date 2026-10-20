using UnityEngine;

namespace Laboon.Mobile.SafeArea
{
    /// <summary>Pure safe-area math. Invalid input is rejected instead of producing NaN anchors.</summary>
    public static class SafeAreaUtility
    {
        private static Rect? s_Override;

        /// <summary>Safe area reported by the device, or the override set for tests and editor previews.</summary>
        public static Rect CurrentSafeArea => s_Override ?? Screen.safeArea;

        /// <summary>Screen size in pixels.</summary>
        public static Vector2 CurrentScreenSize => new Vector2(Screen.width, Screen.height);

        /// <summary>Forces a safe area (editor previews, tests). Pass null to use <see cref="Screen.safeArea"/> again.</summary>
        public static void SetOverride(Rect? safeArea) => s_Override = safeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_Override = null;

        /// <summary>True when the safe area is finite, non-empty and fully inside the screen.</summary>
        public static bool IsValid(Rect safeArea, Vector2 screenSize)
        {
            return screenSize.x >= 2 && screenSize.y >= 2
                && float.IsFinite(screenSize.x) && float.IsFinite(screenSize.y)
                && float.IsFinite(safeArea.x) && float.IsFinite(safeArea.y)
                && float.IsFinite(safeArea.width) && float.IsFinite(safeArea.height)
                && safeArea.width > 0 && safeArea.height > 0
                && safeArea.xMin >= 0 && safeArea.yMin >= 0
                && safeArea.xMax <= screenSize.x && safeArea.yMax <= screenSize.y;
        }

        /// <summary>Normalized point on the top safe edge. <paramref name="horizontal"/>: 0 = left, 1 = right.</summary>
        public static bool TryGetTopAnchor(Rect safeArea, Vector2 screenSize, float horizontal, out Vector2 anchor)
        {
            anchor = default;
            if (!IsValid(safeArea, screenSize)) return false;
            anchor = new Vector2(
                Mathf.Lerp(safeArea.xMin, safeArea.xMax, Mathf.Clamp01(horizontal)) / screenSize.x,
                safeArea.yMax / screenSize.y);
            return true;
        }

        /// <summary>Normalized anchor rectangle covering the safe area.</summary>
        public static bool TryGetNormalizedRect(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = default;
            anchorMax = default;
            if (!IsValid(safeArea, screenSize)) return false;
            anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            return true;
        }

        /// <summary>Insets in pixels: x = left, y = bottom, z = right, w = top. Zero when invalid.</summary>
        public static Vector4 GetInsets(Rect safeArea, Vector2 screenSize)
        {
            if (!IsValid(safeArea, screenSize)) return Vector4.zero;
            return new Vector4(safeArea.xMin, safeArea.yMin, screenSize.x - safeArea.xMax, screenSize.y - safeArea.yMax);
        }

        /// <summary>Safe-area width in pixels (screen width when invalid), never below 1.</summary>
        public static float GetWidth(Rect safeArea, Vector2 screenSize)
        {
            return IsValid(safeArea, screenSize) ? safeArea.width : Mathf.Max(screenSize.x, 1f);
        }
    }
}
