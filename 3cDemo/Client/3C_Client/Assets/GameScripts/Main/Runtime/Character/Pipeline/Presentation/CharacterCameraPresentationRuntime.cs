using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterCameraPresentationRuntime : IDisposable
    {
        const float DefaultFieldOfView = 60f;

        readonly ThirdPersonCameraController m_CameraRig;
        readonly CameraTargetBindingResolver m_CameraTargetResolver;
        readonly Vector3 m_FollowBindPosition;
        readonly Vector3 m_AimBindPosition;
        readonly ICharacterPresentationLookInput m_InputAdapter;
        readonly string m_LookInputId;
        readonly Dictionary<PresentationProducerInstanceId, CameraStateRequest> m_CameraStates =
            new Dictionary<PresentationProducerInstanceId, CameraStateRequest>();
        readonly Dictionary<PresentationProducerInstanceId, CameraResponsePolicy> m_CameraResponses =
            new Dictionary<PresentationProducerInstanceId, CameraResponsePolicy>();
        readonly Dictionary<PresentationProducerInstanceId, CameraTargetRequest> m_CameraTargets =
            new Dictionary<PresentationProducerInstanceId, CameraTargetRequest>();
        readonly List<CameraCue> m_PendingCameraCues = new List<CameraCue>();
        readonly List<CameraStateRequest> m_CameraStateBuffer = new List<CameraStateRequest>();
        readonly List<CameraResponsePolicy> m_CameraResponseBuffer = new List<CameraResponsePolicy>();
        readonly CameraStateResolver m_CameraStateResolver = new CameraStateResolver();
        readonly CameraResponsePolicyResolver m_CameraResponseResolver = new CameraResponsePolicyResolver();
        readonly CameraModifierResolver m_CameraModifierResolver = new CameraModifierResolver();
        readonly HashSet<ulong> m_NoTerminalActions = new HashSet<ulong>();

        ulong m_LastBodyResetSequence;
        bool m_Disposed;

        public CharacterCameraPresentationRuntime(
            CharacterPresentationProjection projection,
            ThirdPersonCameraController cameraRig,
            CharacterPresentationBodyState initialBody,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput inputAdapter,
            string lookInputId)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            m_CameraRig = cameraRig ? cameraRig : throw new ArgumentNullException(nameof(cameraRig));
            if (!followAnchor || !aimAnchor)
                throw new ArgumentException("Presentation Camera requires explicit follow and aim anchors.");
            if (cameraTargetBindings == null)
                throw new ArgumentNullException(nameof(cameraTargetBindings));
            m_InputAdapter = inputAdapter ?? throw new ArgumentNullException(nameof(inputAdapter));
            m_LookInputId = string.IsNullOrWhiteSpace(lookInputId)
                ? throw new ArgumentException("Presentation Camera look input identity is missing.", nameof(lookInputId))
                : lookInputId.Trim();
            m_CameraTargetResolver = new CameraTargetBindingResolver(cameraTargetBindings);
            RequireCameraTargetBindings(projection, m_CameraTargetResolver);
            Quaternion inverse = Quaternion.Inverse(initialBody.Rotation);
            m_FollowBindPosition = inverse * (followAnchor.position - initialBody.Position);
            m_AimBindPosition = inverse * (aimAnchor.position - initialBody.Position);
            Apply(initialBody.Position, initialBody.Rotation, Vector2.zero, 0f);
        }

        public void Publish(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            CharacterPresentationCameraBinding binding = RequireCameraBinding(producer);
            var instance = new PresentationProducerInstanceId(command.ProducerId, command.ProducerGeneration);
            float weight = Mathf.Clamp01(command.Weight);
            switch (binding.Kind)
            {
                case CharacterPresentationCameraBindingKind.State:
                    if (weight <= 0f)
                    {
                        m_CameraStates.Remove(instance);
                        return;
                    }
                    m_CameraStates[instance] = new CameraStateRequest(
                        ToCameraMode(binding.Mode),
                        binding.Priority,
                        weight,
                        binding.BlendInSeconds,
                        binding.BlendOutSeconds,
                        binding.TargetKey,
                        producer.ProgramProducerIdentity,
                        producer.SourceDisplayPath,
                        0,
                        ToCameraInterruptPolicy(binding.InterruptPolicy));
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    if (weight <= 0f)
                    {
                        m_CameraResponses.Remove(instance);
                        return;
                    }
                    m_CameraResponses[instance] = new CameraResponsePolicy(
                        ToCameraResponseMode(binding.LookResponse),
                        binding.ManualOrbitWeight,
                        binding.PitchResponseWeight,
                        binding.YawResponseWeight,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        0);
                    break;
                case CharacterPresentationCameraBindingKind.Cue:
                    m_PendingCameraCues.Add(new CameraCue(
                        binding.CueId,
                        ToCameraCueKind(binding.CueKind),
                        binding.CueType,
                        Mathf.Max(0f, command.Weight),
                        binding.DurationSeconds,
                        binding.Priority,
                        command.Header.EventId.ToString(),
                        producer.SourceDisplayPath,
                        0));
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    if (weight <= 0f)
                    {
                        m_CameraTargets.Remove(instance);
                        return;
                    }
                    m_CameraTargets[instance] = new CameraTargetRequest(
                        binding.TargetKey,
                        binding.AnchorKey,
                        binding.AimPointKey,
                        binding.PreferredBoneKey,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binding.Kind), binding.Kind, null);
            }
        }

        public void Retire(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            CharacterPresentationCameraBinding binding = RequireCameraBinding(producer);
            var instance = new PresentationProducerInstanceId(command.ProducerId, command.ProducerGeneration);
            switch (binding.Kind)
            {
                case CharacterPresentationCameraBindingKind.State:
                    m_CameraStates.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    m_CameraResponses.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Cue:
                    string sourceId = command.Header.EventId.ToString();
                    for (int i = m_PendingCameraCues.Count - 1; i >= 0; i--)
                    {
                        if (string.Equals(m_PendingCameraCues[i].SourceId, sourceId, StringComparison.Ordinal))
                            m_PendingCameraCues.RemoveAt(i);
                    }
                    m_CameraModifierResolver.RetireSource(sourceId);
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    m_CameraTargets.Remove(instance);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binding.Kind), binding.Kind, null);
            }
        }

        public void Present(CharacterBodyPresentationFrame bodyFrame, float presentationDeltaSeconds)
        {
            RequireAlive();
            if (!bodyFrame.IsValid)
                throw new InvalidOperationException("Presentation Camera requires a valid Body frame.");
            Vector2 look = m_InputAdapter.TryGetLatchedVector2(m_LookInputId, out Vector2 value)
                ? value
                : Vector2.zero;
            bool resetTracking = bodyFrame.ResetSequence != m_LastBodyResetSequence;
            m_LastBodyResetSequence = bodyFrame.ResetSequence;
            Apply(
                bodyFrame.VisiblePosition,
                bodyFrame.VisibleRotation,
                look,
                presentationDeltaSeconds,
                resetTracking);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CameraStates.Clear();
            m_CameraResponses.Clear();
            m_CameraTargets.Clear();
            m_PendingCameraCues.Clear();
            m_CameraStateBuffer.Clear();
            m_CameraResponseBuffer.Clear();
            m_CameraStateResolver.Reset();
            m_CameraModifierResolver.Reset();
            m_LastBodyResetSequence = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_Disposed = true;
        }

        void Apply(
            Vector3 position,
            Quaternion rotation,
            Vector2 look,
            float deltaSeconds,
            bool resetTracking = false)
        {
            Vector3 follow = position + rotation * m_FollowBindPosition;
            Vector3 aim = position + rotation * m_AimBindPosition;
            m_CameraStateBuffer.Clear();
            m_CameraStateBuffer.AddRange(m_CameraStates.Values);
            m_CameraResponseBuffer.Clear();
            m_CameraResponseBuffer.AddRange(m_CameraResponses.Values);
            CameraStateRequest state = m_CameraStateResolver.Resolve(
                m_CameraStateBuffer,
                m_NoTerminalActions,
                deltaSeconds,
                out float blendProgress);
            CameraResponsePolicy response = m_CameraResponseResolver.Resolve(
                state,
                m_CameraResponseBuffer,
                m_NoTerminalActions);
            CameraResolvedTargetPlan targetPlan = m_CameraTargetResolver.Resolve(state, m_CameraTargets.Values);
            if (!targetPlan.Valid)
                throw new InvalidOperationException(targetPlan.Error);
            if (targetPlan.HasFollowPoint)
                follow = targetPlan.FollowPoint;
            if (targetPlan.HasAimPoint)
                aim = targetPlan.AimPoint;
            var basePlan = new CameraPosePlan(
                state.Mode,
                follow,
                aim,
                ResolveFieldOfView(state.Mode),
                response,
                response.Apply(look),
                state.SourceId,
                0,
                blendProgress,
                true);
            CameraPosePlan plan = m_CameraModifierResolver.Resolve(
                basePlan,
                m_PendingCameraCues,
                m_NoTerminalActions,
                deltaSeconds);
            m_PendingCameraCues.Clear();
            if (resetTracking)
                m_CameraRig.ApplyAfterTrackingReset(plan);
            else
                m_CameraRig.Apply(plan);
        }

        static CharacterPresentationCameraBinding RequireCameraBinding(
            CharacterPresentationProducerEntry producer)
        {
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Camera || producer.Camera == null)
            {
                throw new InvalidOperationException(
                    $"Camera command targets invalid Projection producer '{producer?.ProgramProducerIdentity}'.");
            }
            return producer.Camera;
        }

        static void RequireCameraTargetBindings(
            CharacterPresentationProjection projection,
            CameraTargetBindingResolver resolver)
        {
            IReadOnlyList<CharacterPresentationProducerEntry> producers = projection.Producers;
            for (int i = 0; i < producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = producers[i];
                if (producer.Kind != CharacterPresentationProducerKind.Camera || producer.Camera == null)
                    continue;
                CharacterPresentationCameraBinding binding = producer.Camera;
                switch (binding.Kind)
                {
                    case CharacterPresentationCameraBindingKind.State:
                        resolver.RequireKey(binding.TargetKey, producer.SourceDisplayPath);
                        break;
                    case CharacterPresentationCameraBindingKind.Target:
                        resolver.RequireKey(binding.TargetKey, producer.SourceDisplayPath);
                        resolver.RequireKey(binding.AnchorKey, producer.SourceDisplayPath);
                        resolver.RequireKey(binding.AimPointKey, producer.SourceDisplayPath);
                        resolver.RequireKey(binding.PreferredBoneKey, producer.SourceDisplayPath);
                        break;
                }
            }
        }

        static float ResolveFieldOfView(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Aim: return 50f;
                case CameraMode.LockOn: return 55f;
                case CameraMode.ActionFocus: return 48f;
                case CameraMode.SkillCloseup: return 42f;
                default: return DefaultFieldOfView;
            }
        }

        static CameraMode ToCameraMode(TimelineCameraMode mode)
        {
            switch (mode)
            {
                case TimelineCameraMode.Aim: return CameraMode.Aim;
                case TimelineCameraMode.LockOn: return CameraMode.LockOn;
                case TimelineCameraMode.ActionFocus: return CameraMode.ActionFocus;
                case TimelineCameraMode.SkillCloseup: return CameraMode.SkillCloseup;
                default: return CameraMode.FreeLook;
            }
        }

        static CameraInterruptPolicy ToCameraInterruptPolicy(TimelineCameraInterruptPolicy policy)
        {
            switch (policy)
            {
                case TimelineCameraInterruptPolicy.Cut: return CameraInterruptPolicy.Cut;
                case TimelineCameraInterruptPolicy.HoldUntilSourceEnds: return CameraInterruptPolicy.HoldUntilSourceEnds;
                default: return CameraInterruptPolicy.BlendOut;
            }
        }

        static CameraLookResponseMode ToCameraResponseMode(TimelineCameraLookResponseMode mode)
        {
            switch (mode)
            {
                case TimelineCameraLookResponseMode.Suppressed: return CameraLookResponseMode.Suppressed;
                case TimelineCameraLookResponseMode.Weighted: return CameraLookResponseMode.Weighted;
                default: return CameraLookResponseMode.Full;
            }
        }

        static CameraCueKind ToCameraCueKind(TimelineCameraCueKind kind)
        {
            switch (kind)
            {
                case TimelineCameraCueKind.Shake: return CameraCueKind.Shake;
                case TimelineCameraCueKind.FovKick: return CameraCueKind.FovKick;
                case TimelineCameraCueKind.Recoil: return CameraCueKind.Recoil;
                case TimelineCameraCueKind.CollisionCorrection: return CameraCueKind.CollisionCorrection;
                default: return CameraCueKind.Custom;
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterCameraPresentationRuntime));
        }

        readonly struct PresentationProducerInstanceId : IEquatable<PresentationProducerInstanceId>
        {
            public PresentationProducerInstanceId(string producerId, ulong generation)
            {
                ProducerId = producerId ?? string.Empty;
                Generation = generation;
            }

            public string ProducerId { get; }
            public ulong Generation { get; }

            public bool Equals(PresentationProducerInstanceId other) =>
                Generation == other.Generation &&
                string.Equals(ProducerId, other.ProducerId, StringComparison.Ordinal);

            public override bool Equals(object obj) =>
                obj is PresentationProducerInstanceId other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(
                Generation,
                ProducerId == null ? 0 : StringComparer.Ordinal.GetHashCode(ProducerId));
        }
    }
}
