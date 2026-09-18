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
            CharacterBodyPresentationProfile bodyPresentationProfile,
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
            if (!bodyPresentationProfile)
                throw new ArgumentNullException(nameof(bodyPresentationProfile));
            if (!worldAwarePresentation)
                throw new ArgumentNullException(nameof(worldAwarePresentation));
            if (sessionHost == null)
                throw new ArgumentNullException(nameof(sessionHost));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (!Enum.IsDefined(typeof(CharacterPresentationRole), presentationRole))
                throw new ArgumentOutOfRangeException(nameof(presentationRole));

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
                    initializeExternalState)
                : null;
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
                presentationRole == CharacterPresentationRole.LocalOwner
                    ? CharacterBodyPresentationSourceMode.CommittedStream
                    : CharacterBodyPresentationSourceMode.SelectedStream,
                bodyPresentationProfile.BuildSettings(),
                rootHierarchy,
                initialBody,
                diagnostics);

            CharacterPoseNativeDomainInstance poseDomain = null;
            CharacterAnimationResourceScope resourceScope = null;
            IActionPresentationClockCoordinator presentationClockCoordinator =
                presentationRole == CharacterPresentationRole.SimulatedActor
                    ? new CommittedFollowPresentationClockCoordinator()
                    : null;
            var runtime = new CharacterPresentationDomainRuntime(
                actorId,
                body,
                tickRate,
                animationPresentationProfile,
                equipment,
                camera,
                presentationClockCoordinator);
            try
            {
                if (poseResources)
                {
                    var inputContract = CharacterAnimationInputContract.Create(animationPresentationProfile);
                    resourceScope = new CharacterAnimationResourceScope(
                        new YooAssetCharacterAnimationAssetLoader(
                            ModuleSystem.GetModule<IResourceModule>(),
                            poseResources.ResourcePackageName),
                        new CharacterAnimationResourceSettings(poseResources.ResourceResidentBudgetBytes));
                    var inbox = new ActionPlaybackCommandInbox(64);
                    var actionCommandSource = new CharacterPoseNativeActionCommandSource(actorId, inbox);
                    var serviceFactory = new CharacterPoseNativeDomainServiceFactory(
                        animationPresentationProfile,
                        animationRig,
                        inputContract,
                        poseResources,
                        resourceScope,
                        actionCommandSource,
                        runtime,
                        worldAwarePresentation,
                        sessionHost,
                        physicsScene,
                        node => presentationClockCoordinator != null
                            ? presentationClockCoordinator.CreatePolicy()
                            : FreeRunPresentationClockPolicy.Shared);
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
                        1,
                        1,
                        "presentation-domain-create",
                        serviceFactory,
                        out CharacterPoseNativeDomainSession session);
                    if (!createResult.IsAdopted)
                        throw new InvalidOperationException(
                            $"Pose Native Domain creation failed: {createResult.FailureCode} {createResult.Source} {createResult.Message}");
                    poseDomain = new CharacterPoseNativeDomainInstance(session, actionCommandSource);
                    runtime.BindPoseDomain(poseDomain, resourceScope, inputContract.Parameters);
                }
                if (characterTimelineHost != null)
                    runtime.InitializeTimelineHost(characterTimelineHost, sessionHost.TimelineNumericTarget, sessionHost.TickRate);
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
    }
}

