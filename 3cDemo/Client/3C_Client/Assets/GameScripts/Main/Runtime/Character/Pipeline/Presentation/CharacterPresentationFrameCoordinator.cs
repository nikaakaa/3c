using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonGameplay.Tick;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationFrameCoordinator
    {
        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterPresentationFactProjector m_FactProjector;
        readonly CharacterAnimationPresentationRuntime m_Animation;
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterLinkedPoseRuntimeSession m_LinkedPoseSession;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly Transform m_VisualRoot;
        readonly Transform m_PoseRoot;
        readonly RuntimeDiagnosticsContext m_Diagnostics;

        bool m_PoseHasOutput;
        ulong m_LastBodyResetSequence;
        double m_LastAnimationSampleTick;
        bool m_AnimationClockInitialized;
        ulong m_AnimationBranchReplacementCount;
        bool m_ReportedPresentationFailure;
        CharacterPosePlanStageSnapshot m_PosePlanStages;
        GameplayPresentationFrameContext m_PendingPresentationContext;
        CharacterBodyPresentationFrame m_PendingBodyFrame;
        bool m_PresentationFrameActive;
        bool m_PendingAnimationFrame;
        bool m_PendingCameraFrame;
        bool m_PerformanceContextActive;

        internal CharacterPresentationFrameCoordinator(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterPresentationFactProjector factProjector,
            CharacterAnimationPresentationRuntime animation,
            CharacterEquipmentVisualRuntime equipment,
            CharacterLinkedPoseRuntimeSession linkedPoseSession,
            CharacterCameraPresentationRuntime camera,
            Transform poseRoot,
            RuntimeDiagnosticsContext diagnostics)
        {
            m_ActorId = actorId;
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            m_FactProjector = factProjector ?? throw new ArgumentNullException(nameof(factProjector));
            m_Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            m_LinkedPoseSession = linkedPoseSession ?? throw new ArgumentNullException(nameof(linkedPoseSession));
            m_Camera = camera;
            m_VisualRoot = m_Body.VisualRoot;
            m_PoseRoot = poseRoot
                ? poseRoot
                : throw new ArgumentNullException(nameof(poseRoot));
            if (m_PoseRoot.parent != m_VisualRoot)
                throw new InvalidOperationException("PoseRoot must be a direct child of the Presentation VisualRoot.");
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        internal CharacterPosePlanStageSnapshot PosePlanStages => m_PosePlanStages;

        internal void RecordAnimationBranchReplacement()
        {
            m_AnimationBranchReplacementCount = checked(
                m_AnimationBranchReplacementCount + 1);
        }

        internal void Begin(GameplayPresentationFrameContext context)
        {
            if (m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation already has an active frame.");
            }
            m_PendingPresentationContext = context;
            m_PresentationFrameActive = true;
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            PerformanceInstrumentationContextRuntime.BeginActor(
                PerformanceInstrumentationIdentity.Hash64(m_ActorId.Value),
                PerformanceInstrumentationIdentity.Hash64(m_Projection.ProgramId),
                PerformanceInstrumentationIdentity.Hash64(m_Projection.ContractHash));
            m_PerformanceContextActive = true;
            try
            {
                m_Equipment.Present();
                m_PendingBodyFrame = m_Body.Present(context);
                if (!m_PendingBodyFrame.IsValid)
                {
                    m_AnimationClockInitialized = false;
                    ResetPoseIfNeeded(
                        context.RenderFrame,
                        m_LastBodyResetSequence,
                        CharacterFootPlacementResetReason.MissingAnimationOutput,
                        CharacterBodyPresentationResetReason.Initialization);
                    return;
                }
                if (m_PendingBodyFrame.ResetSequence !=
                    m_LastBodyResetSequence)
                {
                    m_AnimationClockInitialized = false;
                    if (m_PendingBodyFrame.ResetReason ==
                        CharacterBodyPresentationResetReason
                            .CommittedBranchReplacement)
                    {
                        m_Animation.RetargetBodyBranch(
                            m_PendingBodyFrame.ResetSequence);
                        m_Animation.RetargetFootPlacement(
                            m_PendingBodyFrame.ResetSequence);
                    }
                    else
                    {
                        m_Animation.ResetPoseBranch(
                            m_PendingBodyFrame.ResetSequence);
                        m_Animation.ResetFootPlacement(
                            new CharacterFootPlacementReset(
                                m_ActorId,
                                context.RenderFrame,
                                m_PendingBodyFrame.ResetSequence,
                                CharacterFootPlacementResetReason
                                    .BodyStreamReset,
                                m_PendingBodyFrame.ResetReason));
                        m_PoseHasOutput = false;
                    }
                    m_LastBodyResetSequence =
                        m_PendingBodyFrame.ResetSequence;
                }
                float animationDeltaSeconds = ResolveAnimationDeltaSeconds(
                    in context,
                    in m_PendingBodyFrame);
                m_PendingCameraFrame = m_Camera != null;
                if (animationDeltaSeconds <= 0f)
                    return;
                CharacterPresentationFactFrame factFrame = m_FactProjector.Project(
                    context.RenderFrame,
                    animationDeltaSeconds,
                    in m_PendingBodyFrame);
                try
                {
                    CharacterPresentationProgramParameterFrame parameterFrame =
                        CharacterPresentationProgramParameterFrame.FromFact(
                            in factFrame);
                    m_Animation.BeginPresentation(
                        context.RenderFrame,
                        m_PendingBodyFrame.AnimationSampleTick,
                        m_PendingBodyFrame.AnimationSampleAlpha,
                        animationDeltaSeconds,
                        in m_PendingBodyFrame,
                        in factFrame,
                        in parameterFrame,
                        m_LinkedPoseSession,
                        m_Diagnostics);
                    m_PendingAnimationFrame = true;
                }
                catch (Exception exception)
                {
                    ReportPresentationFailure(exception);
                    throw;
                }
            }
            catch
            {
                ClearPendingPresentationFrame();
                throw;
            }
        }

        internal bool TryAdvance(
            out CharacterPoseWorkerStageLease workerLease)
        {
            if (!m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation has no active frame.");
            }
            workerLease = default;
            if (!m_PendingAnimationFrame)
                return false;
            try
            {
                return m_Animation.TryAdvancePresentation(out workerLease);
            }
            catch (Exception exception)
            {
                ReportPresentationFailure(exception);
                throw;
            }
        }

        internal void Complete()
        {
            if (!m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation has no active frame.");
            }
            try
            {
                if (m_PendingAnimationFrame)
                {
                    ComposedAnimationPoseFrame animationPose;
                    try
                    {
                        animationPose = m_Animation.CompletePresentation();
                    }
                    catch (Exception exception)
                    {
                        ReportPresentationFailure(exception);
                        throw;
                    }
                    CommitFinalPose(
                        m_PendingBodyFrame,
                        m_PendingPresentationContext,
                        in animationPose);
                }
                if (m_PendingCameraFrame)
                    m_Camera.Present(
                        m_PendingBodyFrame,
                        m_PendingPresentationContext
                            .PresentationDeltaSeconds);
            }
            finally
            {
                ClearPendingPresentationFrame();
            }
        }

        internal Exception Abort()
        {
            if (!m_PresentationFrameActive)
                return null;
            Exception failure = m_PendingAnimationFrame
                ? m_Animation.AbortPresentation()
                : null;
            ClearPendingPresentationFrame();
            return failure;
        }

        internal CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics()
        {
            return new CharacterPresentationRuntimeDiagnosticsSnapshot(
                m_Body.BranchReplacementCount,
                m_AnimationBranchReplacementCount,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees,
                m_PosePlanStages,
                m_Animation.HasRuntimeDiagnosticsSnapshot,
                m_Animation.HasRuntimeDiagnosticsSnapshot
                    ? m_Animation.RuntimeDiagnosticsSnapshot
                    : default);
        }

        internal void Reset()
        {
            if (m_PresentationFrameActive)
                Abort();
            m_Animation.ResetFootPlacement(new CharacterFootPlacementReset(
                m_ActorId,
                0,
                0,
                CharacterFootPlacementResetReason.PresentationReset,
                CharacterBodyPresentationResetReason.Initialization));
            m_Animation.Reset();
            m_Body.Reset();
            m_FactProjector.Reset();
            m_PoseHasOutput = false;
            m_LastBodyResetSequence = 0;
            m_LastAnimationSampleTick = 0d;
            m_AnimationClockInitialized = false;
            m_AnimationBranchReplacementCount = 0;
            m_PosePlanStages = default;
        }

        internal void Dispose()
        {
            if (m_PresentationFrameActive)
                Abort();
            EndPerformanceContext();
        }

        void ClearPendingPresentationFrame()
        {
            EndPerformanceContext();
            m_PendingPresentationContext = default;
            m_PendingBodyFrame = default;
            m_PresentationFrameActive = false;
            m_PendingAnimationFrame = false;
            m_PendingCameraFrame = false;
        }

        void EndPerformanceContext()
        {
            if (!m_PerformanceContextActive)
                return;
            PerformanceInstrumentationContextRuntime.EndActor();
            m_PerformanceContextActive = false;
        }

        void ReportPresentationFailure(Exception exception)
        {
            if (m_ReportedPresentationFailure)
                return;
            m_ReportedPresentationFailure = true;
            Debug.LogError(
                $"Presentation failure Actor={m_ActorId}, Frame={m_PendingPresentationContext.RenderFrame}, " +
                $"BodyTick={m_PendingBodyFrame.PreviousTick}->{m_PendingBodyFrame.CurrentTick}@{m_PendingBodyFrame.SampleAlpha:R}, " +
                $"Visible={m_PendingBodyFrame.VisiblePosition:R}, VisualRoot={m_VisualRoot.position:R}, " +
                $"PoseRoot={m_PoseRoot.position:R}, PoseRootLocal={m_PoseRoot.localPosition:R}, " +
                $"CameraSkipped={m_Camera != null}, Error={exception.Message}");
        }

        float ResolveAnimationDeltaSeconds(
            in GameplayPresentationFrameContext context,
            in CharacterBodyPresentationFrame bodyFrame)
        {
            if (m_Body.SourceMode != CharacterBodyPresentationSourceMode.CommittedStream)
                return context.PresentationDeltaSeconds;
            double sampleTick = (double)bodyFrame.PreviousTick +
                                ((double)bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                (double)bodyFrame.SampleAlpha;
            if (!m_AnimationClockInitialized)
            {
                m_LastAnimationSampleTick = sampleTick;
                m_AnimationClockInitialized = true;
                return m_Body.TickDurationSeconds;
            }
            double deltaTicks = sampleTick - m_LastAnimationSampleTick;
            if (deltaTicks < -0.000001d)
                throw new InvalidOperationException("Animation presentation sample clock cannot move backward.");
            m_LastAnimationSampleTick = sampleTick;
            double deltaSeconds = Math.Max(0d, deltaTicks) * m_Body.TickDurationSeconds;
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds > float.MaxValue)
                throw new InvalidOperationException("Animation logic delta is invalid.");
            return (float)deltaSeconds;
        }

        [PerformanceProbe("presentation.final-pose")]
        void CommitFinalPose(
            CharacterBodyPresentationFrame bodyFrame,
            GameplayPresentationFrameContext context,
            in ComposedAnimationPoseFrame animationPose)
        {
            AnimationPoseAvailability availability;
            try
            {
                availability = animationPose.Availability;
            }
            catch (InvalidOperationException)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        AnimationPoseAvailability.Invalid,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.MissingAnimationOutput,
                    bodyFrame.ResetReason);
                return;
            }
            if (availability != AnimationPoseAvailability.Pose)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        availability,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.InvalidPose,
                    bodyFrame.ResetReason);
                return;
            }
            m_PosePlanStages = ShouldCapturePosePlanStages
                ? CharacterPosePlanStageSnapshotFactory.Completed(
                    m_Projection.PosePlan,
                    in animationPose)
                : default;
            m_PoseHasOutput = true;
        }

        bool ShouldCapturePosePlanStages =>
            (m_Animation.DiagnosticsInterest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        void ResetPoseIfNeeded(
            ulong renderFrame,
            ulong resetSequence,
            CharacterFootPlacementResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            if (!m_PoseHasOutput)
                return;
            m_Animation.ResetFootPlacement(new CharacterFootPlacementReset(
                m_ActorId,
                renderFrame,
                resetSequence,
                reason,
                bodyReason));
            m_PoseHasOutput = false;
        }
    }
}
