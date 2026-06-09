using System;
using UnityEngine;

namespace ThirdPersonMovement
{
    [Serializable]
    public struct LocomotionStateGraphTransitionConfig
    {
        [SerializeField] BasicMovementPhase from;
        [SerializeField] BasicMovementPhase to;
        [SerializeField] int priority;
        [SerializeField] LocomotionStateGraphCondition[] conditions;

        public LocomotionStateGraphTransitionConfig(
            BasicMovementPhase from,
            BasicMovementPhase to,
            int priority,
            params LocomotionStateGraphCondition[] conditions)
        {
            this.from = from;
            this.to = to;
            this.priority = priority;
            this.conditions = conditions ?? Array.Empty<LocomotionStateGraphCondition>();
        }

        public BasicMovementPhase From => from;
        public BasicMovementPhase To => to;
        public int Priority => priority;
        public LocomotionStateGraphCondition[] Conditions => conditions ?? Array.Empty<LocomotionStateGraphCondition>();

        public static LocomotionStateGraphTransitionConfig[] CreateDefaultTransitions()
        {
            return new[]
            {
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, 100, LocomotionStateGraphCondition.HasMoveIntent),
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.MoveStart, BasicMovementPhase.MoveStop, 100, LocomotionStateGraphCondition.NoMoveIntent),
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.MoveStart, BasicMovementPhase.MoveLoop, 0, LocomotionStateGraphCondition.HasMoveIntent, LocomotionStateGraphCondition.MoveStartMinTimeReached),
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.MoveLoop, BasicMovementPhase.MoveStop, 100, LocomotionStateGraphCondition.NoMoveIntent),
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.MoveStop, BasicMovementPhase.MoveStart, 100, LocomotionStateGraphCondition.HasMoveIntent),
                new LocomotionStateGraphTransitionConfig(BasicMovementPhase.MoveStop, BasicMovementPhase.Idle, 0, LocomotionStateGraphCondition.NoMoveIntent, LocomotionStateGraphCondition.MoveStopMinTimeReached)
            };
        }
    }
}
