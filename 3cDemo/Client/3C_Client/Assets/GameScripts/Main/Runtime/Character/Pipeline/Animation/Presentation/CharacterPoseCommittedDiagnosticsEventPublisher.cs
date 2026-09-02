using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed partial class
        CharacterPoseCommittedDiagnosticsEventPublisher
    {
        readonly Guid m_RuntimeInstanceId;

        internal CharacterPoseCommittedDiagnosticsEventPublisher(
            Guid runtimeInstanceId)
        {
            m_RuntimeInstanceId = runtimeInstanceId != Guid.Empty
                ? runtimeInstanceId
                : throw new ArgumentException(
                    "Animation Presentation runtime identity is invalid.",
                    nameof(runtimeInstanceId));
        }

        internal bool HasFootCaptureInterest
        {
            get
            {
                bool interested = false;
                QueryFootDiagnosticEventInterest(ref interested);
                return interested;
            }
        }

        internal void PublishCommittedFootDiagnosticEvent(
            in CharacterPoseFrameLineage frame,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterFinalPosePublicationResult publicationResult,
            CharacterPoseProgramRuntime program,
            CharacterPoseConstraintRuntime constraints,
            CharacterFinalPosePublication publication) =>
            PublishFootDiagnosticEvent(
                in frame,
                in constraintResult,
                in publicationResult,
                program,
                constraints,
                publication);

        partial void QueryFootDiagnosticEventInterest(
            ref bool interested);

        partial void PublishFootDiagnosticEvent(
            in CharacterPoseFrameLineage frame,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterFinalPosePublicationResult publicationResult,
            CharacterPoseProgramRuntime program,
            CharacterPoseConstraintRuntime constraints,
            CharacterFinalPosePublication publication);
    }
}
