using UnityEngine;

namespace ThirdPersonMovement
{
    [CreateAssetMenu(fileName = "LocomotionStateGraphConfig", menuName = "3C/Movement/LocomotionStateGraphConfig")]
    public sealed class LocomotionStateGraphConfigSO : ScriptableObject
    {
        [SerializeField] BasicMovementPhase initialState = BasicMovementPhase.Idle;
        [SerializeField] BasicMovementPhase[] enabledStates = LocomotionStateGraphDefinition.CreateDefaultStates();
        [SerializeField] LocomotionStateGraphTransitionConfig[] transitions = LocomotionStateGraphTransitionConfig.CreateDefaultTransitions();

        public BasicMovementPhase InitialState => initialState;
        public BasicMovementPhase[] EnabledStates => enabledStates;
        public LocomotionStateGraphTransitionConfig[] Transitions => transitions;

        public LocomotionStateGraphDefinition ToDefinition()
        {
            return new LocomotionStateGraphDefinition(initialState, enabledStates, transitions);
        }

        public void ResetToDefaultGraph()
        {
            initialState = BasicMovementPhase.Idle;
            enabledStates = LocomotionStateGraphDefinition.CreateDefaultStates();
            transitions = LocomotionStateGraphTransitionConfig.CreateDefaultTransitions();
        }
    }
}
