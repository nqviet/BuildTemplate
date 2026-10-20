using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Laboon.Mobile.TouchInput
{
    /// <summary>
    /// Primary-pointer acquisition for mobile: Input System primary touch, falling back to the mouse.
    /// <see cref="Read"/> is cached per frame, so several callers in one frame see the same edges.
    /// Owns acquisition and UI-hit detection only; gesture meaning (aiming, dragging) stays in the game.
    /// </summary>
    public static class MobilePointer
    {
        private static PointerTracker s_Tracker = CreateTracker();
        private static int s_LastFrame = -1;

        public static PointerTracker Tracker => s_Tracker;

        /// <summary>Samples the devices once per frame and returns the primary pointer state.</summary>
        public static PointerState Read()
        {
            if (s_LastFrame == Time.frameCount) return s_Tracker.Current;
            s_LastFrame = Time.frameCount;
            return s_Tracker.Update(Sample());
        }

        /// <summary>Reads the Input System devices without edge detection.</summary>
        public static PointerSample Sample()
        {
            var touch = Touchscreen.current != null ? Touchscreen.current.primaryTouch : null;
            if (touch != null)
            {
                var phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.None)
                {
                    return new PointerSample
                    {
                        HasDevice = true,
                        // Ended/Canceled frames keep the final position; isPressed is already false there.
                        Pressed = touch.press.isPressed,
                        Canceled = phase == UnityEngine.InputSystem.TouchPhase.Canceled,
                        Position = touch.position.ReadValue(),
                        PointerId = touch.touchId.ReadValue()
                    };
                }
            }

            if (Mouse.current != null)
            {
                return new PointerSample
                {
                    HasDevice = true,
                    Pressed = Mouse.current.leftButton.isPressed,
                    Position = Mouse.current.position.ReadValue(),
                    PointerId = -1
                };
            }
            return default;
        }

        /// <summary>Default UI hit test through the current EventSystem.</summary>
        public static bool IsPointerOverUI(Vector2 position, int pointerId)
        {
            var eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
        }

        /// <summary>Clears gesture state (use on scene change or focus loss).</summary>
        public static void Reset()
        {
            s_Tracker.Reset();
            s_LastFrame = -1;
        }

        private static PointerTracker CreateTracker() => new PointerTracker { IsOverUI = IsPointerOverUI };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Tracker = CreateTracker();
            s_LastFrame = -1;
        }
    }
}
