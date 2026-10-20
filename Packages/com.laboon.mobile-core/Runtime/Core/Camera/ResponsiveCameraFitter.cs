using UnityEngine;
using UnityEngine.Serialization;

namespace Laboon.Mobile.CameraFit
{
    /// <summary>
    /// Fits a camera to a reference resolution: preserves width on tall phones and height on wider tablets.
    /// Derive a project class from it to keep existing scene references.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class ResponsiveCameraFitter : MonoBehaviour
    {
        [Header("Design Reference")]
        [Tooltip("Reference resolution (1080x1920 = 9:16 portrait).")]
        [SerializeField, FormerlySerializedAs("_targetResolution")]
        private Vector2 m_TargetResolution = new Vector2(1080f, 1920f);

        [Header("FOV / Size Settings")]
        [Tooltip("Vertical FOV at the reference aspect.")]
        [SerializeField, FormerlySerializedAs("_defaultFieldOfView")]
        private float m_DefaultFieldOfView = 50f;

        [Tooltip("Orthographic size at the reference aspect.")]
        [SerializeField, FormerlySerializedAs("_defaultOrthoSize")]
        private float m_DefaultOrthoSize = 6.5f;

        private UnityEngine.Camera m_Camera;
        private float m_LastAspect = -1f;

        public Vector2 TargetResolution { get => m_TargetResolution; set { m_TargetResolution = value; UpdateCameraViewport(); } }
        public float DefaultFieldOfView { get => m_DefaultFieldOfView; set { m_DefaultFieldOfView = value; UpdateCameraViewport(); } }
        public float DefaultOrthoSize { get => m_DefaultOrthoSize; set { m_DefaultOrthoSize = value; UpdateCameraViewport(); } }

        protected virtual void OnEnable()
        {
            m_LastAspect = -1f;
            UpdateCameraViewport();
        }

        protected virtual void Update()
        {
            if (m_Camera == null || !Mathf.Approximately(m_Camera.aspect, m_LastAspect)) UpdateCameraViewport();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            m_LastAspect = -1f;
            UpdateCameraViewport();
        }
#endif

        [ContextMenu("Update Viewport Now")]
        public void UpdateCameraViewport()
        {
            if (m_Camera == null) m_Camera = GetComponent<UnityEngine.Camera>();
            if (m_Camera == null) return;

            float referenceAspect = CameraFitMath.ReferenceAspect(m_TargetResolution);
            m_LastAspect = m_Camera.aspect;

            // Write only on change, so edit-mode repaints do not dirty the scene.
            if (m_Camera.orthographic)
            {
                float size = CameraFitMath.FitOrthographicSize(m_DefaultOrthoSize, referenceAspect, m_Camera.aspect);
                if (!Mathf.Approximately(m_Camera.orthographicSize, size)) m_Camera.orthographicSize = size;
            }
            else
            {
                float fov = CameraFitMath.FitFieldOfView(m_DefaultFieldOfView, referenceAspect, m_Camera.aspect);
                if (!Mathf.Approximately(m_Camera.fieldOfView, fov)) m_Camera.fieldOfView = fov;
            }
        }
    }
}
