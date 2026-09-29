using System.Runtime.ExceptionServices;
using ThirdPersonSimulation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeActionCommandSource
    {
        void BeginFrame(ulong frameIdentity);
        void CommitFrame();
        void DiscardFrame();
        bool TryGetCommands(
            ActorId actorId,
            ulong frameIdentity,
            out IReadOnlyList<ActionAnimationPlaybackCommand> commands);
    }

    internal sealed class CharacterPoseNativeActionCommandSource : ICharacterPoseNativeActionCommandSource
    {
        readonly ActionPlaybackCommandInbox m_Inbox;
        readonly ActorId m_ActorId;
        readonly FixedCapacityFrameBuffer<ActionAnimationPlaybackCommand> m_Commands;
        ActionPlaybackInboxReadLease m_Lease;
        ulong m_FrameIdentity;

        internal CharacterPoseNativeActionCommandSource(
            ActorId actorId,
            ActionPlaybackCommandInbox inbox)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Pose Action command Actor identity is invalid.", nameof(actorId));
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
            m_ActorId = actorId;
            m_Commands = new FixedCapacityFrameBuffer<ActionAnimationPlaybackCommand>(inbox.Capacity);
        }

        public void BeginFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0)
                throw new ArgumentException("Pose Action command frame identity is invalid.", nameof(frameIdentity));
            if (m_Lease.IsValid)
                throw new InvalidOperationException("Pose Action command frame is already open.");
            m_Lease = m_Inbox.BeginRead();
            m_Commands.Clear();
            int pendingCount = m_Inbox.PendingCount;
            for (int i = 0; i < pendingCount; i++)
            {
                ref readonly ActionPlaybackInboxEntry entry =
                    ref m_Inbox.ElementAt(i);
                m_Commands.Add(in entry.CommandRef);
            }
            m_FrameIdentity = frameIdentity;
        }

        public bool HasOpenFrame => m_Lease.IsValid;

        public void CommitFrame()
        {
            RequireOpenFrame();
            m_Inbox.Commit(m_Lease);
            ClearFrame();
        }

        public void DiscardFrame()
        {
            RequireOpenFrame();
            m_Inbox.Discard(m_Lease);
            ClearFrame();
        }

        public bool TryGetCommands(
            ActorId actorId,
            ulong frameIdentity,
            out IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            if (!m_Lease.IsValid || actorId != m_ActorId || frameIdentity != m_FrameIdentity)
            {
                commands = Array.Empty<ActionAnimationPlaybackCommand>();
                return false;
            }
            commands = m_Commands;
            return true;
        }

        void RequireOpenFrame()
        {
            if (!m_Lease.IsValid)
                throw new InvalidOperationException("Pose Action command frame is not open.");
        }

        void ClearFrame()
        {
            m_Lease = default;
            m_Commands.Clear();
            m_FrameIdentity = 0;
        }
    }

    internal interface ICharacterPoseNativeEventFrameSource
    {
        CharacterAnimationVariableContract VariableContract { get; }
        bool TryGetFrame(
            ActorId actorId,
            ulong frameIdentity,
            out CharacterAnimationVariableFrame frame);
    }

    internal sealed class CharacterPoseNativeDomainConstraintServices
    {
        internal CharacterPoseNativeDomainConstraintServices(
            CharacterFootPlacementModule footPlacement,
            CharacterPoseBoneContributionCatalog poseBoneContributions,
            CharacterFullBodyIkGoalAssemblerCatalog goalAssemblers,
            CharacterFinalIkFullBodySolver solver,
            int contributionCount,
            int contributionGoalCount)
        {
            FootPlacement = footPlacement;
            PoseBoneContributions = poseBoneContributions;
            GoalAssemblers = goalAssemblers;
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            ContributionCount = contributionCount > 0
                ? contributionCount
                : throw new ArgumentOutOfRangeException(nameof(contributionCount));
            ContributionGoalCount = contributionGoalCount > 0
                ? contributionGoalCount
                : throw new ArgumentOutOfRangeException(nameof(contributionGoalCount));
        }

        internal CharacterFootPlacementModule FootPlacement { get; }
        internal CharacterPoseBoneContributionCatalog PoseBoneContributions { get; }
        internal CharacterFullBodyIkGoalAssemblerCatalog GoalAssemblers { get; }
        internal CharacterFinalIkFullBodySolver Solver { get; }
        internal int ContributionCount { get; }
        internal int ContributionGoalCount { get; }
    }

    internal sealed class CharacterPoseNativeDomainServices
    {
        internal CharacterPoseNativeDomainServices(
            CharacterAnimationResourceScope resourceScope,
            ICharacterPoseNativeActionCommandSource actionCommandSource,
            ICharacterPoseNativeEventFrameSource eventFrameSource,
            CharacterPoseNativeDomainConstraintServices constraints,
            CharacterPoseNativeSourceHandlerComposition sourceHandlers,
            CharacterPoseNativeConstraintHandlerComposition constraintHandlers,
            CharacterPoseNativeManagedHandlerComposition managedHandlers,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> animationProperties,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            int contributionCapacity)
        {
            ResourceScope = resourceScope ?? throw new ArgumentNullException(nameof(resourceScope));
            ActionCommandSource = actionCommandSource ?? throw new ArgumentNullException(nameof(actionCommandSource));
            EventFrameSource = eventFrameSource ?? throw new ArgumentNullException(nameof(eventFrameSource));
            Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            SourceHandlers = sourceHandlers ?? throw new ArgumentNullException(nameof(sourceHandlers));
            ConstraintHandlers = constraintHandlers ?? throw new ArgumentNullException(nameof(constraintHandlers));
            ManagedHandlers = managedHandlers ?? throw new ArgumentNullException(nameof(managedHandlers));
            AnimationProperties = animationProperties ?? throw new ArgumentNullException(nameof(animationProperties));
            PlayerNodeIds = playerNodeIds ?? throw new ArgumentNullException(nameof(playerNodeIds));
            ContributionCapacity = contributionCapacity > 0
                ? contributionCapacity
                : throw new ArgumentOutOfRangeException(nameof(contributionCapacity));
        }

        internal CharacterAnimationResourceScope ResourceScope { get; }
        internal ICharacterPoseNativeActionCommandSource ActionCommandSource { get; }
        internal ICharacterPoseNativeEventFrameSource EventFrameSource { get; }
        internal CharacterPoseNativeDomainConstraintServices Constraints { get; }
        internal CharacterPoseNativeSourceHandlerComposition SourceHandlers { get; }
        internal CharacterPoseNativeConstraintHandlerComposition ConstraintHandlers { get; }
        internal CharacterPoseNativeManagedHandlerComposition ManagedHandlers { get; }
        internal IReadOnlyList<CharacterPresentationAnimationPropertyBinding> AnimationProperties { get; }
        internal IReadOnlyList<PoseNodeId> PlayerNodeIds { get; }
        internal int ContributionCapacity { get; }
    }

    internal sealed class CharacterPoseNativeDomainSession : IDisposable
    {
        readonly CharacterPoseNativeRoleSession m_Session;
        readonly ICharacterPoseNativeActionCommandSource m_ActionCommandSource;
        readonly ICharacterPoseNativeEventFrameSource m_EventFrameSource;
        readonly CharacterPoseNativeDomainServiceSet m_Services;
        readonly Diagnostics.CharacterPoseDiagnosticVariableBindings m_DiagnosticVariables;

        internal CharacterPoseNativeDomainSession(
            CharacterPoseNativeRoleSession session,
            ICharacterPoseNativeActionCommandSource actionCommandSource,
            ICharacterPoseNativeEventFrameSource eventFrameSource,
            CharacterPoseNativeDomainServiceSet services)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_ActionCommandSource = actionCommandSource ?? throw new ArgumentNullException(nameof(actionCommandSource));
            m_EventFrameSource = eventFrameSource ?? throw new ArgumentNullException(nameof(eventFrameSource));
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_DiagnosticVariables = new Diagnostics.CharacterPoseDiagnosticVariableBindings(eventFrameSource.VariableContract);
        }

        internal CharacterPoseNativeRoleSession RoleSession => m_Session;
        internal PoseGraphId GraphId => m_Session.GraphId;
        internal string GraphRevision => m_Session.GraphRevision;
        internal string ResourceRevision => m_Session.ResourceRevision;
        internal ulong InstanceId => m_Session.InstanceId;
        internal ulong ResetGeneration => m_Session.ResetGeneration;

        internal bool CaptureFootDiagnostics => m_Services.Constraints.CaptureDiagnostics;

        internal Diagnostics.CharacterPoseDiagnosticFrame CaptureDiagnosticFrame(in CharacterPoseNativeFrameLineage lineage)
        {
            if (!m_EventFrameSource.TryGetFrame(lineage.ActorId, lineage.PresentationFrame, out CharacterAnimationVariableFrame variables))
                throw new InvalidOperationException("Pose diagnostics require the committed frame's animation variables.");
            return new Diagnostics.CharacterPoseDiagnosticFrame(in lineage, variables, in m_DiagnosticVariables);
        }

        internal Diagnostics.CharacterNativePoseCaptureFrame CapturePresentationDiagnostics(
            in Diagnostics.CharacterPoseDiagnosticFrame frame)
        {
            if (!m_Session.TryObserveFinalPose(out ComposedAnimationPoseFrame pose))
                throw new InvalidOperationException("Committed Pose is unavailable for capture.");
            return new Diagnostics.CharacterNativePoseCaptureFrame(in pose, in frame,
                m_Session.Role.Graph.PreparedBinding.InputContract,
                m_Session.Role.Publication.RequireCommittedClipSamples(pose.CompletionIdentity),
                m_Session.Role.Graph.StateCapture);
        }

        internal void RequireAvailable() => m_Session.Frame.RequireAvailable();

        internal Exception RecordPresentationFault(in AnimationPresentationFault fault, Exception failure) =>
            m_Session.Frame.RecordFault(in fault, failure);

        internal CharacterPoseNativePublicationResult RunFrame(
            in CharacterPoseNativeFrameInput input,
            Guid diagnosticRuntimeId,
            IActionPresentationClockCoordinator clock)
        {
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
            m_Services.Constraints.CaptureDiagnostics =
                Diagnostics.CharacterPoseFootDiagnosticEvent.IsInterested(diagnosticRuntimeId);
#endif
            return m_Session.Frame.RunFrame(in input, clock, m_ActionCommandSource, CaptureFootDiagnostics);
        }

        internal void PublishFootDiagnostics(
            Guid diagnosticRuntimeId,
            in CharacterPoseNativeFrameLineage lineage)
        {
#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
            if (!CaptureFootDiagnostics)
                return;
            try
            {
                if (!m_Session.TryObserveFinalPose(out ComposedAnimationPoseFrame pose))
                    throw new InvalidOperationException("Pose diagnostic publication has no committed pose.");
                AnimationPoseSourceContribution dominant = default;
                float weight = -1f;
                var contributions = pose.Contributions;
                for (int i = 0; i < contributions.Count; i++)
                {
                    AnimationPoseSourceContribution candidate = contributions[i];
                    if (candidate.Kind == AnimationPoseContributionKind.Live && candidate.Weight > weight)
                    {
                        dominant = candidate;
                        weight = candidate.Weight;
                    }
                }
                AnimationFootMotionRuntimeFrame motion = m_Services.WorldContext.LastSampledFootMotion;
                Diagnostics.CharacterPoseDiagnosticFrame frame = CaptureDiagnosticFrame(in lineage);
                Diagnostics.CharacterFootIkPhysicalCapture physical = m_Session.Role.CommittedPhysicalCapture;
                AnimationFootMotionRuntimeSample left = motion.Left;
                AnimationFootMotionRuntimeSample right = motion.Right;
                Diagnostics.CharacterPoseFootDiagnosticEvent.Publish(
                    diagnosticRuntimeId, in frame, m_Services.Constraints, in physical, in left, in right);
            }
            catch (Exception exception)
            {
                Diagnostics.CharacterPoseCaptureFailure.Report(diagnosticRuntimeId,
                    Diagnostics.CharacterPoseFootDiagnosticEvent.EventId, exception);
            }
#endif
        }

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration) =>
            m_Session.Reset(resetGeneration);

        internal void Stop() => m_Session.Stop();

#if UNITY_EDITOR || KK_DIAGNOSTIC_SAMPLING
        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Session.TryObserve(nodeId, portId, out observation);
#endif

        internal bool TryObserveFinalPose(out ComposedAnimationPoseFrame frame) =>
            m_Session.TryObserveFinalPose(out frame);

        internal bool TryGetActionCommands(
            ActorId actorId,
            ulong frameIdentity,
            out IReadOnlyList<ActionAnimationPlaybackCommand> commands) =>
            m_ActionCommandSource.TryGetCommands(actorId, frameIdentity, out commands);

        internal bool TryGetEventFrame(
            ActorId actorId,
            ulong frameIdentity,
            out CharacterAnimationVariableFrame frame) =>
            m_EventFrameSource.TryGetFrame(actorId, frameIdentity, out frame);

        public void Dispose()
        {
            Exception failure = null;
            CharacterPresentationCleanup.Dispose(m_Session, ref failure);
            CharacterPresentationCleanup.Dispose(m_Services, ref failure);
            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    internal sealed class CharacterPoseNativeDomainCreateResult
    {
        CharacterPoseNativeDomainCreateResult(
            CharacterPoseNativeAdoptedResult adoption,
            CharacterPoseNativeDomainSession session,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message)
        {
            Adoption = adoption;
            Session = session;
            FailureCode = failureCode;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal static CharacterPoseNativeDomainCreateResult Ready(
            CharacterPoseNativeAdoptedResult adoption,
            CharacterPoseNativeDomainSession session) =>
            new CharacterPoseNativeDomainCreateResult(
                adoption,
                session,
                CharacterPoseNativeFailureCode.None,
                "CharacterPoseNativeDomainRuntimeFactory",
                "Pose domain runtime was adopted.");

        internal static CharacterPoseNativeDomainCreateResult Failed(
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message) =>
            new CharacterPoseNativeDomainCreateResult(
                default,
                null,
                failureCode == CharacterPoseNativeFailureCode.None
                    ? CharacterPoseNativeFailureCode.GraphInvalid
                    : failureCode,
                source,
                message);

        internal static CharacterPoseNativeDomainCreateResult Failed(
            CharacterPoseNativeAdoptedResult adoption) =>
            new CharacterPoseNativeDomainCreateResult(
                adoption,
                null,
                adoption.FailureCode,
                adoption.Source,
                adoption.Message);

        internal static CharacterPoseNativeDomainCreateResult Valid() =>
            new CharacterPoseNativeDomainCreateResult(
                default,
                null,
                CharacterPoseNativeFailureCode.None,
                string.Empty,
                string.Empty);

        internal CharacterPoseNativeAdoptedResult Adoption { get; }
        internal CharacterPoseNativeDomainSession Session { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal bool IsAdopted => Session != null;
        internal bool IsFailure => FailureCode != CharacterPoseNativeFailureCode.None;
    }
}


