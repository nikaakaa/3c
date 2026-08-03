using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.InputSystem;
using Float32CommittedActorPose = ThirdPersonSimulation.CommittedActorPose<ThirdPersonSimulation.Float32Vector3, ThirdPersonSimulation.Float32Yaw>;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class UnityCharacterSimulationInputAdapter :
        IUnityCharacterControlSourceRuntime,
        ICharacterPresentationLookInput,
        ICharacterControlSourceRosterRuntime
    {
        readonly CharacterInputProfile m_Profile;
        readonly CharacterSimulationProgram m_Program;
        readonly ThirdPersonCameraController m_CameraRig;
        readonly CharacterPipelineHost m_Owner;
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
        readonly List<SimulationInputValue> m_InputValues = new List<SimulationInputValue>();
        readonly List<SimulationInputRequest> m_InputRequests = new List<SimulationInputRequest>();
        readonly bool m_RequiresCameraBasis;
        ulong m_RenderFrame;
        ulong m_RequestSequence;
        CameraBasisSnapshot m_LatchedCameraBasis;
        bool m_Active;
        bool m_Disposed;

        public UnityCharacterSimulationInputAdapter(
            CharacterInputProfile profile,
            CharacterSimulationProgram program,
            ThirdPersonCameraController cameraRig,
            CharacterPipelineHost owner,
            string actionTargetInputValueId,
            ICharacterActionTargetInputProvider actionTargetProvider)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_CameraRig = cameraRig ? cameraRig : throw new ArgumentNullException(nameof(cameraRig));
            m_Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            m_ActionTargetInputValueId = RequireIdentity(actionTargetInputValueId, nameof(actionTargetInputValueId));
            m_ActionTargetProvider = actionTargetProvider;
            if (program.Manifest.NumericProfile != Float32SimulationNumericProfile.Value)
                throw new ArgumentException("Unity Input Adapter requires a Float32 Program.", nameof(program));
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            m_RequiresCameraBasis = RequiresCameraBasis(program);
            BuildBindings();
            RequireActionTargetInput();
            ValidateProgramInputs();
            ResolveDirectionSpaces();
        }

        public string SourceIdentity =>
            $"UnityInputSystem/Float32/{m_ActionTargetInputValueId}/{(m_ActionTargetProvider == null ? "none" : m_ActionTargetProvider.ProviderIdentity)}";
        public SimulationNumericProfile NumericProfile => Float32SimulationNumericProfile.Value;
        public ProgramId CharacterProgramId => m_Program.Manifest.ProgramId;
        public ProgramHash CharacterProgramHash => m_Program.ProgramHash;
        public CharacterControlSourceCapability Capabilities => m_ActionTargetProvider == null
            ? CharacterControlSourceCapability.None
            : CharacterControlSourceCapability.CommittedObservation;
        public InputActionAsset Actions => m_Profile.SourceAsset;

        public void Activate()
        {
            RequireAlive();
            if (m_Active)
                return;
            Actions.Enable();
            foreach (InputValueBinding binding in m_ValueBindings.Values)
                binding.ConflictResolver?.Activate();
            m_Active = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
            foreach (InputValueBinding binding in m_ValueBindings.Values)
                binding.ConflictResolver?.Deactivate();
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
                throw new InvalidOperationException("Unity Input Adapter must be active before render-frame capture.");
            if (renderFrame == 0 || renderFrame <= m_RenderFrame)
                throw new InvalidOperationException("Unity Input Adapter requires a strictly increasing render frame.");
            m_RenderFrame = renderFrame;
            m_LatchedValues.Clear();
            foreach (KeyValuePair<string, InputValueBinding> pair in m_ValueBindings)
                m_LatchedValues.Add(pair.Key, ReadValue(pair.Value));
            if (m_RequiresCameraBasis)
                m_LatchedCameraBasis = m_CameraRig.BasisSnapshot;
            for (int i = 0; i < m_RequestBindings.Count; i++)
            {
                RequestBinding binding = m_RequestBindings[i];
                if (binding.Action.WasPressedThisFrame() || binding.Action.triggered)
                {
                    m_PendingRequests.Add(new PendingRequest(
                        binding.RequestId,
                        NextRequestSequence(),
                        binding.BufferSeconds,
                        binding.Priority));
                }
            }
        }

        public CharacterSimulationInput BuildInput(SimulationInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0)
                throw new InvalidOperationException("Unity Input Adapter has no captured render frame.");
            if (context.NumericProfile != NumericProfile || context.ActorId == default)
                throw new InvalidOperationException("Unity Input Adapter received an incompatible input build context.");

            m_InputValues.Clear();
            foreach (KeyValuePair<string, LatchedInputValue> pair in m_LatchedValues)
                m_InputValues.Add(ToSimulationValue(pair.Key, pair.Value));
            if (m_RequiresCameraBasis)
                AppendCameraBasis(m_InputValues, m_LatchedCameraBasis);
            m_InputValues.Add(SimulationInputValue.FromActionTargetSnapshot(
                m_ActionTargetInputValueId,
                ResolveActionTarget(context)));

            m_InputRequests.Clear();
            for (int i = 0; i < m_PendingRequests.Count; i++)
            {
                PendingRequest pending = m_PendingRequests[i];
                ulong duration = pending.BufferSeconds <= 0f
                    ? 0UL
                    : (ulong)Math.Max(1, Mathf.CeilToInt(pending.BufferSeconds * context.TickRate));
                m_InputRequests.Add(new SimulationInputRequest(
                    pending.RequestId,
                    pending.Sequence,
                    context.Source.SourceTick,
                    checked(context.SimulationTick.Value + duration),
                    pending.Priority));
            }
            m_PendingRequests.Clear();
            return new CharacterSimulationInput(
                NumericProfile,
                context.Source,
                SourceIdentity,
                context.InputSequence,
                m_InputValues,
                m_InputRequests);
        }

        public void ValidateRoster(
            ActorId actorId,
            IReadOnlyList<ActorId> roster,
            StableHash committedObservationCapability)
        {
            if (actorId != new ActorId(m_Owner.ActorId) || roster == null ||
                !committedObservationCapability.Equals(CommittedActorObservationSchema.CapabilityHash))
            {
                throw new InvalidOperationException("Unity player Control Source roster or committed observation capability is incompatible.");
            }
            if (m_ActionTargetProvider == null || !m_ActionTargetProvider.TryGetTargetActorId(m_Owner, out ActorId targetId))
                return;
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == targetId)
                    return;
            }
            throw new InvalidOperationException($"Unity player target Actor '{targetId}' is absent from its locked roster.");
        }

        public bool TryGetLatchedVector2(string inputId, out Vector2 value)
        {
            value = Vector2.zero;
            if (string.IsNullOrEmpty(inputId) ||
                !m_LatchedValues.TryGetValue(inputId, out LatchedInputValue input) ||
                input.Kind != CharacterInputValueType.Vector2)
                return false;
            value = input.Vector2;
            return true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_Disposed = true;
            foreach (InputValueBinding binding in m_ValueBindings.Values)
                binding.ConflictResolver?.Dispose();
            m_ValueBindings.Clear();
            m_RequestBindings.Clear();
            m_CameraRelativeVector2Ids.Clear();
            m_WorldVector2Ids.Clear();
            m_InputValues.Clear();
            m_InputRequests.Clear();
        }

        static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Input identity must be non-empty and trimmed.", parameterName);
            return value;
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
                    new InputValueBinding(
                        definition.InputValueId,
                        definition.ValueType,
                        action,
                        CreateConflictResolver(definition, action)));
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
                    definition.Priority));
            }
        }

        void RequireActionTargetInput()
        {
            if (m_ValueBindings.ContainsKey(m_ActionTargetInputValueId))
                throw new InvalidOperationException($"Action target input '{m_ActionTargetInputValueId}' must be supplied by the target provider, not CharacterInputProfile.");
            string identity = $"input:value:{m_ActionTargetInputValueId}";
            for (int i = 0; i < m_Program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = m_Program.CatalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.InputValue || !string.Equals(entry.Identity, identity, StringComparison.Ordinal))
                    continue;
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    ProgramCatalogField field = entry.Fields[fieldIndex];
                    if (!string.Equals(field.Name, "ValueType", StringComparison.Ordinal) || field.Kind != ProgramCatalogFieldKind.Constant)
                        continue;
                    ProgramConstant constant = m_Program.Constants[field.ConstantIndex];
                    if (constant.Kind == ProgramConstantKind.Int32 &&
                        constant.Int32 == (int)ProgramInputValueKind.ActionTargetSnapshot)
                        return;
                }
                throw new InvalidOperationException($"Action target input '{m_ActionTargetInputValueId}' has an incompatible Program value kind.");
            }
            throw new InvalidOperationException($"Program does not declare Action target input '{m_ActionTargetInputValueId}'.");
        }

        void ValidateProgramInputs()
        {
            var requests = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_RequestBindings.Count; i++)
                requests.Add(m_RequestBindings[i].RequestId);
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                SimulationOperation operation = m_Program.Operations[i];
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
                            throw new InvalidOperationException($"Program input request '{operation.Text0}' has no CharacterInputProfile binding.");
                        continue;
                    default:
                        continue;
                }
                if (!m_ValueBindings.TryGetValue(operation.Text0, out InputValueBinding binding) || binding.Kind != expected)
                    throw new InvalidOperationException($"Program input '{operation.Text0}' has no matching CharacterInputProfile value binding.");
            }
        }

        void ResolveDirectionSpaces()
        {
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                SimulationOperation operation = m_Program.Operations[i];
                if (operation.Code != SimulationOperationCode.LocomotionInputMotion &&
                    operation.Code != SimulationOperationCode.MoveFacingAngle)
                    continue;
                bool cameraRelative = operation.Code == SimulationOperationCode.MoveFacingAngle || (operation.Flags & 1U) != 0;
                HashSet<string> targets = cameraRelative ? m_CameraRelativeVector2Ids : m_WorldVector2Ids;
                bool found = false;
                for (int edgeIndex = 0; edgeIndex < m_Program.ControlFlow.Count; edgeIndex++)
                {
                    ProgramControlFlowEdge edge = m_Program.ControlFlow[edgeIndex];
                    if (edge.Kind != ProgramControlFlowKind.Value || !edge.Target.Equals(operation.Handle))
                        continue;
                    SimulationOperation source = m_Program.Operations[edge.Source.Value];
                    if (source.Code != SimulationOperationCode.InputVector2)
                        continue;
                    targets.Add(source.Text0);
                    found = true;
                }
                if (!found)
                    throw new InvalidOperationException($"Program operation '{operation.Definition.Identity}' must receive its movement Vector2 directly from an InputVector2 operation.");
            }
            foreach (string inputId in m_CameraRelativeVector2Ids)
            {
                if (m_WorldVector2Ids.Contains(inputId))
                    throw new InvalidOperationException($"Input '{inputId}' is used by both camera-relative and world-relative locomotion operations.");
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
                    Vector2 value = binding.Action.ReadValue<Vector2>();
                    return LatchedInputValue.FromVector2(binding.ConflictResolver?.Resolve(value) ?? value);
                default:
                    throw new InvalidOperationException($"Input value '{binding.InputId}' has unsupported type '{binding.Kind}'.");
            }
        }

        SimulationInputValue ToSimulationValue(string inputId, LatchedInputValue value)
        {
            switch (value.Kind)
            {
                case CharacterInputValueType.Bool:
                    return SimulationInputValue.FromBoolean(inputId, value.Boolean);
                case CharacterInputValueType.Float:
                    return SimulationInputValue.FromScalar(
                        inputId,
                        Float32ScalarBoundary.ConvertExternal(value.Scalar, $"input:{inputId}"));
                case CharacterInputValueType.Vector2:
                    Vector2 vector = m_CameraRelativeVector2Ids.Contains(inputId)
                        ? ResolveCameraRelative(inputId, value.Vector2)
                        : value.Vector2;
                    if (vector.sqrMagnitude > 1f)
                        vector.Normalize();
                    return SimulationInputValue.FromVector2(
                        inputId,
                        new Float32Vector2(
                            Float32ScalarBoundary.ConvertExternal(vector.x, $"input:{inputId}/x"),
                            Float32ScalarBoundary.ConvertExternal(vector.y, $"input:{inputId}/y")));
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
            CameraBasisSnapshot basis = m_CameraRig.BasisSnapshot;
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

        static bool RequiresCameraBasis(CharacterSimulationProgram program)
        {
            for (int i = 0; i < program.Operations.Count; i++)
            {
                if (CameraProgramOperationSchema.IsCameraBasisOperation(program.Operations[i].Code))
                    return true;
            }
            return false;
        }

        static void AppendCameraBasis(List<SimulationInputValue> values, CameraBasisSnapshot basis)
        {
            values.Add(SimulationInputValue.FromBoolean(CameraProgramOperationSchema.BasisValidInputId, basis.Valid));
            values.Add(SimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisPlanarForwardInputId,
                ToSimulationVector3(basis.PlanarForward, CameraProgramOperationSchema.BasisPlanarForwardInputId)));
            values.Add(SimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisPlanarRightInputId,
                ToSimulationVector3(basis.PlanarRight, CameraProgramOperationSchema.BasisPlanarRightInputId)));
            values.Add(SimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisLookDirectionInputId,
                ToSimulationVector3(basis.LookDirection, CameraProgramOperationSchema.BasisLookDirectionInputId)));
            values.Add(SimulationInputValue.FromVector3(
                CameraProgramOperationSchema.BasisAimPointInputId,
                ToSimulationVector3(basis.AimPoint, CameraProgramOperationSchema.BasisAimPointInputId)));
            values.Add(SimulationInputValue.FromYaw(
                CameraProgramOperationSchema.BasisYawInputId,
                new Float32Yaw(Float32ScalarBoundary.ConvertExternal(basis.Yaw, CameraProgramOperationSchema.BasisYawInputId))));
            values.Add(SimulationInputValue.FromScalar(
                CameraProgramOperationSchema.BasisPitchInputId,
                Float32ScalarBoundary.ConvertExternal(basis.Pitch, CameraProgramOperationSchema.BasisPitchInputId)));
        }

        static Float32Vector3 ToSimulationVector3(Vector3 value, string inputId)
        {
            return new Float32Vector3(
                Float32ScalarBoundary.ConvertExternal(value.x, $"{inputId}/x"),
                Float32ScalarBoundary.ConvertExternal(value.y, $"{inputId}/y"),
                Float32ScalarBoundary.ConvertExternal(value.z, $"{inputId}/z"));
        }

        SimulationActionTargetSnapshot ResolveActionTarget(SimulationInputBuildContext context)
        {
            if (m_ActionTargetProvider == null || !m_ActionTargetProvider.TryGetTargetActorId(m_Owner, out ActorId targetId))
                return SimulationActionTargetSnapshot.None;
            Float32CommittedActorPose target = context.CommittedObservation.GetRequiredActor(targetId);
            return new SimulationActionTargetSnapshot(
                targetId.Value,
                target.Position,
                target.Yaw);
        }

        ulong NextRequestSequence()
        {
            m_RequestSequence++;
            if (m_RequestSequence == 0)
                throw new OverflowException("Unity Input Adapter request sequence overflowed.");
            return m_RequestSequence;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(UnityCharacterSimulationInputAdapter));
        }

        static CharacterDirectionalInputConflictResolver CreateConflictResolver(
            CharacterInputValueDefinition definition,
            InputAction action)
        {
            return definition.Vector2ConflictPolicy == CharacterVector2ConflictPolicy.LatestActuatedCardinal
                ? new CharacterDirectionalInputConflictResolver(action)
                : null;
        }

        readonly struct InputValueBinding
        {
            public InputValueBinding(
                string inputId,
                CharacterInputValueType kind,
                InputAction action,
                CharacterDirectionalInputConflictResolver conflictResolver)
            {
                InputId = inputId;
                Kind = kind;
                Action = action;
                ConflictResolver = conflictResolver;
            }

            public string InputId { get; }
            public CharacterInputValueType Kind { get; }
            public InputAction Action { get; }
            public CharacterDirectionalInputConflictResolver ConflictResolver { get; }
        }

        readonly struct RequestBinding
        {
            public RequestBinding(string requestId, InputAction action, float bufferSeconds, int priority)
            {
                RequestId = requestId;
                Action = action;
                BufferSeconds = bufferSeconds;
                Priority = priority;
            }

            public string RequestId { get; }
            public InputAction Action { get; }
            public float BufferSeconds { get; }
            public int Priority { get; }
        }

        readonly struct PendingRequest
        {
            public PendingRequest(string requestId, ulong sequence, float bufferSeconds, int priority)
            {
                RequestId = requestId;
                Sequence = sequence;
                BufferSeconds = bufferSeconds;
                Priority = priority;
            }

            public string RequestId { get; }
            public ulong Sequence { get; }
            public float BufferSeconds { get; }
            public int Priority { get; }
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
            public static LatchedInputValue FromBoolean(bool value) => new LatchedInputValue(CharacterInputValueType.Bool, value, 0f, Vector2.zero);
            public static LatchedInputValue FromScalar(float value) => new LatchedInputValue(CharacterInputValueType.Float, false, value, Vector2.zero);
            public static LatchedInputValue FromVector2(Vector2 value) => new LatchedInputValue(CharacterInputValueType.Vector2, false, 0f, value);
        }
    }
}
