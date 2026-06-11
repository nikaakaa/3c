using UnityEngine;
using UnityEngine.Serialization;

namespace ThirdPersonMovement
{
    [CreateAssetMenu(fileName = "BasicMovementConfig", menuName = "3C/Movement/BasicMovementConfig")]
    public sealed class BasicMovementConfigSO : ScriptableObject
    {
        [SerializeField] float walkPlanarSpeed = 2f;
        [FormerlySerializedAs("maxPlanarSpeed")]
        [SerializeField] float runPlanarSpeed = 4f;
        [SerializeField] float inputDeadZone = 0.1f;
        [SerializeField] float rotationSpeed = 720f;
        [SerializeField] float moveStartMinTime = 0.08f;
        [SerializeField] float moveStopMinTime = 0.08f;

        public float MaxPlanarSpeed => RunPlanarSpeed;
        public float WalkPlanarSpeed => Mathf.Max(0f, walkPlanarSpeed);
        public float RunPlanarSpeed => Mathf.Max(0f, runPlanarSpeed);
        public float InputDeadZone => Mathf.Clamp01(inputDeadZone);
        public float RotationSpeed => Mathf.Max(0f, rotationSpeed);
        public float MoveStartMinTime => Mathf.Max(0f, moveStartMinTime);
        public float MoveStopMinTime => Mathf.Max(0f, moveStopMinTime);
    }
}
