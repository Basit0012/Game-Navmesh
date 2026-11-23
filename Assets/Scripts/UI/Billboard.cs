using UnityEngine;

namespace RelicDash.UI
{
    /// <summary>
    /// Billboards the attached transform (e.g. World-Space Canvas) to always face the active camera.
    /// Prevents distortion and backwards text when viewing from any 3D angle.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool lockYRotation = false;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            Vector3 directionToCamera = transform.position - targetCamera.transform.position;

            if (lockYRotation)
            {
                directionToCamera.y = 0;
            }

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
            }
        }
    }
}
