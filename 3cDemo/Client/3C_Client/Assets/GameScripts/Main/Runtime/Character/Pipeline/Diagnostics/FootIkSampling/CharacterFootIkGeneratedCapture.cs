using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    public sealed class CharacterFootIkGeneratedCapture :
        ICharacterFootIkCommittedCaptureConsumer,
        IDisposable
    {
        readonly CharacterFootIkFullCaptureProgram.DiagnosticLifecycle m_Lifecycle;
        readonly CharacterFootIkCaptureMetadata m_Metadata;

        public CharacterFootIkGeneratedCapture(
            in CharacterFootIkCaptureMetadata metadata)
        {
            m_Metadata = metadata;
            m_Lifecycle =
                CharacterFootIkFullCaptureProgram.CreateDiagnosticLifecycle();
        }

        public DiagnosticCaptureFailure? Failure => m_Lifecycle.Failure;

        public bool Start(DiagnosticCaptureStartRequest request) =>
            m_Lifecycle.Start(request);

        public bool Stop(in DiagnosticCaptureStopOutcome outcome) =>
            m_Lifecycle.Stop(in outcome);

        public bool TryGetRuntimeManifest(
            out DiagnosticRuntimeManifest manifest,
            out DiagnosticSealedArtifact artifact) =>
            m_Lifecycle.TryGetRuntimeManifest(out manifest, out artifact);

        public bool TryCapture(
            in CharacterPoseFrameLineage frame,
            in CharacterFullBodyIkEffectorDiagnostics leftEffector,
            in CharacterFootLandingPredictionFootDiagnostics leftFoot,
            in AnimationFootMotionRuntimeSample leftFormalInput,
            in AnimationFootMotionRuntimeSample leftFormalOutput,
            in CharacterFullBodyIkLimbDiagnostics leftLeg,
            in CharacterFullBodyIkEffectorDiagnostics rightEffector,
            in CharacterFootLandingPredictionFootDiagnostics rightFoot,
            in AnimationFootMotionRuntimeSample rightFormalInput,
            in AnimationFootMotionRuntimeSample rightFormalOutput,
            in CharacterFullBodyIkLimbDiagnostics rightLeg,
            in CharacterFootLandingPredictionInputDiagnostics input,
            in CharacterFullBodyIkEffectorDiagnostics pelvis,
            in CharacterFullBodyIkGoal pelvisGoal,
            in CharacterFootPrimarySupportDiagnostics primarySupport,
            in CharacterFullBodyIkSolverDiagnostics solver,
            in CharacterFootStrideHipsDiagnostics stride)
        {
            var lineage = new DiagnosticLineageKey(
                CharacterFootIkDiagnosticIdentity.LineageTypeIdentity,
                frame.PresentationFrame,
                frame.CompletionIdentity);
            return m_Lifecycle.HandleCommitted(
                in lineage,
                in leftEffector,
                in leftFoot,
                in leftFormalInput,
                in leftFormalOutput,
                in frame,
                in input,
                in leftLeg,
                in pelvis,
                in pelvisGoal,
                in primarySupport,
                in solver,
                in stride,
                in m_Metadata,
                in rightEffector,
                in rightFoot,
                in rightFormalInput,
                in rightFormalOutput,
                in frame,
                in input,
                in rightLeg,
                in pelvis,
                in pelvisGoal,
                in primarySupport,
                in solver,
                in stride,
                in m_Metadata);
        }

        public void CaptureFault(Exception failure)
        {
            var diagnosticFailure = new DiagnosticCaptureFailure(
                DiagnosticCaptureFailureStage.Capture,
                CharacterFootIkDiagnosticIdentity.CapabilityId,
                "character-foot-ik/full-program",
                "character-foot-ik/full",
                string.Empty,
                failure?.Message ?? "Foot IK capture failed.");
            DiagnosticCaptureStopOutcome outcome =
                DiagnosticCaptureStopOutcome.Faulted(in diagnosticFailure);
            m_Lifecycle.Stop(in outcome);
        }

        public void Dispose() => m_Lifecycle.Dispose();
    }
}
