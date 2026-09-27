using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using BTSMTL.Authoring.Graph;
using static ThirdPersonCharacter.Editor.CharacterSimulation.CharacterPoseCapabilityDeclarations;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterLinkedPoseCallNodeDefinition :
        CharacterPoseNodeDefinition<CharacterLinkedPoseCallPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LinkedPoseCall;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterLinkedPoseCallPayload>(CharacterPoseNodeKind.LinkedPoseCall, RootAndLinkedEntry, "Linked Pose Call", "Graph", BlendColor,
                Fields(
                    Field("group-id", "Group", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-group"),
                    Field("interface-id", "Interface", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-interface"),
                    Field("entry-id", "Entry", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-entry")),
                Array.Empty<GraphAuthoringPortDescriptor>(),
                GraphAuthoringDynamicPortPolicy.OrderedBidirectional,
                executionDomain: CharacterPoseExecutionDomain.ManagedControl);

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterLinkedPoseCallPayload(
                new LinkedPoseGroupId(input.Require<string>("group-id")),
                new LinkedPoseInterfaceId(input.Require<string>("interface-id")),
                new LinkedPoseEntryId(input.Require<string>("entry-id")));

        protected override object ReadField(
            CharacterLinkedPoseCallPayload payload,
            string field) =>
            field switch
            {
                "group-id" => payload.GroupId.Value,
                "interface-id" => payload.InterfaceId.Value,
                "entry-id" => payload.EntryId.Value,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(
            CharacterLinkedPoseCallPayload payload,
            string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.GroupId.IsValid &&
                payload.InterfaceId.IsValid &&
                payload.EntryId.IsValid,
                sourcePath,
                "Linked Pose Call Group, Interface or Entry identity is missing.");

        protected override IReadOnlyList<CharacterPoseGraphDependency>
            GetGraphDependencies(CharacterLinkedPoseCallPayload payload) =>
            payload.GroupId.IsValid &&
            payload.InterfaceId.IsValid &&
            payload.EntryId.IsValid
                ? new[]
                {
                    new CharacterPoseGraphDependency(
                        CharacterPoseGraphDependencyKind.LinkedPoseEntry,
                        default,
                        string.Join(
                            "/",
                            payload.GroupId.Value,
                            payload.InterfaceId.Value,
                            payload.EntryId.Value))
                }
                : Array.Empty<CharacterPoseGraphDependency>();
    }
}
