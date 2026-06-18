using System;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class CharacterStateMachineRuntime
    {
        CharacterStateMachineRunner stateMachine;
        CharacterStateMachineSnapshot currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
        string debugStatePath = CharacterStateMachineSnapshot.Inactive.ActivePath;
        string debugPendingTransitionPath = CharacterStateMachineSnapshot.Inactive.PendingTransitionPath;
        string lastLoggedStatePath = CharacterStateMachineSnapshot.Inactive.ActivePath;
        string lastLoggedPendingTransitionPath = CharacterStateMachineSnapshot.Inactive.PendingTransitionPath;
        string lastLoggedLocomotionPath = string.Empty;
        BasicMovementPhase lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
        bool loggedInitialLocomotionState = true;

        public CharacterStateMachineRunner StateMachine => stateMachine;
        public CharacterStateMachineSnapshot CurrentStateSnapshot
        {
            get => currentStateSnapshot;
            internal set => currentStateSnapshot = value;
        }
        public string ActiveStatePath => currentStateSnapshot.ActivePath;
        public string PendingStateTransitionPath => currentStateSnapshot.PendingTransitionPath;
        internal string DebugStatePath { get => debugStatePath; set => debugStatePath = value ?? string.Empty; }
        internal string DebugPendingTransitionPath { get => debugPendingTransitionPath; set => debugPendingTransitionPath = value ?? string.Empty; }
        internal string LastLoggedStatePath { get => lastLoggedStatePath; set => lastLoggedStatePath = value ?? string.Empty; }
        internal string LastLoggedPendingTransitionPath { get => lastLoggedPendingTransitionPath; set => lastLoggedPendingTransitionPath = value ?? string.Empty; }
        internal string LastLoggedLocomotionPath { get => lastLoggedLocomotionPath; set => lastLoggedLocomotionPath = value ?? string.Empty; }
        internal BasicMovementPhase LastLoggedLocomotionPhase { get => lastLoggedLocomotionPhase; set => lastLoggedLocomotionPhase = value; }
        internal bool LoggedInitialLocomotionState { get => loggedInitialLocomotionState; set => loggedInitialLocomotionState = value; }

        public bool Rebuild(CharacterStateMachineDefinitionSO definitionAsset, bool logErrors)
        {
            stateMachine = null;

            try
            {
                if (definitionAsset == null)
                    throw new InvalidOperationException("Character state machine config is missing. Assign CharacterConfigSO.StateMachine.");

                CharacterStateMachineDefinition definition = definitionAsset.ToDefinition();
                stateMachine = new CharacterStateMachineRunner(definition);
            }
            catch (Exception exception)
            {
                SetInactive();
                if (logErrors)
                    CharacterFrameDiagnostics.LogStateMachineDefinitionInvalid(exception.Message);
                return false;
            }

            currentStateSnapshot = stateMachine.Snapshot;
            CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshot(in currentStateSnapshot);
            debugStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedStatePath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPhase = stateView.LocomotionPhase;
            lastLoggedLocomotionPath = currentStateSnapshot.ActivePath;
            loggedInitialLocomotionState = true;
            return true;
        }

        public void Reset()
        {
            if (stateMachine != null)
                stateMachine.Reset();

            SetInactive();
        }

        public CommittedActionRestoreState CaptureRestoreState()
        {
            if (stateMachine == null)
                return CommittedActionRestoreState.Inactive;

            CommittedActionGameplayRestoreState gameplay = new CommittedActionGameplayRestoreState(
                stateMachine.CaptureRestoreState());
            CommittedActionDiagnosticRestoreState diagnostic = new CommittedActionDiagnosticRestoreState(
                debugStatePath,
                debugPendingTransitionPath,
                lastLoggedStatePath,
                lastLoggedPendingTransitionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                loggedInitialLocomotionState);
            return new CommittedActionRestoreState(gameplay, diagnostic);
        }

        public bool Restore(in CommittedActionRestoreState restoreState)
        {
            if (stateMachine == null || !stateMachine.Restore(restoreState.StateMachine))
                return false;

            currentStateSnapshot = stateMachine.Snapshot;
            CommittedActionDiagnosticRestoreState diagnostic = restoreState.Diagnostic;
            debugStatePath = string.IsNullOrEmpty(diagnostic.DebugStatePath)
                ? currentStateSnapshot.ActivePath
                : diagnostic.DebugStatePath;
            debugPendingTransitionPath = string.IsNullOrEmpty(diagnostic.DebugPendingTransitionPath)
                ? currentStateSnapshot.PendingTransitionPath
                : diagnostic.DebugPendingTransitionPath;
            lastLoggedStatePath = diagnostic.LastLoggedStatePath;
            lastLoggedPendingTransitionPath = diagnostic.LastLoggedPendingTransitionPath;
            lastLoggedLocomotionPath = diagnostic.LastLoggedLocomotionPath;
            lastLoggedLocomotionPhase = diagnostic.LastLoggedLocomotionPhase;
            loggedInitialLocomotionState = diagnostic.LoggedInitialLocomotionState;
            return true;
        }

        void SetInactive()
        {
            currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
            debugStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedStatePath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPath = string.Empty;
            lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
            loggedInitialLocomotionState = true;
        }
    }
}
