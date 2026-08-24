using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterLinkedPoseCallCompilerHandler :
        CharacterPoseCompilerHandler<CharacterLinkedPoseCallPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.LinkedPoseCall;
        public override CharacterPoseOperationCode Code =>
            CharacterPoseOperationCode.LinkedPoseCall;

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
            CharacterPoseCompilerHandlerValidation.Require(
                payload.GroupId.IsValid &&
                payload.InterfaceId.IsValid &&
                payload.EntryId.IsValid,
                sourcePath,
                "Linked Pose Call Group, Interface or Entry identity is missing.");
    }
}
