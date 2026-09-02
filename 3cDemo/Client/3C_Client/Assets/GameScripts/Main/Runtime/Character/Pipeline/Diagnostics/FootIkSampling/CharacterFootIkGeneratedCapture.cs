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
            in CharacterFootIkCommittedCaptureViewLease view)
        {
            CharacterPoseFrameLineage frame = view.Lineage;
            var lineage = new DiagnosticLineageKey(
                CharacterFootIkDiagnosticIdentity.LineageTypeIdentity,
                frame.PresentationFrame,
                frame.CompletionIdentity);
            ref readonly CharacterFootLandingPredictionDiagnostics landing =
                ref view.LandingPrediction;
            CharacterFootLandingPredictionFootDiagnostics leftFoot =
                landing.Left;
            CharacterFootLandingPredictionFootDiagnostics rightFoot =
                landing.Right;
            CharacterFootLandingPredictionInputDiagnostics input =
                landing.Input;
            CharacterFootStepObservationInputDiagnostics formalInput =
                input.FootStepObservation;
            AnimationFootMotionRuntimeSample leftFormalInput = formalInput.Left;
            AnimationFootMotionRuntimeSample rightFormalInput = formalInput.Right;
            AnimationFootStepObservationRuntimeSnapshot formalOutput =
                view.FootStepObservation;
            AnimationFootMotionRuntimeSample leftFormalOutput = formalOutput.Left;
            AnimationFootMotionRuntimeSample rightFormalOutput = formalOutput.Right;
            AnimationBiomechanicalStepReadPage leftFootSteps = view.LeftFootSteps;
            AnimationBiomechanicalStepReadPage rightFootSteps = view.RightFootSteps;
            CharacterFullBodyIkGoal pelvisGoal = landing.PelvisGoal;
            CharacterFootPrimarySupportDiagnostics primarySupport =
                landing.PrimarySupport;
            CharacterFootStrideHipsDiagnostics stride = landing.StrideHips;
            ref readonly CharacterFullBodyIkEffectorDiagnostics leftEffector =
                ref view.LeftFoot;
            ref readonly CharacterFullBodyIkEffectorDiagnostics rightEffector =
                ref view.RightFoot;
            ref readonly CharacterFullBodyIkLimbDiagnostics leftLeg =
                ref view.LeftLeg;
            ref readonly CharacterFullBodyIkLimbDiagnostics rightLeg =
                ref view.RightLeg;
            ref readonly CharacterFullBodyIkEffectorDiagnostics pelvis =
                ref view.Pelvis;
            ref readonly CharacterFullBodyIkSolverDiagnostics solver =
                ref view.Solver;
            return m_Lifecycle.HandleCommitted(
                in lineage,
                in leftEffector,
                in leftFoot,
                in leftFootSteps,
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
                in rightFootSteps,
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
