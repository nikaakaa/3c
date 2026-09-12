using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPoseGraphAuthoringMaintenance
    {
        [MenuItem("3C/Character/Animation/Pose Graph/Normalize Locomotion Full Body Inputs")]
        public static void NormalizeLocomotionFullBodyInputs()
        {
            string result = NormalizeAssetAtPath(
                "Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset");
            UnityEngine.Debug.Log(result);
        }

        public static string NormalizeAssetAtPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("Pose Graph asset path is missing.", nameof(assetPath));
            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(assetPath);
            if (!asset)
                throw new InvalidOperationException($"Pose Graph asset '{assetPath}' is missing.");
            return Normalize(asset, assetPath);
        }

        public static string Normalize(
            CharacterPresentationPoseGraphAsset asset,
            string assetPath = null)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            assetPath = string.IsNullOrWhiteSpace(assetPath)
                ? AssetDatabase.GetAssetPath(asset)
                : assetPath;
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new InvalidOperationException("Pose Graph asset must be persistent.");

            CharacterPoseCanvasGraph[] graphs = asset.EnumerateGraphs()
                .Where(value => value != null)
                .ToArray();
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Normalize Pose Graph Inputs");
            int removedDeclarations = 0;
            int removedPorts = 0;
            int removedGets = 0;

            foreach (CharacterPoseCanvasGraph graph in graphs)
            {
                var removedEndpoints = new HashSet<string>(StringComparer.Ordinal);
                var removePorts = new List<RemoveDynamicPosePortMutation>();
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    foreach (CharacterPoseDynamicPort port in node.DynamicPorts
                                 .Where(value => value != null &&
                                                 value.InterfacePortId ==
                                                 CharacterPoseInternalInterfacePortIds.FootPlacementWeight))
                    {
                        if (node.Kind != CharacterPoseNodeKind.GraphInput &&
                            node.Kind != CharacterPoseNodeKind.PoseSubgraph)
                        {
                            throw new InvalidOperationException(
                                $"Pose Graph '{graph.GraphId}' exposes the internal Foot Placement port on unsupported node '{node.NodeId}'.");
                        }
                        removePorts.Add(new RemoveDynamicPosePortMutation(
                            graph.GraphId.Value,
                            node.NodeId,
                            port.PortId));
                        removedEndpoints.Add(node.NodeId.Value + "\0" + port.PortId.Value);
                    }
                }

                CharacterPoseCanvasConnection[] remainingEdges = graph.Edges
                    .Where(edge => !removedEndpoints.Contains(
                        edge.SourceNodeId.Value + "\0" + edge.SourcePortId.Value) &&
                        !removedEndpoints.Contains(
                            edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value))
                    .ToArray();
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (!(node.Payload is CharacterProgramParameterInputPosePayload parameter) ||
                        !parameter.ParameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight))
                        continue;
                    if (remainingEdges.Any(edge =>
                            edge.SourceNodeId == node.NodeId ||
                            edge.TargetNodeId == node.NodeId))
                    {
                        throw new InvalidOperationException(
                            $"Pose Graph '{graph.GraphId}' still uses the internal Foot Placement Get '{node.NodeId}' outside the removable boundary.");
                    }
                    transaction.Add(new DeletePoseNodeMutation(
                        graph.GraphId.Value,
                        node.NodeId));
                    removedGets++;
                }

                foreach (RemoveDynamicPosePortMutation mutation in removePorts)
                {
                    transaction.Add(mutation);
                    removedPorts++;
                }

                CharacterPoseParameterDeclaration[] parameters = graph.Parameters
                    .Where(value => value != null && !IsRetiredDeclaration(value.ParameterId))
                    .ToArray();
                if (parameters.Length != graph.Parameters.Count)
                {
                    transaction.Add(new SetPoseGraphParametersMutation(
                        graph.GraphId.Value,
                        parameters));
                    removedDeclarations += graph.Parameters.Count - parameters.Length;
                }
            }

            if (transaction.Mutations.Count != 0)
            {
                new CharacterPresentationMutationService().Apply(
                    new CharacterPoseGraphAssetMutationOwner(asset),
                    transaction);
            }

            int removedOrphanGraphs = RemoveOrphanGraphSubAssets(asset, assetPath);
            AssetDatabase.SaveAssetIfDirty(asset);
            return $"Pose Graph '{assetPath}' normalized: declarations={removedDeclarations}, ports={removedPorts}, gets={removedGets}, orphanGraphs={removedOrphanGraphs}.";
        }

        static bool IsRetiredDeclaration(PoseParameterId parameterId) =>
            parameterId.Equals(AnimationPoseParameterIds.ActionWeight) ||
            parameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight) ||
            parameterId.Value.StartsWith("animation.blendshape.", StringComparison.Ordinal);

        static int RemoveOrphanGraphSubAssets(
            CharacterPresentationPoseGraphAsset asset,
            string assetPath)
        {
            CharacterPoseCanvasGraph[] referenced = asset.EnumerateGraphs()
                .Where(value => value != null)
                .ToArray();
            var referencedObjects = new HashSet<CharacterPoseCanvasGraph>(referenced);
            var referencedIds = new HashSet<string>(
                referenced.Select(value => value.GraphId.Value),
                StringComparer.Ordinal);
            CharacterPoseCanvasGraph[] orphans = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<CharacterPoseCanvasGraph>()
                .Where(value => !referencedObjects.Contains(value) &&
                                referencedIds.Contains(value.GraphId.Value))
                .ToArray();
            foreach (CharacterPoseCanvasGraph orphan in orphans)
                Undo.DestroyObjectImmediate(orphan);
            if (orphans.Length != 0)
                EditorUtility.SetDirty(asset);
            return orphans.Length;
        }
    }
}
