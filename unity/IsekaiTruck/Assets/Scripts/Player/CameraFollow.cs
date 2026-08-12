using UnityEngine;

namespace IsekaiTruck.Player
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -12f);
        [SerializeField] private Vector3 fixedEulerAngles = new Vector3(45f, 0f, 0f);

        public void Configure(Transform followTarget, Vector3 followOffset, Vector3 cameraEulerAngles)
        {
            target = followTarget;
            offset = followOffset;
            fixedEulerAngles = cameraEulerAngles;
            SnapToTarget();
        }

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            SnapToTarget();
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                target.position + offset,
                Quaternion.Euler(fixedEulerAngles));
        }
    }
}
