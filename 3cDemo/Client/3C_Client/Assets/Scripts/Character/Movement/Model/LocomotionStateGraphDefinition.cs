using System;

namespace ThirdPersonMovement
{
    public sealed class LocomotionStateGraphDefinition
    {
        static readonly BasicMovementPhase[] DefaultStates =
        {
            BasicMovementPhase.Idle,
            BasicMovementPhase.MoveStart,
            BasicMovementPhase.MoveLoop,
            BasicMovementPhase.MoveStop
        };

        public LocomotionStateGraphDefinition(
            BasicMovementPhase initialState,
            BasicMovementPhase[] enabledStates,
            LocomotionStateGraphTransitionConfig[] transitions)
        {
            InitialState = initialState;
            EnabledStates = Copy(enabledStates);
            Transitions = Copy(transitions);
        }

        public BasicMovementPhase InitialState { get; }
        public BasicMovementPhase[] EnabledStates { get; }
        public LocomotionStateGraphTransitionConfig[] Transitions { get; }

        public static LocomotionStateGraphDefinition CreateDefault()
        {
            return new LocomotionStateGraphDefinition(
                BasicMovementPhase.Idle,
                DefaultStates,
                LocomotionStateGraphTransitionConfig.CreateDefaultTransitions());
        }

        public static BasicMovementPhase[] CreateDefaultStates()
        {
            return Copy(DefaultStates);
        }

        static BasicMovementPhase[] Copy(BasicMovementPhase[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<BasicMovementPhase>();

            BasicMovementPhase[] copy = new BasicMovementPhase[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        static LocomotionStateGraphTransitionConfig[] Copy(LocomotionStateGraphTransitionConfig[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<LocomotionStateGraphTransitionConfig>();

            LocomotionStateGraphTransitionConfig[] copy = new LocomotionStateGraphTransitionConfig[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
