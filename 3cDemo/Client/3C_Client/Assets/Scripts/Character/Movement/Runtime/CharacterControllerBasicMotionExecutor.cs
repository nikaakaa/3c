using ThirdPersonDiagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    public sealed class CharacterControllerBasicMotionExecutor : IBasicLocomotionMotionExecutor, IMotionExecutorRollbackStateProvider
    {
        const string TurnBackRootMotionLogKeyword = "TURNBACK_RM_CHAIN";
        const string AnimationMotionLogChannel = "Locomotion.animation-motion";
        const string TurnBackDirectionDebugChannel = "Locomotion.turnback-direction-debug";

        readonly CharacterController characterController;
        readonly Transform rotationRoot;
        readonly bool applyGravity;
        readonly float gravity;
        readonly float groundedVerticalVelocity;
        float verticalVelocity;

        public CharacterControllerBasicMotionExecutor(
            CharacterController characterController,
            Transform rotationRoot,
            bool applyGravity,
            float gravity,
            float groundedVerticalVelocity)
        {
            this.characterController = characterController;
            this.rotationRoot = rotationRoot;
            this.applyGravity = applyGravity;
            this.gravity = gravity;
            this.groundedVerticalVelocity = groundedVerticalVelocity;
        }

        public Vector3 LastWorldDirection { get; private set; }
        public float CurrentSpeed { get; private set; }

        public MotionExecutorRollbackState CaptureRollbackState()
        {
            Transform root = ResolveMotionRoot();
            if (root == null)
                return new MotionExecutorRollbackState(CurrentSpeed, LastWorldDirection, verticalVelocity);

            return new MotionExecutorRollbackState(
                CurrentSpeed,
                LastWorldDirection,
                verticalVelocity,
                root.position,
                root.eulerAngles.y,
                true);
        }

        public void RestoreRollbackState(in MotionExecutorRollbackState state)
        {
            CurrentSpeed = state.CurrentSpeed;
            LastWorldDirection = state.LastWorldDirection;
            verticalVelocity = state.VerticalVelocity;
            if (state.HasRootPose)
            {
                Transform root = ResolveMotionRoot();
                if (root != null)
                {
                    bool wasEnabled = characterController != null && characterController.enabled;
                    if (wasEnabled)
                        characterController.enabled = false;
                    root.SetPositionAndRotation(state.RootPosition, Quaternion.Euler(0f, state.RootYaw, 0f));
                    if (wasEnabled)
                        characterController.enabled = true;
                }
            }
        }

        public void ExecuteBasicMovement(in MovementCommand command)
        {
            if (characterController == null)
                return;

            float deltaTime = command.DeltaTime;
            if (deltaTime <= 0f)
                return;

            Vector3 worldDirection = command.WorldDirection;
            worldDirection.y = 0f;
            worldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            Vector3 animationWorldDelta = ResolveAnimationWorldDelta(in command);
            LastWorldDirection = ResolveLastWorldDirection(worldDirection, animationWorldDelta);

            bool suppressInputRotation = command.SuppressInputRotation;
            bool suppressInputPlanarMovement = command.SuppressInputPlanarMovement;
            Transform root = ResolveMotionRoot();
            float yawBefore = ResolveYaw(root);
            bool appliedInputRotation = command.HasMovement && command.RotationSpeed > 0f && !suppressInputRotation;
            float targetYaw = command.DesiredFacing.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(command.DesiredFacing, Vector3.up).eulerAngles.y
                : yawBefore;
            if (command.HasMovement && command.RotationSpeed > 0f && !suppressInputRotation)
                RotateTowards(command.DesiredFacing, command.RotationSpeed, deltaTime);

            float yawAfterInput = ResolveYaw(root);
            bool appliedAnimationYaw = command.HasAnimationMotion && Mathf.Abs(command.AnimationYawDelta) > 0.0001f;
            if (command.HasAnimationMotion && Mathf.Abs(command.AnimationYawDelta) > 0.0001f)
                RotateByAnimationYaw(command.AnimationYawDelta);

            float yawAfterAnimation = ResolveYaw(root);
            bool appliedInputPlanarMovement = command.HasMovement && !suppressInputPlanarMovement;
            Vector3 planarVelocity = appliedInputPlanarMovement ? worldDirection * command.PlanarSpeed : Vector3.zero;
            Vector3 inputPlanarDisplacement = planarVelocity * deltaTime;
            Vector3 rootPositionBefore = root != null ? root.position : Vector3.zero;
            Move(planarVelocity, animationWorldDelta, deltaTime);
            Vector3 rootPositionAfter = root != null ? root.position : Vector3.zero;
            LogAnimationMotion(
                in command,
                yawBefore,
                yawAfterInput,
                yawAfterAnimation,
                targetYaw,
                suppressInputRotation,
                appliedInputRotation,
                appliedAnimationYaw,
                suppressInputPlanarMovement,
                appliedInputPlanarMovement,
                inputPlanarDisplacement,
                animationWorldDelta,
                rootPositionBefore,
                rootPositionAfter);
        }

        public void ExecuteActionMovement(in ActionMovementCommand command)
        {
            if (characterController == null)
                return;

            float deltaTime = command.DeltaTime;
            if (deltaTime <= 0f || !command.HasMovement)
                return;

            Vector3 worldDirection = command.WorldDirection;
            worldDirection.y = 0f;
            worldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            LastWorldDirection = worldDirection;

            if (command.RotateToDirection)
                RotateImmediate(worldDirection);

            Vector3 planarVelocity = worldDirection * (command.PlanarDistance / deltaTime);
            Move(planarVelocity, Vector3.zero, deltaTime);
        }

        void RotateTowards(Vector3 worldDirection, float rotationSpeed, float deltaTime)
        {
            if (rotationRoot == null || worldDirection.sqrMagnitude <= 0.000001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            rotationRoot.rotation = Quaternion.RotateTowards(rotationRoot.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        void RotateByAnimationYaw(float yawDelta)
        {
            Transform root = ResolveMotionRoot();
            if (root == null)
                return;

            root.rotation = root.rotation * Quaternion.Euler(0f, yawDelta, 0f);
        }

        void RotateImmediate(Vector3 worldDirection)
        {
            if (rotationRoot == null || worldDirection.sqrMagnitude <= 0.000001f)
                return;

            rotationRoot.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        }

        Vector3 ResolveAnimationWorldDelta(in MovementCommand command)
        {
            if (!command.HasAnimationMotion)
                return Vector3.zero;

            Vector3 localDelta = command.AnimationLocalPlanarDelta;
            localDelta.y = 0f;
            if (localDelta.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            Transform root = ResolveMotionRoot();
            Vector3 worldDelta;
            switch (command.AnimationPlanarDeltaSpace)
            {
                case BasicMovementPlanarDeltaSpace.World:
                    worldDelta = localDelta;
                    break;
                case BasicMovementPlanarDeltaSpace.EntryLocal:
                    worldDelta = ResolveEntryLocalWorldDelta(localDelta, command.AnimationPlanarBasisForward);
                    break;
                default:
                    worldDelta = root != null ? root.TransformDirection(localDelta) : localDelta;
                    break;
            }

            worldDelta.y = 0f;
            return worldDelta;
        }

        static Vector3 ResolveEntryLocalWorldDelta(Vector3 localDelta, Vector3 entryPlanarBasisForward)
        {
            if (!TryNormalizePlanar(entryPlanarBasisForward, out Vector3 forward))
                return Vector3.zero;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return right * localDelta.x + forward * localDelta.z;
        }

        Vector3 ResolveLastWorldDirection(Vector3 inputWorldDirection, Vector3 animationWorldDelta)
        {
            if (inputWorldDirection.sqrMagnitude > 0.000001f)
                return inputWorldDirection;

            animationWorldDelta.y = 0f;
            return animationWorldDelta.sqrMagnitude > 0.000001f ? animationWorldDelta.normalized : Vector3.zero;
        }

        Transform ResolveMotionRoot()
        {
            if (rotationRoot != null)
                return rotationRoot;

            return characterController != null ? characterController.transform : null;
        }

        static float ResolveYaw(Transform root)
        {
            return root != null ? root.eulerAngles.y : 0f;
        }

        static float DeltaYaw(float from, float to)
        {
            return Mathf.DeltaAngle(from, to);
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }

        static Vector3 ResolvePlanarRightOrZero(Vector3 forward)
        {
            return TryNormalizePlanar(forward, out Vector3 normalizedForward)
                ? Vector3.Cross(Vector3.up, normalizedForward).normalized
                : Vector3.zero;
        }

        void LogAnimationMotion(
            in MovementCommand command,
            float yawBefore,
            float yawAfterInput,
            float yawAfterAnimation,
            float targetYaw,
            bool suppressInputRotation,
            bool appliedInputRotation,
            bool appliedAnimationYaw,
            bool suppressInputPlanarMovement,
            bool appliedInputPlanarMovement,
            Vector3 inputPlanarDisplacement,
            Vector3 animationWorldDelta,
            Vector3 rootPositionBefore,
            Vector3 rootPositionAfter)
        {
            if (!command.HasAnimationMotion && !command.SuppressInputRotation && !command.SuppressInputPlanarMovement)
                return;

            Vector3 requestedPlanarDisplacement = inputPlanarDisplacement + animationWorldDelta;
            Vector3 actualRootDelta = rootPositionAfter - rootPositionBefore;
            bool isTurnBack = command.Phase == BasicMovementPhase.TurnBack;
            Vector3 animationPlanarBasisRight = ResolvePlanarRightOrZero(command.AnimationPlanarBasisForward);
            bool entryBasisMissing = command.AnimationPlanarDeltaSpace == BasicMovementPlanarDeltaSpace.EntryLocal &&
                                     command.AnimationLocalPlanarDelta.sqrMagnitude > 0.000001f &&
                                     command.AnimationPlanarBasisForward.sqrMagnitude <= 0.000001f;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                isTurnBack ? RuntimeDiagnosticLogLevel.Info : RuntimeDiagnosticLogLevel.Trace,
                "animation-motion-executor",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=executor phase={command.Phase} gait={command.Gait} hasMove={command.HasMovement} desiredFacing={command.DesiredFacing.ToString("F3")} targetYaw={targetYaw:F3} yawBefore={yawBefore:F3} yawAfterInput={yawAfterInput:F3} yawAfterAnimation={yawAfterAnimation:F3} inputYawDelta={DeltaYaw(yawBefore, yawAfterInput):F3} animationYawDeltaApplied={DeltaYaw(yawAfterInput, yawAfterAnimation):F3} totalYawDelta={DeltaYaw(yawBefore, yawAfterAnimation):F3} commandAnimationYawDelta={command.AnimationYawDelta:F3} suppressFlag={command.SuppressInputRotation} suppressApplied={suppressInputRotation} appliedInputRotation={appliedInputRotation} appliedAnimationYaw={appliedAnimationYaw} suppressInputPlanarMovement={command.SuppressInputPlanarMovement} suppressInputPlanarMovementApplied={suppressInputPlanarMovement} appliedInputPlanarMovement={appliedInputPlanarMovement} inputPlanarDisplacement={inputPlanarDisplacement.ToString("F3")} requestedPlanarDisplacement={requestedPlanarDisplacement.ToString("F3")} rootPositionBefore={rootPositionBefore.ToString("F3")} rootPositionAfter={rootPositionAfter.ToString("F3")} actualRootDelta={actualRootDelta.ToString("F3")} rotationSpeed={command.RotationSpeed:F3} deltaTime={command.DeltaTime:F3} animationDeltaSpace={command.AnimationPlanarDeltaSpace} animationBasisForward={command.AnimationPlanarBasisForward.ToString("F3")} animationBasisRight={animationPlanarBasisRight.ToString("F3")} entryBasisMissing={entryBasisMissing} animationLocalDelta={command.AnimationLocalPlanarDelta.ToString("F3")} animationWorldDelta={animationWorldDelta.ToString("F3")} planarSpeed={command.PlanarSpeed:F3}",
                isTurnBack ? TurnBackDirectionDebugChannel : AnimationMotionLogChannel));
        }

        void Move(Vector3 planarVelocity, Vector3 animationWorldDelta, float deltaTime)
        {
            if (applyGravity)
                UpdateVerticalVelocity(deltaTime);
            else
                verticalVelocity = 0f;

            Vector3 planarDisplacement = planarVelocity * deltaTime + animationWorldDelta;
            Vector3 displacement = planarDisplacement + Vector3.up * verticalVelocity * deltaTime;
            characterController.Move(displacement);
            CurrentSpeed = deltaTime > 0f ? new Vector3(planarDisplacement.x, 0f, planarDisplacement.z).magnitude / deltaTime : 0f;
        }

        void UpdateVerticalVelocity(float deltaTime)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedVerticalVelocity;
            else
                verticalVelocity += gravity * deltaTime;
        }
    }
}
