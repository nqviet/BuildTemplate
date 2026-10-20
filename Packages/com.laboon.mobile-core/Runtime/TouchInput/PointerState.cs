using UnityEngine;

namespace Laboon.Mobile.TouchInput
{
    /// <summary>Raw device reading for one frame.</summary>
    public struct PointerSample
    {
        /// <summary>The sample has a valid device reading (touch with an active phase, or a mouse).</summary>
        public bool HasDevice;
        public bool Pressed;
        public bool Canceled;
        public Vector2 Position;
        /// <summary>Touch id, or -1 for the mouse (EventSystem convention).</summary>
        public int PointerId;
    }

    /// <summary>Primary pointer state after edge detection.</summary>
    public readonly struct PointerState
    {
        public readonly bool Pressed;
        /// <summary>Press started this frame.</summary>
        public readonly bool Began;
        /// <summary>Press released or canceled this frame.</summary>
        public readonly bool Ended;
        /// <summary>The release this frame was a cancel (system gesture, interruption).</summary>
        public readonly bool Canceled;
        /// <summary>Last known screen position; kept on Ended/Canceled frames.</summary>
        public readonly Vector2 Position;
        public readonly int PointerId;
        /// <summary>The current gesture began over UI. Latched from Began until the gesture ends.</summary>
        public readonly bool OverUI;

        public PointerState(bool pressed, bool began, bool ended, bool canceled, Vector2 position, int pointerId, bool overUI)
        {
            Pressed = pressed;
            Began = began;
            Ended = ended;
            Canceled = canceled;
            Position = position;
            PointerId = pointerId;
            OverUI = overUI;
        }

        public override string ToString() =>
            $"Pressed={Pressed} Began={Began} Ended={Ended} Canceled={Canceled} Pos={Position} Id={PointerId} OverUI={OverUI}";
    }
}
