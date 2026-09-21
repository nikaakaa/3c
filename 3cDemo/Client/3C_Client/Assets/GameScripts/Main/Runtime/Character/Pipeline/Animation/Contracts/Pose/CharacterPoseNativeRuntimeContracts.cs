using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterPoseNativePreparationStatus : byte
    {
        Pending = 1,
        Ready = 2,
        Missing = 3,
        Invalid = 4,
        Failed = 5
    }

    internal enum CharacterPoseNativeGraphBoundary : byte
    {
        Root = 1,
        State = 2,
        Subgraph = 3
    }

    internal enum CharacterPoseNativeFailureCode : byte
    {
        None = 0,
        GraphMissing = 1,
        GraphInvalid = 2,
        ProfileMissing = 3,
        RigMissing = 4,
        InputContractMissing = 5,
        ActorMissing = 6,
        ResourceMissing = 7,
        PortInvalid = 8,
        Cycle = 9,
        UnsupportedNode = 10,
        Stale = 11,
        Disposed = 12,
        SourcePending = 13,
        ConstraintFailed = 14,
        PublicationFailed = 15,
        FrameInvalid = 16
    }

    internal enum CharacterPoseNativeFrameStatus : byte
    {
        Prepared = 1,
        Pending = 2,
        Invalid = 3,
        Evaluated = 4,
        Committed = 5,
        Discarded = 6,
        Faulted = 7,
        Validated = 8
    }

    internal static class CharacterPoseNativeEnumValues
    {
        internal static bool IsValid(CharacterPoseNativePreparationStatus value) =>
            value >= CharacterPoseNativePreparationStatus.Pending &&
            value <= CharacterPoseNativePreparationStatus.Failed;

        internal static bool IsValid(CharacterPoseNativeGraphBoundary value) =>
            value >= CharacterPoseNativeGraphBoundary.Root &&
            value <= CharacterPoseNativeGraphBoundary.Subgraph;

        internal static bool IsValid(CharacterPoseNativeFailureCode value) =>
            value >= CharacterPoseNativeFailureCode.None &&
            value <= CharacterPoseNativeFailureCode.FrameInvalid;

        internal static bool IsValid(CharacterPoseNativeFrameStatus value) =>
            value >= CharacterPoseNativeFrameStatus.Prepared &&
            value <= CharacterPoseNativeFrameStatus.Validated;
    }

    internal readonly struct CharacterPoseNativeGraphPrepareRequest
    {
        internal CharacterPoseNativeGraphPrepareRequest(
            ulong requestId,
            ActorId actorId,
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph graph,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            CharacterPoseNativeGraphBoundary boundary)
        {
            if (requestId == 0 || !actorId.IsValid)
                throw new ArgumentException("Pose native graph request identity is invalid.");
            if (!graphAsset || graph == null || profile == null || rig == null || inputContract == null)
                throw new ArgumentException("Pose native graph request binding is incomplete.");
            if (!graph.GraphId.IsValid || string.IsNullOrWhiteSpace(graph.ContentRevision) ||
                !CharacterPoseNativeEnumValues.IsValid(boundary))
                throw new ArgumentException("Pose native graph request graph identity is invalid.");
            if (profile.PoseGraph != graphAsset ||
                !graphAsset.TryGetGraph(graph.GraphId, out CharacterPoseCanvasGraph resolved) ||
                !ReferenceEquals(resolved, graph) && resolved.GraphId != graph.GraphId)
            {
                throw new ArgumentException("Pose native graph request graph binding is inconsistent.");
            }
            rig.RequireValid();
            if (string.IsNullOrWhiteSpace(inputContract.ContractHash) ||
                string.IsNullOrWhiteSpace(resourceRevision))
            {
                throw new ArgumentException("Pose native graph request version is incomplete.");
            }
            RequestId = requestId;
            ActorId = actorId;
            GraphAsset = graphAsset;
            Graph = graph;
            Profile = profile;
            Rig = rig;
            InputContract = inputContract;
            ResourceRevision = resourceRevision.Trim();
            Boundary = boundary;
        }

        internal ulong RequestId { get; }
        internal ActorId ActorId { get; }
        internal CharacterPresentationPoseGraphAsset GraphAsset { get; }
        internal CharacterPoseCanvasGraph Graph { get; }
        internal CharacterAnimationPresentationProfile Profile { get; }
        internal CharacterAnimationRigPayload Rig { get; }
        internal CharacterAnimationInputContract InputContract { get; }
        internal string ResourceRevision { get; }
        internal CharacterPoseNativeGraphBoundary Boundary { get; }
        internal bool IsValid =>
            RequestId != 0 && ActorId.IsValid && GraphAsset && Graph != null && Profile != null &&
            Rig != null && InputContract != null && Graph.GraphId.IsValid &&
            !string.IsNullOrWhiteSpace(Graph.ContentRevision) &&
            !string.IsNullOrWhiteSpace(ResourceRevision) &&
            CharacterPoseNativeEnumValues.IsValid(Boundary) &&
            !string.IsNullOrWhiteSpace(InputContract.ContractHash);
    }

    internal readonly struct CharacterPoseNativePreparedBinding
    {
        internal CharacterPoseNativePreparedBinding(
            in CharacterPoseNativeGraphPrepareRequest request)
        {
            if (!request.IsValid)
                throw new ArgumentException("Pose native prepared binding request is invalid.", nameof(request));
            RequestId = request.RequestId;
            ActorId = request.ActorId;
            GraphAsset = request.GraphAsset;
            Graph = request.Graph;
            Profile = request.Profile;
            Rig = request.Rig;
            InputContract = request.InputContract;
            Boundary = request.Boundary;
            GraphId = request.Graph.GraphId;
            GraphRevision = request.Graph.ContentRevision;
            RigId = request.Rig.RigId;
            RigRevision = request.Rig.RigRevision;
            ResourceRevision = request.ResourceRevision;
            InputContractHash = request.InputContract.ContractHash;
        }

        internal ulong RequestId { get; }
        internal ActorId ActorId { get; }
        internal CharacterPresentationPoseGraphAsset GraphAsset { get; }
        internal CharacterPoseCanvasGraph Graph { get; }
        internal CharacterAnimationPresentationProfile Profile { get; }
        internal CharacterAnimationRigPayload Rig { get; }
        internal CharacterAnimationInputContract InputContract { get; }
        internal CharacterPoseNativeGraphBoundary Boundary { get; }
        internal PoseGraphId GraphId { get; }
        internal string GraphRevision { get; }
        internal string RigId { get; }
        internal string RigRevision { get; }
        internal string ResourceRevision { get; }
        internal string InputContractHash { get; }
        internal bool IsValid =>
            RequestId != 0 && ActorId.IsValid && GraphAsset && Graph != null && Profile != null &&
            Rig != null && InputContract != null && GraphId.IsValid &&
            !string.IsNullOrWhiteSpace(GraphRevision) && !string.IsNullOrWhiteSpace(RigId) &&
            !string.IsNullOrWhiteSpace(RigRevision) && !string.IsNullOrWhiteSpace(ResourceRevision) &&
            CharacterPoseNativeEnumValues.IsValid(Boundary) &&
            !string.IsNullOrWhiteSpace(InputContractHash);
    }

    internal readonly struct CharacterPoseNativeGraphPrepareResult
    {
        CharacterPoseNativeGraphPrepareResult(
            in CharacterPoseNativeGraphPrepareRequest request,
            CharacterPoseNativePreparationStatus status,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message,
            in CharacterPoseNativePreparedBinding preparedBinding)
        {
            if (!request.IsValid || !CharacterPoseNativeEnumValues.IsValid(status) ||
                !CharacterPoseNativeEnumValues.IsValid(failureCode) ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(message) ||
                status == CharacterPoseNativePreparationStatus.Ready && !preparedBinding.IsValid ||
                status != CharacterPoseNativePreparationStatus.Ready && preparedBinding.IsValid ||
                status == CharacterPoseNativePreparationStatus.Ready && failureCode != CharacterPoseNativeFailureCode.None ||
                status != CharacterPoseNativePreparationStatus.Ready && failureCode == CharacterPoseNativeFailureCode.None)
            {
                throw new ArgumentException("Pose native graph prepare result is invalid.");
            }
            Request = request;
            Status = status;
            FailureCode = failureCode;
            Source = source.Trim();
            Message = message.Trim();
            PreparedBinding = preparedBinding;
        }

        internal static CharacterPoseNativeGraphPrepareResult Pending(
            in CharacterPoseNativeGraphPrepareRequest request,
            CharacterPoseNativeFailureCode reason,
            string source,
            string message) =>
            new CharacterPoseNativeGraphPrepareResult(
                in request,
                CharacterPoseNativePreparationStatus.Pending,
                reason,
                source,
                message,
                default);

        internal static CharacterPoseNativeGraphPrepareResult Ready(
            in CharacterPoseNativeGraphPrepareRequest request,
            in CharacterPoseNativePreparedBinding binding) =>
            new CharacterPoseNativeGraphPrepareResult(
                in request,
                CharacterPoseNativePreparationStatus.Ready,
                CharacterPoseNativeFailureCode.None,
                "Pose",
                "Pose graph is ready for instance creation.",
                in binding);

        internal static CharacterPoseNativeGraphPrepareResult Failed(
            in CharacterPoseNativeGraphPrepareRequest request,
            CharacterPoseNativePreparationStatus status,
            CharacterPoseNativeFailureCode reason,
            string source,
            string message) =>
            new CharacterPoseNativeGraphPrepareResult(
                in request,
                status,
                reason,
                source,
                message,
                default);

        internal CharacterPoseNativeGraphPrepareRequest Request { get; }
        internal CharacterPoseNativePreparationStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal CharacterPoseNativePreparedBinding PreparedBinding { get; }
        internal bool IsValid => Request.IsValid &&
            CharacterPoseNativeEnumValues.IsValid(Status) &&
            CharacterPoseNativeEnumValues.IsValid(FailureCode) &&
            !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Message) &&
            (Status == CharacterPoseNativePreparationStatus.Ready
                ? FailureCode == CharacterPoseNativeFailureCode.None && PreparedBinding.IsValid
                : FailureCode != CharacterPoseNativeFailureCode.None && !PreparedBinding.IsValid);
        internal bool IsReady => IsValid && Status == CharacterPoseNativePreparationStatus.Ready;
    }

    internal readonly struct CharacterPoseNativeInstanceContext
    {
        internal CharacterPoseNativeInstanceContext(
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy)
        {
            if (!actorId.IsValid || !animancer || rig == null || !rigBinding ||
                !rootHierarchy)
                throw new ArgumentException("Pose native instance context is incomplete.");
            if (!string.Equals(rig.RigId, rigBinding.RigId, StringComparison.Ordinal) ||
                !string.Equals(rig.RigRevision, rigBinding.RigRevision, StringComparison.Ordinal))
            {
                throw new ArgumentException("Pose native instance Rig identity does not match its binding.");
            }
            ActorId = actorId;
            Animancer = animancer;
            Rig = rig;
            RigBinding = rigBinding;
            RootHierarchy = rootHierarchy;
        }

        internal ActorId ActorId { get; }
        internal AnimancerComponent Animancer { get; }
        internal CharacterAnimationRigPayload Rig { get; }
        internal CharacterAnimationRigBinding RigBinding { get; }
        internal CharacterRootHierarchyBinding RootHierarchy { get; }
        internal bool IsValid => ActorId.IsValid && Animancer && Rig != null && RigBinding && RootHierarchy &&
            string.Equals(Rig.RigId, RigBinding.RigId, StringComparison.Ordinal) &&
            string.Equals(Rig.RigRevision, RigBinding.RigRevision, StringComparison.Ordinal);
    }

    internal readonly struct CharacterPoseNativeCreateInstanceRequest
    {
        internal CharacterPoseNativeCreateInstanceRequest(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason)
        {
            if (!preparedBinding.IsValid || !context.IsValid || instanceId == 0 || resetGeneration == 0 ||
                string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Pose native instance request is invalid.");
            }
            PreparedBinding = preparedBinding;
            Context = context;
            InstanceId = instanceId;
            ResetGeneration = resetGeneration;
            Reason = reason.Trim();
        }

        internal CharacterPoseNativePreparedBinding PreparedBinding { get; }
        internal CharacterPoseNativeInstanceContext Context { get; }
        internal ulong InstanceId { get; }
        internal ulong ResetGeneration { get; }
        internal string Reason { get; }
        internal bool IsValid => PreparedBinding.IsValid && Context.IsValid && InstanceId != 0 &&
            ResetGeneration != 0 && !string.IsNullOrWhiteSpace(Reason);
    }

    internal readonly struct CharacterPoseNativeAdoptedResult
    {
        CharacterPoseNativeAdoptedResult(
            ActorId actorId,
            ulong instanceId,
            ulong resetGeneration,
            PoseGraphId graphId,
            string graphRevision,
            string resourceRevision,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message)
        {
            if (!actorId.IsValid || resetGeneration == 0 || !graphId.IsValid ||
                string.IsNullOrWhiteSpace(graphRevision) || string.IsNullOrWhiteSpace(resourceRevision) ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(message) ||
                failureCode == CharacterPoseNativeFailureCode.None && instanceId == 0 ||
                failureCode != CharacterPoseNativeFailureCode.None && instanceId != 0)
            {
                throw new ArgumentException("Pose native adopted result is invalid.");
            }
            ActorId = actorId;
            InstanceId = instanceId;
            ResetGeneration = resetGeneration;
            GraphId = graphId;
            GraphRevision = graphRevision.Trim();
            ResourceRevision = resourceRevision.Trim();
            FailureCode = failureCode;
            Source = source.Trim();
            Message = message.Trim();
        }

        internal static CharacterPoseNativeAdoptedResult Adopted(
            in CharacterPoseNativeCreateInstanceRequest request) =>
            new CharacterPoseNativeAdoptedResult(
                request.Context.ActorId,
                request.InstanceId,
                request.ResetGeneration,
                request.PreparedBinding.GraphId,
                request.PreparedBinding.GraphRevision,
                request.PreparedBinding.ResourceRevision,
                CharacterPoseNativeFailureCode.None,
                "Pose",
                "Pose graph instance was adopted.");

        internal static CharacterPoseNativeAdoptedResult Failed(
            in CharacterPoseNativeCreateInstanceRequest request,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message) =>
            new CharacterPoseNativeAdoptedResult(
                request.Context.ActorId,
                0,
                request.ResetGeneration,
                request.PreparedBinding.GraphId,
                request.PreparedBinding.GraphRevision,
                request.PreparedBinding.ResourceRevision,
                failureCode == CharacterPoseNativeFailureCode.None
                    ? CharacterPoseNativeFailureCode.GraphInvalid
                    : failureCode,
                source,
                message);

        internal static CharacterPoseNativeAdoptedResult Failed(
            in CharacterPoseNativeGraphPrepareResult preparation,
            ulong resetGeneration,
            CharacterPoseNativeFailureCode failureCode,
            string message) =>
            new CharacterPoseNativeAdoptedResult(
                preparation.Request.ActorId,
                0,
                resetGeneration,
                preparation.Request.Graph.GraphId,
                preparation.Request.Graph.ContentRevision,
                preparation.Request.ResourceRevision,
                failureCode == CharacterPoseNativeFailureCode.None
                    ? CharacterPoseNativeFailureCode.GraphInvalid
                    : failureCode,
                preparation.Source,
                message);

        internal ActorId ActorId { get; }
        internal ulong InstanceId { get; }
        internal ulong ResetGeneration { get; }
        internal PoseGraphId GraphId { get; }
        internal string GraphRevision { get; }
        internal string ResourceRevision { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal bool IsAdopted => FailureCode == CharacterPoseNativeFailureCode.None && InstanceId != 0;
    }

    internal readonly struct CharacterPoseNativeResetResult
    {
        CharacterPoseNativeResetResult(
            ActorId actorId,
            ulong instanceId,
            ulong previousResetGeneration,
            ulong resetGeneration,
            CharacterPoseNativeFailureCode failureCode,
            string message)
        {
            if (!actorId.IsValid || instanceId == 0 || previousResetGeneration == 0 ||
                resetGeneration == 0 || string.IsNullOrWhiteSpace(message) ||
                failureCode == CharacterPoseNativeFailureCode.None &&
                resetGeneration <= previousResetGeneration ||
                failureCode != CharacterPoseNativeFailureCode.None &&
                resetGeneration < previousResetGeneration)
            {
                throw new ArgumentException("Pose native reset result is invalid.");
            }
            ActorId = actorId;
            InstanceId = instanceId;
            PreviousResetGeneration = previousResetGeneration;
            ResetGeneration = resetGeneration;
            FailureCode = failureCode;
            Message = message.Trim();
        }

        internal static CharacterPoseNativeResetResult Succeeded(
            ActorId actorId,
            ulong instanceId,
            ulong previousResetGeneration,
            ulong resetGeneration) =>
            new CharacterPoseNativeResetResult(
                actorId,
                instanceId,
                previousResetGeneration,
                resetGeneration,
                CharacterPoseNativeFailureCode.None,
                "Pose graph instance was reset.");

        internal static CharacterPoseNativeResetResult Failed(
            ActorId actorId,
            ulong instanceId,
            ulong resetGeneration,
            CharacterPoseNativeFailureCode failureCode,
            string message) =>
            new CharacterPoseNativeResetResult(
                actorId,
                instanceId,
                resetGeneration,
                resetGeneration,
                failureCode == CharacterPoseNativeFailureCode.None
                    ? CharacterPoseNativeFailureCode.Stale
                    : failureCode,
                message);

        internal ActorId ActorId { get; }
        internal ulong InstanceId { get; }
        internal ulong PreviousResetGeneration { get; }
        internal ulong ResetGeneration { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Message { get; }
        internal bool IsReset => FailureCode == CharacterPoseNativeFailureCode.None;
    }

    internal readonly struct CharacterPoseNativeFrameLineage : IEquatable<CharacterPoseNativeFrameLineage>
    {
        internal CharacterPoseNativeFrameLineage(
            ActorId actorId,
            ulong frameIdentity,
            ulong completionIdentity,
            ulong presentationFrame,
            ulong bodyTick,
            PoseGraphId graphId,
            string graphRevision,
            string rigId,
            string rigRevision,
            string inputContractHash,
            ulong instanceId,
            ulong resetGeneration)
        {
            if (!actorId.IsValid || frameIdentity == 0 || presentationFrame == 0 || bodyTick == 0 ||
                !graphId.IsValid || string.IsNullOrWhiteSpace(graphRevision) || string.IsNullOrWhiteSpace(rigId) ||
                string.IsNullOrWhiteSpace(rigRevision) || string.IsNullOrWhiteSpace(inputContractHash) ||
                instanceId == 0 || resetGeneration == 0)
            {
                throw new ArgumentException("Pose native frame lineage is invalid.");
            }
            ActorId = actorId;
            FrameIdentity = frameIdentity;
            CompletionIdentity = completionIdentity;
            PresentationFrame = presentationFrame;
            BodyTick = bodyTick;
            GraphId = graphId;
            GraphRevision = graphRevision.Trim();
            RigId = rigId.Trim();
            RigRevision = rigRevision.Trim();
            InputContractHash = inputContractHash.Trim();
            InstanceId = instanceId;
            ResetGeneration = resetGeneration;
        }

        internal ActorId ActorId { get; }
        internal ulong FrameIdentity { get; }
        internal ulong CompletionIdentity { get; }
        internal ulong PresentationFrame { get; }
        internal ulong BodyTick { get; }
        internal PoseGraphId GraphId { get; }
        internal string GraphRevision { get; }
        internal string RigId { get; }
        internal string RigRevision { get; }
        internal string InputContractHash { get; }
        internal ulong InstanceId { get; }
        internal ulong ResetGeneration { get; }
        internal bool HasValidFrameFields => ActorId.IsValid && FrameIdentity != 0 &&
            PresentationFrame != 0 && BodyTick != 0 && GraphId.IsValid &&
            !string.IsNullOrWhiteSpace(GraphRevision) && !string.IsNullOrWhiteSpace(RigId) &&
            !string.IsNullOrWhiteSpace(RigRevision) && !string.IsNullOrWhiteSpace(InputContractHash) &&
            InstanceId != 0 && ResetGeneration != 0;
        internal bool IsOpenValid => HasValidFrameFields && CompletionIdentity == 0;
        internal bool IsValid => HasValidFrameFields && CompletionIdentity != 0;
        internal CharacterPoseNativeFrameLineage WithCompletion(ulong completionIdentity) =>
            new CharacterPoseNativeFrameLineage(
                ActorId,
                FrameIdentity,
                completionIdentity,
                PresentationFrame,
                BodyTick,
                GraphId,
                GraphRevision,
                RigId,
                RigRevision,
                InputContractHash,
                InstanceId,
                ResetGeneration);
        public bool Equals(CharacterPoseNativeFrameLineage other) =>
            ActorId == other.ActorId && FrameIdentity == other.FrameIdentity &&
            CompletionIdentity == other.CompletionIdentity && PresentationFrame == other.PresentationFrame &&
            BodyTick == other.BodyTick && GraphId == other.GraphId &&
            string.Equals(GraphRevision, other.GraphRevision, StringComparison.Ordinal) &&
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            string.Equals(InputContractHash, other.InputContractHash, StringComparison.Ordinal) &&
            InstanceId == other.InstanceId && ResetGeneration == other.ResetGeneration;
        public override bool Equals(object obj) => obj is CharacterPoseNativeFrameLineage other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(
                ActorId,
                FrameIdentity,
                CompletionIdentity,
                PresentationFrame,
                BodyTick,
                GraphId,
                GraphRevision,
                RigId),
            HashCode.Combine(
                RigRevision,
                InputContractHash,
                InstanceId,
                ResetGeneration));
        public static bool operator ==(CharacterPoseNativeFrameLineage left, CharacterPoseNativeFrameLineage right) => left.Equals(right);
        public static bool operator !=(CharacterPoseNativeFrameLineage left, CharacterPoseNativeFrameLineage right) => !left.Equals(right);
    }

    internal readonly struct CharacterPoseNativeFrameInput
    {
        internal CharacterPoseNativeFrameInput(
            ActorId actorId,
            ulong frameIdentity,
            ulong presentationFrame,
            ulong bodyTick,
            double presentationSampleTick,
            float deltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame,
            IReadOnlyList<ActionAnimationPlaybackCommand> actionCommands)
        {
            if (!actorId.IsValid || frameIdentity == 0 || presentationFrame == 0 || bodyTick == 0 ||
                !double.IsFinite(presentationSampleTick) || presentationSampleTick < bodyTick ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f || !bodyFrame.IsValid ||
                !factFrame.IsValid || !parameterFrame.IsValid || actionCommands == null)
            {
                throw new ArgumentException("Pose native frame input is invalid.");
            }
            ActorId = actorId;
            FrameIdentity = frameIdentity;
            PresentationFrame = presentationFrame;
            BodyTick = bodyTick;
            PresentationSampleTick = presentationSampleTick;
            DeltaSeconds = deltaSeconds;
            BodyFrame = bodyFrame;
            FactFrame = factFrame;
            ParameterFrame = parameterFrame;
            var commands = new ActionAnimationPlaybackCommand[actionCommands.Count];
            for (int i = 0; i < commands.Length; i++)
                commands[i] = actionCommands[i];
            ActionCommands = commands;
        }

        internal ActorId ActorId { get; }
        internal ulong FrameIdentity { get; }
        internal ulong PresentationFrame { get; }
        internal ulong BodyTick { get; }
        internal double PresentationSampleTick { get; }
        internal float DeltaSeconds { get; }
        internal CharacterBodyPresentationFrame BodyFrame { get; }
        internal CharacterPresentationFactFrame FactFrame { get; }
        internal CharacterAnimationPoseInputFrame ParameterFrame { get; }
        internal IReadOnlyList<ActionAnimationPlaybackCommand> ActionCommands { get; }
        internal bool IsValid => ActorId.IsValid && FrameIdentity != 0 && PresentationFrame != 0 &&
            BodyTick != 0 && double.IsFinite(PresentationSampleTick) && PresentationSampleTick >= BodyTick &&
            float.IsFinite(DeltaSeconds) && DeltaSeconds >= 0f && BodyFrame.IsValid &&
            FactFrame.IsValid && ParameterFrame.IsValid && ActionCommands != null;
    }

    internal readonly struct CharacterPoseNativeFrameLease
    {
        internal CharacterPoseNativeFrameLease(in CharacterPoseNativeFrameLineage lineage)
        {
            if (!lineage.IsOpenValid)
                throw new ArgumentException("Pose native frame lease lineage is invalid.", nameof(lineage));
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal bool IsValid => m_IsValid && Lineage.IsOpenValid;
        internal bool Matches(in CharacterPoseNativeFrameLineage lineage) =>
            IsValid && lineage == Lineage;
    }

    internal readonly struct CharacterPoseNativeSourceRequest
    {
        internal CharacterPoseNativeSourceRequest(
            PoseNodeId nodeId,
            CharacterPresentationPoseSourceSlot sourceSlot,
            AnimationPoseSourceId sourceId,
            bool required,
            ulong scopeInstanceId)
        {
            if (!nodeId.IsValid ||
                !sourceId.IsValid ||
                !sourceSlot && sourceId.SourceKind != AnimationPoseSourceKind.Timeline ||
                scopeInstanceId == 0)
                throw new ArgumentException("Pose native source request is invalid.");
            NodeId = nodeId;
            SourceSlot = sourceSlot;
            SourceId = sourceId;
            Required = required;
            ScopeInstanceId = scopeInstanceId;
        }

        internal PoseNodeId NodeId { get; }
        internal CharacterPresentationPoseSourceSlot SourceSlot { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal bool Required { get; }
        internal ulong ScopeInstanceId { get; }
    }

    internal readonly struct CharacterPoseNativeSourceDemand
    {
        internal CharacterPoseNativeSourceDemand(
            in CharacterPoseNativeFrameLineage lineage,
            IReadOnlyList<CharacterPoseNativeSourceRequest> requests)
        {
            if (!lineage.IsValid)
                throw new ArgumentException(
                    "Pose native source demand lineage is invalid " +
                    $"(actor={lineage.ActorId}, frame={lineage.FrameIdentity}, " +
                    $"completion={lineage.CompletionIdentity}, instance={lineage.InstanceId}, " +
                    $"resetGeneration={lineage.ResetGeneration}).");
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterPoseNativeSourceRequest request = requests[i];
                if (!request.NodeId.IsValid ||
                    !request.SourceId.IsValid ||
                    !request.SourceSlot &&
                    request.SourceId.SourceKind != AnimationPoseSourceKind.Timeline ||
                    request.ScopeInstanceId == 0)
                    throw new ArgumentException(
                        "Pose native source demand contains an invalid request " +
                        $"(index={i}, nodeId={request.NodeId.Value}, sourceId={request.SourceId}, " +
                        $"sourceSlot={request.SourceSlot}, scopeInstanceId={request.ScopeInstanceId}).");
                for (int previousIndex = 0; previousIndex < i; previousIndex++)
                {
                    CharacterPoseNativeSourceRequest previous = requests[previousIndex];
                    if (previous.ScopeInstanceId != request.ScopeInstanceId ||
                        previous.NodeId != request.NodeId ||
                        previous.SourceId != request.SourceId)
                    {
                        continue;
                    }
                    throw new ArgumentException(
                        "Pose native source demand contains a duplicate request " +
                        $"(index={i}, nodeId={request.NodeId.Value}, sourceId={request.SourceId}, " +
                        $"sourceSlot={request.SourceSlot}, scopeInstanceId={request.ScopeInstanceId}).");
                }
            }
            Lineage = lineage;
            Requests = requests;
        }

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal IReadOnlyList<CharacterPoseNativeSourceRequest> Requests { get; }
        internal bool IsValid => Lineage.IsValid && Requests != null;
    }

    internal readonly struct CharacterPoseNativePreparationResult
    {
        internal CharacterPoseNativePreparationResult(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFrameStatus status,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message,
            in CharacterPoseNativeSourceDemand demand)
        {
            if (!lineage.IsValid || !CharacterPoseNativeEnumValues.IsValid(status) ||
                !CharacterPoseNativeEnumValues.IsValid(failureCode) ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(message) ||
                status == CharacterPoseNativeFrameStatus.Prepared && !demand.IsValid ||
                status != CharacterPoseNativeFrameStatus.Prepared && demand.IsValid)
            {
                throw new ArgumentException("Pose native preparation result is invalid.");
            }
            Lineage = lineage;
            Status = status;
            FailureCode = failureCode;
            Source = source.Trim();
            Message = message.Trim();
            Demand = demand;
        }

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal CharacterPoseNativeFrameStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal CharacterPoseNativeSourceDemand Demand { get; }
        internal bool IsValid => Lineage.IsValid &&
            CharacterPoseNativeEnumValues.IsValid(Status) &&
            CharacterPoseNativeEnumValues.IsValid(FailureCode) &&
            !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Message) &&
            (Status == CharacterPoseNativeFrameStatus.Prepared
                ? Demand.IsValid
                : !Demand.IsValid);
    }

    internal readonly struct CharacterPoseNativeEvaluationResult
    {
        internal CharacterPoseNativeEvaluationResult(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFrameStatus status,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message,
            CharacterPoseNativePortValue output = null)
        {
            if (!lineage.IsValid || status != CharacterPoseNativeFrameStatus.Evaluated &&
                status != CharacterPoseNativeFrameStatus.Pending &&
                status != CharacterPoseNativeFrameStatus.Invalid &&
                status != CharacterPoseNativeFrameStatus.Faulted ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(message) ||
                status == CharacterPoseNativeFrameStatus.Evaluated && failureCode != CharacterPoseNativeFailureCode.None ||
                status != CharacterPoseNativeFrameStatus.Evaluated && failureCode == CharacterPoseNativeFailureCode.None ||
                status == CharacterPoseNativeFrameStatus.Evaluated && output == null ||
                status != CharacterPoseNativeFrameStatus.Evaluated && output != null)
            {
                throw new ArgumentException("Pose native evaluation result is invalid.");
            }
            Lineage = lineage;
            Status = status;
            FailureCode = failureCode;
            Source = source.Trim();
            Message = message.Trim();
            Output = output;
        }

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal CharacterPoseNativeFrameStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal CharacterPoseNativePortValue Output { get; }
        internal bool IsValid => Lineage.IsValid &&
            (Status == CharacterPoseNativeFrameStatus.Evaluated
                ? FailureCode == CharacterPoseNativeFailureCode.None && Output != null
                : FailureCode != CharacterPoseNativeFailureCode.None) &&
            !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Message);
    }

    internal readonly struct CharacterPoseNativePublicationResult
    {
        internal CharacterPoseNativePublicationResult(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFrameStatus status,
            CharacterPoseNativeFailureCode failureCode,
            ulong appliedCompletionIdentity,
            string source,
            string message)
        {
            if (!lineage.IsValid || status != CharacterPoseNativeFrameStatus.Committed &&
                status != CharacterPoseNativeFrameStatus.Invalid && status != CharacterPoseNativeFrameStatus.Faulted ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(message) ||
                status == CharacterPoseNativeFrameStatus.Committed &&
                    (failureCode != CharacterPoseNativeFailureCode.None || appliedCompletionIdentity != lineage.CompletionIdentity) ||
                status != CharacterPoseNativeFrameStatus.Committed &&
                    (failureCode == CharacterPoseNativeFailureCode.None || appliedCompletionIdentity != 0))
            {
                throw new ArgumentException("Pose native publication result is invalid.");
            }
            Lineage = lineage;
            Status = status;
            FailureCode = failureCode;
            AppliedCompletionIdentity = appliedCompletionIdentity;
            Source = source.Trim();
            Message = message.Trim();
        }

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal CharacterPoseNativeFrameStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal ulong AppliedCompletionIdentity { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal bool IsPublished => Status == CharacterPoseNativeFrameStatus.Committed &&
            FailureCode == CharacterPoseNativeFailureCode.None &&
            AppliedCompletionIdentity == Lineage.CompletionIdentity;
    }

    internal readonly struct CharacterPoseNativeValidationResult
    {
        CharacterPoseNativeValidationResult(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFrameStatus status,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message)
        {
            if (!lineage.IsValid ||
                status != CharacterPoseNativeFrameStatus.Validated &&
                status != CharacterPoseNativeFrameStatus.Invalid &&
                status != CharacterPoseNativeFrameStatus.Faulted ||
                string.IsNullOrWhiteSpace(source) ||
                string.IsNullOrWhiteSpace(message) ||
                status == CharacterPoseNativeFrameStatus.Validated &&
                failureCode != CharacterPoseNativeFailureCode.None ||
                status != CharacterPoseNativeFrameStatus.Validated &&
                failureCode == CharacterPoseNativeFailureCode.None)
            {
                throw new ArgumentException(
                    "Pose native validation result is invalid.");
            }
            Lineage = lineage;
            Status = status;
            FailureCode = failureCode;
            Source = source.Trim();
            Message = message.Trim();
        }

        internal static CharacterPoseNativeValidationResult Succeeded(
            in CharacterPoseNativeFrameLineage lineage,
            string source,
            string message) =>
            new CharacterPoseNativeValidationResult(
                in lineage,
                CharacterPoseNativeFrameStatus.Validated,
                CharacterPoseNativeFailureCode.None,
                source,
                message);

        internal static CharacterPoseNativeValidationResult Failed(
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode failureCode,
            string source,
            string message) =>
            new CharacterPoseNativeValidationResult(
                in lineage,
                CharacterPoseNativeFrameStatus.Invalid,
                failureCode == CharacterPoseNativeFailureCode.None
                    ? CharacterPoseNativeFailureCode.FrameInvalid
                    : failureCode,
                source,
                message);

        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal CharacterPoseNativeFrameStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Source { get; }
        internal string Message { get; }
        internal bool IsValid => Lineage.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Message) &&
            (Status == CharacterPoseNativeFrameStatus.Validated
                ? FailureCode == CharacterPoseNativeFailureCode.None
                : FailureCode != CharacterPoseNativeFailureCode.None);
        internal bool IsValidated => IsValid &&
            Status == CharacterPoseNativeFrameStatus.Validated;
    }

    internal readonly struct CharacterPoseNativeNodeObservation
    {
        internal CharacterPoseNativeNodeObservation(
            PoseGraphId graphId,
            PoseNodeId nodeId,
            PosePortId portId,
            ulong instanceId,
            ulong completionIdentity,
            CharacterPoseNativeFrameStatus status,
            CharacterPoseNativeFailureCode failureCode,
            string message)
        {
            if (!graphId.IsValid || !nodeId.IsValid || !portId.IsValid || instanceId == 0 ||
                completionIdentity == 0 || string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Pose native node observation is invalid.");
            }
            GraphId = graphId;
            NodeId = nodeId;
            PortId = portId;
            InstanceId = instanceId;
            CompletionIdentity = completionIdentity;
            Status = status;
            FailureCode = failureCode;
            Message = message.Trim();
        }

        internal PoseGraphId GraphId { get; }
        internal PoseNodeId NodeId { get; }
        internal PosePortId PortId { get; }
        internal ulong InstanceId { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterPoseNativeFrameStatus Status { get; }
        internal CharacterPoseNativeFailureCode FailureCode { get; }
        internal string Message { get; }
    }

    internal interface ICharacterPoseCanvasNativeRuntime : IDisposable
    {
        void Initialize(CharacterPoseCanvasGraph graph);
        void Start(CharacterPoseCanvasGraph graph);
        void Stop(CharacterPoseCanvasGraph graph);
        T Read<T>(CharacterPoseCanvasNode node, PosePortId portId)
            where T : CharacterPoseNativePortValue;
        bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation);
    }
}
