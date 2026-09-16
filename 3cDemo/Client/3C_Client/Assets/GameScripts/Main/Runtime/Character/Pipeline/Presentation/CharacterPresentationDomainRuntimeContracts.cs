using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterPresentationDomainDiagnosticsSnapshot
    {
        public CharacterPresentationDomainDiagnosticsSnapshot(
            int bodyBranchReplacementCount,
            int animationBranchReplacementCount,
            float followerPositionCorrectionMeters,
            float followerYawCorrectionDegrees)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
        }

        public int BodyBranchReplacementCount { get; }
        public int AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
    }

    public interface ICharacterPresentationDomainRuntime :
        IDisposable,
        IGameplayPresentationFrameTarget
    {
        bool AcceptsTrajectoryIntent { get; }
        ulong BodyResetSequence { get; }
        bool TryGetLatestBody(out CharacterPresentationBodyState body);
        void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals);
        void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent);
        void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections);
        void Publish(CharacterPresentationCommand command);
        void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement);
        void Retire(CharacterPresentationCommand command);
        void Reset();
        CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics();
        bool SupportsCheckpointCapture { get; }
        bool SupportsCheckpointRestore { get; }
        bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
        bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
    }
}
