using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public enum FullBodyOwnerKind
    {
        None = 0,
        Action = 1
    }

    public readonly struct FullBodyOwner
    {
        FullBodyOwner(FullBodyOwnerKind kind, ActionStateId actionState)
        {
            Kind = kind;
            ActionState = actionState;
        }

        public FullBodyOwnerKind Kind { get; }
        public ActionStateId ActionState { get; }
        public bool IsAction => Kind == FullBodyOwnerKind.Action && ActionState.IsValid && ActionState != ActionStateIds.None;

        public static FullBodyOwner None => new FullBodyOwner(FullBodyOwnerKind.None, ActionStateIds.None);

        public static FullBodyOwner Action(ActionStateId actionState)
        {
            return actionState.IsValid && actionState != ActionStateIds.None
                ? new FullBodyOwner(FullBodyOwnerKind.Action, actionState)
                : None;
        }
    }

    public readonly struct FullBodyActionGameplayRestoreState
    {
        public FullBodyActionGameplayRestoreState(CharacterStateMachineRestoreState stateMachine)
            : this(stateMachine, ActionLifecycleRestoreState.Inactive)
        {
        }

        public FullBodyActionGameplayRestoreState(
            CharacterStateMachineRestoreState stateMachine,
            ActionLifecycleRestoreState actionLifecycle)
        {
            StateMachine = stateMachine;
            ActionLifecycle = actionLifecycle;
        }

        public CharacterStateMachineRestoreState StateMachine { get; }
        public ActionLifecycleRestoreState ActionLifecycle { get; }
        public CharacterStateMachineSnapshot Snapshot => StateMachine.Snapshot;

        public static FullBodyActionGameplayRestoreState Inactive =>
            new FullBodyActionGameplayRestoreState(new CharacterStateMachineRestoreState(
                CharacterStateMachineSnapshot.Inactive,
                Vector3.zero,
                false));
    }

    public readonly struct FullBodyActionDiagnosticRestoreState
    {
        public FullBodyActionDiagnosticRestoreState(
            string debugFullBodyStatePath,
            string debugPendingTransitionPath,
            string lastLoggedFullBodyPath,
            string lastLoggedPendingTransitionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            bool loggedInitialLocomotionState)
        {
            DebugFullBodyStatePath = debugFullBodyStatePath ?? string.Empty;
            DebugPendingTransitionPath = debugPendingTransitionPath ?? string.Empty;
            LastLoggedFullBodyPath = lastLoggedFullBodyPath ?? string.Empty;
            LastLoggedPendingTransitionPath = lastLoggedPendingTransitionPath ?? string.Empty;
            LastLoggedLocomotionPath = lastLoggedLocomotionPath ?? string.Empty;
            LastLoggedLocomotionPhase = lastLoggedLocomotionPhase;
            LoggedInitialLocomotionState = loggedInitialLocomotionState;
        }

        public string DebugFullBodyStatePath { get; }
        public string DebugPendingTransitionPath { get; }
        public string LastLoggedFullBodyPath { get; }
        public string LastLoggedPendingTransitionPath { get; }
        public string LastLoggedLocomotionPath { get; }
        public BasicMovementPhase LastLoggedLocomotionPhase { get; }
        public bool LoggedInitialLocomotionState { get; }

        public static FullBodyActionDiagnosticRestoreState Empty =>
            new FullBodyActionDiagnosticRestoreState(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                BasicMovementPhase.Idle,
                true);
    }

    public readonly struct FullBodyActionRestoreState
    {
        public FullBodyActionRestoreState(
            CharacterStateMachineRestoreState stateMachine,
            string debugFullBodyStatePath,
            string debugPendingTransitionPath,
            string lastLoggedFullBodyPath,
            string lastLoggedPendingTransitionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            bool loggedInitialLocomotionState)
            : this(
                new FullBodyActionGameplayRestoreState(stateMachine),
                new FullBodyActionDiagnosticRestoreState(
                    debugFullBodyStatePath,
                    debugPendingTransitionPath,
                    lastLoggedFullBodyPath,
                    lastLoggedPendingTransitionPath,
                    lastLoggedLocomotionPath,
                    lastLoggedLocomotionPhase,
                    loggedInitialLocomotionState))
        {
        }

        public FullBodyActionRestoreState(
            FullBodyActionGameplayRestoreState gameplay,
            FullBodyActionDiagnosticRestoreState diagnostic)
        {
            Gameplay = gameplay;
            Diagnostic = diagnostic;
        }

        public FullBodyActionGameplayRestoreState Gameplay { get; }
        public FullBodyActionDiagnosticRestoreState Diagnostic { get; }
        public CharacterStateMachineRestoreState StateMachine => Gameplay.StateMachine;
        public CharacterStateMachineSnapshot Snapshot => Gameplay.Snapshot;

        public static FullBodyActionRestoreState Inactive =>
            new FullBodyActionRestoreState(
                FullBodyActionGameplayRestoreState.Inactive,
                FullBodyActionDiagnosticRestoreState.Empty);
    }
}
