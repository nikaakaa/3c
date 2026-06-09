using UnityEngine;

namespace ThirdPersonMovement
{
    [CreateAssetMenu(fileName = "BasicMovementConfig", menuName = "3C/Movement/BasicMovementConfig")]
    public sealed class BasicMovementConfigSO : ScriptableObject
    {
        [SerializeField] float maxPlanarSpeed = 4f;
        [SerializeField] float inputDeadZone = 0.1f;
        [SerializeField] float rotationSpeed = 720f;
        [SerializeField] float moveStartMinTime = 0.08f;
        [SerializeField] float moveStopMinTime = 0.08f;

        public float MaxPlanarSpeed => Mathf.Max(0f, maxPlanarSpeed);
        public float InputDeadZone => Mathf.Clamp01(inputDeadZone);
        public float RotationSpeed => Mathf.Max(0f, rotationSpeed);
        public float MoveStartMinTime => Mathf.Max(0f, moveStartMinTime);
        public float MoveStopMinTime => Mathf.Max(0f, moveStopMinTime);
    }
}
