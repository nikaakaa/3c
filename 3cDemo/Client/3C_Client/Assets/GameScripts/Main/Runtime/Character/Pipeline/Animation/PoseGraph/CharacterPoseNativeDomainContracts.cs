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
            for (int i = 0; i < m_Inbox.Count; i++)
            {
                ActionAnimationPlaybackCommand command = m_Inbox[i].Command;
                m_Commands.Add(in command);
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
        }

        internal CharacterPoseNativeRoleSession RoleSession => m_Session;
        internal PoseGraphId GraphId => m_Session.GraphId;
        internal string GraphRevision => m_Session.GraphRevision;
        internal string ResourceRevision => m_Session.ResourceRevision;
        internal ulong InstanceId => m_Session.InstanceId;
        internal ulong ResetGeneration => m_Session.ResetGeneration;

        internal CharacterPoseNativePreparationResult BeginFrame(in CharacterPoseNativeFrameInput input) =>
            m_Session.Frame.BeginFrame(in input);

        internal void PrepareEvaluation(ulong barrierIdentity) =>
            m_Session.Frame.PrepareEvaluation(barrierIdentity);

        internal CharacterPoseNativeEvaluationResult Evaluate(ulong barrierIdentity) =>
            m_Session.Frame.Evaluate(barrierIdentity);

        internal CharacterPoseNativeValidationResult ValidatePending() =>
            m_Session.Frame.ValidatePending();

        internal CharacterPoseNativePublicationResult Commit(bool captureFootIkDiagnostics) =>
            m_Session.Frame.Commit(captureFootIkDiagnostics);

        internal void Discard(CharacterPoseNativeFailureCode reason) =>
            m_Session.Frame.Discard(reason);

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration) =>
            m_Session.Reset(resetGeneration);

        internal void Stop() => m_Session.Stop();

        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Session.TryObserve(nodeId, portId, out observation);

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
            m_Session.Dispose();
            m_Services.Dispose();
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




