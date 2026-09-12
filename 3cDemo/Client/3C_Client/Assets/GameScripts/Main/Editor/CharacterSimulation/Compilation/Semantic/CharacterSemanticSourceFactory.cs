using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSemanticSourceFactory
    {
        public static CharacterSimulationSourceLocation Asset(
            CharacterAuthoringCompilationModel model,
            Object asset,
            string identity)
        {
            string guid = asset ? model.GetAssetGuid(asset) : string.Empty;
            return new CharacterSimulationSourceLocation(
                asset ? asset.GetType().FullName : "MissingAsset",
                model.DefinitionGuid,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"asset:{guid}/{identity}",
                contentHash: model.SourceRevision.Value);
        }

        public static CharacterSimulationSourceLocation Node(
            BaseTree graph,
            BaseNode node,
            string route)
        {
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                graph?.GraphAuthoringId ?? node.Owner?.GraphAuthoringId ?? string.Empty,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/node:{node.GUID}",
                contentHash: GraphAuthoringFingerprint.Compute(graph ?? node.Owner));
        }
    }
}
