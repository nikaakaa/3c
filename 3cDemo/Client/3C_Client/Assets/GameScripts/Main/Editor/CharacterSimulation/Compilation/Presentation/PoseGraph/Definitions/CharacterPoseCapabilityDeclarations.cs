using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseCapabilityDeclarations
    {
        internal static readonly GraphAuthoringDocumentRoleId[] AllPoseGraphs =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph,
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph,
            CharacterPoseGraphAuthoringCapabilities.Subgraph,
            CharacterPoseGraphAuthoringCapabilities.ControlRig
        };
        internal static readonly GraphAuthoringDocumentRoleId[] AllPoseGraphsWithLinkedEntry =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph,
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph,
            CharacterPoseGraphAuthoringCapabilities.Subgraph,
            CharacterPoseGraphAuthoringCapabilities.ControlRig,
            CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
        };
        internal static readonly GraphAuthoringDocumentRoleId[] RootAndState =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph,
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
        };
        internal static readonly GraphAuthoringDocumentRoleId[] RootAndStateWithLinkedEntry =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph,
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph,
            CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
        };
        internal static readonly GraphAuthoringDocumentRoleId[] RootAndLinkedEntry =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph,
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
        };
        internal static readonly GraphAuthoringDocumentRoleId[] RootOnly =
        {
            CharacterPoseGraphAuthoringCapabilities.RootGraph
        };
        internal static readonly GraphAuthoringDocumentRoleId[] StateSubgraphAndLinkedEntry =
        {
            CharacterPoseGraphAuthoringCapabilities.AnimationLayer,
            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph,
            CharacterPoseGraphAuthoringCapabilities.Subgraph,
            CharacterPoseGraphAuthoringCapabilities.ControlRig,
            CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
        };
        internal static readonly Color InputColor = new Color32(58, 103, 138, 255);
        internal static readonly Color SourceColor = new Color32(55, 115, 92, 255);
        internal static readonly Color BlendColor = new Color32(98, 76, 142, 255);
        internal static readonly Color ConstraintColor = new Color32(133, 83, 55, 255);
        internal static readonly Color OutputColor = new Color32(132, 55, 67, 255);

        internal static IReadOnlyList<GraphAuthoringCommandDescriptor> SourceCommands() =>
            new[]
            {
                new GraphAuthoringCommandDescriptor(
                    CharacterPoseGraphAuthoringCapabilities.PingPoseSource,
                    "Ping Source",
                    false),
                new GraphAuthoringCommandDescriptor(
                    CharacterPoseGraphAuthoringCapabilities.OpenPoseSource,
                    "Open Source",
                    false),
                new GraphAuthoringCommandDescriptor(
                    CharacterPoseGraphAuthoringCapabilities.OpenPoseSourceProfile,
                    "Open Profile Owner",
                    false)
            };

        internal static GraphAuthoringCapabilityDescriptor Node<TPayload>(
            CharacterPoseNodeKind kind,
            IReadOnlyList<GraphAuthoringDocumentRoleId> roles,
            string displayName,
            string category,
            Color color,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields,
            IReadOnlyList<GraphAuthoringPortDescriptor> ports,
            GraphAuthoringDynamicPortPolicy dynamicPortPolicy = GraphAuthoringDynamicPortPolicy.None,
            IReadOnlyList<GraphAuthoringChildSurfaceDescriptor> childSurfaces = null,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands = null,
            CharacterPoseExecutionDomain executionDomain = CharacterPoseExecutionDomain.PurePose,
            bool systemOwned = false)
            where TPayload : CharacterPoseNodePayload, new()
        {
            if (new TPayload().Kind != kind)
            {
                throw new InvalidOperationException(
                    $"Pose capability '{CharacterPoseGraphAuthoringCapabilities.Get(kind)}' payload type '{typeof(TPayload).FullName}' declares a different kind.");
            }
            return new GraphAuthoringCapabilityDescriptor(
                        CharacterPoseGraphAuthoringCapabilities.Get(kind),
                        CharacterPoseGraphAuthoringCapabilities.Domain,
                        roles,
                        displayName,
                        category,
                        color,
                        fields,
                        ports,
                        dynamicPortPolicy,
                        childSurfaces,
                        commands: commands,
                        mutationBindingId: "presentation.pose-node",
                        validationBindingId: "presentation.pose-node",
                        compilerBindingId: string.Empty,
                        documentCodecId: "presentation.pose-node",
                        authoringType: typeof(TPayload),
                        externalKind: CharacterPoseGraphAuthoringCapabilities.Get(kind).Value,
                        systemOwned: systemOwned,
                        anchorId: systemOwned ? $"pose.internal.{kind}" : string.Empty,
                        executionDomainId: executionDomain.ToString());
        }

        internal static GraphAuthoringCapabilityDescriptor Surface(
            string identity,
            string displayName,
            GraphAuthoringNodePresentationKind presentationKind,
            GraphAuthoringDocumentRoleId? role = null,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields = null)
        {
            return new GraphAuthoringCapabilityDescriptor(
                new GraphAuthoringCapabilityId(identity),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                new[] { role ?? CharacterPoseGraphAuthoringCapabilities.StateMachine },
                displayName,
                "State Machine",
                new Color32(74, 91, 126, 255),
                fields,
                presentationKind: presentationKind,
                mutationBindingId: "presentation.pose-state-machine",
                validationBindingId: "presentation.pose-state-machine",
                compilerBindingId: "presentation.pose-state-machine",
                documentCodecId: "presentation.pose-state-machine");
        }

        internal static GraphAuthoringCapabilityDescriptor RuleOperation(
            PoseTransitionRuleOperationKind kind)
        {
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields =
                kind switch
                {
                    PoseTransitionRuleOperationKind.FactInput =>
                        Fields(Field(
                            "fact-id",
                            "Presentation Fact",
                            GraphAuthoringFieldValueKind.IdentityReference,
                            "presentation-fact")),
                    PoseTransitionRuleOperationKind.BoolLiteral =>
                        Fields(BoolField(
                            "bool-literal",
                            "Value",
                            false)),
                    PoseTransitionRuleOperationKind.FloatLiteral =>
                        Fields(FloatField(
                            "float-literal",
                            "Value",
                            0f)),
                    PoseTransitionRuleOperationKind.EnumLiteral =>
                        Fields(
                            ReadOnlyField(
                                "enum-type-id",
                                "Enum Type",
                                GraphAuthoringFieldValueKind
                                    .IdentityReference),
                            EnumField(
                                "enum-literal",
                                "Value",
                                typeof(
                                    CharacterPresentationMotionPhase))),
                    PoseTransitionRuleOperationKind.IdentityLiteral =>
                        Fields(Field(
                            "identity-literal",
                            "Movement Mode",
                            GraphAuthoringFieldValueKind.IdentityReference,
                            "gameplay-state")),
                    _ => Array.Empty<
                        GraphAuthoringFieldDescriptor>()
                };
            IReadOnlyList<GraphAuthoringPortDescriptor> ports =
                kind switch
                {
                    PoseTransitionRuleOperationKind.Not =>
                        Ports(
                            In(
                                "input-a",
                                "Value",
                                "pose.rule.value"),
                            Out(
                                "result",
                                "Result",
                                "pose.rule.value")),
                    PoseTransitionRuleOperationKind.And or
                    PoseTransitionRuleOperationKind.Or or
                    PoseTransitionRuleOperationKind.Equal or
                    PoseTransitionRuleOperationKind.NotEqual or
                    PoseTransitionRuleOperationKind.Greater or
                    PoseTransitionRuleOperationKind.GreaterOrEqual or
                    PoseTransitionRuleOperationKind.Less or
                    PoseTransitionRuleOperationKind.LessOrEqual =>
                        Ports(
                            In(
                                "input-a",
                                "A",
                                "pose.rule.value"),
                            In(
                                "input-b",
                                "B",
                                "pose.rule.value"),
                            Out(
                                "result",
                                "Result",
                                "pose.rule.value")),
                    _ => Ports(
                        Out(
                            "result",
                            "Value",
                            "pose.rule.value"))
                };
            bool canBeOutput =
                kind == PoseTransitionRuleOperationKind.FactInput ||
                kind == PoseTransitionRuleOperationKind.BoolLiteral ||
                kind == PoseTransitionRuleOperationKind.Not ||
                kind == PoseTransitionRuleOperationKind.And ||
                kind == PoseTransitionRuleOperationKind.Or ||
                kind == PoseTransitionRuleOperationKind.Equal ||
                kind == PoseTransitionRuleOperationKind.NotEqual ||
                kind == PoseTransitionRuleOperationKind.Greater ||
                kind == PoseTransitionRuleOperationKind.GreaterOrEqual ||
                kind == PoseTransitionRuleOperationKind.Less ||
                kind == PoseTransitionRuleOperationKind.LessOrEqual;
            return new GraphAuthoringCapabilityDescriptor(
                CharacterPoseGraphAuthoringCapabilities.Get(kind),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                new[] { CharacterPoseGraphAuthoringCapabilities.TransitionRule },
                RuleOperationDisplayName(kind),
                "Transition Rule",
                new Color32(89, 74, 126, 255),
                fields,
                ports,
                commands: canBeOutput
                    ? new[]
                    {
                        new GraphAuthoringCommandDescriptor(
                            new GraphAuthoringCommandId(
                                "set-rule-output"),
                            "Set as Rule Output",
                            false)
                    }
                    : Array.Empty<
                        GraphAuthoringCommandDescriptor>(),
                presentationKind:
                    GraphAuthoringNodePresentationKind
                        .TransitionRule,
                mutationBindingId:
                    "presentation.pose-transition-rule",
                validationBindingId:
                    "presentation.pose-transition-rule",
                compilerBindingId:
                    "presentation.pose-transition-rule." +
                    CharacterPoseGraphAuthoringCapabilities.ToKebabCase(kind.ToString()),
                documentCodecId:
                    "presentation.pose-transition-rule");
        }

        static string RuleOperationDisplayName(
            PoseTransitionRuleOperationKind kind) =>
            kind switch
            {
                PoseTransitionRuleOperationKind.FactInput =>
                    "Presentation Fact",
                PoseTransitionRuleOperationKind.BoolLiteral =>
                    "Bool Literal",
                PoseTransitionRuleOperationKind.FloatLiteral =>
                    "Float Literal",
                PoseTransitionRuleOperationKind.EnumLiteral =>
                    "Enum Literal",
                PoseTransitionRuleOperationKind.IdentityLiteral =>
                    "Identity Literal",
                PoseTransitionRuleOperationKind.TimeInState =>
                    "Time in State",
                PoseTransitionRuleOperationKind
                    .StatePoseRemainingTime =>
                    "State Pose Remaining Time",
                PoseTransitionRuleOperationKind.GreaterOrEqual =>
                    "Greater or Equal",
                PoseTransitionRuleOperationKind.LessOrEqual =>
                    "Less or Equal",
                PoseTransitionRuleOperationKind.NotEqual =>
                    "Not Equal",
                _ => kind.ToString()
            };

        internal static GraphAuthoringFieldDescriptor Field(string id, string name, GraphAuthoringFieldValueKind kind, string pickerKind, Type objectType = null) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                kind,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                constraint: new GraphAuthoringFieldConstraint(
                    nonEmpty:
                    kind == GraphAuthoringFieldValueKind.IdentityReference ||
                    kind == GraphAuthoringFieldValueKind.AssetReference),
                pickerKind: pickerKind,
                objectType: objectType,
                tuning: Tuning(id, kind, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));

        internal static GraphAuthoringFieldDescriptor SourceField(Type slotType) =>
            Field("pose-source-slot", "Pose Source", GraphAuthoringFieldValueKind.AssetReference, "pose-source-slot", slotType);
        internal static GraphAuthoringFieldDescriptor ResourceField(string id, string name) =>
            AssetField(id, name, "pose-resource-slot", typeof(CharacterPoseResourceSlot));
        internal static GraphAuthoringFieldDescriptor SelectionAvailabilityField() => EnumField("selection-availability", "Availability", typeof(AnimationSelectionAvailabilityPolicy));
        static GraphAuthoringFieldDescriptor AssetField(string id, string name, string pickerKind, Type objectType) =>
            Field(id, name, GraphAuthoringFieldValueKind.AssetReference, pickerKind, objectType);
        internal static GraphAuthoringFieldDescriptor ReferenceIdentityField(string id, string name, string pickerKind) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.IdentityReference, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite | GraphAuthoringFieldAccess.ReferenceRead, pickerKind: pickerKind);
        internal static GraphAuthoringFieldDescriptor TypedEnumField(string id, string name, Type enumType) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Enum, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: Enum.GetValues(enumType).GetValue(0), objectType: enumType);
        internal static GraphAuthoringFieldDescriptor ConditionalAssetField(
            string id,
            string name,
            string pickerKind,
            Type objectType,
            string controllerFieldId,
            string expectedValue) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.AssetReference,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                constraint: new GraphAuthoringFieldConstraint(nonEmpty: true),
                pickerKind: pickerKind,
                objectType: objectType,
                visibility: new GraphAuthoringFieldVisibilityCondition(
                    new GraphAuthoringFieldId(controllerFieldId),
                    expectedValue),
                tuning: Tuning(id, GraphAuthoringFieldValueKind.AssetReference, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        internal static GraphAuthoringFieldDescriptor StringField(string id, string name, string defaultValue) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.String, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(nonEmpty: true), tuning: Tuning(id, GraphAuthoringFieldValueKind.String, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        internal static GraphAuthoringFieldDescriptor BoolField(string id, string name, bool defaultValue) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Boolean, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, tuning: Tuning(id, GraphAuthoringFieldValueKind.Boolean, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        internal static GraphAuthoringFieldDescriptor FloatField(string id, string name, float defaultValue, float? minimum = null, float? maximum = null) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Float, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(minimum, maximum, true), tuning: Tuning(id, GraphAuthoringFieldValueKind.Float, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, minimum ?? double.MinValue, maximum ?? double.MaxValue));
        internal static GraphAuthoringFieldDescriptor IntegerField(string id, string name, int defaultValue, int? minimum = null, int? maximum = null) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Integer, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(minimum, maximum), tuning: Tuning(id, GraphAuthoringFieldValueKind.Integer, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, minimum ?? int.MinValue, maximum ?? int.MaxValue));
        internal static GraphAuthoringFieldDescriptor ReadOnlyField(string id, string name, GraphAuthoringFieldValueKind kind) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, kind, GraphAuthoringFieldAccess.AuthoringRead, tuning: Tuning(id, kind, GraphAuthoringFieldAccess.AuthoringRead));
        internal static GraphAuthoringFieldDescriptor Vector3Field(string id, string name, Vector3 defaultValue = default) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Vector3, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, tuning: Tuning(id, GraphAuthoringFieldValueKind.Vector3, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        internal static GraphAuthoringFieldDescriptor EnumField(string id, string name, Type enumType) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Enum, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, constraint: new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(enumType)), tuning: Tuning(id, GraphAuthoringFieldValueKind.Enum, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));

        static GraphAuthoringFieldTuningMetadata Tuning(
            string id,
            GraphAuthoringFieldValueKind kind,
            GraphAuthoringFieldAccess access,
            double minimum = double.MinValue,
            double maximum = double.MaxValue)
        {
            bool writable = (access & GraphAuthoringFieldAccess.AuthoringWrite) != 0;
            bool tunable = writable &&
                (string.Equals(id, "weight", StringComparison.Ordinal) ||
                 string.Equals(id, "play-rate", StringComparison.Ordinal) ||
                 string.Equals(id, "duration-seconds", StringComparison.Ordinal));
            GraphAuthoringFieldInteractionPolicy interaction = !writable
                ? GraphAuthoringFieldInteractionPolicy.DerivedReadOnly
                : tunable
                    ? GraphAuthoringFieldInteractionPolicy.TunableDefault
                    : GraphAuthoringFieldInteractionPolicy.Structural;
            string unit = string.Equals(id, "weight", StringComparison.Ordinal)
                ? "normalized"
                : string.Equals(id, "play-rate", StringComparison.Ordinal)
                    ? "multiplier"
                    : string.Equals(id, "duration-seconds", StringComparison.Ordinal)
                        ? "seconds"
                        : string.Empty;
            GraphAuthoringFieldApplyTiming timing =
                string.Equals(id, "duration-seconds", StringComparison.Ordinal)
                    ? GraphAuthoringFieldApplyTiming.NextActivation
                    : GraphAuthoringFieldApplyTiming.NextFrame;
            if (kind != GraphAuthoringFieldValueKind.Float &&
                kind != GraphAuthoringFieldValueKind.Integer)
            {
                minimum = 0d;
                maximum = 1d;
            }
            return new GraphAuthoringFieldTuningMetadata(
                interaction,
                kind,
                unit,
                minimum,
                maximum,
                true,
                timing,
                GraphAuthoringFieldStatePolicy.PreserveState,
                "pose-graph",
                0,
                0);
        }

        internal static GraphAuthoringPortDescriptor In(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Input, true);
        internal static GraphAuthoringPortDescriptor OptionalIn(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Input, false);
        internal static GraphAuthoringPortDescriptor Out(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Output, false);
        internal static GraphAuthoringPortDescriptor InterfaceOut(string id, string name, string valueType, string interfacePortId) =>
            new GraphAuthoringPortDescriptor(new GraphAuthoringPortId(id), name, valueType, GraphAuthoringPortDirection.Output, GraphAuthoringPortCapacity.Multiple, true, 0, interfacePortId);
        static GraphAuthoringPortDescriptor Port(string id, string name, string valueType, GraphAuthoringPortDirection direction, bool required) =>
            new GraphAuthoringPortDescriptor(new GraphAuthoringPortId(id), name, valueType, direction, direction == GraphAuthoringPortDirection.Input ? GraphAuthoringPortCapacity.Single : GraphAuthoringPortCapacity.Multiple, required, 0);
        internal static GraphAuthoringPortDescriptor[] Ports(
            params GraphAuthoringPortDescriptor[] ports)
        {
            var ordered =
                new GraphAuthoringPortDescriptor[ports.Length];
            for (int i = 0; i < ports.Length; i++)
            {
                GraphAuthoringPortDescriptor port = ports[i];
                ordered[i] = new GraphAuthoringPortDescriptor(
                    port.PortId,
                    port.DisplayName,
                    port.ValueTypeId,
                    port.Direction,
                    port.Capacity,
                    port.Required,
                    i,
                    port.InterfacePortId);
            }
            return ordered;
        }
        internal static GraphAuthoringFieldDescriptor[] Fields(params GraphAuthoringFieldDescriptor[] fields) => fields;
        internal static GraphAuthoringChildSurfaceDescriptor Child(string id, string name, GraphAuthoringDocumentRoleId role) =>
            new GraphAuthoringChildSurfaceDescriptor(new GraphAuthoringCommandId(id), role, name);
        internal static GraphAuthoringPortDescriptor[] UnaryLocalPosePorts() => Ports(In("pose", "Local Pose", "pose.local"), Out("result", "Local Pose", "pose.local"));
        internal static GraphAuthoringPortDescriptor[] UnaryComponentPoseWithWeight() => Ports(In("pose", "Component Pose", "pose.component"), OptionalIn("weight", "Weight", "pose.parameter"), Out("result", "Component Pose", "pose.component"));
        internal static GraphAuthoringPortDescriptor[] BinaryLocalPoseWithWeight(string first, string second) => Ports(In("base", first + " Local Pose", "pose.local"), In("overlay", second + " Local Pose", "pose.local"), OptionalIn("weight", "Weight", "pose.parameter"), Out("result", "Local Pose", "pose.local"));
    }
}
