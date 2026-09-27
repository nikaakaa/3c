using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
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
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly CameraRuntimeBinding m_Binding;
        readonly CameraBindingAdoptedResult m_Adopted;
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly ICameraRigAdapter m_Rig;
        readonly CameraTargetBindingResolver m_TargetResolver;
        readonly CameraSequenceRequestResolver m_SequenceResolver = new CameraSequenceRequestResolver();
        readonly CameraResponseRequestResolver m_ResponseResolver;
        readonly CharacterCameraSequenceEvaluator m_SequenceEvaluator;
        readonly CameraEffectEvaluator m_EffectEvaluator;
        readonly CameraEnvironmentConstraintSolver m_EnvironmentSolver;
        readonly ICharacterPresentationLookInput m_Input;
        readonly string m_LookInputId;
        readonly Vector3 m_FollowBindPosition;
        readonly Vector3 m_AimBindPosition;
        readonly List<CameraTargetSnapshot> m_Targets = new List<CameraTargetSnapshot>();
        readonly List<CameraSequenceRequest> m_SequenceRequests = new List<CameraSequenceRequest>();
        readonly List<CameraResponseRequest> m_ResponseRequests = new List<CameraResponseRequest>();
        readonly List<CameraTargetSelectionRequest> m_TargetRequests = new List<CameraTargetSelectionRequest>();
        readonly List<CameraEffectRequest> m_EffectRequests = new List<CameraEffectRequest>();
        readonly List<ActiveCameraRequest> m_ActiveRequests;
        readonly List<ActiveCameraRequest> m_CandidateRequests;
        readonly CameraPresentationStopReason[] m_CandidateRetirements;
        readonly int m_RequestCapacity;
        bool m_FrameOpen;
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
            RuntimeDiagnosticsContext diagnostics,
            bool initializeExternalState)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Camera domain Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
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
            m_RequestCapacity = profile.RequestCapacity;
            m_ActiveRequests = new List<ActiveCameraRequest>(m_RequestCapacity);
            m_CandidateRequests = new List<ActiveCameraRequest>(m_RequestCapacity);
            m_CandidateRetirements = new CameraPresentationStopReason[m_RequestCapacity];
            m_SequenceRequests.Capacity = checked(m_RequestCapacity + 1);
            m_ResponseRequests.Capacity = m_RequestCapacity;
            m_TargetRequests.Capacity = m_RequestCapacity;
            m_EffectRequests.Capacity = m_RequestCapacity;
            m_Targets.Capacity = checked(m_Projection.TargetSlots.Count + 2);
            m_TargetResolver = new CameraTargetBindingResolver(m_Binding.TargetBindings);
            m_ResponseResolver = new CameraResponseRequestResolver(m_Projection.Input);
            m_SequenceEvaluator = new CharacterCameraSequenceEvaluator(m_Projection);
            m_EffectEvaluator = new CameraEffectEvaluator(m_Projection, m_RequestCapacity);
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
        internal int RequestCapacity => m_RequestCapacity;

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Camera frame candidate is already open.");
            m_CandidateRequests.Clear();
            for (int index = 0; index < m_ActiveRequests.Count; index++)
                m_CandidateRequests.Add(new ActiveCameraRequest(m_ActiveRequests[index].Command, index));
            Array.Clear(m_CandidateRetirements, 0, m_CandidateRetirements.Length);
            m_FrameOpen = true;
        }

        internal void ValidateFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Camera validation requires an open frame candidate.");
            m_Rig.ValidateBinding(string.Empty);
            for (int index = 0; index < m_CandidateRequests.Count; index++)
                ValidateRequest(m_CandidateRequests[index].Command.CameraRequest);
        }

        internal void CommitFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Camera frame candidate is not open.");
            for (int index = 0; index < m_ActiveRequests.Count; index++)
                if (m_CandidateRetirements[index] != 0)
                    RetireRuntimeRequest(m_ActiveRequests[index].Command, m_CandidateRetirements[index]);
            m_ActiveRequests.Clear();
            m_ActiveRequests.AddRange(m_CandidateRequests);
            m_CandidateRequests.Clear();
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            if (!m_FrameOpen)
                return;
            m_CandidateRequests.Clear();
            m_FrameOpen = false;
        }

        List<ActiveCameraRequest> Requests => m_FrameOpen ? m_CandidateRequests : m_ActiveRequests;

        void RetireRequest(ActiveCameraRequest request, CameraPresentationStopReason reason)
        {
            if (!m_FrameOpen)
                RetireRuntimeRequest(request.Command, reason);
            else if (request.AcceptedIndex >= 0)
                m_CandidateRetirements[request.AcceptedIndex] = reason;
        }

        internal void ValidateRequest(PresentationCameraRequest request)
        {
            bool valid = request.Kind switch
            {
                PresentationCameraRequestKind.Sequence => m_Projection.TryGetSequence(request.SequenceId, out _),
                PresentationCameraRequestKind.Response => request.Mode >= 0 && request.Mode <= 2,
                PresentationCameraRequestKind.Target => true,
                PresentationCameraRequestKind.Effect => RequireEffectKind(request.EffectKind) switch
                {
                    CameraEffectKind.Override => m_Projection.TryGetOverride(request.ResourceId, out _),
                    CameraEffectKind.Zoom => m_Projection.TryGetZoom(request.ResourceId, out _),
                    CameraEffectKind.Stretch => m_Projection.TryGetStretch(request.ResourceId, out _),
                    CameraEffectKind.Shake => m_Projection.TryGetShake(request.ResourceId, out _),
                    CameraEffectKind.Shot => m_Projection.TryGetShot(request.ResourceId, out _),
                    _ => false
                },
                _ => false
            };
            if (!valid)
                throw new InvalidOperationException($"Camera request '{request.RequestId}' has unsupported mode or missing resource '{request.ResourceId}/{request.SequenceId}'.");
            if (request.Kind == PresentationCameraRequestKind.Sequence)
                RequireSequenceInterruptPolicy(request.InterruptPolicy);
            if (request.Kind == PresentationCameraRequestKind.Effect && request.EffectKind == (int)CameraEffectKind.Shot)
                m_Rig.ValidateBinding(request.ResourceId);
            ValidateTargetKey(request.TargetKey);
            ValidateTargetKey(request.AnchorKey);
            ValidateTargetKey(request.AimPointKey);
            ValidateTargetKey(request.PreferredBoneKey);
        }

        void ValidateTargetKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key == CameraTargetBindingKeys.Body)
                return;
            for (int index = 0; index < m_Projection.TargetSlots.Count; index++)
                if (m_Projection.TargetSlots[index].SlotId == key)
                    return;
            m_TargetResolver.RequireKey(key, "Timeline Camera");
        }

        internal void Publish(CharacterPresentationCommand command)
        {
            RequireAlive();
            RequireCameraCommand(command);
            if (command.CameraRequest.Lifecycle == PresentationCameraRequestLifecycle.Retire)
            {
                Retire(command);
                return;
            }
            List<ActiveCameraRequest> requests = Requests;
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                ActiveCameraRequest current = requests[i];
                if (!SameRequest(current.Command, command))
                    continue;
                if (current.Command.Header.EventId.Equals(command.Header.EventId) &&
                    current.Command.ProducerGeneration == command.ProducerGeneration &&
                    current.Command.CameraRequest.EffectKind == command.CameraRequest.EffectKind &&
                    string.Equals(current.Command.CameraRequest.ResourceId, command.CameraRequest.ResourceId, StringComparison.Ordinal))
                {
                    requests[i] = new ActiveCameraRequest(command, current.AcceptedIndex);
                    PublishRequestDiagnostics(command, "Updated");
                    return;
                }
                RetireRequest(current, CameraPresentationStopReason.EventRevoked);
                PublishRequestDiagnostics(current.Command, "Replaced");
                requests.RemoveAt(i);
            }
            if (requests.Count == m_RequestCapacity)
                throw new InvalidOperationException($"Camera request capacity {m_RequestCapacity} is exhausted.");
            requests.Add(new ActiveCameraRequest(command));
            PublishRequestDiagnostics(command, "Activated");
        }

        internal void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            RequireCameraCommand(current);
            RequireCameraCommand(replacement);
            Retire(current);
            Publish(replacement);
        }

        internal void Retire(CharacterPresentationCommand command)
        {
            RequireAlive();
            RequireCameraCommand(command);
            CameraPresentationStopReason reason = command.CameraRequest.Lifecycle ==
                PresentationCameraRequestLifecycle.Retire
                    ? CameraPresentationStopReason.NaturalComplete
                    : CameraPresentationStopReason.EventRevoked;
            List<ActiveCameraRequest> requests = Requests;
            bool retired = false;
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                CharacterPresentationCommand active = requests[i].Command;
                if (active.ProducerGeneration != command.ProducerGeneration || !SameRequest(active, command))
                    continue;
                RetireRequest(requests[i], reason);
                requests.RemoveAt(i);
                retired = true;
            }
            if (retired)
                PublishRequestDiagnostics(
                    command,
                    reason == CameraPresentationStopReason.NaturalComplete
                        ? "NaturalComplete"
                        : "EventRevoked");
        }

        internal CharacterDomainRuntimeFact CaptureDomainFact() =>
            new CharacterDomainRuntimeFact(
                CharacterDomainRuntimeFactKind.Camera,
                m_Adopted.Adopted
                    ? CharacterDomainRuntimeFactState.Adopted
                    : CharacterDomainRuntimeFactState.Failed,
                $"camera:{m_Binding.ProfileId}:{m_Binding.ProfileRevision}",
                m_Adopted.Adopted
                    ? $"camera:{m_Adopted.ProfileId}:{m_Adopted.ProfileRevision}:{m_Adopted.InstanceId}"
                    : string.Empty,
                m_Adopted.Adopted ? string.Empty : m_Adopted.FailureMessage);

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
            if (!UnityEngine.Application.isFocused)
                look = Vector2.zero;
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
            m_SequenceRequests.Clear();
            m_ResponseRequests.Clear();
            m_TargetRequests.Clear();
            m_EffectRequests.Clear();
            m_ActiveRequests.Clear();
            m_CandidateRequests.Clear();
            m_FrameOpen = false;
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
            CaptureRequests(
                presentationFrame,
                m_SequenceRequests,
                m_ResponseRequests,
                m_TargetRequests,
                m_EffectRequests);
            m_SequenceRequests.Insert(0, m_DefaultSequenceRequest);
            CameraSequenceRequest sequence = m_SequenceResolver.Resolve(
                m_SequenceRequests,
                m_DefaultSequenceRequest.SequenceId);
            CameraResolvedTargetPlan resolvedTarget = m_TargetResolver.Resolve(
                sequence,
                m_TargetRequests,
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
                if (sequence.IsDefault)
                    m_DefaultSequenceRequest = m_DefaultSequenceRequest.WithTargetKey(resolvedTarget.SourceKey);
                else
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
                resetReason,
                m_Targets);
            CameraResponseRequest response = m_ResponseResolver.Resolve(m_ResponseRequests);
            CameraFramePlan plan = m_SequenceEvaluator.Evaluate(
                in frameInput,
                in sequence,
                in response);
            plan = m_EffectEvaluator.Resolve(plan, m_EffectRequests, in frameInput);
            for (int index = m_ActiveRequests.Count - 1; index >= 0; index--)
            {
                CharacterPresentationCommand command = m_ActiveRequests[index].Command;
                if (command.CameraRequest.Kind == PresentationCameraRequestKind.Effect &&
                    m_EffectEvaluator.IsComplete(command.Header.EventId, command.ProducerGeneration,
                        command.ProducerId, command.SourceActionInstanceId, command.Cycle))
                    m_ActiveRequests.RemoveAt(index);
            }
            plan = plan.WithPitchClamped(
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);
            plan = m_EnvironmentSolver.Apply(plan, in frameInput);
            m_Rig.Apply(in plan);
            AppliedPlan = plan;
            AppliedTargetValid = resolvedTarget.Valid;
            AppliedResetReason = resetReason;
            PublishSnapshotDiagnostics(in plan, resetReason);
        }

        internal System.Collections.ObjectModel.ReadOnlyCollection<CameraEffectContribution> EffectContributions => m_EffectEvaluator.Contributions;
        internal string ProfileId => m_Binding.ProfileId;
        internal string ProfileRevision => m_Binding.ProfileRevision;
        internal CameraFramePlan AppliedPlan { get; private set; }
        internal bool AppliedTargetValid { get; private set; }
        internal CameraResetReason AppliedResetReason { get; private set; }

        void PublishRequestDiagnostics(CharacterPresentationCommand command, string status)
        {
            if (!m_Diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.CameraRequest))
            {
                return;
            }
            PresentationCameraRequest request = command.CameraRequest;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.CameraRequest,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = status,
                    Name = request.Kind.ToString(),
                    OwnerId = command.ProducerId,
                    RelatedElementId = request.RequestId,
                    ActionInstanceId = command.SourceActionInstanceId,
                    Time = command.SampleTime,
                    Weight = request.Weight,
                    Priority = request.Priority,
                    Cycle = command.Cycle,
                    Detail = $"lifecycle={request.Lifecycle};sequence={request.SequenceId};resource={request.ResourceId};target={request.TargetKey};anchor={request.AnchorKey};aim={request.AimPointKey};mode={request.Mode};effect={request.EffectKind};generation={command.ProducerGeneration}"
                });
        }

        void PublishSnapshotDiagnostics(in CameraFramePlan plan, CameraResetReason resetReason)
        {
            if (!m_Diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.CameraSnapshot))
            {
                return;
            }
            CameraBasisSnapshot basis = m_Rig.BasisSnapshot;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.CameraSnapshot,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = plan.Valid && basis.Valid ? "Ready" : "Invalid",
                    Name = plan.SequenceId,
                    OwnerId = plan.SourceId,
                    RelatedElementId = plan.ShotId,
                    ActionInstanceId = plan.SourceActionInstanceId,
                    Time = plan.BlendProgress,
                    SecondaryTime = plan.FieldOfView,
                    Flag = plan.ResetHistory,
                    Cause = resetReason.ToString(),
                    Detail = $"position={plan.Location};pivot={plan.PivotLocation};yaw={plan.Yaw:0.###};pitch={plan.Pitch:0.###};basisYaw={basis.Yaw:0.###};basisPitch={basis.Pitch:0.###};look={basis.LookDirection};aim={basis.AimPoint};collision={plan.Collision.Status};reset={plan.ResetHistory};resetReason={resetReason};activeRequests={m_ActiveRequests.Count}"
                });
        }

        void CaptureRequests(
            ulong presentationFrame,
            List<CameraSequenceRequest> sequences,
            List<CameraResponseRequest> responses,
            List<CameraTargetSelectionRequest> targets,
            List<CameraEffectRequest> effects)
        {
            sequences.Clear();
            responses.Clear();
            targets.Clear();
            effects.Clear();
            for (int i = 0; i < m_ActiveRequests.Count; i++)
            {
                CharacterPresentationCommand command = m_ActiveRequests[i].Command;
                PresentationCameraRequest payload = command.CameraRequest;
                EventId eventId = command.Header.EventId;
                switch (payload.Kind)
                {
                    case PresentationCameraRequestKind.Sequence:
                        sequences.Add(new CameraSequenceRequest(
                            payload.SequenceId,
                            payload.Priority,
                            payload.Weight,
                            payload.BlendInSeconds,
                            payload.BlendOutSeconds,
                            payload.TargetKey,
                            command.ProducerId,
                            command.ProducerGeneration,
                            command.SourceActionInstanceId,
                            RequireSequenceInterruptPolicy(payload.InterruptPolicy),
                            false,
                            eventId,
                            command.Cycle,
                            command.SampleTime));
                        break;
                    case PresentationCameraRequestKind.Effect:
                        effects.Add(new CameraEffectRequest(
                            RequireEffectKind(payload.EffectKind),
                            payload.ResourceId,
                            payload.Weight,
                            payload.Priority,
                            command.ProducerId,
                            command.ProducerGeneration,
                            eventId,
                            command.SourceActionInstanceId,
                            command.Cycle,
                            command.SampleTime));
                        break;
                    case PresentationCameraRequestKind.Response:
                        responses.Add(new CameraResponseRequest(
                            RequireResponseMode(payload.Mode),
                            payload.ManualOrbitWeight,
                            payload.PitchWeight,
                            payload.YawWeight,
                            payload.Priority,
                            payload.Weight,
                            command.ProducerId,
                            command.ProducerGeneration,
                            command.SourceActionInstanceId,
                            eventId,
                            command.Cycle,
                            command.SampleTime));
                        break;
                    case PresentationCameraRequestKind.Target:
                        targets.Add(new CameraTargetSelectionRequest(
                            payload.TargetKey,
                            payload.AnchorKey,
                            payload.AimPointKey,
                            payload.PreferredBoneKey,
                            payload.Priority,
                            payload.Weight,
                            command.ProducerId,
                            command.ProducerGeneration,
                            command.SourceActionInstanceId,
                            command.Cycle,
                            eventId));
                        break;
                    default:
                        throw new InvalidOperationException($"Camera request kind '{payload.Kind}' is unsupported.");
                }
            }
        }

        void RetireRuntimeRequest(
            CharacterPresentationCommand command,
            CameraPresentationStopReason reason)
        {
            PresentationCameraRequest request = command.CameraRequest;
            switch (request.Kind)
            {
                case PresentationCameraRequestKind.Sequence:
                    m_SequenceEvaluator.Retire(
                        command.ProducerId,
                        command.ProducerGeneration,
                        command.SourceActionInstanceId,
                        command.Cycle,
                        request.BlendOutSeconds,
                        reason);
                    break;
                case PresentationCameraRequestKind.Effect:
                    m_EffectEvaluator.Retire(
                        command.Header.EventId,
                        command.ProducerGeneration,
                        command.ProducerId,
                        command.SourceActionInstanceId,
                        command.Cycle,
                        reason);
                    break;
            }
        }

        static bool SameRequest(CharacterPresentationCommand left, CharacterPresentationCommand right)
        {
            return left.Kind == CharacterPresentationCommandKind.Camera &&
                   right.Kind == CharacterPresentationCommandKind.Camera &&
                   string.Equals(left.ProducerId, right.ProducerId, StringComparison.Ordinal) &&
                   left.SourceActionInstanceId == right.SourceActionInstanceId &&
                   left.Cycle == right.Cycle &&
                   left.CameraRequest.Kind == right.CameraRequest.Kind &&
                   string.Equals(left.CameraRequest.RequestId, right.CameraRequest.RequestId, StringComparison.Ordinal) &&
                   string.Equals(left.CameraRequest.SequenceId, right.CameraRequest.SequenceId, StringComparison.Ordinal);
        }

        static CameraSequenceInterruptPolicy RequireSequenceInterruptPolicy(int value) => value switch
        {
            (int)CameraSequenceInterruptPolicy.BlendOut => CameraSequenceInterruptPolicy.BlendOut,
            (int)CameraSequenceInterruptPolicy.Cut => CameraSequenceInterruptPolicy.Cut,
            (int)CameraSequenceInterruptPolicy.HoldUntilSourceEnds => CameraSequenceInterruptPolicy.HoldUntilSourceEnds,
            _ => throw new InvalidOperationException($"Camera sequence interrupt policy '{value}' is unsupported.")
        };

        static CameraEffectKind RequireEffectKind(int value) => value switch
        {
            (int)CameraEffectKind.Override => CameraEffectKind.Override,
            (int)CameraEffectKind.Zoom => CameraEffectKind.Zoom,
            (int)CameraEffectKind.Stretch => CameraEffectKind.Stretch,
            (int)CameraEffectKind.Shake => CameraEffectKind.Shake,
            (int)CameraEffectKind.Shot => CameraEffectKind.Shot,
            _ => throw new InvalidOperationException($"Camera effect kind '{value}' is unsupported.")
        };

        static CameraResponseMode RequireResponseMode(int value)
        {
            CameraResponseMode mode = value switch
            {
                0 => CameraResponseMode.Full,
                1 => CameraResponseMode.Suppressed,
                2 => CameraResponseMode.Weighted,
                _ => throw new InvalidOperationException($"Camera response mode '{value}' is unsupported.")
            };
            return mode;
        }

        static void RequireCameraCommand(CharacterPresentationCommand command)
        {
            if (command.Kind != CharacterPresentationCommandKind.Camera || !command.CameraRequest.IsValid)
                throw new InvalidOperationException("Camera domain received a non-camera presentation command.");
            if (command.SourceActionInstanceId == 0)
                throw new InvalidOperationException("Camera PresentationCommand requires an Action instance.");
        }

        readonly struct ActiveCameraRequest
        {
            public ActiveCameraRequest(CharacterPresentationCommand command, int acceptedIndex = -1)
            {
                Command = command;
                AcceptedIndex = acceptedIndex;
            }

            public CharacterPresentationCommand Command { get; }
            public int AcceptedIndex { get; }
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
