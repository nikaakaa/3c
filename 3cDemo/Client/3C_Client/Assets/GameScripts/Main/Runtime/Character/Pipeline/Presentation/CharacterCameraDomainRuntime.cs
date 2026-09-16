using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterCameraDomainRuntime : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CameraRuntimeBinding m_Binding;
        readonly CameraBindingAdoptedResult m_Adopted;
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly ICameraRigAdapter m_Rig;
        readonly CameraTargetBindingResolver m_TargetResolver;
        readonly CameraResponseRequestResolver m_ResponseResolver;
        readonly CharacterCameraSequenceEvaluator m_SequenceEvaluator;
        readonly CameraEffectEvaluator m_EffectEvaluator;
        readonly CameraEnvironmentConstraintSolver m_EnvironmentSolver;
        readonly ICharacterPresentationLookInput m_Input;
        readonly string m_LookInputId;
        readonly Vector3 m_FollowBindPosition;
        readonly Vector3 m_AimBindPosition;
        readonly List<CameraTargetSnapshot> m_Targets = new List<CameraTargetSnapshot>();
        CameraSequenceRequest m_DefaultSequenceRequest;
        readonly CameraResponseRequest m_DefaultResponseRequest;
        ulong m_LastBodyResetSequence;
        CameraResetReason m_PendingResetReason;
        bool m_Disposed;

        internal CharacterCameraDomainRuntime(
            ActorId actorId,
            CharacterCameraProfile profile,
            CinemachineCameraRigAdapter cameraRig,
            IReadOnlyList<CameraTargetBinding> targetBindings,
            CharacterPresentationBodyState initialBody,
            Transform followAnchor,
            Transform aimAnchor,
            ICharacterPresentationLookInput input,
            string lookInputId,
            PhysicsScene physicsScene,
            CharacterRootHierarchyBinding rootHierarchy,
            bool initializeExternalState)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Camera domain Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!cameraRig)
                throw new InvalidOperationException("Camera domain requires an explicit Cinemachine rig.");
            m_Rig = cameraRig;
            if (!followAnchor || !aimAnchor)
                throw new ArgumentException("Camera domain requires explicit follow and aim anchors.");
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(lookInputId))
                throw new ArgumentException("Camera domain look input id is required.", nameof(lookInputId));
            m_LookInputId = lookInputId;
            if (targetBindings == null || targetBindings.Count == 0)
                throw new ArgumentException("Camera domain requires target bindings.", nameof(targetBindings));
            if (rootHierarchy == null || !rootHierarchy.LogicRoot)
                throw new ArgumentException("Camera domain requires a Root Hierarchy LogicRoot.", nameof(rootHierarchy));
            var environmentQuery = new UnityCameraEnvironmentQuery(
                physicsScene,
                rootHierarchy.LogicRoot);
            CameraBindingPreparationResult preparation = CharacterCameraRuntimeBindingBuilder.Prepare(
                $"camera/{actorId.Value}",
                profile.ProfileId,
                profile.Revision,
                profile,
                cameraRig,
                targetBindings,
                environmentQuery);
            if (!preparation.IsReady)
                throw new InvalidOperationException(
                    $"Camera runtime binding failed: {preparation.FailureCode} {preparation.FailureMessage}.");
            m_Binding = preparation.PreparedBinding;
            m_Adopted = m_Binding.Adopt(actorId.Value, $"{actorId.Value}:camera");
            if (!m_Adopted.Adopted)
                throw new InvalidOperationException($"Camera runtime binding was not adopted: {m_Adopted.FailureMessage}.");
            m_Projection = m_Binding.Projection;
            m_TargetResolver = new CameraTargetBindingResolver(m_Binding.TargetBindings);
            m_ResponseResolver = new CameraResponseRequestResolver(m_Projection.Input);
            m_SequenceEvaluator = new CharacterCameraSequenceEvaluator(m_Projection);
            m_EffectEvaluator = new CameraEffectEvaluator(m_Projection);
            m_EnvironmentSolver = new CameraEnvironmentConstraintSolver(m_Projection, environmentQuery);
            Quaternion inverse = Quaternion.Inverse(initialBody.Rotation);
            m_FollowBindPosition = inverse * (followAnchor.position - initialBody.Position);
            m_AimBindPosition = inverse * (aimAnchor.position - initialBody.Position);
            m_DefaultSequenceRequest = new CameraSequenceRequest(
                m_Projection.DefaultSequence.SequenceId,
                int.MinValue,
                1f,
                0f,
                0f,
                string.Empty,
                "camera.default.sequence",
                0,
                0,
                CameraSequenceInterruptPolicy.BlendOut,
                true);
            m_DefaultResponseRequest = new CameraResponseRequest(
                CameraResponseMode.Weighted,
                m_Projection.Input.DefaultResponseWeight,
                m_Projection.Input.PitchResponseWeight,
                m_Projection.Input.YawResponseWeight,
                int.MinValue,
                1f,
                "camera.default.response",
                0,
                0);
            if (initializeExternalState)
                Apply(
                    initialBody.Position,
                    initialBody.Rotation,
                    Vector2.zero,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    0f,
                    0f,
                    false,
                    false,
                    false,
                    true,
                    CameraResetReason.Initialization);
        }

        internal CameraBasisSnapshot BasisSnapshot => m_Rig.BasisSnapshot;

        internal void SetInitialState(in CameraInitialState state)
        {
            RequireAlive();
            m_SequenceEvaluator.SetInitialState(in state);
            m_Rig.Reset();
            m_LastBodyResetSequence = 0;
            m_PendingResetReason = CameraResetReason.Initialization;
        }

        internal void Present(
            CharacterBodyPresentationFrame bodyFrame,
            in GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (!bodyFrame.IsValid)
                throw new InvalidOperationException("Camera domain requires a valid Body Presentation frame.");
            Vector2 look = m_Input.TryGetLatchedVector2(m_LookInputId, out Vector2 value)
                ? value
                : Vector2.zero;
            bool resetHistory = m_PendingResetReason != CameraResetReason.None ||
                bodyFrame.ResetSequence != m_LastBodyResetSequence;
            CameraResetReason resetReason = m_PendingResetReason != CameraResetReason.None
                ? m_PendingResetReason
                : resetHistory
                    ? ResolveResetReason(bodyFrame.ResetReason)
                    : CameraResetReason.None;
            m_PendingResetReason = CameraResetReason.None;
            m_LastBodyResetSequence = bodyFrame.ResetSequence;
            Apply(
                bodyFrame.VisiblePosition,
                bodyFrame.VisibleRotation,
                look,
                context.ScaledDeltaSeconds,
                context.UnscaledDeltaSeconds,
                context.PresentationDeltaSeconds,
                context.RenderFrame,
                context.LocalLogicTick,
                context.OwnerTimeScale,
                context.LocalAvatarTimeScale,
                context.HasOwnerTimeScale,
                context.HasLocalAvatarTimeScale,
                context.Paused,
                resetHistory,
                resetReason);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_SequenceEvaluator.Reset();
            m_EffectEvaluator.Reset();
            m_EnvironmentSolver.Reset();
            m_Rig.Reset();
            m_LastBodyResetSequence = 0;
            m_PendingResetReason = CameraResetReason.Initialization;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Targets.Clear();
        }

        void Apply(
            Vector3 position,
            Quaternion rotation,
            Vector2 look,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            ulong presentationFrame,
            ulong localLogicTick,
            float ownerTimeScale,
            float localAvatarTimeScale,
            bool ownerTimeScaleAvailable,
            bool localAvatarTimeScaleAvailable,
            bool paused,
            bool resetHistory,
            CameraResetReason resetReason)
        {
            Vector3 follow = position + rotation * m_FollowBindPosition;
            Vector3 aim = position + rotation * m_AimBindPosition;
            m_Targets.Clear();
            m_Targets.Add(new CameraTargetSnapshot(
                CameraTargetBindingKeys.Body,
                follow,
                aim,
                Vector3.zero,
                true,
                false));
            m_TargetResolver.CaptureSlotSnapshots(
                m_Projection.TargetSlots,
                m_Targets);
            CameraResolvedTargetPlan resolvedTarget = m_TargetResolver.Resolve(
                m_DefaultSequenceRequest,
                Array.Empty<CameraTargetSelectionRequest>(),
                m_Targets);
            if (!resolvedTarget.Valid)
                throw new InvalidOperationException(resolvedTarget.Error);
            if (!string.IsNullOrEmpty(resolvedTarget.SourceKey))
            {
                bool duplicate = false;
                for (int i = 0; i < m_Targets.Count; i++)
                {
                    if (string.Equals(m_Targets[i].Key, resolvedTarget.SourceKey, StringComparison.Ordinal))
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate)
                {
                    m_Targets.Add(new CameraTargetSnapshot(
                        resolvedTarget.SourceKey,
                        resolvedTarget.HasFollowPoint ? resolvedTarget.FollowPoint : follow,
                        resolvedTarget.HasAimPoint ? resolvedTarget.AimPoint : aim,
                        Vector3.zero,
                        true,
                        resolvedTarget.HasAimPoint));
                }
                m_DefaultSequenceRequest = m_DefaultSequenceRequest.WithTargetKey(resolvedTarget.SourceKey);
            }
            var frameInput = new CameraFrameInput(
                position,
                rotation,
                look,
                scaledDeltaSeconds,
                unscaledDeltaSeconds,
                presentationDeltaSeconds,
                ownerTimeScale,
                localAvatarTimeScale,
                ownerTimeScaleAvailable,
                localAvatarTimeScaleAvailable,
                paused,
                resetHistory,
                resetReason,
                m_Targets);
            CameraResponseRequest response = m_ResponseResolver.Resolve(Array.Empty<CameraResponseRequest>());
            CameraFramePlan plan = m_SequenceEvaluator.Evaluate(
                in frameInput,
                in m_DefaultSequenceRequest,
                in response);
            plan = m_EffectEvaluator.Resolve(plan, Array.Empty<CameraEffectRequest>(), in frameInput);
            plan = plan.WithPitchClamped(
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);
            plan = m_EnvironmentSolver.Apply(plan, in frameInput);
            m_Rig.Apply(in plan);
        }

        static CameraResetReason ResolveResetReason(CharacterBodyPresentationResetReason reason)
        {
            switch (reason)
            {
                case CharacterBodyPresentationResetReason.Initialization:
                    return CameraResetReason.Initialization;
                case CharacterBodyPresentationResetReason.CommittedBranchReplacement:
                    return CameraResetReason.BodyCommittedBranchReplacement;
                case CharacterBodyPresentationResetReason.SelectedStreamReset:
                    return CameraResetReason.BodySelectedStreamReset;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterCameraDomainRuntime));
        }
    }
}