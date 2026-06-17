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
        string debugFullBodyStatePath = CharacterStateMachineSnapshot.Inactive.ActivePath;
        string debugPendingTransitionPath = CharacterStateMachineSnapshot.Inactive.PendingTransitionPath;
        string lastLoggedFullBodyPath = CharacterStateMachineSnapshot.Inactive.ActivePath;
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
        public string ActiveFullBodyStatePath => currentStateSnapshot.ActivePath;
        public string PendingFullBodyTransitionPath => currentStateSnapshot.PendingTransitionPath;
        internal string DebugFullBodyStatePath { get => debugFullBodyStatePath; set => debugFullBodyStatePath = value ?? string.Empty; }
        internal string DebugPendingTransitionPath { get => debugPendingTransitionPath; set => debugPendingTransitionPath = value ?? string.Empty; }
        internal string LastLoggedFullBodyPath { get => lastLoggedFullBodyPath; set => lastLoggedFullBodyPath = value ?? string.Empty; }
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
                    FullBodyDiagnostics.LogStateMachineDefinitionInvalid(exception.Message);
                return false;
            }

            currentStateSnapshot = stateMachine.Snapshot;
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in currentStateSnapshot);
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedFullBodyPath = currentStateSnapshot.ActivePath;
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

        public FullBodyActionRestoreState CaptureRestoreState()
        {
            if (stateMachine == null)
                return FullBodyActionRestoreState.Inactive;

            FullBodyActionGameplayRestoreState gameplay = new FullBodyActionGameplayRestoreState(
                stateMachine.CaptureRestoreState());
            FullBodyActionDiagnosticRestoreState diagnostic = new FullBodyActionDiagnosticRestoreState(
                debugFullBodyStatePath,
                debugPendingTransitionPath,
                lastLoggedFullBodyPath,
                lastLoggedPendingTransitionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                loggedInitialLocomotionState);
            return new FullBodyActionRestoreState(gameplay, diagnostic);
        }

        public bool Restore(in FullBodyActionRestoreState restoreState)
        {
            if (stateMachine == null || !stateMachine.Restore(restoreState.StateMachine))
                return false;

            currentStateSnapshot = stateMachine.Snapshot;
            FullBodyActionDiagnosticRestoreState diagnostic = restoreState.Diagnostic;
            debugFullBodyStatePath = string.IsNullOrEmpty(diagnostic.DebugFullBodyStatePath)
                ? currentStateSnapshot.ActivePath
                : diagnostic.DebugFullBodyStatePath;
            debugPendingTransitionPath = string.IsNullOrEmpty(diagnostic.DebugPendingTransitionPath)
                ? currentStateSnapshot.PendingTransitionPath
                : diagnostic.DebugPendingTransitionPath;
            lastLoggedFullBodyPath = diagnostic.LastLoggedFullBodyPath;
            lastLoggedPendingTransitionPath = diagnostic.LastLoggedPendingTransitionPath;
            lastLoggedLocomotionPath = diagnostic.LastLoggedLocomotionPath;
            lastLoggedLocomotionPhase = diagnostic.LastLoggedLocomotionPhase;
            loggedInitialLocomotionState = diagnostic.LoggedInitialLocomotionState;
            return true;
        }

        void SetInactive()
        {
            currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedFullBodyPath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPath = string.Empty;
            lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
            loggedInitialLocomotionState = true;
        }
    }
}
