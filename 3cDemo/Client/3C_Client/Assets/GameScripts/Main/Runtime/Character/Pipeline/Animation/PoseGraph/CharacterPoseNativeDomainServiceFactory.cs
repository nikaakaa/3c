using System;
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
        readonly Dictionary<CharacterPresentationPoseSourceSlot, int> m_SourceIndexBySlot;
        readonly Dictionary<PoseNodeId, int> m_IndexByNode;

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
            PhysicsScene physicsScene)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_InputContract = inputContract ?? throw new ArgumentNullException(nameof(inputContract));
            m_Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            m_ResourceScope = resourceScope ?? throw new ArgumentNullException(nameof(resourceScope));
            m_ActionCommandSource = actionCommandSource ?? throw new ArgumentNullException(nameof(actionCommandSource));
            m_EventFrameSource = eventFrameSource ?? throw new ArgumentNullException(nameof(eventFrameSource));
            m_World = world ? world : throw new ArgumentNullException(nameof(world));
            m_FutureBodyTranslationSource = futureBodyTranslationSource ?? throw new ArgumentNullException(nameof(futureBodyTranslationSource));
            if (!physicsScene.IsValid())
                throw new ArgumentException("Pose domain requires a valid PhysicsScene.", nameof(physicsScene));
            m_PhysicsScene = physicsScene;
            m_SourceIndexBySlot = BuildSourceIndexes();
            m_IndexByNode = BuildNodeIndexes();
            resources.RequireSourceCatalogComplete(profile);
        }

        internal CharacterPoseNativeDomainServices Create(
            ActorId actorId,
            CharacterAnimationRigBinding rigBinding,
            string posePlanHash,
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
            CharacterPoseNativeSourceResourceCatalog sourceCatalog = m_Resources.CreateSourceCatalog(m_Rig, m_ResourceScope);
            CharacterPoseNativeConstraintResourceCatalog constraintCatalog = m_Resources.CreateConstraintCatalog(m_Rig);
            CharacterPoseNativeManagedSourceResourceCatalog managedCatalog = m_Resources.CreateManagedCatalog(m_Profile, m_Rig);
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
                posePlanHash);
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
                ThrowBlendStack,
                ThrowActionSample,
                ThrowProviderSample,
                RequireBindingIndex,
                CreateBuffer);
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
                (node, context) => instanceId,
                (node, context) => resetGeneration,
                (node, context) => $"pose-subgraph/{node.NodeId}",
                ThrowRootOrientationCurve,
                ThrowRootOrientationSource,
                CreateBuffer);
            return new CharacterPoseNativeDomainServices(
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
                m_Resources.ContributionCount);
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
                payload.ClockSource,
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
            var parents = new NativeArray<int>(m_Rig.PhysicalBoneCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var virtualBones = new NativeArray<CharacterVirtualBoneDescriptor>(m_Rig.VirtualBoneCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int i = 0; i < m_Rig.PhysicalBoneCount; i++)
                    parents[i] = m_Rig.PhysicalBones[i].ParentPhysicalIndex;
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
            CharacterPoseNativeInstanceContext context) =>
            new CharacterPoseNativeNodePoseBuffer(
                m_IndexByNode[node.NodeId],
                m_Rig.PoseBoneCount,
                Math.Max(1, m_InputContract.Parameters.Count),
                m_Resources.ContributionCount);

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
            var result = new List<PoseNodeId>();
            foreach (CharacterPoseCanvasGraph graph in m_Profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node != null &&
                        (node.PresentationPoseSourceSlot ||
                         node.Kind == CharacterPoseNodeKind.PoseStateMachine ||
                         node.Kind == CharacterPoseNodeKind.AnimationSlot) &&
                        !result.Contains(node.NodeId))
                    {
                        result.Add(node.NodeId);
                    }
                }
            }
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

        static AnimationBlendStackRuntime ThrowBlendStack(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context) =>
            throw new InvalidOperationException($"Pose Animation Slot '{node.NodeId}' lifecycle stack is not assembled.");

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
}




