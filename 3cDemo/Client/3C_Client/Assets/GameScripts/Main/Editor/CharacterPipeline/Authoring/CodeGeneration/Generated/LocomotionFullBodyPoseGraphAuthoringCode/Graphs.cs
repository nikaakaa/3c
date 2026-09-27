using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Generated
{
    public sealed partial class LocomotionFullBodyPoseGraphAuthoringCode
    {
        static GraphsParts BuildGraphs(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new GraphsParts();
            parts.poseGraph9 = CharacterPoseCanvasGraph.CreateAuthoring(new PoseGraphId("corin.pose.spine-transform"), "e7f60a9e734c4e6788ab0e78491c02b1", new CharacterPoseParameterDeclaration[0], new CharacterPoseCanvasNode[] { new CharacterPoseCanvasNode(new PoseNodeId("corin.pose.spine-transform/input"), "Pose / Rotation", new CharacterGraphInputPosePayload(), new CharacterPoseDynamicPort[] { new CharacterPoseDynamicPort(new PosePortId("pose"), "Pose", CharacterPosePortKind.LocalPose, CharacterPosePortDirection.Output, true, 0, new PoseInterfacePortId("bone-transform.pose")), new CharacterPoseDynamicPort(new PosePortId("rotation"), "Rotation (Quaternion)", CharacterPosePortKind.Parameter, CharacterPosePortDirection.Output, true, 1, new PoseInterfacePortId("bone-transform.rotation")) }, new Vector2(-560f, 0f)), new CharacterPoseCanvasNode(new PoseNodeId("corin.pose.spine-transform/component"), "Local To Component", new CharacterLocalToComponentPosePayload(), new CharacterPoseDynamicPort[0], new Vector2(-280f, 0f)), new CharacterPoseCanvasNode(new PoseNodeId("corin.pose.spine-transform/modify"), "Spine Rotation", BtsmtlPoseAuthoringCode.CreatePayload(CharacterPoseNodeKind.ModifyBone, new System.Collections.Generic.Dictionary<string, object> { { "bone-id", "animation-bone/Bip001/Bip001_Pelvis/Bip001_Spine" }, { "reference-space", "Component" }, { "position-mode", "Ignore" }, { "position-source", "Constant" }, { "position", new Vector3(0f, 0f, 0f) }, { "rotation-mode", "Add" }, { "rotation-source", "Port" }, { "rotation", new Quaternion(0f, 0f, 0f, 1f) }, { "scale-mode", "Ignore" }, { "scale-source", "Constant" }, { "scale", new Vector3(1f, 1f, 1f) }, { "propagate-to-children", true }, { "weight", 1f } }), new CharacterPoseDynamicPort[0], new Vector2(0f, 0f)), new CharacterPoseCanvasNode(new PoseNodeId("corin.pose.spine-transform/local"), "Component To Local", new CharacterComponentToLocalPosePayload(), new CharacterPoseDynamicPort[0], new Vector2(280f, 0f)), new CharacterPoseCanvasNode(new PoseNodeId("corin.pose.spine-transform/output"), "Modified Pose", new CharacterGraphOutputPosePayload(), new CharacterPoseDynamicPort[] { new CharacterPoseDynamicPort(new PosePortId("result"), "Pose", CharacterPosePortKind.LocalPose, CharacterPosePortDirection.Input, true, 2, new PoseInterfacePortId("bone-transform.result")) }, new Vector2(560f, 0f)) }, new CharacterPoseCanvasConnection[] { new CharacterPoseCanvasConnection("corin.pose.spine-transform/component-modify", new PoseNodeId("corin.pose.spine-transform/component"), new PosePortId("component-pose"), new PoseNodeId("corin.pose.spine-transform/modify"), new PosePortId("pose")), new CharacterPoseCanvasConnection("corin.pose.spine-transform/local-output", new PoseNodeId("corin.pose.spine-transform/local"), new PosePortId("local-pose"), new PoseNodeId("corin.pose.spine-transform/output"), new PosePortId("result")), new CharacterPoseCanvasConnection("corin.pose.spine-transform/modify-local", new PoseNodeId("corin.pose.spine-transform/modify"), new PosePortId("result"), new PoseNodeId("corin.pose.spine-transform/local"), new PosePortId("component-pose")), new CharacterPoseCanvasConnection("corin.pose.spine-transform/pose-component", new PoseNodeId("corin.pose.spine-transform/input"), new PosePortId("pose"), new PoseNodeId("corin.pose.spine-transform/component"), new PosePortId("local-pose")), new CharacterPoseCanvasConnection("corin.pose.spine-transform/rotation-modify", new PoseNodeId("corin.pose.spine-transform/input"), new PosePortId("rotation"), new PoseNodeId("corin.pose.spine-transform/modify"), new PosePortId("rotation")) }, new CharacterPoseGraphLayoutEntry[] { new CharacterPoseGraphLayoutEntry(new PoseNodeId("corin.pose.spine-transform/component"), new Vector2(-280f, 0f)), new CharacterPoseGraphLayoutEntry(new PoseNodeId("corin.pose.spine-transform/input"), new Vector2(-560f, 0f)), new CharacterPoseGraphLayoutEntry(new PoseNodeId("corin.pose.spine-transform/local"), new Vector2(280f, 0f)), new CharacterPoseGraphLayoutEntry(new PoseNodeId("corin.pose.spine-transform/modify"), new Vector2(0f, 0f)), new CharacterPoseGraphLayoutEntry(new PoseNodeId("corin.pose.spine-transform/output"), new Vector2(560f, 0f)) }, CharacterPoseAuthoringGraphRole.Subgraph );
            return parts;
        }

        sealed class GraphsParts
        {
            internal CharacterPoseCanvasGraph poseGraph;
            internal CharacterPoseCanvasGraph poseGraph1;
            internal CharacterPoseCanvasGraph poseGraph2;
            internal CharacterPoseCanvasGraph poseGraph3;
            internal CharacterPoseCanvasGraph poseGraph4;
            internal CharacterPoseCanvasGraph poseGraph5;
            internal CharacterPoseCanvasGraph poseGraph6;
            internal CharacterPoseCanvasGraph poseGraph7;
            internal CharacterPoseCanvasGraph poseGraph8;
            internal CharacterPoseCanvasGraph poseGraph9;
        }
    }
}
