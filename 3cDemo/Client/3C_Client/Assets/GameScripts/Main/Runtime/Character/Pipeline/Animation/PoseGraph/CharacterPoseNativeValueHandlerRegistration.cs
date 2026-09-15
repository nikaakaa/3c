using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeValueHandlerRegistration
    {
        internal static void Register(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationInputContract inputContract)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (inputContract == null)
                throw new ArgumentNullException(nameof(inputContract));
            registry.RegisterPureValueHandlers(
                (node, context) => CreateBuffer(node, context, inputContract.Parameters.Count),
                (node, context) => ResolveBoneMask(node, profile, context));
            registry.Register(
                (node, context) => CreateBuffer(node, context, inputContract.Parameters.Count),
                (node, context) => ResolveInertializationPolicy(node, profile));
            registry.Register(profile);
        }

        static CharacterPoseNativeNodePoseBuffer CreateBuffer(
            CharacterPoseCanvasNode node,
            CharacterPoseNativeInstanceContext context,
            int parameterCount)
        {
            CharacterPoseCanvasGraph graph = node.graph as CharacterPoseCanvasGraph ??
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' is not attached to a Pose Canvas graph.");
            int nodeIndex = -1;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (!ReferenceEquals(graph.Nodes[i], node))
                    continue;
                nodeIndex = i;
                break;
            }
            if (nodeIndex < 0)
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' is not owned by graph '{graph.GraphId}'.");
            return new CharacterPoseNativeNodePoseBuffer(
                nodeIndex,
                context.Rig.PoseBoneCount,
                parameterCount,
                Math.Max(1, graph.Nodes.Count));
        }

        static IReadOnlyList<float> ResolveBoneMask(
            CharacterPoseCanvasNode node,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseNativeInstanceContext context)
        {
            CharacterPoseResourceBinding binding =
                profile.FindPoseResourceBinding(node.BoneMaskSlot);
            CharacterAnimationBoneMaskAsset mask = binding?.Resource as
                CharacterAnimationBoneMaskAsset ??
                throw new InvalidOperationException(
                    $"Pose Layered Bone Blend node '{node.NodeId}' has no exact Bone Mask resource binding.");
            float[] dense = mask.BuildDense(profile.RigDefinition);
            if (dense.Length != context.Rig.PoseBoneCount)
                throw new InvalidOperationException(
                    $"Pose Layered Bone Blend node '{node.NodeId}' Bone Mask does not match the runtime Rig.");
            return dense;
        }

        static CharacterPoseInertializationPolicy ResolveInertializationPolicy(
            CharacterPoseCanvasNode node,
            CharacterAnimationPresentationProfile profile)
        {
            CharacterPoseResourceBinding binding =
                profile.FindPoseResourceBinding(node.InertializationPolicySlot);
            return binding?.Resource as CharacterPoseInertializationPolicy ??
                throw new InvalidOperationException(
                    $"Pose Inertialization node '{node.NodeId}' has no exact policy resource binding.");
        }
    }
}
