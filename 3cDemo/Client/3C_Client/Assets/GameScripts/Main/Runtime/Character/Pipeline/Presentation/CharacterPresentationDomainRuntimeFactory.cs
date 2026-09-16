using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public static class CharacterPresentationDomainRuntimeFactory
    {
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
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            CharacterEquipmentRigBindingCatalog equipmentRigBindings,
            SimulationSessionHost sessionHost,
            RuntimeDiagnosticsContext diagnostics,
            bool initializeExternalState)
        {
            _ = physicsScene;
            _ = sessionHost;
            _ = initializeExternalState;
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
            if (poseGraph != null && poseGraph.Nodes.Count > 0)
                throw new InvalidOperationException("Pose Native Source, Constraint, and Handler composition is not yet bound to the presentation domain factory.");
            if (presentationRole == CharacterPresentationRole.LocalOwner && cameraRig)
                throw new InvalidOperationException("Camera domain runtime is not yet bound to the presentation domain factory.");
            if (presentationRole == CharacterPresentationRole.LocalOwner && (!followAnchor || !aimAnchor || lookInput == null || string.IsNullOrWhiteSpace(lookInputId)))
                throw new ArgumentException("Local Presentation Camera inputs are incomplete.");
            if (presentationRole == CharacterPresentationRole.SimulatedActor &&
                (cameraRig || followAnchor || aimAnchor || cameraTargetBindings is { Count: > 0 } || lookInput != null || !string.IsNullOrEmpty(lookInputId)))
                throw new ArgumentException("Simulated Presentation cannot receive Camera owner inputs.");

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
            return new CharacterPresentationDomainRuntime(actorId, body, tickRate, animationPresentationProfile, equipment);
        }
    }
}



