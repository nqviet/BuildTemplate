using Laboon.Mobile.TouchInput;
using NUnit.Framework;
using UnityEngine;

namespace Laboon.Mobile.Tests
{
    public sealed class PointerTrackerTests
    {
        private static PointerSample Touch(bool pressed, Vector2 position, bool canceled = false, int id = 3) =>
            new PointerSample { HasDevice = true, Pressed = pressed, Position = position, Canceled = canceled, PointerId = id };

        [Test]
        public void PressDragRelease()
        {
            var tracker = new PointerTracker();
            var began = tracker.Update(Touch(true, new Vector2(10, 10)));
            Assert.IsTrue(began.Began && began.Pressed && !began.Ended);
            Assert.AreEqual(3, began.PointerId);

            var drag = tracker.Update(Touch(true, new Vector2(40, 10)));
            Assert.IsFalse(drag.Began);
            Assert.AreEqual(new Vector2(40, 10), drag.Position);

            var ended = tracker.Update(Touch(false, new Vector2(50, 10)));
            Assert.IsTrue(ended.Ended && !ended.Canceled && !ended.Pressed);
            Assert.AreEqual(new Vector2(50, 10), ended.Position, "release keeps the final position");

            var idle = tracker.Update(Touch(false, new Vector2(50, 10)));
            Assert.IsFalse(idle.Began || idle.Ended || idle.Pressed);
        }

        [Test]
        public void CancelIsReportedOnTheEndFrame()
        {
            var tracker = new PointerTracker();
            tracker.Update(Touch(true, Vector2.zero));
            var canceled = tracker.Update(Touch(false, Vector2.one, canceled: true));
            Assert.IsTrue(canceled.Ended && canceled.Canceled);
        }

        [Test]
        public void MissingDeviceKeepsLastPositionAndEndsGesture()
        {
            var tracker = new PointerTracker();
            tracker.Update(Touch(true, new Vector2(7, 8)));
            var state = tracker.Update(default);
            Assert.IsTrue(state.Ended);
            Assert.AreEqual(new Vector2(7, 8), state.Position);
        }

        [Test]
        public void UiHitIsLatchedForTheWholeGesture()
        {
            int hits = 0;
            var tracker = new PointerTracker { IsOverUI = (position, id) => { hits++; return position.x < 100; } };
            Assert.IsTrue(tracker.Update(Touch(true, new Vector2(50, 0))).OverUI);
            Assert.IsTrue(tracker.Update(Touch(true, new Vector2(500, 0))).OverUI, "dragging off UI stays a UI gesture");
            Assert.IsTrue(tracker.Update(Touch(false, new Vector2(500, 0))).OverUI);
            Assert.AreEqual(1, hits, "hit test runs only on press");

            Assert.IsFalse(tracker.Update(Touch(true, new Vector2(500, 0))).OverUI);
        }

        [Test]
        public void MouseFallbackUsesPointerIdMinusOne()
        {
            var tracker = new PointerTracker();
            var state = tracker.Update(new PointerSample { HasDevice = true, Pressed = true, Position = Vector2.one, PointerId = -1 });
            Assert.AreEqual(-1, state.PointerId);
        }

        [Test]
        public void ResetForgetsGesture()
        {
            var tracker = new PointerTracker();
            tracker.Update(Touch(true, Vector2.zero));
            tracker.Reset();
            Assert.IsTrue(tracker.Update(Touch(true, Vector2.zero)).Began);
        }
    }
}
