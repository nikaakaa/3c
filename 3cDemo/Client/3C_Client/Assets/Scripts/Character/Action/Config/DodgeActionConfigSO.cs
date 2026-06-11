using UnityEngine;

namespace ThirdPersonAction
{
    [CreateAssetMenu(fileName = "DodgeActionConfig", menuName = "3C/Action/DodgeActionConfig")]
    public sealed class DodgeActionConfigSO : ScriptableObject
    {
        [SerializeField, Min(0f)] float directionalDuration = 0.35f;
        [SerializeField, Min(0f)] float directionalDistance = 4f;
        [SerializeField, Min(0f)] float backstepDuration = 0.35f;
        [SerializeField, Min(0f)] float backstepDistance = 3f;
        [SerializeField, Min(0)] int priority = 30;
        [SerializeField, Min(0)] int resistance = 20;
        [SerializeField] bool directionalRotateToDirection = true;
        [SerializeField] bool backstepRotateToDirection;

        public DodgeActionConfig ToConfig()
        {
            return new DodgeActionConfig(
                directionalDuration,
                directionalDistance,
                backstepDuration,
                backstepDistance,
                priority,
                resistance,
                directionalRotateToDirection,
                backstepRotateToDirection);
        }
    }
}
