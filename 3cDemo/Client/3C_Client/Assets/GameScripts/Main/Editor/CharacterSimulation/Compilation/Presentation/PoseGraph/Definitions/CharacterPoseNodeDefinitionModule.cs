using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum CharacterPoseGraphDependencyKind : byte
    {
        StatePose = 1,
        Subgraph = 2,
        MotionMatchingEntry = 3,
        LinkedPoseEntry = 4
    }

    internal readonly struct CharacterPoseGraphDependency
    {
        public CharacterPoseGraphDependency(
            CharacterPoseGraphDependencyKind kind,
            PoseGraphId graphId,
            string ownerIdentity)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseGraphDependencyKind), kind) ||
                string.IsNullOrWhiteSpace(ownerIdentity))
            {
                throw new ArgumentException("Pose Graph dependency is invalid.");
            }
            Kind = kind;
            GraphId = graphId;
            OwnerIdentity = ownerIdentity.Trim();
        }

        public CharacterPoseGraphDependencyKind Kind { get; }
        public PoseGraphId GraphId { get; }
        public string OwnerIdentity { get; }
    }

    internal enum CharacterPoseCanvasCreationKind : byte
    {
        Direct = 1,
        DedicatedSurface = 2,
        BlackboardOnly = 3
    }

    internal abstract class CharacterPoseNodeDefinition
    {
        GraphAuthoringCapabilityDescriptor m_Capability;

        public abstract CharacterPoseNodeKind Kind { get; }
        public abstract Type PayloadType { get; }
        public abstract GraphAuthoringCapabilityDescriptor Declare();
        public CharacterPoseCanvasCreationKind CanvasCreation =>
            Kind == CharacterPoseNodeKind.LinkedPoseCall
                ? CharacterPoseCanvasCreationKind.DedicatedSurface
                : Kind == CharacterPoseNodeKind.ProgramParameterInput
                    ? CharacterPoseCanvasCreationKind.BlackboardOnly
                : CharacterPoseCanvasCreationKind.Direct;
        public bool Copyable =>
            Kind != CharacterPoseNodeKind.GraphInput &&
            Kind != CharacterPoseNodeKind.GraphOutput &&
            Kind != CharacterPoseNodeKind.OutputPose &&
            Kind != CharacterPoseNodeKind.PoseStateMachine;
        public bool UsesPoseSourceSlot =>
            Kind == CharacterPoseNodeKind.SelectedPosePlayer ||
            Kind == CharacterPoseNodeKind.BlendSpacePlayer ||
            Kind == CharacterPoseNodeKind.ClipPlayer ||
            Kind == CharacterPoseNodeKind.BlendStack;
        public GraphAuthoringCapabilityDescriptor Capability =>
            m_Capability ?? throw new InvalidOperationException(
                $"Pose Node Definition '{Kind}' has no capability projection.");
        public string CapabilityIdentity => Capability.CapabilityId.Value;
        public CharacterPoseExecutionDomain ExecutionDomain
        {
            get
            {
                if (!Enum.TryParse(
                        Capability.ExecutionDomainId,
                        false,
                        out CharacterPoseExecutionDomain value) ||
                    !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), value))
                {
                    throw new InvalidOperationException(
                        $"Pose Node Definition '{Kind}' has invalid execution domain '{Capability.ExecutionDomainId}'.");
                }
                return value;
            }
        }

        internal void BindCapability(
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (m_Capability != null)
                throw new InvalidOperationException(
                    $"Pose Node Definition '{Kind}' capability is already bound.");
            if (capability == null ||
                capability.AuthoringType != PayloadType ||
                !string.Equals(
                    capability.ExternalKind,
                    CharacterPoseGraphAuthoringCapabilities.Get(Kind).Value,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pose Node Definition '{Kind}' capability projection does not match payload '{PayloadType.FullName}'.");
            }
            m_Capability = capability;
        }

        public abstract CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input);

        public CharacterPoseNodePayload CreateDefaultPayload() =>
            (CharacterPoseNodePayload)Activator.CreateInstance(PayloadType);

        public CharacterPoseNodePayload MutatePayload(
            CharacterPoseNodePayload payload,
            IReadOnlyDictionary<string, object> fields)
        {
            RequirePayload(payload);
            if (fields == null || fields.Count == 0)
                throw new ArgumentException(
                    "Pose payload field mutation set is empty.",
                    nameof(fields));
            foreach (string fieldId in fields.Keys)
            {
                if (!Capability.TryGetField(
                        new GraphAuthoringFieldId(fieldId),
                        out GraphAuthoringFieldDescriptor field) ||
                    !field.AuthoringWritable)
                {
                    throw new InvalidOperationException(
                        $"Pose capability '{Capability.CapabilityId}' does not declare writable field '{fieldId}'.");
                }
            }
            return CreatePayload(new CharacterPoseAuthoringPayloadInput(
                (fieldId, expectedType) =>
                {
                    object value = fields.TryGetValue(fieldId, out object replacement)
                        ? replacement
                        : ReadField(payload, fieldId);
                    return ConvertFieldValue(fieldId, value, expectedType);
                },
                payload is CharacterPoseStateMachineNodePayload stateMachine
                    ? new Func<CharacterPoseStateMachineDefinition>(() =>
                        stateMachine.StateMachine)
                    : null));
        }

        public abstract object ReadField(
            CharacterPoseNodePayload payload,
            string field);

        public abstract void RequirePayload(
            CharacterPoseNodePayload payload);

        public abstract void ValidatePayload(
            CharacterPoseNodePayload payload,
            string sourcePath);

        public abstract void ValidateRig(
            CharacterPoseNodePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath);

        public abstract CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload);

        public abstract AnimationChannelId Channel(
            CharacterPoseNodePayload payload);

        public abstract PoseParameterId Parameter(
            CharacterPoseNodePayload payload);

        public abstract AnimationSelectionAvailabilityPolicy Availability(
            CharacterPoseNodePayload payload,
            bool stateLocal);

        public abstract CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload);

        public abstract float Weight(CharacterPoseNodePayload payload);

        public abstract CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseNodePayload payload);

        public abstract IReadOnlyList<CharacterPoseParameterPolicy>
            ParameterPolicies(CharacterPoseNodePayload payload);

        public virtual IReadOnlyList<CharacterPoseResourceSlot>
            ProjectResourceSlots(CharacterPoseNodePayload payload)
        {
            RequirePayload(payload);
            return Capability.Fields
                .Where(field =>
                    field.ObjectType != null &&
                    typeof(CharacterPoseResourceSlot).IsAssignableFrom(
                        field.ObjectType))
                .Select(field => ReadField(payload, field.FieldId.Value))
                .OfType<CharacterPoseResourceSlot>()
                .Distinct()
                .ToArray();
        }

        public abstract IReadOnlyList<CharacterPoseGraphDependency>
            ProjectGraphDependencies(CharacterPoseNodePayload payload);

        public abstract string ProjectChildDocumentId(
            CharacterPoseNodePayload payload);

        public abstract string SourceMapName(
            CharacterPoseNodePayload payload);

        public IReadOnlyList<GraphAuthoringTypedPropertyValue>
            ProjectTypedProperties(CharacterPoseNodePayload payload)
        {
            RequirePayload(payload);
            return Capability.PortVariants
                .Select(value => value.When.FieldId)
                .Distinct()
                .Select(fieldId =>
                {
                    GraphAuthoringFieldDescriptor field = Capability.Fields
                        .Single(value => value.FieldId.Equals(fieldId));
                    return new GraphAuthoringTypedPropertyValue(
                        fieldId,
                        field.ValueKind,
                        Canonical(ReadField(payload, fieldId.Value)));
                })
                .ToArray();
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectPortShape(CharacterPoseCanvasNode node)
        {
            if (node == null || node.Kind != Kind)
                throw new ArgumentException(
                    "Pose node does not match Node Definition.",
                    nameof(node));
            return ProjectDeclaredPortShape(node.Payload)
                .Concat(node.DynamicPorts.Select(ProjectDynamicPort)).ToArray();
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectAdditionalPorts(CharacterPoseCanvasNode node)
        {
            if (node == null || node.Kind != Kind)
                throw new ArgumentException(
                    "Pose node does not match Node Definition.",
                    nameof(node));
            return GraphAuthoringNodePortShapeProjector.Project(
                Capability,
                ProjectTypedProperties(node.Payload),
                node.DynamicPorts.Select(ProjectDynamicPort).ToArray());
        }

        public virtual IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectDeclaredPortShape(CharacterPoseNodePayload payload) =>
            GraphAuthoringNodePortShapeProjector.ProjectComplete(
                Capability,
                ProjectTypedProperties(payload));

        static GraphAuthoringDynamicPortProjection ProjectDynamicPort(
            CharacterPoseDynamicPort port)
        {
            if (port == null)
                throw new InvalidOperationException("Pose dynamic port is missing.");
            return new GraphAuthoringDynamicPortProjection(
                new GraphAuthoringPortId(port.PortId.Value),
                port.DisplayName,
                CharacterPoseAuthoringPortProjection.ValueType(port.Kind),
                port.Direction == CharacterPosePortDirection.Input
                    ? GraphAuthoringPortDirection.Input
                    : GraphAuthoringPortDirection.Output,
                port.Direction == CharacterPosePortDirection.Input
                    ? GraphAuthoringPortCapacity.Single
                    : GraphAuthoringPortCapacity.Multiple,
                port.Required,
                port.Order,
                port.InterfacePortId.Value);
        }

        static string Canonical(object value)
        {
            if (value == null)
                return string.Empty;
            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }

        static object ConvertFieldValue(
            string fieldId,
            object value,
            Type expectedType)
        {
            if (value == null || expectedType.IsInstanceOfType(value))
                return value;
            try
            {
                if (expectedType == typeof(string))
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                if (expectedType.IsEnum)
                    return Enum.Parse(expectedType, value.ToString(), false);
                if (typeof(IConvertible).IsAssignableFrom(expectedType) &&
                    value is IConvertible)
                {
                    return Convert.ChangeType(
                        value,
                        expectedType,
                        CultureInfo.InvariantCulture);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Pose field '{fieldId}' cannot convert to '{expectedType.Name}'.",
                    exception);
            }
            throw new InvalidOperationException(
                $"Pose field '{fieldId}' requires '{expectedType.Name}'.");
        }

    }

    internal sealed class CharacterPoseNodeDefinitionModule
    {
        readonly Dictionary<CharacterPoseNodeKind, CharacterPoseNodeDefinition>
            m_Definitions;
        readonly Dictionary<Type, CharacterPoseNodeDefinition>
            m_ByPayloadType;
        readonly Dictionary<string, CharacterPoseNodeDefinition>
            m_ByCapability = new Dictionary<string, CharacterPoseNodeDefinition>(
                StringComparer.Ordinal);
        bool m_CapabilitiesBound;

        public static CharacterPoseNodeDefinitionModule Shared { get; } =
            new CharacterPoseNodeDefinitionModule();

        CharacterPoseNodeDefinitionModule()
        {
            CharacterPoseNodeDefinition[] definitions =
            {
                new CharacterProgramParameterInputPoseNodeDefinition(),
                new CharacterActionPlaybackInputPoseNodeDefinition(),
                new CharacterSelectedPosePlayerNodeDefinition(),
                new CharacterBlendSpacePlayerPoseNodeDefinition(),
                new CharacterClipPlayerPoseNodeDefinition(),
                new CharacterPoseStateMachineNodeDefinition(),
                new CharacterAnimationSlotPoseNodeDefinition(),
                new CharacterBlendStackPoseNodeDefinition(),
                new CharacterInertializationPoseNodeDefinition(),
                new CharacterBlendPoseNodeDefinition(),
                new CharacterLayeredBoneBlendPoseNodeDefinition(),
                new CharacterAdditivePoseNodeDefinition(),
                new CharacterPoseParameterResolveNodeDefinition(),
                new CharacterModifyBonePoseNodeDefinition(),
                new CharacterRootOrientationWarpPoseNodeDefinition(),
                new CharacterPoseSubgraphNodeDefinition(),
                new CharacterLocalToComponentPoseNodeDefinition(),
                new CharacterComponentToLocalPoseNodeDefinition(),
                new CharacterGraphInputPoseNodeDefinition(),
                new CharacterGraphOutputPoseNodeDefinition(),
                new CharacterOutputPoseNodeDefinition(),
                new CharacterFootPlacementPoseNodeDefinition(),
                new CharacterPoseBoneIkGoalsNodeDefinition(),
                new CharacterFullBodyIkPoseNodeDefinition(),
                new CharacterFullBodyIkGoalAssemblerNodeDefinition(),
                new CharacterLinkedPoseCallNodeDefinition(),
                new CharacterMotionMatchingPoseNodeDefinition(),
                new CharacterPoseHistoryCollectorNodeDefinition(),
                new CharacterEntryPoseInputNodeDefinition()
            };
            m_Definitions = new Dictionary<CharacterPoseNodeKind,
                CharacterPoseNodeDefinition>();
            m_ByPayloadType = new Dictionary<Type,
                CharacterPoseNodeDefinition>();
            foreach (CharacterPoseNodeDefinition definition in definitions)
            {
                if (m_Definitions.TryGetValue(
                        definition.Kind,
                        out CharacterPoseNodeDefinition duplicateKind))
                {
                    throw new InvalidOperationException(
                        $"Pose Node Kind '{definition.Kind}' is declared by both '{duplicateKind.PayloadType.FullName}' and '{definition.PayloadType.FullName}'.");
                }
                if (m_ByPayloadType.TryGetValue(
                        definition.PayloadType,
                        out CharacterPoseNodeDefinition duplicatePayload))
                {
                    throw new InvalidOperationException(
                        $"Pose payload '{definition.PayloadType.FullName}' is declared by both '{duplicatePayload.Kind}' and '{definition.Kind}'.");
                }
                m_Definitions.Add(definition.Kind, definition);
                m_ByPayloadType.Add(
                    definition.PayloadType,
                    definition);
            }
            CharacterPoseNodeKind[] formalKinds = Enum
                .GetValues(typeof(CharacterPoseNodeKind))
                .Cast<CharacterPoseNodeKind>()
                .ToArray();
            if (m_Definitions.Count != formalKinds.Length ||
                formalKinds.Any(value => !m_Definitions.ContainsKey(value)))
            {
                throw new InvalidOperationException(
                    "Every formal Pose Node Kind must have exactly one Node Definition adapter.");
            }
        }

        public IReadOnlyCollection<CharacterPoseNodeDefinition> All
        {
            get
            {
                EnsureCapabilities();
                return m_Definitions.Values;
            }
        }

        internal IReadOnlyCollection<CharacterPoseNodeDefinition> Declarations =>
            m_Definitions.Values;

        public GraphAuthoringCapabilityDescriptor ProjectCapability(
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (m_CapabilitiesBound)
                throw new InvalidOperationException(
                    "Pose Node Definition capabilities are already sealed.");
            if (capability?.AuthoringType == null ||
                !m_ByPayloadType.TryGetValue(
                    capability.AuthoringType,
                    out CharacterPoseNodeDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Pose capability '{capability?.CapabilityId}' has no matching Node Definition adapter.");
            }
            definition.BindCapability(capability);
            if (!m_ByCapability.TryAdd(
                    capability.CapabilityId.Value,
                    definition))
            {
                throw new InvalidOperationException(
                    $"Pose capability '{capability.CapabilityId}' is registered more than once.");
            }
            return capability;
        }

        public void SealCapabilities()
        {
            if (m_CapabilitiesBound)
                return;
            CharacterPoseNodeDefinition[] missing = m_Definitions.Values
                .Where(value => !m_ByCapability.ContainsKey(
                    CharacterPoseGraphAuthoringCapabilities.Get(value.Kind).Value))
                .ToArray();
            if (missing.Length != 0 ||
                m_ByCapability.Count != m_Definitions.Count)
            {
                throw new InvalidOperationException(
                    $"Pose Node Definition capability projection is incomplete: [{string.Join(",", missing.Select(value => value.Kind))}].");
            }
            m_CapabilitiesBound = true;
        }

        public CharacterPoseNodeDefinition Require(
            CharacterPoseNodeKind kind)
        {
            EnsureCapabilities();
            return m_Definitions.TryGetValue(kind, out CharacterPoseNodeDefinition value)
                ? value
                : throw new InvalidOperationException(
                    $"Pose Node Kind '{kind}' has no Node Definition.");
        }

        public CharacterPoseNodeDefinition RequireCapability(
            string capabilityIdentity)
        {
            EnsureCapabilities();
            return !string.IsNullOrWhiteSpace(capabilityIdentity) &&
                   m_ByCapability.TryGetValue(
                       capabilityIdentity,
                       out CharacterPoseNodeDefinition value)
                ? value
                : throw new InvalidOperationException(
                    $"Pose capability '{capabilityIdentity ?? "<null>"}' has no Node Definition.");
        }

        public bool TryGetCapability(
            string capabilityIdentity,
            out CharacterPoseNodeDefinition definition)
        {
            EnsureCapabilities();
            if (string.IsNullOrWhiteSpace(capabilityIdentity))
            {
                definition = null;
                return false;
            }
            return m_ByCapability.TryGetValue(capabilityIdentity, out definition);
        }

        public CharacterPoseNodeDefinition RequirePayload(
            CharacterPoseNodePayload payload)
        {
            EnsureCapabilities();
            return payload != null &&
                   m_ByPayloadType.TryGetValue(
                       payload.GetType(),
                       out CharacterPoseNodeDefinition value) &&
                   value.Kind == payload.Kind
                ? value
                : throw new InvalidOperationException(
                    $"Pose payload '{payload?.GetType().FullName ?? "null"}' has no Node Definition.");
        }

        void EnsureCapabilities()
        {
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
            if (m_CapabilitiesBound)
                return;
            foreach (CharacterPoseNodeDefinition definition in m_Definitions.Values)
                ProjectCapability(CharacterPoseGraphCapabilityProjector.Require(definition.Kind));
            SealCapabilities();
        }
    }
}
