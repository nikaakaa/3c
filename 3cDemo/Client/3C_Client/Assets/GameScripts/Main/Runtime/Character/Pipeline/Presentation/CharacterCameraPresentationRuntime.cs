using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonGameplay.Tick;
using ThirdPersonPerformance.Instrumentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterCameraPresentationRuntime : IDisposable
    {
        readonly ICameraRigAdapter m_CameraRig;
        readonly CharacterCameraProjectionPayload m_CameraProjection;
        readonly CameraTargetBindingResolver m_TargetResolver;
        readonly CameraSequenceRequestResolver m_SequenceResolver = new CameraSequenceRequestResolver();
        readonly CameraResponseRequestResolver m_ResponseResolver = new CameraResponseRequestResolver();
        readonly CharacterCameraSequenceEvaluator m_SequenceEvaluator;
        readonly CameraEffectEvaluator m_EffectEvaluator;
        readonly Vector3 m_FollowBindPosition;
        readonly Vector3 m_AimBindPosition;
        readonly ICharacterPresentationLookInput m_InputAdapter;
        readonly string m_LookInputId;
        readonly Dictionary<PresentationProducerInstanceId, CameraSequenceRequest> m_Sequences =
            new Dictionary<PresentationProducerInstanceId, CameraSequenceRequest>();
        readonly Dictionary<PresentationProducerInstanceId, CameraResponseRequest> m_Responses =
            new Dictionary<PresentationProducerInstanceId, CameraResponseRequest>();
        readonly Dictionary<PresentationProducerInstanceId, CameraTargetSelectionRequest> m_Targets =
            new Dictionary<PresentationProducerInstanceId, CameraTargetSelectionRequest>();
        readonly List<CameraSequenceRequest> m_SequenceBuffer = new List<CameraSequenceRequest>();
        readonly List<CameraResponseRequest> m_ResponseBuffer = new List<CameraResponseRequest>();
        readonly List<CameraTargetSelectionRequest> m_TargetBuffer = new List<CameraTargetSelectionRequest>();
        readonly List<CameraEffectRequest> m_PendingEffects = new List<CameraEffectRequest>();
        readonly List<CameraTargetSnapshot> m_FrameTargets = new List<CameraTargetSnapshot>();
        readonly CameraDebugSnapshot m_Debug = new CameraDebugSnapshot();

        ulong m_LastBodyResetSequence;
        bool m_Disposed;

        public CharacterCameraPresentationRuntime(
            CharacterPresentationProjection projection,
            ICameraRigAdapter cameraRig,
            CharacterPresentationBodyState initialBody,
            Transform followAnchor,
            Transform aimAnchor,
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
            ICharacterPresentationLookInput inputAdapter,
            string lookInputId)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireCameraPayload();
            m_CameraProjection = projection.Camera;
            m_CameraRig = cameraRig ?? throw new ArgumentNullException(nameof(cameraRig));
            if (!followAnchor || !aimAnchor)
                throw new ArgumentException("Presentation Camera requires explicit follow and aim anchors.");
            if (cameraTargetBindings == null)
                throw new ArgumentNullException(nameof(cameraTargetBindings));
            m_InputAdapter = inputAdapter ?? throw new ArgumentNullException(nameof(inputAdapter));
            m_LookInputId = string.IsNullOrWhiteSpace(lookInputId)
                ? throw new ArgumentException("Presentation Camera look input identity is missing.", nameof(lookInputId))
                : lookInputId.Trim();
            m_TargetResolver = new CameraTargetBindingResolver(cameraTargetBindings);
            RequireCameraTargetBindings(projection, m_TargetResolver);
            m_SequenceEvaluator = new CharacterCameraSequenceEvaluator(m_CameraProjection);
            m_EffectEvaluator = new CameraEffectEvaluator(m_CameraProjection);
            Quaternion inverse = Quaternion.Inverse(initialBody.Rotation);
            m_FollowBindPosition = inverse * (followAnchor.position - initialBody.Position);
            m_AimBindPosition = inverse * (aimAnchor.position - initialBody.Position);
            PresentInitial(initialBody.Position, initialBody.Rotation);
        }

        public CameraBasisSnapshot BasisSnapshot => m_CameraRig.BasisSnapshot;
        public CameraRigResult RigResult => m_CameraRig.Result;
        public CameraDebugSnapshot DebugSnapshot => m_Debug;

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
                case CharacterPresentationCameraBindingKind.Sequence:
                    if (weight <= 0f)
                    {
                        m_SequenceEvaluator.Retire(
                            producer.ProgramProducerIdentity,
                            command.ProducerGeneration,
                            binding.BlendOutSeconds);
                        m_Sequences.Remove(instance);
                        return;
                    }
                    m_Sequences[instance] = new CameraSequenceRequest(
                        binding.SequenceId,
                        binding.Priority,
                        weight,
                        binding.BlendInSeconds,
                        binding.BlendOutSeconds,
                        binding.TargetKey,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        binding.InterruptPolicy);
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    if (weight <= 0f)
                    {
                        m_Responses.Remove(instance);
                        return;
                    }
                    m_Responses[instance] = new CameraResponseRequest(
                        binding.ResponseMode,
                        binding.ManualOrbitWeight,
                        binding.PitchResponseWeight,
                        binding.YawResponseWeight,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId);
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    if (weight <= 0f)
                    {
                        m_Targets.Remove(instance);
                        return;
                    }
                    m_Targets[instance] = new CameraTargetSelectionRequest(
                        binding.TargetKey,
                        binding.AnchorKey,
                        binding.AimPointKey,
                        binding.PreferredBoneKey,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration);
                    break;
                case CharacterPresentationCameraBindingKind.Override:
                case CharacterPresentationCameraBindingKind.Zoom:
                case CharacterPresentationCameraBindingKind.Stretch:
                case CharacterPresentationCameraBindingKind.Shake:
                case CharacterPresentationCameraBindingKind.Shot:
                    if (weight <= 0f)
                    {
                        m_EffectEvaluator.Retire(
                            string.Empty,
                            command.ProducerGeneration,
                            producer.ProgramProducerIdentity);
                        RemovePendingEffects(producer.ProgramProducerIdentity, command.ProducerGeneration);
                        return;
                    }
                    m_PendingEffects.Add(new CameraEffectRequest(
                        binding.EffectKind,
                        binding.ResourceId,
                        Mathf.Max(0f, command.Weight),
                        binding.Priority,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.Header.EventId.ToString(),
                        command.SourceActionInstanceId));
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
                case CharacterPresentationCameraBindingKind.Sequence:
                    m_SequenceEvaluator.Retire(
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        binding.BlendOutSeconds);
                    m_Sequences.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    m_Responses.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    m_Targets.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Override:
                case CharacterPresentationCameraBindingKind.Zoom:
                case CharacterPresentationCameraBindingKind.Stretch:
                case CharacterPresentationCameraBindingKind.Shake:
                case CharacterPresentationCameraBindingKind.Shot:
                    m_EffectEvaluator.Retire(
                        command.Header.EventId.ToString(),
                        command.ProducerGeneration,
                        producer.ProgramProducerIdentity);
                    RemovePendingEffect(command.Header.EventId.ToString(), command.ProducerGeneration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binding.Kind), binding.Kind, null);
            }
        }

        [PerformanceProbe("presentation.camera")]
        public void Present(
            CharacterBodyPresentationFrame bodyFrame,
            in GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (!bodyFrame.IsValid)
                throw new InvalidOperationException("Presentation Camera requires a valid Body frame.");
            Vector2 look = m_InputAdapter.TryGetLatchedVector2(m_LookInputId, out Vector2 value)
                ? value
                : Vector2.zero;
            bool resetHistory = bodyFrame.ResetSequence != m_LastBodyResetSequence;
            m_LastBodyResetSequence = bodyFrame.ResetSequence;
            Apply(
                bodyFrame.VisiblePosition,
                bodyFrame.VisibleRotation,
                look,
                context.ScaledDeltaSeconds,
                context.UnscaledDeltaSeconds,
                context.PresentationDeltaSeconds,
                resetHistory);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_Sequences.Clear();
            m_Responses.Clear();
            m_Targets.Clear();
            m_SequenceBuffer.Clear();
            m_ResponseBuffer.Clear();
            m_TargetBuffer.Clear();
            m_PendingEffects.Clear();
            m_FrameTargets.Clear();
            m_SequenceEvaluator.Reset();
            m_EffectEvaluator.Reset();
            m_CameraRig.Reset();
            m_Debug.Clear();
            m_LastBodyResetSequence = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_Disposed = true;
        }

        void PresentInitial(Vector3 position, Quaternion rotation)
        {
            Apply(position, rotation, Vector2.zero, 0f, 0f, 0f, true);
        }

        void Apply(
            Vector3 position,
            Quaternion rotation,
            Vector2 look,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            bool resetHistory)
        {
            Vector3 follow = position + rotation * m_FollowBindPosition;
            Vector3 aim = position + rotation * m_AimBindPosition;
            m_SequenceBuffer.Clear();
            foreach (CameraSequenceRequest request in m_Sequences.Values)
                if (request.Active)
                    m_SequenceBuffer.Add(request);
            CameraSequenceRequest sequence = m_SequenceResolver.Resolve(
                m_SequenceBuffer,
                m_CameraProjection.DefaultSequence.SequenceId);
            m_ResponseBuffer.Clear();
            foreach (CameraResponseRequest request in m_Responses.Values)
                if (request.Active)
                    m_ResponseBuffer.Add(request);
            CameraResponseRequest response = m_ResponseResolver.Resolve(m_ResponseBuffer);
            m_TargetBuffer.Clear();
            foreach (CameraTargetSelectionRequest request in m_Targets.Values)
                if (request.Active)
                    m_TargetBuffer.Add(request);
            CameraResolvedTargetPlan resolvedTarget = m_TargetResolver.Resolve(sequence, m_TargetBuffer);
            if (!resolvedTarget.Valid)
                throw new InvalidOperationException(resolvedTarget.Error);

            m_FrameTargets.Clear();
            m_FrameTargets.Add(new CameraTargetSnapshot(
                CameraTargetBindingKeys.Body,
                follow,
                aim,
                Vector3.zero,
                true));
            if (!string.IsNullOrEmpty(resolvedTarget.SourceKey))
            {
                m_FrameTargets.Add(new CameraTargetSnapshot(
                    resolvedTarget.SourceKey,
                    resolvedTarget.HasFollowPoint ? resolvedTarget.FollowPoint : follow,
                    resolvedTarget.HasAimPoint ? resolvedTarget.AimPoint : aim,
                    Vector3.zero,
                    true));
                sequence = sequence.WithTargetKey(resolvedTarget.SourceKey);
            }
            var frameInput = new CameraFrameInput(
                position,
                rotation,
                look,
                scaledDeltaSeconds,
                unscaledDeltaSeconds,
                presentationDeltaSeconds,
                1f,
                1f,
                false,
                resetHistory,
                m_FrameTargets);
            CameraFramePlan plan = m_SequenceEvaluator.Evaluate(
                in frameInput,
                in sequence,
                in response);
            plan = m_EffectEvaluator.Resolve(plan, m_PendingEffects, in frameInput);
            m_PendingEffects.Clear();
            m_CameraRig.Apply(in plan);
            m_Debug.Set(plan, m_CameraRig.Result, resolvedTarget.SourceKey, m_CameraProjection.ProfileRevision);
        }

        void RemovePendingEffect(string eventId, ulong generation)
        {
            for (int i = m_PendingEffects.Count - 1; i >= 0; i--)
            {
                CameraEffectRequest request = m_PendingEffects[i];
                if (request.Generation == generation &&
                    string.Equals(request.EventId, eventId, StringComparison.Ordinal))
                    m_PendingEffects.RemoveAt(i);
            }
        }

        void RemovePendingEffects(string sourceId, ulong generation)
        {
            for (int i = m_PendingEffects.Count - 1; i >= 0; i--)
            {
                CameraEffectRequest request = m_PendingEffects[i];
                if (request.Generation == generation &&
                    string.Equals(request.SourceId, sourceId, StringComparison.Ordinal))
                    m_PendingEffects.RemoveAt(i);
            }
        }

        static CharacterPresentationCameraBinding RequireCameraBinding(
            CharacterPresentationProducerEntry producer)
        {
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Camera ||
                producer.Camera == null)
                throw new InvalidOperationException(
                    $"Camera command targets invalid Projection producer '{producer?.ProgramProducerIdentity}'.");
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
                if (binding.Kind == CharacterPresentationCameraBindingKind.Sequence)
                    resolver.RequireKey(binding.TargetKey, producer.SourceDisplayPath);
                if (binding.Kind == CharacterPresentationCameraBindingKind.Target)
                {
                    resolver.RequireKey(binding.TargetKey, producer.SourceDisplayPath);
                    resolver.RequireKey(binding.AnchorKey, producer.SourceDisplayPath);
                    resolver.RequireKey(binding.AimPointKey, producer.SourceDisplayPath);
                    resolver.RequireKey(binding.PreferredBoneKey, producer.SourceDisplayPath);
                }
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
                StringComparer.Ordinal.GetHashCode(ProducerId));
        }
    }
}
