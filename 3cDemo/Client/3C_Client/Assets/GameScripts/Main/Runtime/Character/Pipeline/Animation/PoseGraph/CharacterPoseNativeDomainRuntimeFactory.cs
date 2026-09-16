using ThirdPersonSimulation;

using System;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeDomainRuntimeFactory
    {
        internal static CharacterPoseNativeDomainCreateResult Create(
            ulong requestId,
            ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseNativeDomainServices services,
            out CharacterPoseNativeDomainSession session)
        {
            session = null;
            CharacterPoseNativeDomainCreateResult validation =
                Validate(
                    requestId,
                    in actorId,
                    profile,
                    rig,
                    inputContract,
                    resourceRevision,
                    animancer,
                    rigBinding,
                    rootHierarchy,
                    instanceId,
                    resetGeneration,
                    reason,
                    services);
            if (!validation.IsAdopted)
                return validation;

            CharacterPoseSourceModule source = null;
            CharacterPoseConstraintRuntime constraints = null;
            try
            {
                int nodeCount = CountNodes(profile);
                source = new CharacterPoseSourceModule(
                    animancer,
                    profile,
                    rigBinding,
                    rig,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    services.ResourceScope,
                    inputContract.Parameters.Count);
                constraints = new CharacterPoseConstraintRuntime(
                    services.Constraints.FootPlacement,
                    services.Constraints.PoseBoneContributions,
                    services.Constraints.GoalAssemblers,
                    services.Constraints.Solver,
                    services.Constraints.ContributionCount,
                    services.Constraints.ContributionGoalCount,
                    rig.RigId,
                    rig.RigRevision);
                var context = new CharacterPoseNativeInstanceContext(
                    actorId,
                    animancer,
                    rig,
                    rigBinding,
                    rootHierarchy);
                CharacterPoseNativeAdoptedResult adopted =
                    CharacterPoseNativeRoleEntry.Create(
                        requestId,
                        actorId,
                        profile,
                        rig,
                        inputContract,
                        resourceRevision,
                        in context,
                        instanceId,
                        resetGeneration,
                        reason,
                        source,
                        constraints,
                        services.SourceHandlers,
                        services.ConstraintHandlers,
                        services.ManagedHandlers,
                        services.AnimationProperties,
                        services.PlayerNodeIds,
                        services.ContributionCapacity,
                        out CharacterPoseNativeRoleSession roleSession);
                if (!adopted.IsAdopted)
                    return CharacterPoseNativeDomainCreateResult.Failed(adopted);
                session = new CharacterPoseNativeDomainSession(
                    roleSession,
                    services.ActionCommandSource,
                    services.EventFrameSource);
                return CharacterPoseNativeDomainCreateResult.Ready(adopted, session);
            }
            catch (Exception exception)
            {
                source?.Dispose();
                constraints?.Dispose();
                return CharacterPoseNativeDomainCreateResult.Failed(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    "CharacterPoseNativeDomainRuntimeFactory.Create",
                    exception.Message);
            }
        }

        internal static CharacterPoseNativeDomainCreateResult Replace(
            CharacterPoseNativeDomainSession current,
            ulong requestId,
            ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseNativeDomainServices services,
            out CharacterPoseNativeDomainSession session)
        {
            session = null;
            if (current == null)
                return CharacterPoseNativeDomainCreateResult.Failed(
                    CharacterPoseNativeFailureCode.Disposed,
                    "CharacterPoseNativeDomainRuntimeFactory.Replace",
                    "Current Pose domain session is missing.");
            CharacterPoseNativeDomainCreateResult validation =
                Validate(
                    requestId,
                    in actorId,
                    profile,
                    rig,
                    inputContract,
                    resourceRevision,
                    animancer,
                    rigBinding,
                    rootHierarchy,
                    instanceId,
                    resetGeneration,
                    reason,
                    services);
            if (!validation.IsAdopted)
                return validation;

            CharacterPoseSourceModule source = null;
            CharacterPoseConstraintRuntime constraints = null;
            try
            {
                int nodeCount = CountNodes(profile);
                source = new CharacterPoseSourceModule(
                    animancer,
                    profile,
                    rigBinding,
                    rig,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    nodeCount,
                    services.ResourceScope,
                    inputContract.Parameters.Count);
                constraints = new CharacterPoseConstraintRuntime(
                    services.Constraints.FootPlacement,
                    services.Constraints.PoseBoneContributions,
                    services.Constraints.GoalAssemblers,
                    services.Constraints.Solver,
                    services.Constraints.ContributionCount,
                    services.Constraints.ContributionGoalCount,
                    rig.RigId,
                    rig.RigRevision);
                var context = new CharacterPoseNativeInstanceContext(
                    actorId,
                    animancer,
                    rig,
                    rigBinding,
                    rootHierarchy);
                CharacterPoseNativeAdoptedResult adopted =
                    CharacterPoseNativeRoleEntry.Replace(
                        current.RoleSession,
                        requestId,
                        actorId,
                        profile,
                        rig,
                        inputContract,
                        resourceRevision,
                        in context,
                        instanceId,
                        resetGeneration,
                        reason,
                        source,
                        constraints,
                        services.SourceHandlers,
                        services.ConstraintHandlers,
                        services.ManagedHandlers,
                        services.AnimationProperties,
                        services.PlayerNodeIds,
                        services.ContributionCapacity,
                        out CharacterPoseNativeRoleSession roleSession);
                if (!adopted.IsAdopted)
                    return CharacterPoseNativeDomainCreateResult.Failed(adopted);
                session = new CharacterPoseNativeDomainSession(
                    roleSession,
                    services.ActionCommandSource,
                    services.EventFrameSource);
                return CharacterPoseNativeDomainCreateResult.Ready(adopted, session);
            }
            catch (Exception exception)
            {
                source?.Dispose();
                constraints?.Dispose();
                return CharacterPoseNativeDomainCreateResult.Failed(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    "CharacterPoseNativeDomainRuntimeFactory.Replace",
                    exception.Message);
            }
        }

        static CharacterPoseNativeDomainCreateResult Validate(
            ulong requestId,
            in ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseNativeDomainServices services)
        {
            if (requestId == 0 || !actorId.IsValid)
                return FailInput("request identity", CharacterPoseNativeFailureCode.ActorMissing);
            if (!profile)
                return FailInput("Presentation Profile", CharacterPoseNativeFailureCode.ProfileMissing);
            if (rig == null)
                return FailInput("Rig Payload", CharacterPoseNativeFailureCode.RigMissing);
            if (inputContract == null)
                return FailInput("Animation Input Contract", CharacterPoseNativeFailureCode.InputContractMissing);
            if (string.IsNullOrWhiteSpace(resourceRevision))
                return FailInput("resource revision", CharacterPoseNativeFailureCode.ResourceMissing);
            if (!animancer)
                return FailInput("Animancer Component", CharacterPoseNativeFailureCode.ActorMissing);
            if (!rigBinding || !rootHierarchy)
                return FailInput("Rig or Root Hierarchy binding", CharacterPoseNativeFailureCode.RigMissing);
            if (instanceId == 0 || resetGeneration == 0 || string.IsNullOrWhiteSpace(reason))
                return FailInput("instance identity", CharacterPoseNativeFailureCode.GraphInvalid);
            if (services == null)
                return FailInput("domain services", CharacterPoseNativeFailureCode.ResourceMissing);
            if (services.ResourceScope == null)
                return FailInput("animation resource scope", CharacterPoseNativeFailureCode.ResourceMissing);
            if (services.ActionCommandSource == null)
                return FailInput("action command source", CharacterPoseNativeFailureCode.ResourceMissing);
            if (services.EventFrameSource == null)
                return FailInput("EventGraph typed Frame source", CharacterPoseNativeFailureCode.ResourceMissing);
            if (services.SourceHandlers == null || services.ConstraintHandlers == null ||
                services.ManagedHandlers == null)
                return FailInput("handler composition", CharacterPoseNativeFailureCode.UnsupportedNode);
            if (services.Constraints == null || services.Constraints.Solver == null)
                return FailInput("Constraint runtime service", CharacterPoseNativeFailureCode.ConstraintFailed);
            if (RequiresFootPlacement(profile) && services.Constraints.FootPlacement == null)
                return FailInput("Foot Placement runtime service", CharacterPoseNativeFailureCode.ConstraintFailed);
            return CharacterPoseNativeDomainCreateResult.Failed(
                CharacterPoseNativeFailureCode.None,
                string.Empty,
                string.Empty);
        }

        static CharacterPoseNativeDomainCreateResult FailInput(
            string name,
            CharacterPoseNativeFailureCode failureCode) =>
            CharacterPoseNativeDomainCreateResult.Failed(
                failureCode,
                "CharacterPoseNativeDomainRuntimeFactory.Validate",
                $"Pose domain {name} is missing.");

        static int CountNodes(CharacterAnimationPresentationProfile profile)
        {
            int count = 0;
            foreach (CharacterPoseCanvasGraph graph in profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node != null)
                        count++;
                }
            }
            return count > 0 ? count : throw new InvalidOperationException("Pose domain graph has no nodes.");
        }

        static bool RequiresFootPlacement(CharacterAnimationPresentationProfile profile)
        {
            foreach (CharacterPoseCanvasGraph graph in profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node != null && node.Kind == CharacterPoseNodeKind.FootPlacement)
                        return true;
                }
            }
            return false;
        }
    }
}

