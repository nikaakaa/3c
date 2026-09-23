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
            CharacterPoseNativeDomainServiceFactory serviceFactory,
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
                    serviceFactory);
            if (validation.IsFailure)
                return validation;

            CharacterPoseSourceModule source = null;
            CharacterPoseNativeDomainServiceSet services = null;
            try
            {
                animancer.Graph.UpdateMode = UnityEngine.Playables.DirectorUpdateMode.Manual;
                int nodeCount = CountNodes(profile);
                source = CreateSource(
                    animancer,
                    profile,
                    rigBinding,
                    rig,
                    inputContract,
                    nodeCount,
                    serviceFactory);
                services = serviceFactory.Create(
                    actorId,
                    rigBinding,
                    requestId,
                    instanceId,
                    resetGeneration,
                    source,
                    () => source.CurrentLease);
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
                        services.Constraints,
                        services.Services.SourceHandlers,
                        services.Services.ConstraintHandlers,
                        services.Services.ManagedHandlers,
                        services.Services.AnimationProperties,
                        services.Services.PlayerNodeIds,
                        services.Services.ContributionCapacity,
                        out CharacterPoseNativeRoleSession roleSession);
                if (!adopted.IsAdopted)
                {
                    services.Dispose();
                    return CharacterPoseNativeDomainCreateResult.Failed(adopted);
                }
                session = new CharacterPoseNativeDomainSession(
                    roleSession,
                    services.Services.ActionCommandSource,
                    services.Services.EventFrameSource,
                    services);
                return CharacterPoseNativeDomainCreateResult.Ready(adopted, session);
            }
            catch (Exception exception)
            {
                services?.Dispose();
                source?.Dispose();
                return CharacterPoseNativeDomainCreateResult.Failed(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    "CharacterPoseNativeDomainRuntimeFactory.Create",
                    exception.ToString());
            }
        }

        static CharacterPoseSourceModule CreateSource(
            AnimancerComponent animancer,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            int nodeCount,
            CharacterPoseNativeDomainServiceFactory serviceFactory)
        {
            if (serviceFactory == null)
                throw new ArgumentNullException(nameof(serviceFactory));
            return new CharacterPoseSourceModule(
                animancer,
                profile,
                rigBinding,
                rig,
                nodeCount,
                nodeCount,
                nodeCount,
                nodeCount,
                nodeCount,
                serviceFactory.ResourceScope,
                inputContract.Parameters.Count);
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
            CharacterPoseNativeDomainServiceFactory serviceFactory)
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
            if (serviceFactory == null)
                return FailInput("domain service factory", CharacterPoseNativeFailureCode.ResourceMissing);
            return CharacterPoseNativeDomainCreateResult.Valid();
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
    }
}
