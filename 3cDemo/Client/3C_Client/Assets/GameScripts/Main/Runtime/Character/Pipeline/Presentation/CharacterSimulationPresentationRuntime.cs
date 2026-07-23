using System;
using System.Collections.Generic;
using System.Text;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using Unity.Profiling;
using UnityEngine;

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
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterFootPlacementRuntime m_FootPlacement;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly Transform m_VisualRoot;
        readonly Transform m_AnimationRoot;
        readonly Transform m_Hips;
        readonly Guid m_PoseDiagnosticOwnerId = Guid.NewGuid();
        readonly CharacterAnimationStartupPolicy m_AnimationStartupPolicy;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly List<CharacterPresentationCommand> m_CurrentFrameSignals =
            new List<CharacterPresentationCommand>();

        bool m_AnimationStarted;
        bool m_PoseHasOutput;
        ulong m_LastBodyResetSequence;
        ulong m_AnimationBranchReplacementCount;
        int m_SpatialDiagnosticCount;
        bool m_ReportedPresentationFailure;
        bool m_ReportedVisualRootMutation;
        bool m_HasAnimationDiagnosticState;
        AnimationPoseAvailability m_LastAnimationDiagnosticAvailability;
        AnimationPoseNativeInvalidReason m_LastAnimationDiagnosticReason;
        int m_LastAnimationDiagnosticOperationIndex = -1;
        string m_LastAnimationDiagnosticLifecycle = string.Empty;
        ulong m_LastAnimationDiagnosticLifecycleFrame;
        bool m_AnimationDiagnosticProgressPending;
        FinalAnimationPoseFrame m_LastFinalPose;
        CharacterPosePlanStageSnapshot m_PosePlanStages;
        bool m_Disposed;

        internal CharacterSimulationPresentationRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterAnimationPlaybackRuntime animation,
            CharacterEquipmentVisualRuntime equipment,
            CharacterFootPlacementRuntime footPlacement,
            CharacterCameraPresentationRuntime camera,
            Transform animationRoot,
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
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            bool requiresFootPlacement = projection.PosePlan.FootPlacementNodes.Count == 1;
            if (requiresFootPlacement != (footPlacement != null))
                throw new InvalidOperationException("Foot Placement runtime must match the compiled Pose Graph node exactly.");
            m_FootPlacement = footPlacement;
            m_Camera = camera;
            m_VisualRoot = m_Body.VisualRoot;
            m_AnimationRoot = animationRoot
                ? animationRoot
                : throw new ArgumentNullException(nameof(animationRoot));
            if (m_AnimationRoot == m_VisualRoot || !m_AnimationRoot.IsChildOf(m_VisualRoot))
                throw new InvalidOperationException("Animation Root must be a strict child of the Presentation VisualRoot.");
            Animator animator = m_AnimationRoot.GetComponent<Animator>();
            m_Hips = animator && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Hips)
                : null;
            m_Animation.SetPoseWatchInterests(
                m_PoseDiagnosticOwnerId,
                BuildPoseDiagnosticInterests(projection.PosePlan));
            m_AnimationStartupPolicy = animationStartupPolicy;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
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
        public bool AcceptsTrajectoryIntent => m_Animation.AcceptsMotionMatchingTrajectoryIntent;
        public bool MotionMatchingRuntimeEnabled => m_Animation.MotionMatchingRuntimeEnabled;
        public IReadOnlyList<AnimationMarkerSyncRelationSnapshot> MarkerSyncSnapshots => m_Animation.MarkerSyncSnapshots;
        public IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> MarkerSyncPlaybackSnapshots => m_Animation.MarkerSyncPlaybackSnapshots;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public CharacterPosePlanStageSnapshot PosePlanStages => m_PosePlanStages;

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

        public bool TryGetPosePlanStages(out CharacterPosePlanStageSnapshot snapshot)
        {
            snapshot = m_PosePlanStages;
            return !m_Disposed && snapshot.IsValid;
        }

        public bool TryCaptureMotionMatchingSearchReplay(
            string programProducerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            if (m_Disposed)
            {
                artifact = null;
                return false;
            }
            return m_Animation.TryCaptureMotionMatchingSearchReplay(programProducerId, out artifact);
        }

        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            m_Animation.SetPoseWatchInterests(ownerId, interests);
        }

        public void RemovePoseWatchInterests(Guid ownerId)
        {
            if (!m_Disposed)
                m_Animation.RemovePoseWatchInterests(ownerId);
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            m_Equipment.Capture(selections);
        }

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Trajectory Intent targets another Actor.");
            m_Animation.CaptureMotionMatchingTrajectoryIntent(intent);
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
                        m_LastBodyResetSequence,
                        CharacterPosePostProcessResetReason.MissingAnimationOutput,
                        CharacterBodyPresentationResetReason.Initialization);
                    return;
                }
                if (bodyFrame.ResetSequence != m_LastBodyResetSequence)
                {
                    if (bodyFrame.ResetReason == CharacterBodyPresentationResetReason.CommittedBranchReplacement)
                        m_Animation.RetargetBodyBranch(bodyFrame.ResetSequence);
                    else
                        m_Animation.ResetPoseBranch(bodyFrame.ResetSequence);
                    m_FootPlacement?.Reset(new CharacterPosePostProcessReset(
                        m_ActorId,
                        context.RenderFrame,
                        bodyFrame.ResetSequence,
                        CharacterPosePostProcessResetReason.BodyStreamReset,
                        bodyFrame.ResetReason));
                    m_LastBodyResetSequence = bodyFrame.ResetSequence;
                    m_PoseHasOutput = false;
                }
                Vector3 rootBeforeAnimation = m_VisualRoot.position;
                Vector3 animationRootBefore = m_AnimationRoot.position;
                Vector3 animationRootLocalBefore = m_AnimationRoot.localPosition;
                ComposedAnimationPoseFrame animationPose;
                try
                {
                    animationPose = PresentAnimation(
                        bodyFrame,
                        context.RenderFrame,
                        context.ScaledDeltaSeconds);
                }
                catch (Exception exception)
                {
                    if (!m_ReportedPresentationFailure)
                    {
                        m_ReportedPresentationFailure = true;
                        Debug.LogError(
                            $"Presentation failure Actor={m_ActorId}, Frame={context.RenderFrame}, " +
                            $"BodyTick={bodyFrame.PreviousTick}->{bodyFrame.CurrentTick}@{bodyFrame.SampleAlpha:R}, " +
                            $"Visible={bodyFrame.VisiblePosition:R}, VisualRoot={m_VisualRoot.position:R}, " +
                            $"AnimationRoot={m_AnimationRoot.position:R}, AnimationRootLocal={m_AnimationRoot.localPosition:R}, " +
                            $"HipsWorld={(m_Hips ? m_Hips.position.ToString("R") : "Missing")}, " +
                            $"CameraSkipped={m_Camera != null}, Error={exception.Message}");
                    }
                    throw;
                }
                Vector3 rootAfterAnimation = m_VisualRoot.position;
                Vector3 animationRootAfter = m_AnimationRoot.position;
                Vector3 animationRootLocalAfter = m_AnimationRoot.localPosition;
                ReportAnimationPoseDiagnostic(context.RenderFrame);
                if (!m_ReportedVisualRootMutation &&
                    (rootAfterAnimation - rootBeforeAnimation).sqrMagnitude > 0.000001f)
                {
                    m_ReportedVisualRootMutation = true;
                    Debug.LogError(
                        $"Animation mutated VisualRoot Actor={m_ActorId}, Frame={context.RenderFrame}, " +
                        $"Before={rootBeforeAnimation:R}, After={rootAfterAnimation:R}, Visible={bodyFrame.VisiblePosition:R}.");
                }
                if (m_SpatialDiagnosticCount < 8 &&
                    bodyFrame.VisibleTranslationDelta.sqrMagnitude > 0.000001f &&
                    context.RenderFrame % 30UL == 0)
                {
                    m_SpatialDiagnosticCount++;
                    Debug.Log(
                        $"Presentation spatial Actor={m_ActorId}, Frame={context.RenderFrame}, " +
                        $"BodyTick={bodyFrame.PreviousTick}->{bodyFrame.CurrentTick}@{bodyFrame.SampleAlpha:R}, " +
                        $"Visible={bodyFrame.VisiblePosition:R}, RootBefore={rootBeforeAnimation:R}, " +
                        $"RootAfter={rootAfterAnimation:R}, HipsWorld={(m_Hips ? m_Hips.position.ToString("R") : "Missing")}, " +
                        $"HipsLocal={(m_Hips ? m_Hips.localPosition.ToString("R") : "Missing")}, " +
                        $"AnimationRootBefore={animationRootBefore:R}, AnimationRootAfter={animationRootAfter:R}, " +
                        $"AnimationRootLocalBefore={animationRootLocalBefore:R}, AnimationRootLocalAfter={animationRootLocalAfter:R}, " +
                        $"Pose={animationPose.Availability}, CameraScheduled={m_Camera != null}.");
                }
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
                m_FootPlacement?.Snapshot ?? default,
                m_PosePlanStages);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CurrentFrameSignals.Clear();
            m_Equipment.Reset();
            m_Camera?.Reset();
            m_FootPlacement?.Reset(new CharacterPosePostProcessReset(
                m_ActorId,
                0,
                0,
                CharacterPosePostProcessResetReason.PresentationReset,
                CharacterBodyPresentationResetReason.Initialization));
            m_Animation.Reset();
            m_Body.Reset();
            m_AnimationStarted = false;
            m_PoseHasOutput = false;
            m_LastBodyResetSequence = 0;
            m_AnimationBranchReplacementCount = 0;
            m_HasAnimationDiagnosticState = false;
            m_LastAnimationDiagnosticAvailability = default;
            m_LastAnimationDiagnosticReason = default;
            m_LastAnimationDiagnosticOperationIndex = -1;
            m_LastAnimationDiagnosticLifecycle = string.Empty;
            m_LastAnimationDiagnosticLifecycleFrame = 0;
            m_AnimationDiagnosticProgressPending = false;
            m_LastFinalPose = default;
            m_PosePlanStages = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CurrentFrameSignals.Clear();
            m_AnimationStarted = false;
            m_Animation.RemovePoseWatchInterests(m_PoseDiagnosticOwnerId);
            CharacterPresentationModuleLifetime.Dispose(m_Camera, m_FootPlacement, m_Equipment, m_Animation, m_Body);
        }

        ComposedAnimationPoseFrame PresentAnimation(
            CharacterBodyPresentationFrame bodyFrame,
            ulong presentationFrame,
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
                    in bodyFrame,
                    m_Diagnostics);
            }
        }

        void ReportAnimationPoseDiagnostic(ulong presentationFrame)
        {
            if (!m_Animation.HasRuntimeDiagnosticsSnapshot)
                return;
            AnimationPresentationRuntimeSnapshot snapshot = m_Animation.RuntimeDiagnosticsSnapshot;

            var lifecycleDetail = new StringBuilder();
            var lifecycleIdentity = new StringBuilder();
            AnimationReadOnlyBuffer<AnimationPlaybackLifecycleSnapshot> lifecycle = snapshot.Lifecycle;
            for (int i = 0; i < lifecycle.Count; i++)
            {
                if (i > 0)
                {
                    lifecycleDetail.Append(" | ");
                    lifecycleIdentity.Append('|');
                }
                AnimationPlaybackLifecycleSnapshot playback = lifecycle[i];
                lifecycleIdentity
                    .Append(playback.AnimationChannelId)
                    .Append('/')
                    .Append(playback.Phase)
                    .Append('/')
                    .Append(playback.Availability)
                    .Append('/')
                    .Append(playback.PoseNodeId)
                    .Append('/')
                    .Append(playback.PlaybackId)
                    .Append('/')
                    .Append(playback.SourceId)
                    .Append('/')
                    .Append(playback.HasVisualSample);
                lifecycleDetail
                    .Append(playback.AnimationChannelId)
                    .Append(':')
                    .Append(playback.Phase)
                    .Append('/')
                    .Append(playback.Availability)
                    .Append(", Player=")
                    .Append(playback.PoseNodeId)
                    .Append(", Playback=")
                    .Append(playback.PlaybackId)
                    .Append(", Source=")
                    .Append(playback.SourceId)
                    .Append(", Sample=")
                    .Append(playback.HasVisualSample)
                    .Append(", Time=")
                    .Append(playback.SampleTime.ToString("R"))
                    .Append(", Scale=")
                    .Append(playback.VisualTimeScale.ToString("R"))
                    .Append(", Weight=")
                    .Append(playback.OutputWeight.ToString("R"));
            }
            if (lifecycleDetail.Length == 0)
            {
                lifecycleDetail.Append("None");
                lifecycleIdentity.Append("None");
            }
            string lifecycleSignature = lifecycleIdentity.ToString();

            bool lifecycleChanged =
                !string.Equals(lifecycleSignature, m_LastAnimationDiagnosticLifecycle, StringComparison.Ordinal);
            bool progressDue =
                m_AnimationDiagnosticProgressPending &&
                presentationFrame >= m_LastAnimationDiagnosticLifecycleFrame &&
                presentationFrame - m_LastAnimationDiagnosticLifecycleFrame >= 30UL;
            bool changed = !m_HasAnimationDiagnosticState ||
                           snapshot.FinalAvailability != m_LastAnimationDiagnosticAvailability ||
                           snapshot.FinalInvalidReason != m_LastAnimationDiagnosticReason ||
                           snapshot.InvalidOperationIndex != m_LastAnimationDiagnosticOperationIndex ||
                           lifecycleChanged ||
                           progressDue;
            if (!changed)
                return;

            m_HasAnimationDiagnosticState = true;
            m_LastAnimationDiagnosticAvailability = snapshot.FinalAvailability;
            m_LastAnimationDiagnosticReason = snapshot.FinalInvalidReason;
            m_LastAnimationDiagnosticOperationIndex = snapshot.InvalidOperationIndex;
            if (lifecycleChanged)
            {
                m_LastAnimationDiagnosticLifecycle = lifecycleSignature;
                m_LastAnimationDiagnosticLifecycleFrame = presentationFrame;
                m_AnimationDiagnosticProgressPending = true;
            }
            else if (progressDue)
            {
                m_AnimationDiagnosticProgressPending = false;
            }

            AnimationPoseOperationSnapshot invalidOperation = default;
            bool hasInvalidOperation = false;
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations = snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.OperationIndex != snapshot.InvalidOperationIndex)
                    continue;
                invalidOperation = operation;
                hasInvalidOperation = true;
                break;
            }

            string operationDetail = hasInvalidOperation
                ? $"{invalidOperation.OperationIndex}:{invalidOperation.Code}@{invalidOperation.NodeId}" +
                  $"/{invalidOperation.Availability}/{invalidOperation.InvalidReason}"
                : "None";
            string inertialDetail = FormatInertializationDiagnostics(snapshot);
            string poseDetail = FormatPoseDiagnostics(snapshot);
            string message =
                $"Animation Pose diagnostic Actor={m_ActorId}, Frame={presentationFrame}, " +
                $"Completion={snapshot.CompletionIdentity}, Final={snapshot.FinalAvailability}/{snapshot.FinalInvalidReason}, " +
                $"InvalidOperation={operationDetail}, Inertial=[{inertialDetail}], PoseWatch=[{poseDetail}], " +
                $"HipsLocal={(m_Hips ? $"{m_Hips.localPosition:R}/{m_Hips.localRotation:R}" : "Missing")}, " +
                $"Lifecycle=[{lifecycleDetail}].";
            if (snapshot.FinalAvailability == AnimationPoseAvailability.Invalid)
                Debug.LogWarning(message);
            else
                Debug.Log(message);
        }

        static IReadOnlyList<AnimationPoseWatchIdentity> BuildPoseDiagnosticInterests(
            CharacterPresentationPosePlan plan)
        {
            var interests = new List<AnimationPoseWatchIdentity>(3);
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                    operation.Code != CharacterPoseOperationCode.Inertialization &&
                    operation.Code != CharacterPoseOperationCode.OutputPose)
                {
                    continue;
                }
                CharacterPresentationPoseSourceMapEntry source = plan.SourceMap[i];
                interests.Add(new AnimationPoseWatchIdentity(
                    source.GraphId,
                    plan.ContentRevision,
                    operation.NodeId,
                    source.CallSite));
            }
            return interests;
        }

        static string FormatInertializationDiagnostics(AnimationPresentationRuntimeSnapshot snapshot)
        {
            AnimationReadOnlyBuffer<PoseInertializationSnapshot> values = snapshot.Inertializations;
            if (values.Count == 0)
                return "None";
            var detail = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    detail.Append(" | ");
                PoseInertializationSnapshot value = values[i];
                detail
                    .Append(value.NodeId)
                    .Append(':')
                    .Append(value.State)
                    .Append(", Event=")
                    .Append(value.EventIdentity)
                    .Append(", Elapsed=")
                    .Append(value.ElapsedSeconds.ToString("R"))
                    .Append('/')
                    .Append(value.DurationSeconds.ToString("R"))
                    .Append(", Acc=")
                    .Append(value.AccumulatorGeneration)
                    .Append(", Continuity=")
                    .Append(value.PreviousContinuityIdentity)
                    .Append("->")
                    .Append(value.CurrentContinuityIdentity);
            }
            return detail.ToString();
        }

        static string FormatPoseDiagnostics(AnimationPresentationRuntimeSnapshot snapshot)
        {
            AnimationReadOnlyBuffer<AnimationPoseWatchSnapshot> watches = snapshot.PoseWatches;
            if (watches.Count == 0)
                return "None";
            var detail = new StringBuilder();
            for (int i = 0; i < watches.Count; i++)
            {
                if (i > 0)
                    detail.Append(" | ");
                AnimationPoseWatchSnapshot watch = watches[i];
                detail
                    .Append(watch.OperationIndex)
                    .Append(':')
                    .Append(watch.Identity.NodeId)
                    .Append('/')
                    .Append(watch.Availability);
                if (watch.Availability == AnimationPoseWatchAvailability.Pose)
                    detail.Append(", Hash=").Append(HashPose(snapshot.GetPoseWatchLocalPoses(i)).ToString("X8"));
            }
            return detail.ToString();
        }

        static uint HashPose(AnimationReadOnlyBuffer<AnimationLocalBonePose> pose)
        {
            uint hash = 2166136261;
            for (int i = 0; i < pose.Count; i++)
            {
                AnimationLocalBonePose bone = pose[i];
                hash = HashFloat(hash, bone.Position.x);
                hash = HashFloat(hash, bone.Position.y);
                hash = HashFloat(hash, bone.Position.z);
                hash = HashFloat(hash, bone.Rotation.x);
                hash = HashFloat(hash, bone.Rotation.y);
                hash = HashFloat(hash, bone.Rotation.z);
                hash = HashFloat(hash, bone.Rotation.w);
            }
            return hash;
        }

        static uint HashFloat(uint hash, float value)
        {
            unchecked
            {
                int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
                hash ^= (uint)bits;
                return hash * 16777619;
            }
        }

        void PresentPosePostProcess(
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
                m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Unavailable(
                    m_Projection.PosePlan,
                    AnimationPoseAvailability.Invalid,
                    0,
                    CharacterPosePlanPhaseUnavailableReason.ComposedPoseUnavailable);
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterPosePostProcessResetReason.MissingAnimationOutput,
                    bodyFrame.ResetReason);
                return;
            }
            if (availability != AnimationPoseAvailability.Pose)
            {
                m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Unavailable(
                    m_Projection.PosePlan,
                    availability,
                    animationPose.CompletionIdentity,
                    CharacterPosePlanPhaseUnavailableReason.ComposedPoseUnavailable);
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterPosePostProcessResetReason.InvalidPose,
                    bodyFrame.ResetReason);
                return;
            }
            using (PosePostProcessMarker.Auto())
            {
                if (m_FootPlacement != null)
                    m_FootPlacement.Present(new CharacterPosePostProcessFrame(
                        m_ActorId,
                        context.RenderFrame,
                        context.ScaledDeltaSeconds,
                        bodyFrame,
                        animationPose));
            }
            m_LastFinalPose = new FinalAnimationPoseFrame(in animationPose, animationPose.CompletionIdentity);
            m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Completed(
                m_Projection.PosePlan,
                in animationPose,
                m_FootPlacement != null);
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
            m_FootPlacement?.Reset(new CharacterPosePostProcessReset(
                m_ActorId,
                renderFrame,
                resetSequence,
                reason,
                bodyReason));
            m_PoseHasOutput = false;
            m_LastFinalPose = default;
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
