using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterMotionMatchingPoseAuthoringCapabilities
    {
        static readonly GraphAuthoringFieldAccess s_WritableReference =
            GraphAuthoringFieldAccess.AuthoringRead |
            GraphAuthoringFieldAccess.AuthoringWrite |
            GraphAuthoringFieldAccess.ReferenceRead;

        static bool s_Registered;

        static CharacterMotionMatchingPoseAuthoringCapabilities()
        {
            EnsureRegistered();
        }

        internal static void EnsureRegistered()
        {
            if (s_Registered)
                return;
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
            if (s_Registered)
                return;
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain(
                "character-presentation.motion-matching-pose",
                Register);
            s_Registered = true;
        }

        static void Register(GraphAuthoringCapabilityCatalog catalog)
        {
            Color sourceColor = new Color32(55, 115, 92, 255);
            Color inputColor = new Color32(58, 103, 138, 255);
            catalog.Register(new GraphAuthoringCapabilityDescriptor(
                CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.MotionMatchingPose),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                new[] { CharacterPoseGraphAuthoringCapabilities.StatePoseGraph },
                "Motion Matching Pose",
                "Sources",
                sourceColor,
                new[]
                {
                    AssetField("binding", "Binding", "motion-matching-binding", typeof(CharacterMotionMatchingBinding)),
                    AssetField("jump-blend-policy", "Jump Blend Policy", "animation-blend-policy", typeof(CharacterAnimationBlendPolicy)),
                    IdentityField("entry-graph-id", "Entry Processing Graph", "pose-graph"),
                    EnumField("relevance-reset-policy", "Relevance Reset", typeof(CharacterMotionMatchingRelevanceResetPolicy)),
                    EnumField("search-cadence-policy", "Search Cadence", typeof(CharacterMotionMatchingSearchCadencePolicy))
                },
                new[]
                {
                    In("history.pose", "Previous Pose History", "pose.history", 0, true),
                    In("trajectory.query", "Trajectory", "motion-matching.trajectory", 1, false),
                    In("presentation.facts", "Presentation Facts", "presentation.facts", 2, false),
                    In("motion-matching.binding", "Binding", "motion-matching.binding", 3, false),
                    Out("pose.local", "Local Pose", "pose.local", 4)
                },
                childSurfaces: new[]
                {
                    new GraphAuthoringChildSurfaceDescriptor(
                        new GraphAuthoringCommandId("open-entry-processing-graph"),
                        CharacterPoseGraphAuthoringCapabilities.Subgraph,
                        "Open Entry Processing Graph")
                },
                mutationBindingId: "presentation.pose-node",
                validationBindingId: "presentation.pose-node",
                compilerBindingId: "presentation.pose-node.motion-matching-pose",
                documentCodecId: "presentation.pose-node",
                authoringType: typeof(CharacterMotionMatchingPosePayload),
                externalKind: CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.MotionMatchingPose).Value,
                executionDomainId: CharacterPoseExecutionDomain.SourceCapture.ToString()));

            catalog.Register(new GraphAuthoringCapabilityDescriptor(
                CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.PoseHistoryCollector),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                new[] { CharacterPoseGraphAuthoringCapabilities.StatePoseGraph },
                "Pose History Collector",
                "Sources",
                sourceColor,
                new[] { IdentityField("history-id", "History", "pose-history") },
                new[]
                {
                    In("pose.local.input", "Local Pose", "pose.local", 0, true),
                    Out("pose.local", "Local Pose", "pose.local", 1),
                    Out("history.pose", "Previous Pose History", "pose.history", 2)
                },
                mutationBindingId: "presentation.pose-node",
                validationBindingId: "presentation.pose-node",
                compilerBindingId: "presentation.pose-node.pose-history-collector",
                documentCodecId: "presentation.pose-node",
                authoringType: typeof(CharacterPoseHistoryCollectorPayload),
                externalKind: CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.PoseHistoryCollector).Value,
                executionDomainId: CharacterPoseExecutionDomain.PurePose.ToString()));

            catalog.Register(new GraphAuthoringCapabilityDescriptor(
                CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.EntryPoseInput),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                new[] { CharacterPoseGraphAuthoringCapabilities.Subgraph },
                "Entry Pose Input",
                "Inputs",
                inputColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                new[] { Out("pose.local", "Local Pose", "pose.local", 0, true, "entry.pose") },
                mutationBindingId: "presentation.pose-node",
                validationBindingId: "presentation.pose-node",
                compilerBindingId: "presentation.pose-node.entry-pose-input",
                documentCodecId: "presentation.pose-node",
                authoringType: typeof(CharacterEntryPoseInputPayload),
                externalKind: CharacterPoseGraphAuthoringCapabilities.Get(CharacterPoseNodeKind.EntryPoseInput).Value,
                executionDomainId: CharacterPoseExecutionDomain.SourceCapture.ToString()));
        }

        static GraphAuthoringFieldDescriptor AssetField(string id, string name, string pickerKind, Type type) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.AssetReference,
                s_WritableReference,
                pickerKind: pickerKind,
                objectType: type);

        static GraphAuthoringFieldDescriptor IdentityField(string id, string name, string pickerKind) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.IdentityReference,
                s_WritableReference,
                pickerKind: pickerKind);

        static GraphAuthoringFieldDescriptor EnumField(string id, string name, Type type) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.Enum,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                defaultValue: Enum.GetValues(type).GetValue(0),
                objectType: type);

        static GraphAuthoringPortDescriptor In(string id, string name, string valueType, int order, bool required) =>
            new GraphAuthoringPortDescriptor(
                new GraphAuthoringPortId(id),
                name,
                valueType,
                GraphAuthoringPortDirection.Input,
                GraphAuthoringPortCapacity.Single,
                required,
                order);

        static GraphAuthoringPortDescriptor Out(
            string id,
            string name,
            string valueType,
            int order,
            bool required = false,
            string interfacePortId = "") =>
            new GraphAuthoringPortDescriptor(
                new GraphAuthoringPortId(id),
                name,
                valueType,
                GraphAuthoringPortDirection.Output,
                GraphAuthoringPortCapacity.Multiple,
                required,
                order,
                interfacePortId);
    }
}
