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
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
                footPlacementProfile,
                footPlacementRig,
                footPlacementSolver,
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
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
                CharacterAnimationStartupPolicy.RequireCommittedSelection,
                footPlacementProfile,
                footPlacementRig,
                footPlacementSolver,
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
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
                footPlacementProfile,
                footPlacementRig,
                footPlacementSolver,
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
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
                CharacterAnimationStartupPolicy.RequireCommittedSelection,
                footPlacementProfile,
                footPlacementRig,
                footPlacementSolver,
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
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
                CharacterAnimationStartupPolicy.AwaitCommittedSelection,
                footPlacementProfile,
                footPlacementRig,
                footPlacementSolver,
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
            CharacterAnimationStartupPolicy animationStartupPolicy,
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRig footPlacementRig,
            ICharacterFootPlacementSolver footPlacementSolver,
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
            projection.RequireContract(contract);
            animationRigBinding.RequireValid(projection.Rig);

            CharacterBodyPresentationRuntime body = null;
            CharacterAnimationPlaybackRuntime animation = null;
            CharacterFootPlacementRuntime footPlacement = null;
            CharacterCameraPresentationRuntime camera = null;
            CharacterEquipmentVisualRuntime equipment = null;
            ICharacterMotionMatchingTrajectorySource motionMatchingTrajectorySource = null;
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
                animation = new CharacterAnimationPlaybackRuntime(
                    contract,
                    projection,
                    animancer,
                    animationRigBinding,
                    true);
                motionMatchingTrajectorySource = CreateMotionMatchingTrajectorySource(
                    projection,
                    bodySourceMode,
                    actorId);
                equipment = new CharacterEquipmentVisualRuntime(
                    actorId,
                    projection,
                    equipmentRigCatalog,
                    diagnostics);
                if (!footPlacementProfile)
                    throw new ArgumentNullException(nameof(footPlacementProfile));
                if (!footPlacementRig)
                    throw new ArgumentNullException(nameof(footPlacementRig));
                if (footPlacementSolver == null)
                    throw new ArgumentNullException(nameof(footPlacementSolver));
                if (!physicsScene.IsValid())
                    throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
                CharacterFootPlacementRigBinding rig = footPlacementRig.BuildBinding();
                if (rig.VisualRoot != visualRoot)
                    throw new InvalidOperationException("Foot Placement Rig VisualRoot must match Presentation VisualRoot exactly.");
                CharacterFootPlacementRuntimeSettings footPlacementSettings =
                    footPlacementProfile.BuildSettings(projection, rig);
                footPlacement = new CharacterFootPlacementRuntime(
                    actorId,
                    footPlacementSettings,
                    rig,
                    footPlacementSolver,
                    physicsScene,
                    diagnostics);
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
                    motionMatchingTrajectorySource,
                    equipment,
                    footPlacement,
                    camera,
                    animationStartupPolicy,
                    diagnostics);
                body = null;
                animation = null;
                motionMatchingTrajectorySource = null;
                equipment = null;
                footPlacement = null;
                camera = null;
                return new CharacterPresentationRuntimeBinding(projection, runtime);
            }
            catch
            {
                try
                {
                    motionMatchingTrajectorySource?.Dispose();
                }
                finally
                {
                    CharacterPresentationModuleLifetime.Dispose(camera, footPlacement, equipment, animation, body);
                }
                throw;
            }
        }

        static ICharacterMotionMatchingTrajectorySource CreateMotionMatchingTrajectorySource(
            CharacterPresentationProjection projection,
            CharacterBodyPresentationSourceMode bodySourceMode,
            ActorId actorId)
        {
            if (projection.MotionMatching == null)
                return null;
            string suffix = actorId.ToString();
            return bodySourceMode == CharacterBodyPresentationSourceMode.SelectedStream
                ? (ICharacterMotionMatchingTrajectorySource)new SelectedBodyMotionMatchingTrajectorySource(
                    new MotionMatchingTrajectorySourceIdentity("selected-body/" + suffix))
                : new AcceptedIntentMotionMatchingTrajectorySource(
                    new MotionMatchingTrajectorySourceIdentity("accepted-intent/" + suffix));
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
            CharacterAnimationPlaybackRuntime animation,
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
