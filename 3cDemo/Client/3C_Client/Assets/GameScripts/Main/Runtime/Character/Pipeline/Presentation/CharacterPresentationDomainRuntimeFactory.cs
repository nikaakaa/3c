using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using TEngine;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Unity.Resources;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public static class CharacterPresentationDomainRuntimeFactory
    {
        static ulong m_NextRequestId = 1;
#if UNITY_EDITOR
        public static event Action<string, string, long> StartupMilestone;

        static void MarkStartup(ActorId actorId, string phase)
        {
            var listener = StartupMilestone;
            if (listener != null)
                listener(actorId.Value, phase, System.Diagnostics.Stopwatch.GetTimestamp());
        }
#endif

        public static ICharacterPresentationDomainRuntime Create(
            int tickRate,
            CharacterAnimationPresentationProfile animationPresentationProfile,
            CharacterAnimationRigPayload animationRig,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPresentationBodyState initialBody,
            CharacterPresentationRole presentationRole,
            PreparedCharacterLocomotionPresentationBinding locomotionBinding,
            CharacterWorldAwarePresentationBinding worldAwarePresentation,
            PhysicsScene physicsScene,
            CinemachineCameraRigAdapter cameraRig,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput lookInput,
            string lookInputId,
            CharacterCameraProfile cameraProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            CharacterEquipmentRigBindingCatalog equipmentRigBindings,
            SimulationSessionHost sessionHost,
            RuntimeDiagnosticsContext diagnostics,
            bool initializeExternalState,
            ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterTimelineHost characterTimelineHost = null)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (!animationPresentationProfile)
                throw new ArgumentNullException(nameof(animationPresentationProfile));
            animationRig = animationRig ?? throw new ArgumentNullException(nameof(animationRig));
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Actor identity is invalid.", nameof(actorId));
            if (!animancer || !animancer.Animator)
                throw new ArgumentException("Presentation Animancer is incomplete.", nameof(animancer));
            if (!animationRigBinding || !rootHierarchy)
                throw new ArgumentException("Presentation rig or root hierarchy binding is incomplete.");
            if (!worldAwarePresentation)
                throw new ArgumentNullException(nameof(worldAwarePresentation));
            if (characterTimelineHost != null && sessionHost == null)
                throw new ArgumentNullException(nameof(sessionHost));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (presentationRole != CharacterPresentationRole.LocalOwner &&
                presentationRole != CharacterPresentationRole.SimulatedActor)
                throw new ArgumentOutOfRangeException(nameof(presentationRole));
            locomotionBinding.RequireValid();
            CharacterBodyPresentationProfile bodyPresentationProfile = locomotionBinding.BodyProfile;

            rootHierarchy.RequireValid();
            worldAwarePresentation.RequireValid();
            animationRigBinding.RequireValid(animationRig);
            if (animancer.Animator.transform != rootHierarchy.PoseRoot)
                throw new InvalidOperationException("Presentation Animancer Animator must use the formal PoseRoot.");
            if (worldAwarePresentation.PresentationRoot != rootHierarchy.VisualRoot ||
                worldAwarePresentation.SelfColliderRoot != rootHierarchy.LogicRoot)
                throw new InvalidOperationException("Presentation World-Aware binding must match the formal root hierarchy.");

            var poseGraph = animationPresentationProfile.PoseGraph ? animationPresentationProfile.PoseGraph.Graph : null;
            CharacterPoseNativeDomainResourceSet poseResources = animationPresentationProfile.PoseNativeDomainResources;
            if (poseGraph != null && poseGraph.Nodes.Count > 0 && !poseResources)
                throw new InvalidOperationException("Pose Native Domain Resource Set is missing.");
            if (presentationRole == CharacterPresentationRole.SimulatedActor &&
                (cameraRig || followAnchor || aimAnchor || cameraTargetBindings is { Count: > 0 } || lookInput != null || !string.IsNullOrEmpty(lookInputId)))
                throw new ArgumentException("Simulated Presentation cannot receive Camera owner inputs.");

            bool hasCamera = cameraRig;
            bool hasCameraInputs = followAnchor || aimAnchor || cameraTargetBindings is { Count: > 0 } || lookInput != null || !string.IsNullOrEmpty(lookInputId);
            if (presentationRole == CharacterPresentationRole.LocalOwner && (!hasCamera || !cameraProfile || !hasCameraInputs))
                throw new ArgumentException("Local Presentation Camera inputs are incomplete.");
            if (presentationRole != CharacterPresentationRole.LocalOwner && (hasCamera || cameraProfile || hasCameraInputs))
                throw new ArgumentException("Simulated Presentation cannot receive Camera owner inputs.");
            CharacterCameraDomainRuntime camera = presentationRole == CharacterPresentationRole.LocalOwner
                ? new CharacterCameraDomainRuntime(
                    actorId,
                    cameraProfile,
                    cameraRig,
                    cameraTargetBindings,
                    initialBody,
                    followAnchor,
                    aimAnchor,
                    lookInput,
                    lookInputId,
                    physicsScene,
                    rootHierarchy,
                    diagnostics,
                    initializeExternalState)
                : null;
#if UNITY_EDITOR
            MarkStartup(actorId, "presentation-camera-created");
#endif
            bool hasEquipmentProfile = equipmentPresentationProfile;
            bool hasEquipmentCatalog = equipmentRigBindings;
            if (hasEquipmentProfile != hasEquipmentCatalog)
                throw new ArgumentException("Equipment presentation profile or rig binding is incomplete.");
            CharacterEquipmentDomainRuntime equipment = equipmentPresentationProfile
                ? new CharacterEquipmentDomainRuntime(
                    actorId,
                    equipmentPresentationProfile,
                    equipmentRigBindings,
                    initializeExternalState)
                : null;
            var body = new CharacterBodyPresentationRuntime(
                actorId,
                tickRate,
                locomotionBinding.RuntimeBodySource,
                bodyPresentationProfile.BuildSettings(),
                rootHierarchy,
                initialBody,
                diagnostics);

            CharacterPoseNativeDomainInstance poseDomain = null;
            CharacterAnimationResourceScope resourceScope = null;
            var actionPlayback = new ActionAnimationPlaybackRuntime(64, 128);
            IActionPresentationClockCoordinator presentationClockCoordinator =
                new CommittedFollowPresentationClockCoordinator(actionPlayback);
            IActionPresentationClockPolicy locomotionClockPolicy =
                locomotionBinding.ClockMode == CharacterLocomotionClockMode.CommittedMovement
                    ? new CommittedMovementPresentationClockPolicy()
                    : FreeRunPresentationClockPolicy.Shared;
            var runtime = new CharacterPresentationDomainRuntime(
                actorId,
                body,
                locomotionBinding,
                tickRate,
                animationPresentationProfile,
                equipment,
                camera,
                presentationClockCoordinator,
                diagnostics);
#if UNITY_EDITOR
            MarkStartup(actorId, "presentation-body-created");
#endif
            try
            {
#if KK_DIAGNOSTIC_SAMPLING
                runtime.BindRenderCapture(animationRigBinding, animationRig, rootHierarchy,
                    cameraRig ? cameraRig.Brain.OutputCamera : null);
#endif
                if (poseResources)
                {
                    var inputContract = CharacterAnimationInputContract.Create(animationPresentationProfile);
                    resourceScope = new CharacterAnimationResourceScope(
                        new YooAssetCharacterAnimationAssetLoader(
                            ModuleSystem.GetModule<IResourceModule>(),
                            poseResources.ResourcePackageName),
                        new CharacterAnimationResourceSettings(poseResources.ResourceResidentBudgetBytes));
                    var inbox = new ActionPlaybackCommandInbox(64);
                    runtime.BindPoseActionPublisher(new CharacterPoseActionCommandPublisher(inbox));
                    var actionCommandSource = new CharacterPoseNativeActionCommandSource(actorId, inbox);
                    var serviceFactory = new CharacterPoseNativeDomainServiceFactory(
                        animationPresentationProfile,
                        animationRig,
                        inputContract,
                        poseResources,
                        resourceScope,
                        actionCommandSource,
                        actionPlayback,
                        runtime,
                        worldAwarePresentation,
                        sessionHost,
                        physicsScene,
                        node => node.IsLocomotionParticipant
                            ? locomotionClockPolicy
                            : node.AnimationChannelId.IsValid
                                ? presentationClockCoordinator.CreatePolicy()
                                : FreeRunPresentationClockPolicy.Shared);
#if UNITY_EDITOR
                    MarkStartup(actorId, "pose-service-factory-created");
#endif
                    var createResult = CharacterPoseNativeDomainRuntimeFactory.Create(
                        NextRequestId(),
                        actorId,
                        animationPresentationProfile,
                        animationRig,
                        inputContract,
                        inputContract.ContractHash,
                        animancer,
                        animationRigBinding,
                        rootHierarchy,
                        ResolvePoseInstanceId(actorId),
                        1,
                        "presentation-domain-create",
                        serviceFactory,
                        out CharacterPoseNativeDomainSession session);
#if UNITY_EDITOR
                    MarkStartup(actorId, "pose-domain-created");
#endif
                    if (!createResult.IsAdopted)
                        throw new InvalidOperationException(
                            $"Pose Native Domain creation failed: {createResult.FailureCode} {createResult.Source} {createResult.Message}");
                    poseDomain = new CharacterPoseNativeDomainInstance(session, actionCommandSource);
                    runtime.BindPoseDomain(poseDomain, resourceScope, inputContract.Parameters);
                }
                if (characterTimelineHost != null)
                    runtime.InitializeTimelineHost(characterTimelineHost, sessionHost.TimelineNumericTarget, sessionHost.TickRate);
#if UNITY_EDITOR
                MarkStartup(actorId, "presentation-timeline-initialized");
#endif
                return runtime;
            }
            catch
            {
                poseDomain?.Dispose();
                resourceScope?.Dispose();
                runtime.Dispose();
                throw;
            }
        }

        static ulong NextRequestId()
        {
            m_NextRequestId++;
            return m_NextRequestId;
        }

        static ulong ResolvePoseInstanceId(ActorId actorId)
        {
            string stableHash = StableHash.Compute(actorId.Value).Value;
            ulong instanceId = Convert.ToUInt64(stableHash.Substring(0, 16), 16);
            return instanceId != 0
                ? instanceId
                : throw new InvalidOperationException(
                    $"Pose instance identity for actor '{actorId.Value}' hashed to zero.");
        }
    }
}
