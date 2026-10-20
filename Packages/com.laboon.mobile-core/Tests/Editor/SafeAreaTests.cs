using Laboon.Mobile.SafeArea;
using NUnit.Framework;
using UnityEngine;

namespace Laboon.Mobile.Tests
{
    public sealed class SafeAreaTests
    {
        private static readonly Vector2 s_Screen = new Vector2(1170, 2532);

        [TearDown]
        public void TearDown() => SafeAreaUtility.SetOverride(null);

        [Test]
        public void TopAnchorSitsUnderCutout()
        {
            var safe = new Rect(0, 102, 1170, 2532 - 102 - 141);
            Assert.IsTrue(SafeAreaUtility.TryGetTopAnchor(safe, s_Screen, 0.5f, out var anchor));
            Assert.AreEqual(0.5f, anchor.x, 1e-5f);
            Assert.AreEqual((2532f - 141f) / 2532f, anchor.y, 1e-5f);
        }

        [Test]
        public void HorizontalAnchorFollowsLandscapeInsets()
        {
            var screen = new Vector2(2532, 1170);
            var safe = new Rect(141, 63, 2532 - 282, 1170 - 63);
            Assert.IsTrue(SafeAreaUtility.TryGetTopAnchor(safe, screen, 0f, out var anchor));
            Assert.AreEqual(141f / 2532f, anchor.x, 1e-5f);
        }

        [TestCase(float.NaN, 0, 100, 100)]
        [TestCase(0, 0, 0, 100)]
        [TestCase(-1, 0, 100, 100)]
        [TestCase(0, 0, 2000, 100)]
        public void InvalidSafeAreaIsRejected(float x, float y, float w, float h)
        {
            var safe = new Rect(x, y, w, h);
            Assert.IsFalse(SafeAreaUtility.IsValid(safe, s_Screen));
            Assert.IsFalse(SafeAreaUtility.TryGetTopAnchor(safe, s_Screen, .5f, out _));
            Assert.AreEqual(Vector4.zero, SafeAreaUtility.GetInsets(safe, s_Screen));
        }

        [Test]
        public void TinyScreenIsRejected()
        {
            Assert.IsFalse(SafeAreaUtility.IsValid(new Rect(0, 0, 1, 1), Vector2.one));
        }

        [Test]
        public void InsetsAndWidth()
        {
            var safe = new Rect(10, 20, 1150, 2400);
            Assert.AreEqual(new Vector4(10, 20, 10, 112), SafeAreaUtility.GetInsets(safe, s_Screen));
            Assert.AreEqual(1150f, SafeAreaUtility.GetWidth(safe, s_Screen));
            Assert.AreEqual(1170f, SafeAreaUtility.GetWidth(new Rect(0, 0, 0, 0), s_Screen));
        }

        [Test]
        public void OverrideReplacesScreenSafeArea()
        {
            var forced = new Rect(1, 2, 3, 4);
            SafeAreaUtility.SetOverride(forced);
            Assert.AreEqual(forced, SafeAreaUtility.CurrentSafeArea);
            SafeAreaUtility.SetOverride(null);
            Assert.AreEqual(Screen.safeArea, SafeAreaUtility.CurrentSafeArea);
        }

        [Test]
        public void AnchorComponentAppliesAndIgnoresInvalidInput()
        {
            var go = new GameObject("anchor", typeof(RectTransform));
            try
            {
                var anchor = go.AddComponent<SafeAreaAnchor>();
                var rect = go.GetComponent<RectTransform>();
                var safe = new Rect(0, 0, 1170, 2400);
                Assert.IsTrue(anchor.Apply(safe, s_Screen));
                Assert.AreEqual(2400f / 2532f, rect.anchorMin.y, 1e-5f);
                Assert.AreEqual(rect.anchorMin, rect.anchorMax);

                Assert.IsFalse(anchor.Apply(new Rect(float.NaN, 0, 1, 1), s_Screen));
                Assert.AreEqual(2400f / 2532f, rect.anchorMin.y, 1e-5f, "invalid input must not move anchors");

                anchor.Mode = SafeAreaAnchor.AnchorMode.FullRect;
                Assert.IsTrue(anchor.Apply(new Rect(0, 100, 1170, 2300), s_Screen));
                Assert.AreEqual(100f / 2532f, rect.anchorMin.y, 1e-5f);
                Assert.AreEqual(2400f / 2532f, rect.anchorMax.y, 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
