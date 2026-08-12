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
    public static class CharacterPresentationRuntimeFactory
    {
        public static CharacterPresentationProjection LoadProjection(
            CharacterPresentationProjectionAsset projectionAsset,
            CharacterPresentationSemanticContract contract)
        {
            if (!projectionAsset)
                throw new ArgumentNullException(nameof(projectionAsset));
            return projectionAsset.Load(contract);
        }

        public static CharacterPresentationRuntimeBinding CreateLocalOwner(
            CharacterPresentationProjectionAsset projectionAsset,
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            ThirdPersonCameraController cameraRig,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput inputAdapter,
            string lookInputId,
            RuntimeDiagnosticsContext diagnostics)
        {
            return CreateLocalOwner(
                contract,
                simulationTickRate,
                LoadProjection(projectionAsset, contract),
                actorId,
                animancer,
                animationRigBinding,
                visualRoot,
                initialBody,
                bodyProfile,
                worldAwareBinding,
                physicsScene,
                cameraRig,
                followAnchor,
                aimAnchor,
                cameraTargetBindings,
                inputAdapter,
                lookInputId,
                null,
                diagnostics);
        }

        public static CharacterPresentationRuntimeBinding CreateLocalOwner(
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            CharacterPresentationProjection projection,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            ThirdPersonCameraController cameraRig,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput inputAdapter,
            string lookInputId,
            CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
            RuntimeDiagnosticsContext diagnostics)
        {
            return Create(
                contract,
                simulationTickRate,
                projection,
                actorId,
                animancer,
                animationRigBinding,
                visualRoot,
                initialBody,
                CharacterBodyPresentationSourceMode.CommittedStream,
                RequireBodySettings(bodyProfile),
                worldAwareBinding,
                physicsScene,
                cameraRig,
                followAnchor,
                aimAnchor,
                cameraTargetBindings,
                inputAdapter,
                lookInputId,
                equipmentRigCatalog,
                diagnostics);
        }

        public static CharacterPresentationRuntimeBinding CreateSimulatedActor(
            CharacterPresentationProjectionAsset projectionAsset,
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            RuntimeDiagnosticsContext diagnostics)
        {
            return CreateSimulatedActor(
                contract,
                simulationTickRate,
                LoadProjection(projectionAsset, contract),
                actorId,
                animancer,
                animationRigBinding,
                visualRoot,
                initialBody,
                bodyProfile,
                worldAwareBinding,
                physicsScene,
                null,
                diagnostics);
        }

        public static CharacterPresentationRuntimeBinding CreateSimulatedActor(
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            CharacterPresentationProjection projection,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
            RuntimeDiagnosticsContext diagnostics)
        {
            return Create(
                contract,
                simulationTickRate,
                projection,
                actorId,
                animancer,
                animationRigBinding,
                visualRoot,
                initialBody,
                CharacterBodyPresentationSourceMode.CommittedStream,
                RequireBodySettings(bodyProfile),
                worldAwareBinding,
                physicsScene,
                null,
                null,
                null,
                null,
                null,
                string.Empty,
                equipmentRigCatalog,
                diagnostics);
        }

        public static CharacterPresentationRuntimeBinding CreateObservedActor(
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            CharacterPresentationProjection projection,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            RuntimeDiagnosticsContext diagnostics)
        {
            return Create(
                contract,
                simulationTickRate,
                projection,
                actorId,
                animancer,
                animationRigBinding,
                visualRoot,
                initialBody,
                CharacterBodyPresentationSourceMode.SelectedStream,
                RequireBodySettings(bodyProfile),
                worldAwareBinding,
                physicsScene,
                null,
                null,
                null,
                null,
                null,
                string.Empty,
                null,
                diagnostics);
        }

        static CharacterPresentationRuntimeBinding Create(
            CharacterPresentationSemanticContract contract,
            int simulationTickRate,
            CharacterPresentationProjection projection,
            ActorId actorId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            CharacterBodyPresentationSourceMode bodySourceMode,
            CharacterBodyPresentationSettings bodySettings,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            ThirdPersonCameraController cameraRig,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput inputAdapter,
            string lookInputId,
            CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (!animationRigBinding)
                throw new ArgumentNullException(nameof(animationRigBinding));
            if (!visualRoot)
                throw new ArgumentNullException(nameof(visualRoot));
            if (!animancer || !animancer.Animator)
                throw new ArgumentNullException(nameof(animancer));
            projection.RequireContract(contract);
            projection.RequirePosePayload();
            projection.RequireTuningPayload();
            animationRigBinding.RequireValid(projection.Rig);
            Transform animatorRoot = animancer.Animator.transform;
            if (animatorRoot == visualRoot || !animatorRoot.IsChildOf(visualRoot))
                throw new InvalidOperationException("Animator Root must be a strict child of the Presentation VisualRoot.");
            if (animationRigBinding.Animator != animancer.Animator ||
                animationRigBinding.PhysicalBones[projection.Rig.RootPhysicalBoneIndex] != animatorRoot)
            {
                throw new InvalidOperationException("Animation Rig root must match the Animancer Animator Root exactly.");
            }

            CharacterBodyPresentationRuntime body = null;
            CharacterAnimationPresentationRuntime animation = null;
            CharacterFootPlacementRuntime footPlacement = null;
            CharacterCameraPresentationRuntime camera = null;
            CharacterEquipmentVisualRuntime equipment = null;
            CharacterMotionMatchingPresentationModule motionMatching = null;
            try
            {
                body = new CharacterBodyPresentationRuntime(
                    actorId,
                    simulationTickRate,
                    bodySourceMode,
                    bodySettings,
                    visualRoot,
                    initialBody,
                    diagnostics);
                CharacterAnimationPresentationBindings animationBindings =
                    CharacterAnimationPresentationBindingFactory.Build(
                        contract,
                        projection);
                if (projection.MotionMatching != null)
                {
                    motionMatching = new CharacterMotionMatchingPresentationModule(
                        actorId,
                        bodySourceMode,
                        projection);
                }
                animation = new CharacterAnimationPresentationRuntime(
                    actorId,
                    animationBindings,
                    motionMatching,
                    animancer,
                    animationRigBinding,
                    true);
                animation.SetTuningBinding(
                    new CharacterPoseTuningRuntimeBinding(
                        new CharacterPoseTuningTargetIdentity(
                            actorId.Value,
                            projection.ProgramId,
                            projection.ProjectionRevision,
                            projection.PosePlan.PlanHash,
                            projection.Rig.RigId,
                            projection.Rig.RigRevision,
                            projection.TuningLayout.LayoutHash),
                        projection.TuningLayout,
                        projection.TuningDefaultBlock,
                        projection.PublishedParameterRevision));
                motionMatching = null;
                equipment = new CharacterEquipmentVisualRuntime(
                    actorId,
                    projection,
                    equipmentRigCatalog,
                    diagnostics);
                if (projection.PosePlan.FootGroundings.Count == 1)
                {
                    if (!worldAwareBinding)
                        throw new ArgumentNullException(nameof(worldAwareBinding));
                    if (!physicsScene.IsValid())
                        throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
                    CharacterPresentationFootGroundingDescriptor descriptor =
                        projection.PosePlan.FootGroundings[0];
                    CharacterFootPlacementPublicationValidation.Require(projection, descriptor.Calibration);
                    CharacterFootPlacementPoseRig rig = new CharacterFootPlacementPoseRig(
                        descriptor.Calibration,
                        projection.Rig,
                        animationRigBinding,
                        worldAwareBinding);
                    rig.RequireValid();
                    if (rig.VisualRoot != visualRoot)
                        throw new InvalidOperationException("World-Aware Presentation Root must match Presentation VisualRoot exactly.");
                    CharacterFootPlacementRuntimeSettings footPlacementSettings =
                        descriptor.Profile.BuildSettings(projection, rig);
                    footPlacement = new CharacterFootPlacementRuntime(
                        actorId,
                        footPlacementSettings,
                        rig,
                        physicsScene);
                }
                if (cameraRig)
                {
                    camera = new CharacterCameraPresentationRuntime(
                        projection,
                        cameraRig,
                        initialBody,
                        followAnchor,
                        aimAnchor,
                        cameraTargetBindings,
                        inputAdapter,
                        lookInputId);
                }
                else if (followAnchor || aimAnchor || inputAdapter != null ||
                         cameraTargetBindings != null || !string.IsNullOrEmpty(lookInputId))
                {
                    throw new ArgumentException("Camera-less Presentation received partial Camera configuration.");
                }

                var runtime = new CharacterSimulationPresentationRuntime(
                    actorId,
                    projection,
                    body,
                    animation,
                    equipment,
                    footPlacement,
                    camera,
                    animatorRoot,
                    diagnostics);
                body = null;
                animation = null;
                equipment = null;
                footPlacement = null;
                camera = null;
                return new CharacterPresentationRuntimeBinding(projection, runtime);
            }
            catch
            {
                motionMatching?.Dispose();
                CharacterPresentationModuleLifetime.Dispose(camera, footPlacement, equipment, animation, body);
                throw;
            }
        }

        static CharacterBodyPresentationSettings RequireBodySettings(CharacterBodyPresentationProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            return profile.BuildSettings();
        }
    }

    internal static class CharacterPresentationModuleLifetime
    {
        public static void Dispose(
            CharacterCameraPresentationRuntime camera,
            CharacterFootPlacementRuntime footPlacement,
            CharacterEquipmentVisualRuntime equipment,
            CharacterAnimationPresentationRuntime animation,
            CharacterBodyPresentationRuntime body)
        {
            try
            {
                camera?.Dispose();
            }
            finally
            {
                try
                {
                    footPlacement?.Dispose();
                }
                finally
                {
                    try
                    {
                        equipment?.Dispose();
                    }
                    finally
                    {
                        try
                        {
                            animation?.Dispose();
                        }
                        finally
                        {
                            body?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
