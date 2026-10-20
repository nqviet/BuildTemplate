using Laboon.Mobile.CameraFit;
using NUnit.Framework;
using UnityEngine;

namespace Laboon.Mobile.Tests
{
    public sealed class CameraFitTests
    {
        private const float k_Reference = 1080f / 1920f;

        [Test]
        public void ReferenceAspectKeepsDefaults()
        {
            Assert.AreEqual(6.5f, CameraFitMath.FitOrthographicSize(6.5f, k_Reference, k_Reference), 1e-4f);
            Assert.AreEqual(50f, CameraFitMath.FitFieldOfView(50f, k_Reference, k_Reference), 1e-4f);
        }

        [Test]
        public void TallPhoneWidensVerticalViewToKeepBoardWidth()
        {
            float tall = 1170f / 2532f;
            float size = CameraFitMath.FitOrthographicSize(6.5f, k_Reference, tall);
            Assert.AreEqual(6.5f * k_Reference / tall, size, 1e-4f);

            float fov = CameraFitMath.FitFieldOfView(50f, k_Reference, tall);
            Assert.Greater(fov, 50f);
            float referenceHorizontal = 2f * Mathf.Atan(Mathf.Tan(25f * Mathf.Deg2Rad) * k_Reference);
            float horizontal = 2f * Mathf.Atan(Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * tall);
            Assert.AreEqual(referenceHorizontal, horizontal, 1e-4f, "board width must not be cropped");
        }

        [Test]
        public void TabletKeepsReferenceHeight()
        {
            float tablet = 3f / 4f;
            Assert.AreEqual(6.5f, CameraFitMath.FitOrthographicSize(6.5f, k_Reference, tablet), 1e-4f);
            Assert.AreEqual(50f, CameraFitMath.FitFieldOfView(50f, k_Reference, tablet), 1e-4f);
        }

        [Test]
        public void DegenerateAspectIsClamped()
        {
            Assert.IsTrue(float.IsFinite(CameraFitMath.FitOrthographicSize(6.5f, k_Reference, 0f)));
            Assert.IsTrue(float.IsFinite(CameraFitMath.FitFieldOfView(50f, k_Reference, 0f)));
            Assert.AreEqual(1080f, CameraFitMath.ReferenceAspect(new Vector2(1080, 0)));
        }

        [Test]
        public void FitterComponentDrivesOrthographicCamera()
        {
            var go = new GameObject("camera", typeof(UnityEngine.Camera));
            try
            {
                var camera = go.GetComponent<UnityEngine.Camera>();
                camera.orthographic = true;
                camera.aspect = 0.4f;
                var fitter = go.AddComponent<ResponsiveCameraFitter>();
                fitter.UpdateCameraViewport();
                Assert.AreEqual(6.5f * k_Reference / 0.4f, camera.orthographicSize, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
