using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeDomainServiceFactory
    {
        readonly CharacterAnimationPresentationProfile m_Profile;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterAnimationInputContract m_InputContract;
        readonly CharacterPoseNativeDomainResourceSet m_Resources;
        readonly CharacterAnimationResourceScope m_ResourceScope;
        readonly ICharacterPoseNativeActionCommandSource m_ActionCommandSource;
        readonly ICharacterPoseNativeEventFrameSource m_EventFrameSource;
        readonly CharacterWorldAwarePresentationBinding m_World;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly PhysicsScene m_PhysicsScene;
        readonly Func<CharacterPoseCanvasNode, IActionPresentationClockPolicy> m_ClockPolicyFactory;
        readonly Dictionary<CharacterPresentationPoseSourceSlot, int> m_SourceIndexBySlot;
        readonly Dictionary<PoseNodeId, int> m_IndexByNode;
        ulong m_NextSubgraphInstanceSequence = 1;

        internal CharacterPoseNativeDomainServiceFactory(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            CharacterPoseNativeDomainResourceSet resources,
            CharacterAnimationResourceScope resourceScope,
            ICharacterPoseNativeActionCommandSource actionCommandSource,
            ICharacterPoseNativeEventFrameSource eventFrameSource,
            CharacterWorldAwarePresentationBinding world,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource,
            PhysicsScene physicsScene,
            Func<CharacterPoseCanvasNode, IActionPresentationClockPolicy> clockPolicyFactory)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_InputContract = inputContract ?? throw new ArgumentNullException(nameof(inputContract));
            m_Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            m_ResourceScope = resourceScope ?? throw new ArgumentNullException(nameof(resourceScope));
            m_ActionCommandSource = actionCommandSource ?? throw new ArgumentNullException(nameof(actionCommandSource));
            m_EventFrameSource = eventFrameSource ?? throw new ArgumentNullException(nameof(eventFrameSource));
            m_World = world ? world : throw new ArgumentNullException(nameof(world));
            m_FutureBodyTranslationSource = futureBodyTranslationSource;
            m_ClockPolicyFactory = clockPolicyFactory ?? throw new ArgumentNullException(nameof(clockPolicyFactory));
            if (!physicsScene.IsValid())
                throw new ArgumentException("Pose domain requires a valid PhysicsScene.", nameof(physicsScene));
            m_PhysicsScene = physicsScene;
            m_SourceIndexBySlot = BuildSourceIndexes();
            m_IndexByNode = BuildNodeIndexes();
            resources.RequireSourceCatalogComplete(profile);
        }

        internal CharacterAnimationResourceScope ResourceScope => m_ResourceScope;

        internal CharacterPoseNativeDomainServiceSet Create(
            ActorId actorId,
            CharacterAnimationRigBinding rigBinding,
            ulong requestId,
            ulong instanceId,
            ulong resetGeneration,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceLeaseProvider == null)
                throw new ArgumentNullException(nameof(sourceLeaseProvider));
            var owned = new List<IDisposable>();
            try
            {
            CharacterPoseNativeSourceResourceCatalog sourceCatalog = m_Resources.CreateSourceCatalog(m_Rig, m_ResourceScope);
            CharacterPoseNativeConstraintResourceCatalog constraintCatalog = m_Resources.CreateConstraintCatalog(m_Rig);
            owned.Add(constraintCatalog);
            CharacterPoseNativeManagedSourceResourceCatalog managedCatalog = m_Resources.CreateManagedCatalog(m_Profile, m_Rig);
            owned.Add(managedCatalog);
            CharacterPoseNativeFootPlacementResource footResource = m_Resources.CreateFootPlacementResource(
                m_World,
                m_FutureBodyTranslationSource,
                new CharacterFootPlacementWorldQueryBackend(
                    m_PhysicsScene,
                    new CharacterFootPlacementPoseRig(
                        m_Resources.FootPlacementCalibration,
                        m_Rig,
                        rigBinding,
                        m_World),
                    m_Resources.FootPlacementProfile.GroundDetection.Build().ContactCapacity,
                    m_Resources.FootPlacementProfile.GroundDetection.Build().SegmentHitCapacity));
            CharacterFootPlacementModule footPlacement = footResource.CreateModule(
                actorId,
                m_Rig,
                rigBinding,
                m_Profile.PoseGraph.Graph.ContentRevision);
            CharacterFinalIkFullBodySolver solver = CreateSolver();
            var constraints = new CharacterPoseConstraintRuntime(
                footPlacement,
                constraintCatalog.PoseBoneContributions,
                constraintCatalog.GoalAssemblers,
                solver,
                m_Resources.ContributionCount,
                m_Resources.ContributionGoalCount,
                m_Rig.RigId,
                m_Rig.RigRevision);
            var worldContext = new CharacterPoseWorldContextAdapter(
                m_Profile.PoseGraph.Graph.GraphId.Value,
                source,
                CollectPlayerNodeIds(),
                ResolveFootMotion,
                m_Resources.ContributionCount);
            var constraintService = new CharacterPoseNativeConstraintServiceBinding(
                constraints,
                worldContext,
                RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight));
            var sourceHandlers = new CharacterPoseNativeSourceHandlerComposition(
                source,
                sourceLeaseProvider,
                CreateClipPlayer,
                ThrowBlendSpace,
                ThrowSelectedPlayer,
                ThrowSelectedSample,
                CreateBlendStack,
                ThrowActionSample,
                ThrowProviderSample,
                RequireBindingIndex,
                CreateBuffer,
                m_ClockPolicyFactory);
            var constraintHandlers = new CharacterPoseNativeConstraintHandlerComposition(
                constraintService,
                (node, context) => new CharacterFootPlacementConstraintHandle(0, 0, 0, 0, 0),
                (node, context) => new CharacterPoseBoneContributionConstraintHandle(1, 0, 0, 0, 0, 0, m_Resources.PoseBoneIkGoalBindings.Count),
                (node, context) => new CharacterFullBodyIkGoalAssemblerConstraintHandle(2, 0, 0, 0, 1),
                (node, context) => new CharacterFullBodyIkConstraintHandle(3, 0, 0, 0, 1, 0),
                CreateBuffer);
            var managedHandlers = new CharacterPoseNativeManagedHandlerComposition(
                ThrowLinkedPose,
                ThrowMotionMatching,
                (node, context) => new CharacterPoseHistoryId($"history/{node.NodeId}"),
                ThrowHistorySource,
                ThrowEntryPose,
                (node, context) => requestId,
                (node, context) => AllocateSubgraphInstanceId(instanceId, node.NodeId.Value),
                (node, context) => resetGeneration,
                (node, context) => $"pose-subgraph/{node.NodeId}",
                ThrowRootOrientationCurve,
                ThrowRootOrientationSource,
                CreateBuffer);
            return new CharacterPoseNativeDomainServiceSet(
                new CharacterPoseNativeDomainServices(
                m_ResourceScope,
                m_ActionCommandSource,
                m_EventFrameSource,
                new CharacterPoseNativeDomainConstraintServices(
                    footPlacement,
                    constraintCatalog.PoseBoneContributions,
                    constraintCatalog.GoalAssemblers,
                    solver,
                    m_Resources.ContributionCount,
                    m_Resources.ContributionGoalCount),
                sourceHandlers,
                constraintHandlers,
                managedHandlers,
                CompilePropertyBindings(),
                CollectPlayerNodeIds(),
                        m_Resources.ContributionCount),
                constraints,
                owned);
            }
            catch
            {
                for (int i = owned.Count - 1; i >= 0; i--)
                    owned[i].Dispose();
                throw;
            }
        }

        AnimationClipPlayerRuntime CreateClipPlayer(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context)
        {
            if (node.Payload is not CharacterClipPlayerPosePayload payload)
                throw new InvalidOperationException($"Pose Clip Player '{node.NodeId}' has an invalid payload.");
            if (!m_SourceIndexBySlot.TryGetValue(payload.SourceSlot, out int sourceIndex))
                throw new InvalidOperationException($"Pose Clip Player '{node.NodeId}' has no source binding.");
            var plan = m_Resources.CreateSourceCatalog(m_Rig, m_ResourceScope)
                .RequirePlan(new PresentationPoseSourceIndex(sourceIndex));
            var descriptor = new CharacterPresentationClipPlayerDescriptor(
                m_IndexByNode[node.NodeId],
                node.NodeId,
                plan.SourceIndex,
                payload.PlayRate,
                payload.InitialTime,
                payload.LoopAnimation,
                payload.IsLocomotionParticipant,
                m_IndexByNode[node.NodeId]);
            int footIndex = RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight);
            return new AnimationClipPlayerRuntime(descriptor, plan, m_InputContract.Parameters, footIndex, m_Rig);
        }

        CharacterPoseFootMotionSource ResolveFootMotion(
            AnimationPoseSourceContribution contribution,
            ClipSamplePlan clipSample)
        {
            CharacterPresentationPoseSourcePlan plan = m_Resources.CreateSourceCatalog(m_Rig, m_ResourceScope)
                .Plans.FirstOrDefault(value =>
                    value.SourceIndex.Value == contribution.SourceId.PresentationPoseSourceIndex.Value) ??
                throw new InvalidOperationException(
                    $"Pose Foot Motion source '{contribution.SourceId}' has no plan.");
            return new CharacterPoseFootMotionSource(
                plan.DisplayName,
                (ulong)plan.ContentRevision.GetHashCode(),
                plan.FootStepObservation);
        }

        CharacterFinalIkFullBodySolver CreateSolver()
        {
            var parents = new NativeArray<int>(m_Rig.PoseBoneCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var virtualBones = new NativeArray<CharacterVirtualBoneDescriptor>(m_Rig.VirtualBoneCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int i = 0; i < m_Rig.PoseBoneCount; i++)
                    parents[i] = m_Rig.GetPoseParentIndex(i);
                for (int i = 0; i < m_Rig.VirtualBoneCount; i++)
                {
                    CharacterAnimationVirtualBonePayload bone = m_Rig.VirtualBones[i];
                    virtualBones[i] = new CharacterVirtualBoneDescriptor(
                        new CharacterPoseBoneRuntimeId(bone.VirtualBoneId),
                        bone.SourcePhysicalBoneIndex,
                        bone.TargetPhysicalBoneIndex,
                        bone.PoseBoneIndex);
                }
                return new CharacterFinalIkFullBodySolver(m_Rig, m_Profile.FullBodyIkProfile, parents, virtualBones);
            }
            finally
            {
                if (parents.IsCreated)
                    parents.Dispose();
                if (virtualBones.IsCreated)
                    virtualBones.Dispose();
            }
        }

        CharacterPoseNativeNodePoseBuffer CreateBuffer(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context)
        {
            int contributionCapacity = m_Resources.ContributionCount;
            if (node.Kind == CharacterPoseNodeKind.AnimationSlot ||
                node.Kind == CharacterPoseNodeKind.BlendStack)
            {
                CharacterAnimationSlotPosePayload slotPayload =
                    node.Payload as CharacterAnimationSlotPosePayload;
                CharacterBlendStackPosePayload stackPayload =
                    node.Payload as CharacterBlendStackPosePayload;
                CharacterPoseResourceSlot policySlot = slotPayload != null
                    ? slotPayload.BlendPolicySlot
                    : stackPayload != null ? stackPayload.BlendPolicySlot : null;
                CharacterPoseResourceBinding binding = policySlot == null
                    ? null
                    : m_Profile.FindPoseResourceBinding(policySlot);
                CharacterAnimationBlendPolicy policy = binding?.Resource as CharacterAnimationBlendPolicy;
                if (policy == null)
                    throw new InvalidOperationException(
                        $"Pose Blend Stack '{node.NodeId}' has no valid blend policy resource.");
                contributionCapacity = checked(policy.StackPolicy.MaxActiveSourceEntries + 1);
            }
            return new CharacterPoseNativeNodePoseBuffer(
                m_IndexByNode[node.NodeId],
                m_Rig.PoseBoneCount,
                Math.Max(1, m_InputContract.Parameters.Count),
                contributionCapacity);
        }

        ulong AllocateSubgraphInstanceId(ulong parentInstanceId, string subgraphNodeId)
        {
            if (string.IsNullOrWhiteSpace(subgraphNodeId))
                throw new ArgumentNullException(nameof(subgraphNodeId));
            if (m_NextSubgraphInstanceSequence == ulong.MaxValue)
                throw new InvalidOperationException("Pose subgraph instance identity was exhausted.");
            string stableHash = StableHash.Compute(
                parentInstanceId.ToString(CultureInfo.InvariantCulture),
                subgraphNodeId,
                m_NextSubgraphInstanceSequence.ToString(CultureInfo.InvariantCulture)).Value;
            m_NextSubgraphInstanceSequence++;
            ulong child = Convert.ToUInt64(stableHash.Substring(0, 16), 16);
            if (child == 0 || child == parentInstanceId)
                throw new InvalidOperationException(
                    $"Pose subgraph instance identity for '{subgraphNodeId}' collided with its parent instance.");
            return child;
        }

        int RequireBindingIndex(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            m_IndexByNode[node.NodeId];

        int RequireParameterIndex(PoseParameterId parameterId)
        {
            for (int i = 0; i < m_InputContract.Parameters.Count; i++)
            {
                if (m_InputContract.Parameters[i].ParameterId.Equals(parameterId))
                    return i;
            }
            throw new InvalidOperationException($"Pose domain parameter '{parameterId}' is missing.");
        }

        IReadOnlyList<PoseNodeId> CollectPlayerNodeIds()
        {
            var result = new PoseNodeId[m_IndexByNode.Count];
            foreach (KeyValuePair<PoseNodeId, int> node in m_IndexByNode)
                result[node.Value] = node.Key;
            return result;
        }

        IReadOnlyList<CharacterPresentationAnimationPropertyBinding> CompilePropertyBindings()
        {
            var bindings = new List<CharacterPresentationAnimationPropertyBinding>();
            foreach (CharacterAnimationPropertyAuthoringBinding authoring in m_Profile.AnimationPropertyBindings)
            {
                authoring.RequireValid();
                int parameterIndex = RequireParameterIndex(authoring.ParameterId);
                CharacterPoseParameterDeclaration declaration = m_InputContract.Parameters[parameterIndex];
                bindings.Add(new CharacterPresentationAnimationPropertyBinding(
                    $"property/{authoring.ParameterId}",
                    authoring.ParameterId,
                    parameterIndex,
                    declaration.Unit,
                    declaration.DefaultValue,
                    authoring.RendererBindingId,
                    authoring.ExpectedMesh,
                    authoring.MeshContentHash,
                    authoring.BlendShapeName,
                    authoring.BlendShapeIndex));
            }
            return bindings;
        }

        Dictionary<CharacterPresentationPoseSourceSlot, int> BuildSourceIndexes()
        {
            var result = new Dictionary<CharacterPresentationPoseSourceSlot, int>();
            for (int i = 0; i < m_Profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding = m_Profile.PoseSourceBindings[i];
                if (binding && binding.Slot && !result.TryAdd(binding.Slot, i))
                    throw new InvalidOperationException($"Pose Source binding #{i} duplicates a Slot.");
            }
            return result;
        }

        Dictionary<PoseNodeId, int> BuildNodeIndexes()
        {
            var result = new Dictionary<PoseNodeId, int>();
            foreach (CharacterPoseCanvasGraph graph in m_Profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node != null && !result.TryAdd(node.NodeId, result.Count))
                        throw new InvalidOperationException($"Pose node identity '{node.NodeId}' is duplicated.");
                }
            }
            return result;
        }

        static AnimationBlendSpacePlayerRuntime ThrowBlendSpace(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose Blend Space Player '{node.NodeId}' resource plan is not assembled.");

        static AnimationSelectedPosePlayerRuntime ThrowSelectedPlayer(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose Selected Player '{node.NodeId}' resource plan is not assembled.");

        static PresentationPoseSourceSample ThrowSelectedSample(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose Selected Player '{node.NodeId}' sample provider is not assembled.");

        AnimationBlendStackRuntime CreateBlendStack(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context)
        {
            CharacterAnimationSlotPosePayload slotPayload =
                node.Payload as CharacterAnimationSlotPosePayload;
            CharacterBlendStackPosePayload stackPayload =
                node.Payload as CharacterBlendStackPosePayload;
            CharacterPoseResourceSlot policySlot = slotPayload != null
                ? slotPayload.BlendPolicySlot
                : stackPayload != null ? stackPayload.BlendPolicySlot : null;
            if (policySlot == null)
                throw new InvalidOperationException($"Pose Blend Stack '{node.NodeId}' has no blend policy slot.");
            CharacterPoseResourceBinding binding =
                m_Profile.FindPoseResourceBinding(policySlot) ??
                throw new InvalidOperationException($"Pose Blend Stack '{node.NodeId}' has no blend policy resource binding.");
            CharacterAnimationBlendPolicy policy = binding.Resource as CharacterAnimationBlendPolicy ??
                throw new InvalidOperationException($"Pose Blend Stack '{node.NodeId}' blend policy resource is not a CharacterAnimationBlendPolicy.");
            var stackPolicyPayload = new AnimationBlendStackPolicyPayload(policy.StackPolicy);
            var transitions = new List<AnimationBlendTransitionPayload>();
            var curveEntries = new List<AnimationBlendCurveCatalogEntry>();
            var profileEntries = new List<AnimationBlendProfileCatalogEntry>();
            BuildTransition(
                policy.DefaultTransition, 0, transitions, curveEntries, profileEntries);
            for (int i = 0; i < policy.Overrides.Count; i++)
                BuildTransition(policy.Overrides[i].Rule, i + 1, transitions, curveEntries, profileEntries);
            var slotPayload2 = new AnimationBlendNodePayload(
                node.NodeId,
                policy.PolicyId,
                policy.Revision,
                stackPolicyPayload,
                transitions.ToArray(),
                null);
            var curveCatalog = new AnimationBlendCurveCatalogPayload(curveEntries.ToArray());
            var profileCatalog = new AnimationBlendProfileCatalogPayload(profileEntries.ToArray());
            AnimationSelectionAvailabilityPolicy availability = slotPayload != null
                ? slotPayload.SelectionAvailability
                : AnimationSelectionAvailabilityPolicy.RequireSelection;
            AnimationChannelId channelId = slotPayload != null
                ? slotPayload.AnimationChannelId
                : default;
            var stackBuffer = new CharacterPoseNativeNodePoseBuffer(
                m_IndexByNode[node.NodeId],
                m_Rig.PoseBoneCount,
                Math.Max(1, m_InputContract.Parameters.Count),
                checked(policy.StackPolicy.MaxActiveSourceEntries + 1));
            try
            {
                var writeBinding = stackBuffer.RequireWriteBinding(1);
                var runtime = new AnimationBlendStackRuntime(
                    slotPayload2,
                    channelId,
                    default,
                    default,
                    availability,
                    curveCatalog,
                    profileCatalog,
                    m_Rig,
                    in writeBinding);
                return runtime;
            }
            catch
            {
                stackBuffer.Dispose();
                throw;
            }
        }

        void BuildTransition(
            CharacterAnimationBlendTransitionRule rule,
            int index,
            List<AnimationBlendTransitionPayload> transitions,
            List<AnimationBlendCurveCatalogEntry> curveEntries,
            List<AnimationBlendProfileCatalogEntry> profileEntries)
        {
            AnimationBlendCurvePayload curvePayload = BuildCurvePayload(rule);
            int curveIndex = RequireOrAddCurve(curveEntries, curvePayload);
            var profilePayload = new AnimationBlendProfilePayload(
                rule.BlendProfile, m_Profile.RigDefinition);
            int profileIndex = RequireOrAddProfile(profileEntries, profilePayload);
            transitions.Add(new AnimationBlendTransitionPayload(
                index,
                AnimationBlendTransitionEndpointKind.SourceOwner,
                $"owner/{index}",
                index + 1,
                AnimationBlendTransitionEndpointKind.SourceOwner,
                $"owner/{index + 1}",
                rule.BlendLogic,
                rule.DurationSeconds,
                curveIndex,
                profileIndex));
        }

        static int RequireOrAddCurve(
            List<AnimationBlendCurveCatalogEntry> entries,
            AnimationBlendCurvePayload curve)
        {
            string hash = StableHash.Compute(AnimationBlendCanonicalPayload.CurveKey(curve)).ToString();
            for (int i = 0; i < entries.Count; i++)
            {
                AnimationBlendCurveCatalogEntry existing = entries[i];
                if (!string.Equals(existing.CanonicalHash, hash, StringComparison.Ordinal))
                    continue;
                if (!AnimationBlendCanonicalPayload.CurveEquals(existing.Curve, curve))
                    throw new InvalidOperationException(
                        $"Animation Blend Curve canonical hash collision occurs at entry #{i}.");
                return existing.Index;
            }
            int index = entries.Count;
            entries.Add(new AnimationBlendCurveCatalogEntry(index, curve));
            return index;
        }

        static int RequireOrAddProfile(
            List<AnimationBlendProfileCatalogEntry> entries,
            AnimationBlendProfilePayload profile)
        {
            string hash = StableHash.Compute(AnimationBlendCanonicalPayload.ProfileKey(profile)).ToString();
            for (int i = 0; i < entries.Count; i++)
            {
                AnimationBlendProfileCatalogEntry existing = entries[i];
                if (!string.Equals(existing.Profile.ProfileId, profile.ProfileId, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(existing.CanonicalHash, hash, StringComparison.Ordinal) ||
                    !AnimationBlendCanonicalPayload.ProfileEquals(existing.Profile, profile))
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Profile identity '{profile.ProfileId}' has conflicting payloads.");
                }
                return existing.Index;
            }
            int index = entries.Count;
            entries.Add(new AnimationBlendProfileCatalogEntry(index, profile));
            return index;
        }

        static AnimationBlendCurvePayload BuildCurvePayload(CharacterAnimationBlendTransitionRule rule)
        {
            if (rule.BlendMode == CharacterAnimationBlendMode.Custom && rule.CustomBlendCurve)
                return rule.CustomBlendCurve.Compile();
            var keys = new CharacterAnimationBlendCurveKey[2];
            switch (rule.BlendMode)
            {
                case CharacterAnimationBlendMode.Linear:
                    keys[0] = new CharacterAnimationBlendCurveKey(0f, 0f, 1f, 1f);
                    keys[1] = new CharacterAnimationBlendCurveKey(1f, 1f, 1f, 1f);
                    break;
                case CharacterAnimationBlendMode.EaseIn:
                    keys[0] = new CharacterAnimationBlendCurveKey(0f, 0f, 0f, 0f);
                    keys[1] = new CharacterAnimationBlendCurveKey(1f, 1f, 2f, 0f);
                    break;
                case CharacterAnimationBlendMode.EaseOut:
                    keys[0] = new CharacterAnimationBlendCurveKey(0f, 0f, 0f, 2f);
                    keys[1] = new CharacterAnimationBlendCurveKey(1f, 1f, 0f, 0f);
                    break;
                case CharacterAnimationBlendMode.EaseInOut:
                default:
                    keys[0] = new CharacterAnimationBlendCurveKey(0f, 0f, 0f, 0f);
                    keys[1] = new CharacterAnimationBlendCurveKey(1f, 1f, 0f, 0f);
                    break;
            }
            return new CharacterAnimationBlendCurve(keys).Compile();
        }

        static AnimationResolvedPoseSourceSample ThrowActionSample(
            CharacterPoseNativeInstanceContext context,
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId) =>
            throw new InvalidOperationException($"Pose Action source '{nodeId}/{sourceId}' has no committed sample.");

        static PresentationPoseSourceSample ThrowProviderSample(
            CharacterPoseNativeInstanceContext context,
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId) =>
            throw new InvalidOperationException($"Pose Provider source '{nodeId}/{sourceId}' has no sample.");

        static ICharacterPoseNativeLinkedPoseSource ThrowLinkedPose(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Linked Pose source '{node.NodeId}' is not assembled.");

        static ICharacterPoseNativeMotionMatchingSource ThrowMotionMatching(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Motion Matching source '{node.NodeId}' is not assembled.");

        static ICharacterPoseNativeHistoryCollectorSource ThrowHistorySource(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose History collector '{node.NodeId}' is not assembled.");

        static ICharacterPoseNativeEntryPoseSource ThrowEntryPose(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose Entry source '{node.NodeId}' is not assembled.");

        static RootMotionCurveAsset ThrowRootOrientationCurve(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Root Orientation curve '{node.NodeId}' is not assembled.");

        static ICharacterPoseNativeRootOrientationSource ThrowRootOrientationSource(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Root Orientation source '{node.NodeId}' is not assembled.");
    }

internal sealed class CharacterPoseNativeDomainServiceSet : IDisposable
{
    readonly IReadOnlyList<IDisposable> m_OwnedResources;
    bool m_Disposed;

    internal CharacterPoseNativeDomainServiceSet(
        CharacterPoseNativeDomainServices services,
        CharacterPoseConstraintRuntime constraints,
        IReadOnlyList<IDisposable> ownedResources)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        m_OwnedResources = ownedResources ?? throw new ArgumentNullException(nameof(ownedResources));
    }

    internal CharacterPoseNativeDomainServices Services { get; }
    internal CharacterPoseConstraintRuntime Constraints { get; }
    public void Dispose()
    {
        if (m_Disposed)
            return;
        m_Disposed = true;
        for (int i = m_OwnedResources.Count - 1; i >= 0; i--)
            m_OwnedResources[i].Dispose();
    }
}
}




