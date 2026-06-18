using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public enum CharacterStateOwnerKind
    {
        None = 0,
        Action = 1
    }

    public readonly struct CharacterStateOwner
    {
        CharacterStateOwner(CharacterStateOwnerKind kind, ActionStateId actionState)
        {
            Kind = kind;
            ActionState = actionState;
        }

        public CharacterStateOwnerKind Kind { get; }
        public ActionStateId ActionState { get; }
        public bool IsAction => Kind == CharacterStateOwnerKind.Action && ActionState.IsValid && ActionState != ActionStateIds.None;

        public static CharacterStateOwner None => new CharacterStateOwner(CharacterStateOwnerKind.None, ActionStateIds.None);

        public static CharacterStateOwner Action(ActionStateId actionState)
        {
            return actionState.IsValid && actionState != ActionStateIds.None
                ? new CharacterStateOwner(CharacterStateOwnerKind.Action, actionState)
                : None;
        }
    }

    public readonly struct CommittedActionGameplayRestoreState
    {
        public CommittedActionGameplayRestoreState(CharacterStateMachineRestoreState stateMachine)
            : this(stateMachine, ActionLifecycleRestoreState.Inactive)
        {
        }

        public CommittedActionGameplayRestoreState(
            CharacterStateMachineRestoreState stateMachine,
            ActionLifecycleRestoreState actionLifecycle)
        {
            StateMachine = stateMachine;
            ActionLifecycle = actionLifecycle;
        }

        public CharacterStateMachineRestoreState StateMachine { get; }
        public ActionLifecycleRestoreState ActionLifecycle { get; }
        public CharacterStateMachineSnapshot Snapshot => StateMachine.Snapshot;

        public static CommittedActionGameplayRestoreState Inactive =>
            new CommittedActionGameplayRestoreState(new CharacterStateMachineRestoreState(
                CharacterStateMachineSnapshot.Inactive,
                Vector3.zero,
                false));
    }

    public readonly struct CommittedActionDiagnosticRestoreState
    {
        public CommittedActionDiagnosticRestoreState(
            string debugStatePath,
            string debugPendingTransitionPath,
            string lastLoggedStatePath,
            string lastLoggedPendingTransitionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            bool loggedInitialLocomotionState)
        {
            DebugStatePath = debugStatePath ?? string.Empty;
            DebugPendingTransitionPath = debugPendingTransitionPath ?? string.Empty;
            LastLoggedStatePath = lastLoggedStatePath ?? string.Empty;
            LastLoggedPendingTransitionPath = lastLoggedPendingTransitionPath ?? string.Empty;
            LastLoggedLocomotionPath = lastLoggedLocomotionPath ?? string.Empty;
            LastLoggedLocomotionPhase = lastLoggedLocomotionPhase;
            LoggedInitialLocomotionState = loggedInitialLocomotionState;
        }

        public string DebugStatePath { get; }
        public string DebugPendingTransitionPath { get; }
        public string LastLoggedStatePath { get; }
        public string LastLoggedPendingTransitionPath { get; }
        public string LastLoggedLocomotionPath { get; }
        public BasicMovementPhase LastLoggedLocomotionPhase { get; }
        public bool LoggedInitialLocomotionState { get; }

        public static CommittedActionDiagnosticRestoreState Empty =>
            new CommittedActionDiagnosticRestoreState(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                BasicMovementPhase.Idle,
                true);
    }

    public readonly struct CommittedActionRestoreState
    {
        public CommittedActionRestoreState(
            CharacterStateMachineRestoreState stateMachine,
            string debugStatePath,
            string debugPendingTransitionPath,
            string lastLoggedStatePath,
            string lastLoggedPendingTransitionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            bool loggedInitialLocomotionState)
            : this(
                new CommittedActionGameplayRestoreState(stateMachine),
                new CommittedActionDiagnosticRestoreState(
                    debugStatePath,
                    debugPendingTransitionPath,
                    lastLoggedStatePath,
                    lastLoggedPendingTransitionPath,
                    lastLoggedLocomotionPath,
                    lastLoggedLocomotionPhase,
                    loggedInitialLocomotionState))
        {
        }

        public CommittedActionRestoreState(
            CommittedActionGameplayRestoreState gameplay,
            CommittedActionDiagnosticRestoreState diagnostic)
        {
            Gameplay = gameplay;
            Diagnostic = diagnostic;
        }

        public CommittedActionGameplayRestoreState Gameplay { get; }
        public CommittedActionDiagnosticRestoreState Diagnostic { get; }
        public CharacterStateMachineRestoreState StateMachine => Gameplay.StateMachine;
        public CharacterStateMachineSnapshot Snapshot => Gameplay.Snapshot;

        public static CommittedActionRestoreState Inactive =>
            new CommittedActionRestoreState(
                CommittedActionGameplayRestoreState.Inactive,
                CommittedActionDiagnosticRestoreState.Empty);
    }
}
