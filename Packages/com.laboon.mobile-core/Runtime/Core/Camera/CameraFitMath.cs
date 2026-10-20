using UnityEngine;

namespace Laboon.Mobile.CameraFit
{
    /// <summary>Pure aspect-fit math: keeps the reference width visible on narrower screens.</summary>
    public static class CameraFitMath
    {
        public static float ReferenceAspect(Vector2 referenceResolution)
            => referenceResolution.x / Mathf.Max(referenceResolution.y, 1f);

        /// <summary>Orthographic size that never crops the reference width.</summary>
        public static float FitOrthographicSize(float referenceSize, float referenceAspect, float currentAspect)
        {
            currentAspect = Mathf.Max(currentAspect, 0.01f);
            return currentAspect < referenceAspect ? referenceSize * (referenceAspect / currentAspect) : referenceSize;
        }

        /// <summary>Vertical field of view in degrees that preserves the reference horizontal FOV.</summary>
        public static float FitFieldOfView(float referenceFieldOfView, float referenceAspect, float currentAspect)
        {
            currentAspect = Mathf.Max(currentAspect, 0.01f);
            if (currentAspect >= referenceAspect) return referenceFieldOfView;
            float referenceRad = referenceFieldOfView * Mathf.Deg2Rad;
            float horizontal = 2f * Mathf.Atan(Mathf.Tan(referenceRad * 0.5f) * referenceAspect);
            return 2f * Mathf.Atan(Mathf.Tan(horizontal * 0.5f) / currentAspect) * Mathf.Rad2Deg;
        }
    }
}
