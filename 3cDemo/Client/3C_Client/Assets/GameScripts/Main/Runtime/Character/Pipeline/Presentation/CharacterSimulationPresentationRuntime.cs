using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterAnimationStartupPolicy : byte
    {
        RequireCommittedSelection = 1,
        AwaitCommittedSelection = 2
    }

    public sealed class CharacterSimulationPresentationRuntime :
        ICharacterPresentationRuntime,
        ISimulationPresentationOutputPort,
        IAnimationPresentationRuntimeSnapshotProvider
    {
        static readonly ProfilerMarker AnimationMarker = new ProfilerMarker("ThirdPerson.Presentation.Animation");
        static readonly ProfilerMarker PosePostProcessMarker = new ProfilerMarker("ThirdPerson.Presentation.PosePostProcess");

        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterAnimationPlaybackRuntime m_Animation;
        readonly ICharacterMotionMatchingTrajectorySource m_MotionMatchingTrajectorySource;
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterFootPlacementRuntime m_FootPlacement;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly CharacterAnimationStartupPolicy m_AnimationStartupPolicy;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly List<CharacterPresentationCommand> m_CurrentFrameSignals =
            new List<CharacterPresentationCommand>();

        bool m_AnimationStarted;
        bool m_PoseHasOutput;
        ulong m_LastPoseResetSequence;
        ulong m_AnimationBranchReplacementCount;
        bool m_Disposed;
        CharacterPresentationTrajectoryIntent m_LatestTrajectoryIntent;
        bool m_HasTrajectoryIntent;
        ulong m_SelectedTrajectorySequence;

        internal CharacterSimulationPresentationRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterAnimationPlaybackRuntime animation,
            ICharacterMotionMatchingTrajectorySource motionMatchingTrajectorySource,
            CharacterEquipmentVisualRuntime equipment,
            CharacterFootPlacementRuntime footPlacement,
            CharacterCameraPresentationRuntime camera,
            CharacterAnimationStartupPolicy animationStartupPolicy,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Runtime Actor identity is invalid.", nameof(actorId));
            if (animationStartupPolicy != CharacterAnimationStartupPolicy.RequireCommittedSelection &&
                animationStartupPolicy != CharacterAnimationStartupPolicy.AwaitCommittedSelection)
            {
                throw new ArgumentOutOfRangeException(nameof(animationStartupPolicy));
            }
            m_ActorId = actorId;
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            m_Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            m_MotionMatchingTrajectorySource = motionMatchingTrajectorySource;
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            m_FootPlacement = footPlacement ?? throw new ArgumentNullException(nameof(footPlacement));
            m_Camera = camera;
            m_AnimationStartupPolicy = animationStartupPolicy;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (m_Projection.MotionMatching != null)
            {
                bool sourceMatches = m_Body.SourceMode == CharacterBodyPresentationSourceMode.SelectedStream
                    ? motionMatchingTrajectorySource is SelectedBodyMotionMatchingTrajectorySource
                    : motionMatchingTrajectorySource is AcceptedIntentMotionMatchingTrajectorySource;
                if (!sourceMatches)
                    throw new InvalidOperationException("Motion Matching Trajectory Source does not match the Presentation Body source.");
            }
            else if (motionMatchingTrajectorySource != null)
            {
                throw new InvalidOperationException("Presentation without Motion Matching payload cannot allocate a Trajectory Source.");
            }
        }

        public void CaptureBodyInterval(CharacterPresentationBodyInterval interval)
        {
            RequireAlive();
            if (interval.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Body interval targets another Actor.");
            m_Body.Capture(interval);
        }

        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> AnimationSnapshots => m_Animation.Snapshots;
        public bool HasAnimationRuntimeSnapshot => m_Animation.HasRuntimeDiagnosticsSnapshot;
        public AnimationPresentationRuntimeSnapshot AnimationRuntimeSnapshot => m_Animation.RuntimeDiagnosticsSnapshot;
        public bool AcceptsTrajectoryIntent => m_MotionMatchingTrajectorySource is AcceptedIntentMotionMatchingTrajectorySource;
        public ulong BodyResetSequence => m_Body.ResetSequence;

        public bool TryGetAnimationPresentationSnapshot(out AnimationPresentationRuntimeSnapshot snapshot)
        {
            if (m_Disposed || !m_Animation.HasRuntimeDiagnosticsSnapshot)
            {
                snapshot = default;
                return false;
            }
            snapshot = m_Animation.RuntimeDiagnosticsSnapshot;
            return true;
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            m_Equipment.Capture(selections);
        }

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (!(m_MotionMatchingTrajectorySource is AcceptedIntentMotionMatchingTrajectorySource))
                throw new InvalidOperationException("Selected Body Presentation does not accept an Accepted Intent trajectory input.");
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Trajectory Intent targets another Actor.");
            if (m_HasTrajectoryIntent && intent.SourceSequence <= m_LatestTrajectoryIntent.SourceSequence)
                throw new InvalidOperationException("Presentation Trajectory Intent sequence did not advance.");
            m_LatestTrajectoryIntent = intent;
            m_HasTrajectoryIntent = true;
        }

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals)
        {
            RequireAlive();
            m_Body.CaptureTransaction(intervals);
        }

        public void Publish(PresentationCommand command) =>
            Publish(CharacterPresentationCommand.FromFloat32(command));

        public void Publish(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation command targets another Actor.");
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                    if (producer.Kind != CharacterPresentationProducerKind.Cue || producer.Cue == null)
                    {
                        throw new InvalidOperationException(
                            $"Cue command targets invalid Projection producer '{producer.ProgramProducerIdentity}'.");
                    }
                    m_CurrentFrameSignals.Add(command);
                    break;
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    m_CurrentFrameSignals.Add(command);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation retirement targets another Actor.");
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(command.Header.EventId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement)
        {
            RequireAlive();
            if (current.Header.ActorId != m_ActorId || replacement.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation replacement targets another Actor.");
            CharacterPresentationProducerEntry currentProducer = RequireProducer(current.ProducerId);
            CharacterPresentationProducerEntry replacementProducer = RequireProducer(replacement.ProducerId);
            switch (replacement.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_AnimationBranchReplacementCount = checked(m_AnimationBranchReplacementCount + 1);
                    m_Animation.Replace(current, replacement, currentProducer, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(current, currentProducer);
                    RequireCamera().Publish(replacement, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(current.Header.EventId);
                    m_CurrentFrameSignals.Add(replacement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(replacement.Kind), replacement.Kind, null);
            }
        }

        public void Present(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            try
            {
                m_Equipment.Present();
                CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
                if (!bodyFrame.IsValid)
                {
                    ResetPoseIfNeeded(
                        context.RenderFrame,
                        m_LastPoseResetSequence,
                        CharacterPosePostProcessResetReason.MissingAnimationOutput,
                        CharacterBodyPresentationResetReason.Initialization);
                    return;
                }
                if (bodyFrame.ResetSequence != m_LastPoseResetSequence)
                {
                    m_MotionMatchingTrajectorySource?.Reset(bodyFrame.ResetSequence);
                    m_Animation.ResetPoseBranch(bodyFrame.ResetSequence);
                    m_FootPlacement.Reset(new CharacterPosePostProcessReset(
                        m_ActorId,
                        context.RenderFrame,
                        bodyFrame.ResetSequence,
                        CharacterPosePostProcessResetReason.BodyStreamReset,
                        bodyFrame.ResetReason));
                    m_LastPoseResetSequence = bodyFrame.ResetSequence;
                    m_PoseHasOutput = false;
                }
                MotionMatchingTrajectorySourceFrame? trajectoryFrame = PublishMotionMatchingTrajectory(bodyFrame);
                FinalAnimationPoseFrame animationPose = PresentAnimation(
                    bodyFrame,
                    context.RenderFrame,
                    trajectoryFrame,
                    context.ScaledDeltaSeconds);
                PresentPosePostProcess(bodyFrame, context, in animationPose);
                m_Camera?.Present(bodyFrame, context.ScaledDeltaSeconds);
            }
            finally
            {
                m_CurrentFrameSignals.Clear();
            }
        }

        public CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics()
        {
            return new CharacterPresentationRuntimeDiagnosticsSnapshot(
                m_Body.BranchReplacementCount,
                m_AnimationBranchReplacementCount,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees,
                m_FootPlacement.Snapshot);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CurrentFrameSignals.Clear();
            m_Equipment.Reset();
            m_Camera?.Reset();
            m_FootPlacement.Reset(new CharacterPosePostProcessReset(
                m_ActorId,
                0,
                0,
                CharacterPosePostProcessResetReason.PresentationReset,
                CharacterBodyPresentationResetReason.Initialization));
            m_Animation.Reset();
            m_MotionMatchingTrajectorySource?.Reset(0);
            m_Body.Reset();
            m_LatestTrajectoryIntent = default;
            m_HasTrajectoryIntent = false;
            m_SelectedTrajectorySequence = 0;
            m_AnimationStarted = false;
            m_PoseHasOutput = false;
            m_LastPoseResetSequence = 0;
            m_AnimationBranchReplacementCount = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CurrentFrameSignals.Clear();
            m_AnimationStarted = false;
            try
            {
                m_MotionMatchingTrajectorySource?.Dispose();
            }
            finally
            {
                CharacterPresentationModuleLifetime.Dispose(m_Camera, m_FootPlacement, m_Equipment, m_Animation, m_Body);
            }
        }

        FinalAnimationPoseFrame PresentAnimation(
            CharacterBodyPresentationFrame bodyFrame,
            ulong presentationFrame,
            MotionMatchingTrajectorySourceFrame? motionMatchingTrajectory,
            float presentationDeltaSeconds)
        {
            if (m_AnimationStartupPolicy == CharacterAnimationStartupPolicy.AwaitCommittedSelection &&
                !m_AnimationStarted)
            {
                if (!m_Animation.HasRequiredOutput)
                    return default;
                m_AnimationStarted = true;
            }
            using (AnimationMarker.Auto())
            {
                return m_Animation.Present(
                    presentationFrame,
                    bodyFrame.AnimationSampleTick,
                    bodyFrame.AnimationSampleAlpha,
                    presentationDeltaSeconds,
                    bodyFrame.ResetSequence,
                    motionMatchingTrajectory,
                    m_Diagnostics);
            }
        }

        MotionMatchingTrajectorySourceFrame? PublishMotionMatchingTrajectory(CharacterBodyPresentationFrame bodyFrame)
        {
            if (m_MotionMatchingTrajectorySource == null)
                return null;
            if (m_MotionMatchingTrajectorySource is AcceptedIntentMotionMatchingTrajectorySource accepted)
            {
                if (!m_HasTrajectoryIntent)
                    return null;
                if (m_LatestTrajectoryIntent.ResetSequence != bodyFrame.ResetSequence ||
                    m_LatestTrajectoryIntent.CurrentTick.Value > bodyFrame.CurrentTick)
                    throw new InvalidOperationException("Accepted Intent trajectory input does not match the current Body presentation branch.");
                accepted.Publish(
                    m_LatestTrajectoryIntent,
                    bodyFrame.VisiblePosition,
                    bodyFrame.VisibleRotation,
                    new UnityEngine.Vector2(bodyFrame.VisibleVelocity.x, bodyFrame.VisibleVelocity.z));
            }
            else if (m_MotionMatchingTrajectorySource is SelectedBodyMotionMatchingTrajectorySource selected)
            {
                if (m_SelectedTrajectorySequence == ulong.MaxValue)
                    throw new InvalidOperationException("Selected Body trajectory sequence was exhausted.");
                selected.PublishSelectedBody(
                    m_ActorId,
                    new SimulationTick(bodyFrame.CurrentTick),
                    ++m_SelectedTrajectorySequence,
                    bodyFrame.TargetPosition,
                    bodyFrame.TargetRotation,
                    new UnityEngine.Vector2(bodyFrame.TargetVelocity.x, bodyFrame.TargetVelocity.z),
                    bodyFrame.TargetYawVelocityDegreesPerSecond,
                    bodyFrame.TargetGrounded,
                    0f,
                    bodyFrame.ResetSequence);
            }
            return m_MotionMatchingTrajectorySource.TryGetFrame(out MotionMatchingTrajectorySourceFrame frame)
                ? frame
                : (MotionMatchingTrajectorySourceFrame?)null;
        }

        void PresentPosePostProcess(
            CharacterBodyPresentationFrame bodyFrame,
            GameplayPresentationFrameContext context,
            in FinalAnimationPoseFrame animationPose)
        {
            PoseSlotFrameAvailability availability;
            try
            {
                availability = animationPose.Availability;
            }
            catch (InvalidOperationException)
            {
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterPosePostProcessResetReason.MissingAnimationOutput,
                    bodyFrame.ResetReason);
                return;
            }
            if (availability != PoseSlotFrameAvailability.Pose)
            {
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterPosePostProcessResetReason.InvalidPose,
                    bodyFrame.ResetReason);
                return;
            }
            using (PosePostProcessMarker.Auto())
            {
                m_FootPlacement.Present(new CharacterPosePostProcessFrame(
                    m_ActorId,
                    context.RenderFrame,
                    context.ScaledDeltaSeconds,
                    bodyFrame,
                    animationPose));
            }
            m_PoseHasOutput = true;
        }

        void ResetPoseIfNeeded(
            ulong renderFrame,
            ulong resetSequence,
            CharacterPosePostProcessResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            if (!m_PoseHasOutput)
                return;
            m_FootPlacement.Reset(new CharacterPosePostProcessReset(
                m_ActorId,
                renderFrame,
                resetSequence,
                reason,
                bodyReason));
            m_PoseHasOutput = false;
        }

        CharacterPresentationProducerEntry RequireProducer(string producerId)
        {
            if (!m_Projection.TryGetProducer(producerId, out CharacterPresentationProducerEntry producer))
            {
                throw new InvalidOperationException(
                    $"Presentation producer '{producerId}' is absent from the compiled Projection.");
            }
            return producer;
        }

        CharacterCameraPresentationRuntime RequireCamera()
        {
            return m_Camera ?? throw new InvalidOperationException(
                "Camera PresentationCommand targets an Actor without an explicit Camera composition.");
        }

        void RetireSignal(EventId eventId)
        {
            for (int i = m_CurrentFrameSignals.Count - 1; i >= 0; i--)
            {
                if (m_CurrentFrameSignals[i].Header.EventId.Equals(eventId))
                    m_CurrentFrameSignals.RemoveAt(i);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterSimulationPresentationRuntime));
        }
    }
}
