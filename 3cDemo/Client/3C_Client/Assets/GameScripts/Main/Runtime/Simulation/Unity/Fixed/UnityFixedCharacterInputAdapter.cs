using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using UnityEngine.InputSystem;
using FixedCharacterSimulationInput = ThirdPersonSimulation.Fixed.CharacterSimulationInput;
using FixedSimulationInputRequest = ThirdPersonSimulation.Fixed.SimulationInputRequest;
using FixedSimulationInputValue = ThirdPersonSimulation.Fixed.SimulationInputValue;
using FixedSimulationOperation = ThirdPersonSimulation.Fixed.SimulationOperation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class UnityFixedCharacterInputAdapter :
        IUnityFixedCharacterControlSourceRuntime,
        ICharacterPresentationLookInput,
        IDisposable
    {
        readonly CharacterInputProfile m_Profile;
        readonly ThirdPersonSimulation.Fixed.CharacterSimulationProgram m_Program;
        readonly ThirdPersonCameraController m_CameraRig;
        readonly ISimulationSessionActorHost m_Owner;
        readonly string m_ActionTargetInputValueId;
        readonly ICharacterActionTargetInputProvider m_ActionTargetProvider;
        readonly Dictionary<string, InputValueBinding> m_ValueBindings =
            new Dictionary<string, InputValueBinding>(StringComparer.Ordinal);
        readonly List<RequestBinding> m_RequestBindings = new List<RequestBinding>();
        readonly HashSet<string> m_CameraRelativeVector2Ids = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_WorldVector2Ids = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, LatchedInputValue> m_LatchedValues =
            new Dictionary<string, LatchedInputValue>(StringComparer.Ordinal);
        readonly List<PendingRequest> m_PendingRequests = new List<PendingRequest>();
        readonly List<FixedSimulationInputValue> m_InputValues = new List<FixedSimulationInputValue>();
        readonly List<FixedSimulationInputRequest> m_InputRequests = new List<FixedSimulationInputRequest>();
        readonly List<string> m_ActionTargetInputIds = new List<string>();
        readonly bool m_RequiresCameraBasis;

        ulong m_RenderFrame;
        ulong m_RequestSequence;
        CameraBasisSnapshot m_LatchedCameraBasis;
        bool m_Active;
        bool m_Disposed;

        public UnityFixedCharacterInputAdapter(
            CharacterInputProfile profile,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program,
            ThirdPersonCameraController cameraRig)
            : this(profile, program, cameraRig, null, string.Empty, null)
        {
        }

        public UnityFixedCharacterInputAdapter(
            CharacterInputProfile profile,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program,
            ThirdPersonCameraController cameraRig,
            ISimulationSessionActorHost owner,
            string actionTargetInputValueId,
            ICharacterActionTargetInputProvider actionTargetProvider)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_CameraRig = cameraRig ? cameraRig : throw new ArgumentNullException(nameof(cameraRig));
            m_Owner = owner;
            m_ActionTargetInputValueId = string.IsNullOrWhiteSpace(actionTargetInputValueId)
                ? string.Empty
                : actionTargetInputValueId.Trim();
            m_ActionTargetProvider = actionTargetProvider;
            if ((m_ActionTargetProvider == null) != string.IsNullOrEmpty(m_ActionTargetInputValueId) ||
                m_ActionTargetProvider != null && m_Owner == null)
            {
                throw new ArgumentException("Fixed Action target input requires an owner, input value id, and target provider together.");
            }
            if (program.Manifest.NumericProfile != FixedSimulationNumericProfile.Value)
                throw new ArgumentException("Unity Fixed Input Adapter requires a FixedQ32.32 Program.", nameof(program));
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            BuildBindings();
            BuildActionTargetInputs();
            ValidateActionTargetInput();
            ValidateProgramInputs();
            ResolveDirectionSpaces();
            m_RequiresCameraBasis = RequiresCameraBasis(program);
        }

        public string SourceIdentity =>
            $"UnityInputSystem/FixedQ32.32/{m_Program.ProgramHash}/{m_ActionTargetInputValueId}/{(m_ActionTargetProvider == null ? "none" : m_ActionTargetProvider.ProviderIdentity)}";
        public ProgramId CharacterProgramId => m_Program.Manifest.ProgramId;
        public ProgramHash CharacterProgramHash => m_Program.ProgramHash;
        public InputActionAsset Actions => m_Profile.SourceAsset;

        public void Activate()
        {
            RequireAlive();
            if (m_Active)
                return;
            Actions.Enable();
            m_Active = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
            Actions.Disable();
            m_LatchedValues.Clear();
            m_PendingRequests.Clear();
            m_InputValues.Clear();
            m_InputRequests.Clear();
            m_LatchedCameraBasis = default;
            m_RenderFrame = 0;
            m_Active = false;
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Active)
                throw new InvalidOperationException("Unity Fixed Input Adapter must be active before render-frame capture.");
            if (renderFrame == 0 || renderFrame <= m_RenderFrame)
                throw new InvalidOperationException("Unity Fixed Input Adapter requires a strictly increasing render frame.");
            m_RenderFrame = renderFrame;
            m_LatchedValues.Clear();
            foreach (KeyValuePair<string, InputValueBinding> pair in m_ValueBindings)
                m_LatchedValues.Add(pair.Key, ReadValue(pair.Value));
            if (m_RequiresCameraBasis || m_CameraRelativeVector2Ids.Count != 0)
                m_LatchedCameraBasis = m_CameraRig.BasisSnapshot;
            for (int i = 0; i < m_RequestBindings.Count; i++)
            {
                RequestBinding binding = m_RequestBindings[i];
                if (binding.Action.WasPressedThisFrame() || binding.Action.triggered)
                {
                    m_PendingRequests.Add(new PendingRequest(
                        binding.RequestId,
                        NextRequestSequence(),
                        m_RenderFrame,
                        binding.BufferSeconds,
                        binding.Priority,
                        binding.TimingClass));
                }
            }
        }

        public FixedCharacterSimulationInput BuildInput(FixedCharacterInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0)
                throw new InvalidOperationException("Unity Fixed Input Adapter has no captured render frame.");
            m_InputValues.Clear();
            foreach (KeyValuePair<string, LatchedInputValue> pair in m_LatchedValues)
                m_InputValues.Add(ToSimulationValue(pair.Key, pair.Value));
            if (m_RequiresCameraBasis)
                AppendCameraBasis(m_InputValues, m_LatchedCameraBasis);
            for (int i = 0; i < m_ActionTargetInputIds.Count; i++)
            {
                m_InputValues.Add(FixedSimulationInputValue.FromActionTargetSnapshot(
                    m_ActionTargetInputIds[i],
                    string.Equals(m_ActionTargetInputIds[i], m_ActionTargetInputValueId, StringComparison.Ordinal)
                        ? ResolveActionTarget(context)
                        : ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot.None));
            }

            if (m_PendingRequests.Count > context.MaximumPendingRequests)
                throw new InvalidOperationException("Unity Fixed Input Adapter pending request capacity is exhausted.");
            for (int i = 0; i < m_PendingRequests.Count; i++)
            {
                PendingRequest pending = m_PendingRequests[i];
                if (pending.CaptureTick != 0)
                    continue;
                pending.Schedule(
                    context.SimulationTick.Value,
                    pending.TimingClass == CharacterActionRequestTimingClass.Offensive
                        ? context.OffensiveRequestDelayTicks
                        : 0);
            }
            m_InputRequests.Clear();
            int emittedCount = 0;
            for (int i = 0; i < m_PendingRequests.Count; i++)
            {
                PendingRequest pending = m_PendingRequests[i];
                if (pending.EligibleTick > context.SimulationTick.Value)
                    break;
                ulong duration = pending.BufferSeconds <= 0f
                    ? 0UL
                    : (ulong)Math.Max(1, Mathf.CeilToInt(pending.BufferSeconds * context.TickRate));
                m_InputRequests.Add(new FixedSimulationInputRequest(
                    pending.RequestId,
                    pending.Sequence,
                    pending.CaptureTick,
                    checked(context.SimulationTick.Value + duration),
                    pending.Priority));
                emittedCount++;
            }
            if (emittedCount != 0)
                m_PendingRequests.RemoveRange(0, emittedCount);
            return new FixedCharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                context.Source,
                SourceIdentity,
                context.InputSequence,
                m_InputValues,
                m_InputRequests);
        }

        public byte[] CaptureState()
        {
            RequireAlive();
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x49584655);
            writer.WriteInt32(1);
            writer.WriteString(SourceIdentity);
            writer.WriteUInt64(m_RequestSequence);
            writer.WriteInt32(m_PendingRequests.Count);
            for (int i = 0; i < m_PendingRequests.Count; i++)
            {
                PendingRequest pending = m_PendingRequests[i];
                writer.WriteString(pending.RequestId);
                writer.WriteUInt64(pending.Sequence);
                writer.WriteUInt64(pending.CaptureRenderFrame);
                writer.WriteDouble(pending.BufferSeconds);
                writer.WriteInt32(pending.Priority);
                writer.WriteByte((byte)pending.TimingClass);
                writer.WriteUInt64(pending.CaptureTick);
                writer.WriteUInt64(pending.EligibleTick);
            }
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            RequireAlive();
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x49584655 || reader.ReadInt32() != 1 ||
                !string.Equals(reader.ReadString(), SourceIdentity, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unity Fixed Input Adapter state identity is invalid.");
            }
            ulong requestSequence = reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Unity Fixed Input Adapter state request count is invalid.");
            var pendingRequests = new PendingRequest[count];
            for (int i = 0; i < count; i++)
            {
                string requestId = reader.ReadString();
                ulong sequence = reader.ReadUInt64();
                ulong captureRenderFrame = reader.ReadUInt64();
                float bufferSeconds = checked((float)reader.ReadDouble());
                int priority = reader.ReadInt32();
                var timingClass = (CharacterActionRequestTimingClass)reader.ReadByte();
                ulong captureTick = reader.ReadUInt64();
                ulong eligibleTick = reader.ReadUInt64();
                if (!Enum.IsDefined(typeof(CharacterActionRequestTimingClass), timingClass) ||
                    captureTick == 0 != (eligibleTick == 0) || eligibleTick < captureTick)
                {
                    throw new InvalidDataException("Unity Fixed Input Adapter pending request state is invalid.");
                }
                var pending = new PendingRequest(
                    requestId,
                    sequence,
                    captureRenderFrame,
                    bufferSeconds,
                    priority,
                    timingClass);
                if (captureTick != 0)
                    pending.Schedule(captureTick, checked((int)(eligibleTick - captureTick)));
                pendingRequests[i] = pending;
            }
            reader.RequireComplete();
            m_RequestSequence = requestSequence;
            m_PendingRequests.Clear();
            m_PendingRequests.AddRange(pendingRequests);
        }

        public void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(FixedCharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        public FixedCharacterControlSourceDiagnosticsSnapshot CaptureDiagnostics()
        {
            int count = 0;
            ulong oldestCaptureTick = 0;
            ulong oldestEligibleTick = 0;
            for (int i = 0; i < m_PendingRequests.Count; i++)
            {
                PendingRequest pending = m_PendingRequests[i];
                if (pending.TimingClass != CharacterActionRequestTimingClass.Offensive)
                    continue;
                count++;
                if (pending.CaptureTick != 0 && (oldestCaptureTick == 0 || pending.CaptureTick < oldestCaptureTick))
                    oldestCaptureTick = pending.CaptureTick;
                if (pending.EligibleTick != 0 && (oldestEligibleTick == 0 || pending.EligibleTick < oldestEligibleTick))
                    oldestEligibleTick = pending.EligibleTick;
            }
            return new FixedCharacterControlSourceDiagnosticsSnapshot(count, oldestCaptureTick, oldestEligibleTick);
        }

        public bool TryGetLatchedVector2(string inputId, out Vector2 value)
        {
            value = Vector2.zero;
            if (string.IsNullOrEmpty(inputId) ||
                !m_LatchedValues.TryGetValue(inputId, out LatchedInputValue input) ||
                input.Kind != CharacterInputValueType.Vector2)
            {
                return false;
            }
            value = input.Vector2;
            return true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_Disposed = true;
            m_ValueBindings.Clear();
            m_RequestBindings.Clear();
            m_CameraRelativeVector2Ids.Clear();
            m_WorldVector2Ids.Clear();
            m_InputValues.Clear();
            m_InputRequests.Clear();
            m_ActionTargetInputIds.Clear();
        }

        void BuildBindings()
        {
            for (int i = 0; i < m_Profile.InputValues.Count; i++)
            {
                CharacterInputValueDefinition definition = m_Profile.InputValues[i];
                if (CameraProgramOperationSchema.IsCameraBasisInputId(definition.InputValueId))
                    throw new InvalidOperationException($"Input value '{definition.InputValueId}' is reserved for the Camera basis snapshot.");
                if (!definition.TryResolveAction(Actions, out InputAction action, out string error))
                    throw new InvalidOperationException($"Input value '{definition.InputValueId}' {error}");
                m_ValueBindings.Add(
                    definition.InputValueId,
                    new InputValueBinding(definition.InputValueId, definition.ValueType, action));
            }
            for (int i = 0; i < m_Profile.ActionRequests.Count; i++)
            {
                CharacterActionRequestDefinition definition = m_Profile.ActionRequests[i];
                if (!definition.TryResolveAction(Actions, out InputAction action, out string error))
                    throw new InvalidOperationException($"Input request '{definition.RequestId}' {error}");
                m_RequestBindings.Add(new RequestBinding(
                    definition.RequestId,
                    action,
                    definition.BufferSeconds,
                    definition.Priority,
                    definition.TimingClass));
            }
        }

        void BuildActionTargetInputs()
        {
            const string prefix = "input:value:";
            for (int entryIndex = 0; entryIndex < m_Program.CatalogEntries.Count; entryIndex++)
            {
                ProgramCatalogEntry entry = m_Program.CatalogEntries[entryIndex];
                if (entry.Kind != ProgramCatalogEntryKind.InputValue ||
                    !entry.Identity.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                ProgramCatalogField typeField = null;
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    if (string.Equals(entry.Fields[fieldIndex].Name, "ValueType", StringComparison.Ordinal))
                    {
                        typeField = entry.Fields[fieldIndex];
                        break;
                    }
                }
                if (typeField == null || typeField.Kind != ProgramCatalogFieldKind.Constant)
                    throw new InvalidOperationException($"Program input '{entry.Identity}' has no ValueType field.");
                ThirdPersonSimulation.Fixed.ProgramConstant constant = m_Program.Constants[typeField.ConstantIndex];
                if (constant.Kind != ThirdPersonSimulation.Fixed.ProgramConstantKind.Int32)
                    throw new InvalidOperationException($"Program input '{entry.Identity}' ValueType is not Int32.");
                if ((ProgramInputValueKind)constant.Int32 != ProgramInputValueKind.ActionTargetSnapshot)
                    continue;
                string inputId = entry.Identity.Substring(prefix.Length);
                if (m_ValueBindings.ContainsKey(inputId))
                    throw new InvalidOperationException($"Action target input '{inputId}' must not be bound to an InputAction.");
                m_ActionTargetInputIds.Add(inputId);
            }
            m_ActionTargetInputIds.Sort(StringComparer.Ordinal);
        }

        void ValidateActionTargetInput()
        {
            if (m_ActionTargetProvider == null)
                return;
            if (!m_ActionTargetInputIds.Contains(m_ActionTargetInputValueId))
                throw new InvalidOperationException($"Fixed Program does not declare Action target input '{m_ActionTargetInputValueId}'.");
        }

        ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot ResolveActionTarget(
            FixedCharacterInputBuildContext context)
        {
            if (m_ActionTargetProvider == null || !m_ActionTargetProvider.TryGetTargetActorId(m_Owner, out ActorId targetId))
                return ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot.None;
            if (context.CommittedObservation == null)
                throw new InvalidOperationException("Fixed Action target input requires a committed Actor observation port from its Session Source.");
            CommittedActorPose<FixedVector3, FixedYaw> target =
                context.CommittedObservation.GetRequiredActor(targetId);
            return new ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot(
                target.ActorId.Value,
                target.Position,
                target.Yaw);
        }

        void ValidateProgramInputs()
        {
            var requests = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_RequestBindings.Count; i++)
                requests.Add(m_RequestBindings[i].RequestId);
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                FixedSimulationOperation operation = m_Program.Operations[i];
                CharacterInputValueType expected;
                switch (operation.Code)
                {
                    case SimulationOperationCode.InputBoolean:
                        expected = CharacterInputValueType.Bool;
                        break;
                    case SimulationOperationCode.InputScalar:
                        expected = CharacterInputValueType.Float;
                        break;
                    case SimulationOperationCode.InputVector2:
                    case SimulationOperationCode.InputVector2Magnitude:
                        expected = CharacterInputValueType.Vector2;
                        break;
                    case SimulationOperationCode.InputRequest:
                        if (!requests.Contains(operation.Text0))
                            throw new InvalidOperationException($"Fixed Program input request '{operation.Text0}' has no CharacterInputProfile binding.");
                        continue;
                    default:
                        continue;
                }
                if (!m_ValueBindings.TryGetValue(operation.Text0, out InputValueBinding binding) || binding.Kind != expected)
                    throw new InvalidOperationException($"Fixed Program input '{operation.Text0}' has no matching CharacterInputProfile value binding.");
            }
        }

        void ResolveDirectionSpaces()
        {
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                FixedSimulationOperation operation = m_Program.Operations[i];
                if (operation.Code != SimulationOperationCode.LocomotionInputMotion &&
                    operation.Code != SimulationOperationCode.MoveFacingAngle)
                {
                    continue;
                }
                bool cameraRelative = operation.Code == SimulationOperationCode.MoveFacingAngle ||
                                      (operation.Flags & 1U) != 0;
                HashSet<string> targets = cameraRelative ? m_CameraRelativeVector2Ids : m_WorldVector2Ids;
                bool found = false;
                for (int edgeIndex = 0; edgeIndex < m_Program.ControlFlow.Count; edgeIndex++)
                {
                    ProgramControlFlowEdge edge = m_Program.ControlFlow[edgeIndex];
                    if (edge.Kind != ProgramControlFlowKind.Value || !edge.Target.Equals(operation.Handle))
                        continue;
                    FixedSimulationOperation source = m_Program.Operations[edge.Source.Value];
                    if (source.Code != SimulationOperationCode.InputVector2)
                        continue;
                    targets.Add(source.Text0);
                    found = true;
                }
                if (!found)
                    throw new InvalidOperationException($"Fixed Program operation '{operation.Definition.Identity}' must receive movement directly from InputVector2.");
            }
            foreach (string inputId in m_CameraRelativeVector2Ids)
            {
                if (m_WorldVector2Ids.Contains(inputId))
                    throw new InvalidOperationException($"Input '{inputId}' is used by camera-relative and world-relative locomotion operations.");
            }
        }

        LatchedInputValue ReadValue(InputValueBinding binding)
        {
            switch (binding.Kind)
            {
                case CharacterInputValueType.Bool:
                    return LatchedInputValue.FromBoolean(binding.Action.IsPressed());
                case CharacterInputValueType.Float:
                    return LatchedInputValue.FromScalar(binding.Action.ReadValue<float>());
                case CharacterInputValueType.Vector2:
                    return LatchedInputValue.FromVector2(binding.Action.ReadValue<Vector2>());
                default:
                    throw new InvalidOperationException($"Input value '{binding.InputId}' has unsupported type '{binding.Kind}'.");
            }
        }

        FixedSimulationInputValue ToSimulationValue(string inputId, LatchedInputValue value)
        {
            switch (value.Kind)
            {
                case CharacterInputValueType.Bool:
                    return FixedSimulationInputValue.FromBoolean(inputId, value.Boolean);
                case CharacterInputValueType.Float:
                    return FixedSimulationInputValue.FromScalar(inputId, FixedScalar.FromSingle(value.Scalar));
                case CharacterInputValueType.Vector2:
                    Vector2 vector = m_CameraRelativeVector2Ids.Contains(inputId)
                        ? ResolveCameraRelative(inputId, value.Vector2)
                        : value.Vector2;
                    if (vector.sqrMagnitude > 1f)
                        vector.Normalize();
                    return FixedSimulationInputValue.FromVector2(
                        inputId,
                        new FixedVector2(FixedScalar.FromSingle(vector.x), FixedScalar.FromSingle(vector.y)));
                default:
                    throw new InvalidOperationException($"Input value '{inputId}' has unsupported type '{value.Kind}'.");
            }
        }

        Vector2 ResolveCameraRelative(string inputId, Vector2 input)
        {
            if (input.sqrMagnitude > 1f)
                input.Normalize();
            if (input.sqrMagnitude <= 0.000001f)
                return Vector2.zero;
            CameraBasisSnapshot basis = m_LatchedCameraBasis;
            if (!basis.Valid)
                throw new InvalidOperationException($"Camera-relative input '{inputId}' requires a valid camera basis snapshot.");
            Vector3 forward = basis.PlanarForward;
            Vector3 right = basis.PlanarRight;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude <= 0.000001f || right.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException($"Camera-relative input '{inputId}' received a degenerate camera basis.");
            Vector3 direction = right.normalized * input.x + forward.normalized * input.y;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();
            return new Vector2(direction.x, direction.z);
        }

        static bool RequiresCameraBasis(ThirdPersonSimulation.Fixed.CharacterSimulationProgram program)
        {
            for (int i = 0; i < program.Operations.Count; i++)
            {
                if (CameraProgramOperationSchema.IsCameraBasisOperation(program.Operations[i].Code))
                    return true;
            }
            return false;
        }

        static void AppendCameraBasis(List<FixedSimulationInputValue> values, CameraBasisSnapshot basis)
        {
            values.Add(FixedSimulationInputValue.FromBoolean(CameraProgramOperationSchema.BasisValidInputId, basis.Valid));
            values.Add(FixedSimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisPlanarForwardInputId,
                ToSimulationVector3(basis.PlanarForward)));
            values.Add(FixedSimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisPlanarRightInputId,
                ToSimulationVector3(basis.PlanarRight)));
            values.Add(FixedSimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisLookDirectionInputId,
                ToSimulationVector3(basis.LookDirection)));
            values.Add(FixedSimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisAimPointInputId,
                ToSimulationVector3(basis.AimPoint)));
            values.Add(FixedSimulationInputValue.FromYaw(
                CameraProgramOperationSchema.BasisYawInputId,
                new FixedYaw(FixedScalar.FromSingle(basis.Yaw))));
            values.Add(FixedSimulationInputValue.FromScalar(
                CameraProgramOperationSchema.BasisPitchInputId,
                FixedScalar.FromSingle(basis.Pitch)));
        }

        static FixedVector3 ToSimulationVector3(Vector3 value)
        {
            return new FixedVector3(
                FixedScalar.FromSingle(value.x),
                FixedScalar.FromSingle(value.y),
                FixedScalar.FromSingle(value.z));
        }

        ulong NextRequestSequence()
        {
            m_RequestSequence++;
            if (m_RequestSequence == 0)
                throw new OverflowException("Unity Fixed Input Adapter request sequence overflowed.");
            return m_RequestSequence;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(UnityFixedCharacterInputAdapter));
        }

        readonly struct InputValueBinding
        {
            public InputValueBinding(string inputId, CharacterInputValueType kind, InputAction action)
            {
                InputId = inputId;
                Kind = kind;
                Action = action;
            }

            public string InputId { get; }
            public CharacterInputValueType Kind { get; }
            public InputAction Action { get; }
        }

        readonly struct RequestBinding
        {
            public RequestBinding(
                string requestId,
                InputAction action,
                float bufferSeconds,
                int priority,
                CharacterActionRequestTimingClass timingClass)
            {
                RequestId = requestId;
                Action = action;
                BufferSeconds = bufferSeconds;
                Priority = priority;
                TimingClass = timingClass;
            }

            public string RequestId { get; }
            public InputAction Action { get; }
            public float BufferSeconds { get; }
            public int Priority { get; }
            public CharacterActionRequestTimingClass TimingClass { get; }
        }

        sealed class PendingRequest
        {
            public PendingRequest(
                string requestId,
                ulong sequence,
                ulong captureRenderFrame,
                float bufferSeconds,
                int priority,
                CharacterActionRequestTimingClass timingClass)
            {
                RequestId = requestId;
                Sequence = sequence;
                CaptureRenderFrame = captureRenderFrame;
                BufferSeconds = bufferSeconds;
                Priority = priority;
                TimingClass = timingClass;
            }

            public string RequestId { get; }
            public ulong Sequence { get; }
            public ulong CaptureRenderFrame { get; }
            public float BufferSeconds { get; }
            public int Priority { get; }
            public CharacterActionRequestTimingClass TimingClass { get; }
            public ulong CaptureTick { get; private set; }
            public ulong EligibleTick { get; private set; }

            public void Schedule(ulong captureTick, int delayTicks)
            {
                if (CaptureTick != 0 || captureTick == 0 || delayTicks < 0)
                    throw new InvalidOperationException("Unity Fixed Input Adapter request schedule is invalid.");
                CaptureTick = captureTick;
                EligibleTick = checked(captureTick + (ulong)delayTicks);
            }

        }

        readonly struct LatchedInputValue
        {
            LatchedInputValue(CharacterInputValueType kind, bool boolean, float scalar, Vector2 vector2)
            {
                Kind = kind;
                Boolean = boolean;
                Scalar = scalar;
                Vector2 = vector2;
            }

            public CharacterInputValueType Kind { get; }
            public bool Boolean { get; }
            public float Scalar { get; }
            public Vector2 Vector2 { get; }
            public static LatchedInputValue FromBoolean(bool value) =>
                new LatchedInputValue(CharacterInputValueType.Bool, value, 0f, Vector2.zero);
            public static LatchedInputValue FromScalar(float value) =>
                new LatchedInputValue(CharacterInputValueType.Float, false, value, Vector2.zero);
            public static LatchedInputValue FromVector2(Vector2 value) =>
                new LatchedInputValue(CharacterInputValueType.Vector2, false, 0f, value);
        }
    }
}
