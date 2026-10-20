using System;
using UnityEngine;

namespace Laboon.Mobile.TouchInput
{
    /// <summary>
    /// Converts per-frame samples into Began/Ended edges. Pure and device-free, so gestures can be unit-tested.
    /// </summary>
    public sealed class PointerTracker
    {
        private bool m_WasPressed;
        private bool m_GestureOverUI;
        private Vector2 m_LastPosition;
        private int m_LastPointerId = -1;

        /// <summary>UI hit test for a screen position and pointer id. Null = no UI exclusion.</summary>
        public Func<Vector2, int, bool> IsOverUI { get; set; }

        public PointerState Current { get; private set; }

        public PointerState Update(PointerSample sample)
        {
            bool pressed = false;
            bool canceled = false;
            if (sample.HasDevice)
            {
                pressed = sample.Pressed;
                canceled = sample.Canceled;
                m_LastPosition = sample.Position;
                m_LastPointerId = sample.PointerId;
            }

            bool began = pressed && !m_WasPressed;
            bool ended = !pressed && m_WasPressed;
            m_WasPressed = pressed;

            if (began) m_GestureOverUI = IsOverUI != null && IsOverUI(m_LastPosition, m_LastPointerId);
            bool overUI = (pressed || ended) && m_GestureOverUI;
            if (!pressed) m_GestureOverUI = false;

            Current = new PointerState(pressed, began, ended, ended && canceled, m_LastPosition, m_LastPointerId, overUI);
            return Current;
        }

        /// <summary>Forgets the current gesture (scene change, focus loss).</summary>
        public void Reset()
        {
            m_WasPressed = false;
            m_GestureOverUI = false;
            Current = default;
        }
    }
}
