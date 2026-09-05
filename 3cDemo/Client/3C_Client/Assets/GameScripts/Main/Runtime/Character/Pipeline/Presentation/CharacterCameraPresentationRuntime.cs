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
        readonly CameraResponseRequestResolver m_ResponseResolver;
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
        readonly Dictionary<PresentationProducerInstanceId, PendingSequenceTermination> m_PendingSequenceTerminations =
            new Dictionary<PresentationProducerInstanceId, PendingSequenceTermination>();
        readonly List<CameraSequenceRequest> m_SequenceBuffer = new List<CameraSequenceRequest>();
        readonly List<CameraResponseRequest> m_ResponseBuffer = new List<CameraResponseRequest>();
        readonly List<CameraTargetSelectionRequest> m_TargetBuffer = new List<CameraTargetSelectionRequest>();
        readonly List<CameraEffectRequest> m_PendingEffects = new List<CameraEffectRequest>();
        readonly List<CameraTargetSnapshot> m_FrameTargets = new List<CameraTargetSnapshot>();
        readonly CameraDebugSnapshot m_Debug = new CameraDebugSnapshot();
        readonly HashSet<CameraPresentationScopeKey> m_ForcedScopes =
            new HashSet<CameraPresentationScopeKey>();
        readonly Dictionary<CameraPresentationScopeKey, CharacterCameraSequenceEvaluator.State> m_SuspendedSequenceStates =
            new Dictionary<CameraPresentationScopeKey, CharacterCameraSequenceEvaluator.State>();

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
            m_ResponseResolver = new CameraResponseRequestResolver(m_CameraProjection.Input);
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
            var instance = new PresentationProducerInstanceId(
                command.ProducerId,
                command.ProducerGeneration,
                command.SourceActionInstanceId,
                command.Cycle);
            float weight = Mathf.Clamp01(command.Weight);
            switch (binding.Kind)
            {
                case CharacterPresentationCameraBindingKind.Sequence:
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
                        binding.InterruptPolicy,
                        false,
                        command.Header.EventId.ToString(),
                        command.Cycle,
                        command.SampleTime);
                    m_PendingSequenceTerminations.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    m_Responses[instance] = new CameraResponseRequest(
                        binding.ResponseMode,
                        binding.ManualOrbitWeight,
                        binding.PitchResponseWeight,
                        binding.YawResponseWeight,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        command.Header.EventId.ToString(),
                        command.Cycle,
                        command.SampleTime);
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    m_Targets[instance] = new CameraTargetSelectionRequest(
                        binding.TargetKey,
                        binding.AnchorKey,
                        binding.AimPointKey,
                        binding.PreferredBoneKey,
                        binding.Priority,
                        weight,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        command.Cycle,
                        command.Header.EventId.ToString());
                    break;
                case CharacterPresentationCameraBindingKind.Override:
                case CharacterPresentationCameraBindingKind.Zoom:
                case CharacterPresentationCameraBindingKind.Stretch:
                case CharacterPresentationCameraBindingKind.Shake:
                case CharacterPresentationCameraBindingKind.Shot:
                    RemovePendingEffect(
                        command.Header.EventId.ToString(),
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        command.Cycle);
                    m_PendingEffects.Add(new CameraEffectRequest(
                        binding.EffectKind,
                        binding.ResourceId,
                        Mathf.Max(0f, command.Weight),
                        binding.Priority,
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.Header.EventId.ToString(),
                        command.SourceActionInstanceId,
                        command.Cycle,
                        command.SampleTime));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binding.Kind), binding.Kind, null);
            }
        }

        public void Retire(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            Terminate(command, producer, CameraPresentationStopReason.EventRevoked);
        }

        public void Complete(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            Terminate(command, producer, CameraPresentationStopReason.NaturalComplete);
        }

        public void Release(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            Terminate(command, producer, CameraPresentationStopReason.Cancel);
        }

        public void ForceTeardown(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireCameraBinding(producer);
            SuspendForceScope(command, producer);
        }

        public void ResumeForceTeardown(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireCameraBinding(producer);
            CameraPresentationScopeKey scope = new CameraPresentationScopeKey(
                producer.ProgramProducerIdentity,
                command.ProducerGeneration,
                command.SourceActionInstanceId);
            if (!m_ForcedScopes.Remove(scope))
                return;
            if (m_SuspendedSequenceStates.TryGetValue(scope, out CharacterCameraSequenceEvaluator.State state))
            {
                m_SequenceEvaluator.RestoreState(state);
                m_SuspendedSequenceStates.Remove(scope);
            }
            m_EffectEvaluator.ResumeScope(scope);
        }

        public void ConfirmForceTeardown(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireCameraBinding(producer);
            CameraPresentationScopeKey scope = new CameraPresentationScopeKey(
                producer.ProgramProducerIdentity,
                command.ProducerGeneration,
                command.SourceActionInstanceId);
            m_ForcedScopes.Remove(scope);
            m_SuspendedSequenceStates.Remove(scope);
            m_EffectEvaluator.ConfirmScope(scope);
            m_SequenceEvaluator.ForceTeardown(
                producer.ProgramProducerIdentity,
                command.ProducerGeneration,
                command.SourceActionInstanceId);
            RemoveScopeInstances(command);
            RemovePendingEffectsAllCycles(
                producer.ProgramProducerIdentity,
                command.ProducerGeneration,
                command.SourceActionInstanceId);
            RemovePendingSequenceTerminations(scope);
        }

        void SuspendForceScope(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            CameraPresentationScopeKey scope = new CameraPresentationScopeKey(
                producer.ProgramProducerIdentity,
                command.ProducerGeneration,
                command.SourceActionInstanceId);
            if (m_ForcedScopes.Add(scope) &&
                m_SequenceEvaluator.TryCaptureScopeState(scope, out CharacterCameraSequenceEvaluator.State state))
                m_SuspendedSequenceStates[scope] = state;
            m_EffectEvaluator.SuspendScope(scope);
            RemovePendingSequenceTerminations(scope);
        }

        void Terminate(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer,
            CameraPresentationStopReason reason)
        {
            RequireAlive();
            CharacterPresentationCameraBinding binding = RequireCameraBinding(producer);
            var instance = new PresentationProducerInstanceId(
                command.ProducerId,
                command.ProducerGeneration,
                command.SourceActionInstanceId,
                command.Cycle);
            if (reason == CameraPresentationStopReason.ForceTeardown)
            {
                SuspendForceScope(command, producer);
                return;
            }
            switch (binding.Kind)
            {
                case CharacterPresentationCameraBindingKind.Sequence:
                    if (!m_Sequences.TryGetValue(instance, out CameraSequenceRequest sequenceRequest) ||
                        reason == CameraPresentationStopReason.EventRevoked &&
                        !string.Equals(sequenceRequest.EventId, command.Header.EventId.ToString(), StringComparison.Ordinal))
                    {
                        return;
                    }
                    bool retired = m_SequenceEvaluator.Retire(
                        producer.ProgramProducerIdentity,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        command.Cycle,
                        binding.BlendOutSeconds,
                        reason);
                    if (retired || reason == CameraPresentationStopReason.EventRevoked)
                        m_Sequences.Remove(instance);
                    else
                        m_PendingSequenceTerminations[instance] = new PendingSequenceTermination(
                            producer.ProgramProducerIdentity,
                            command.ProducerGeneration,
                            command.SourceActionInstanceId,
                            command.Cycle,
                            binding.BlendOutSeconds,
                            reason);
                    break;
                case CharacterPresentationCameraBindingKind.Response:
                    if (reason == CameraPresentationStopReason.EventRevoked &&
                        (!m_Responses.TryGetValue(instance, out CameraResponseRequest responseRequest) ||
                         !string.Equals(responseRequest.EventId, command.Header.EventId.ToString(), StringComparison.Ordinal)))
                        return;
                    m_Responses.Remove(instance);
                    break;
                case CharacterPresentationCameraBindingKind.Target:
                    if (reason == CameraPresentationStopReason.EventRevoked &&
                        (!m_Targets.TryGetValue(instance, out CameraTargetSelectionRequest targetRequest) ||
                         !string.Equals(targetRequest.EventId, command.Header.EventId.ToString(), StringComparison.Ordinal)))
                        return;
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
                        producer.ProgramProducerIdentity,
                        command.SourceActionInstanceId,
                        command.Cycle,
                        reason);
                    if (reason == CameraPresentationStopReason.EventRevoked)
                    {
                        RemovePendingEffect(
                            command.Header.EventId.ToString(),
                            producer.ProgramProducerIdentity,
                            command.ProducerGeneration,
                            command.SourceActionInstanceId,
                            command.Cycle);
                    }
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
                context.OwnerTimeScale,
                context.LocalAvatarTimeScale,
                context.HasOwnerTimeScale,
                context.HasLocalAvatarTimeScale,
                context.Paused,
                resetHistory);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_Sequences.Clear();
            m_Responses.Clear();
            m_Targets.Clear();
            m_PendingSequenceTerminations.Clear();
            m_ForcedScopes.Clear();
            m_SuspendedSequenceStates.Clear();
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
            Apply(position, rotation, Vector2.zero, 0f, 0f, 0f, 0f, 0f, false, false, false, true);
        }

        void Apply(
            Vector3 position,
            Quaternion rotation,
            Vector2 look,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            float ownerTimeScale,
            float localAvatarTimeScale,
            bool ownerTimeScaleAvailable,
            bool localAvatarTimeScaleAvailable,
            bool paused,
            bool resetHistory)
        {
            Vector3 follow = position + rotation * m_FollowBindPosition;
            Vector3 aim = position + rotation * m_AimBindPosition;
            m_SequenceBuffer.Clear();
            foreach (CameraSequenceRequest request in m_Sequences.Values)
                if (!string.IsNullOrWhiteSpace(request.SequenceId) &&
                    !IsForced(request.SourceId, request.Generation, request.SourceActionInstanceId))
                    m_SequenceBuffer.Add(request);
            CameraSequenceRequest sequence = m_SequenceResolver.Resolve(
                m_SequenceBuffer,
                m_CameraProjection.DefaultSequence.SequenceId);
            PrunePendingSequenceCandidates(sequence);
            m_ResponseBuffer.Clear();
            foreach (CameraResponseRequest request in m_Responses.Values)
                if (request.Active && !IsForced(request.SourceId, request.Generation, request.SourceActionInstanceId))
                    m_ResponseBuffer.Add(request);
            CameraResponseRequest response = m_ResponseResolver.Resolve(m_ResponseBuffer);
            m_TargetBuffer.Clear();
            foreach (CameraTargetSelectionRequest request in m_Targets.Values)
                if (request.Active && !IsForced(request.SourceId, request.Generation, request.SourceActionInstanceId))
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
                true,
                false));
            if (!string.IsNullOrEmpty(resolvedTarget.SourceKey))
            {
                m_FrameTargets.Add(new CameraTargetSnapshot(
                    resolvedTarget.SourceKey,
                    resolvedTarget.HasFollowPoint ? resolvedTarget.FollowPoint : follow,
                    resolvedTarget.HasAimPoint ? resolvedTarget.AimPoint : aim,
                    Vector3.zero,
                    true,
                    resolvedTarget.HasAimPoint));
                sequence = sequence.WithTargetKey(resolvedTarget.SourceKey);
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
                m_FrameTargets);
            CameraFramePlan plan = sequence.IsDefault && HasForcedCurrentSequence()
                ? m_SequenceEvaluator.EvaluateSuppressed(in frameInput, in response)
                : IsForced(
                    sequence.SourceId,
                    sequence.Generation,
                    sequence.SourceActionInstanceId)
                    ? m_SequenceEvaluator.EvaluateSuppressed(in frameInput, in response)
                    : m_SequenceEvaluator.Evaluate(in frameInput, in sequence, in response);
            ApplyPendingSequenceTerminations();
            plan = m_EffectEvaluator.Resolve(plan, m_PendingEffects, in frameInput);
            m_PendingEffects.Clear();
            m_CameraRig.Apply(in plan);
            m_Debug.Set(plan, m_CameraRig.Result, resolvedTarget.SourceKey, m_CameraProjection.ProfileRevision);
        }

        void RemovePendingEffect(
            string eventId,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            int cycle)
        {
            for (int i = m_PendingEffects.Count - 1; i >= 0; i--)
            {
                CameraEffectRequest request = m_PendingEffects[i];
                if (request.Generation == generation &&
                    request.SourceActionInstanceId == sourceActionInstanceId &&
                    request.Cycle == cycle &&
                    string.Equals(request.EventId, eventId, StringComparison.Ordinal) &&
                    string.Equals(request.SourceId, sourceId, StringComparison.Ordinal))
                    m_PendingEffects.RemoveAt(i);
            }
        }

        void RemovePendingEffectsAllCycles(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            for (int i = m_PendingEffects.Count - 1; i >= 0; i--)
            {
                CameraEffectRequest request = m_PendingEffects[i];
                if (request.Generation == generation &&
                    request.SourceActionInstanceId == sourceActionInstanceId &&
                    string.Equals(request.SourceId, sourceId, StringComparison.Ordinal))
                    m_PendingEffects.RemoveAt(i);
            }
        }

        void ApplyPendingSequenceTerminations()
        {
            if (m_PendingSequenceTerminations.Count == 0)
                return;
            var applied = new List<PresentationProducerInstanceId>();
            foreach (KeyValuePair<PresentationProducerInstanceId, PendingSequenceTermination> pair in m_PendingSequenceTerminations)
            {
                if (!m_Sequences.ContainsKey(pair.Key))
                {
                    applied.Add(pair.Key);
                    continue;
                }
                PendingSequenceTermination termination = pair.Value;
                if (m_SequenceEvaluator.Retire(
                        termination.SourceId,
                        termination.Generation,
                        termination.SourceActionInstanceId,
                        termination.Cycle,
                        termination.BlendOutSeconds,
                        termination.Reason))
                {
                    m_Sequences.Remove(pair.Key);
                    applied.Add(pair.Key);
                }
            }
            for (int i = 0; i < applied.Count; i++)
                m_PendingSequenceTerminations.Remove(applied[i]);
        }

        void PrunePendingSequenceCandidates(CameraSequenceRequest selected)
        {
            if (m_PendingSequenceTerminations.Count == 0)
                return;
            var remove = new List<PresentationProducerInstanceId>();
            foreach (KeyValuePair<PresentationProducerInstanceId, PendingSequenceTermination> pair in m_PendingSequenceTerminations)
            {
                if (!m_Sequences.TryGetValue(pair.Key, out CameraSequenceRequest request) ||
                    !SameSequenceInstance(request, selected))
                    remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                m_PendingSequenceTerminations.Remove(remove[i]);
                m_Sequences.Remove(remove[i]);
            }
        }

        void RemovePendingSequenceTerminations(CameraPresentationScopeKey scope)
        {
            var remove = new List<PresentationProducerInstanceId>();
            foreach (KeyValuePair<PresentationProducerInstanceId, PendingSequenceTermination> pair in m_PendingSequenceTerminations)
            {
                PendingSequenceTermination termination = pair.Value;
                if (string.Equals(termination.SourceId, scope.SourceId, StringComparison.Ordinal) &&
                    termination.Generation == scope.Generation &&
                    termination.SourceActionInstanceId == scope.SourceActionInstanceId)
                    remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                m_PendingSequenceTerminations.Remove(remove[i]);
        }

        bool IsForced(string sourceId, ulong generation, ulong sourceActionInstanceId) =>
            m_ForcedScopes.Contains(new CameraPresentationScopeKey(
                sourceId,
                generation,
                sourceActionInstanceId));

        bool HasForcedCurrentSequence()
        {
            foreach (CameraPresentationScopeKey scope in m_ForcedScopes)
                if (m_SequenceEvaluator.IsCurrentScope(scope))
                    return true;
            return false;
        }

        static bool SameSequenceInstance(
            CameraSequenceRequest left,
            CameraSequenceRequest right)
        {
            return left.Generation == right.Generation &&
                   left.SourceActionInstanceId == right.SourceActionInstanceId &&
                   left.Cycle == right.Cycle &&
                   string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal);
        }

        void RemoveScopeInstances(CharacterPresentationCommand command)
        {
            var remove = new List<PresentationProducerInstanceId>();
            foreach (PresentationProducerInstanceId key in m_Sequences.Keys)
                if (MatchesScope(key, command))
                    remove.Add(key);
            for (int i = 0; i < remove.Count; i++)
                m_Sequences.Remove(remove[i]);
            remove.Clear();
            foreach (PresentationProducerInstanceId key in m_Responses.Keys)
                if (MatchesScope(key, command))
                    remove.Add(key);
            for (int i = 0; i < remove.Count; i++)
                m_Responses.Remove(remove[i]);
            remove.Clear();
            foreach (PresentationProducerInstanceId key in m_Targets.Keys)
                if (MatchesScope(key, command))
                    remove.Add(key);
            for (int i = 0; i < remove.Count; i++)
                m_Targets.Remove(remove[i]);
        }

        static bool MatchesScope(
            PresentationProducerInstanceId key,
            CharacterPresentationCommand command) =>
            key.Generation == command.ProducerGeneration &&
            key.SourceActionInstanceId == command.SourceActionInstanceId &&
            string.Equals(key.ProducerId, command.ProducerId, StringComparison.Ordinal);

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
            public PresentationProducerInstanceId(
                string producerId,
                ulong generation,
                ulong sourceActionInstanceId,
                int cycle)
            {
                ProducerId = producerId ?? string.Empty;
                Generation = generation;
                SourceActionInstanceId = sourceActionInstanceId;
                Cycle = cycle;
            }

            public string ProducerId { get; }
            public ulong Generation { get; }
            public ulong SourceActionInstanceId { get; }
            public int Cycle { get; }

            public bool Equals(PresentationProducerInstanceId other) =>
                Generation == other.Generation &&
                SourceActionInstanceId == other.SourceActionInstanceId &&
                Cycle == other.Cycle &&
                string.Equals(ProducerId, other.ProducerId, StringComparison.Ordinal);

            public override bool Equals(object obj) =>
                obj is PresentationProducerInstanceId other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(
                Generation,
                SourceActionInstanceId,
                Cycle,
                StringComparer.Ordinal.GetHashCode(ProducerId));
        }

        readonly struct PendingSequenceTermination
        {
            public PendingSequenceTermination(
                string sourceId,
                ulong generation,
                ulong sourceActionInstanceId,
                int cycle,
                float blendOutSeconds,
                CameraPresentationStopReason reason)
            {
                SourceId = sourceId;
                Generation = generation;
                SourceActionInstanceId = sourceActionInstanceId;
                Cycle = cycle;
                BlendOutSeconds = blendOutSeconds;
                Reason = reason;
            }

            public string SourceId { get; }
            public ulong Generation { get; }
            public ulong SourceActionInstanceId { get; }
            public int Cycle { get; }
            public float BlendOutSeconds { get; }
            public CameraPresentationStopReason Reason { get; }
        }
    }
}
