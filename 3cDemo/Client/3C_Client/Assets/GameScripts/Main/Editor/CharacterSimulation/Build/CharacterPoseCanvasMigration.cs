using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterPoseCanvasMigration
    {
        const string CorinPoseGraphPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset";

        [MenuItem("Tools/3C/Pose Canvas/Migrate Corin Legacy Typed Graph")]
        public static void MigrateCorin()
        {
            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                    CorinPoseGraphPath);
            if (!asset)
                throw new InvalidOperationException(
                    $"Corin Pose Graph asset '{CorinPoseGraphPath}' is missing.");

            string assetPath = AssetDatabase.GetAssetPath(asset);
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationState state =
                asset.CaptureLegacyCanvasMigrationState();
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationSource source =
                asset.CaptureLegacyCanvasMigrationSource();
            JObject sourceCanonical = CaptureLegacyCanonicalExpression(
                asset,
                source);
            HashSet<string> originalCanvasGraphIdentities =
                CaptureGraphSubassetIdentities(assetPath);
            CharacterPoseCanvasGraph[] graphs = null;
            try
            {
                graphs = asset.CreateLegacyCanvasGraphs();
                if (graphs.Length == 0)
                    throw new InvalidOperationException(
                        "Corin Pose Graph migration produced no graphs.");
                var graphIds = new HashSet<PoseGraphId>();
                foreach (CharacterPoseCanvasGraph graph in graphs)
                {
                    if (!graphIds.Add(graph.GraphId))
                        throw new InvalidOperationException(
                            $"Corin Pose Graph migration contains duplicate Graph identity '{graph.GraphId}'.");
                    CharacterPoseCanvasMutationPreflight.RequireValid(graph);
                    graph.RequireValid();
                }

                JObject convertedSemantic = CaptureCanonicalExpression(
                    asset,
                    graphs[0],
                    graphs,
                    includeNativeSerialization: false);
                if (!JToken.DeepEquals(sourceCanonical, convertedSemantic))
                    throw new InvalidOperationException(
                        "Corin Pose Graph migration changed its canonical authoring expression during Legacy conversion.");
                JObject convertedCanonical = CaptureCanonicalExpression(
                    asset,
                    graphs[0],
                    graphs,
                    includeNativeSerialization: true);
                asset.SetMigratedCanvasGraphs(
                    graphs[0],
                    graphs.Skip(1).ToArray());
                foreach (CharacterPoseCanvasGraph graph in graphs)
                {
                    graph.SelfSerialize();
                    EditorUtility.SetDirty(graph);
                }
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceSynchronousImport);

                CharacterPresentationPoseGraphAsset reloaded =
                    AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                        CorinPoseGraphPath);
                CharacterPoseCanvasGraph[] reloadedGraphs = reloaded
                    ? reloaded.EnumerateGraphs()
                        .Where(value => value)
                        .ToArray()
                    : Array.Empty<CharacterPoseCanvasGraph>();
                if (!reloaded || !reloaded.Graph ||
                    reloaded.Graph.GraphId != graphs[0].GraphId ||
                    reloadedGraphs.Length != graphs.Length)
                    throw new InvalidOperationException(
                        "Corin Pose Graph migration did not round-trip the Canvas Graph catalog.");
                var reloadedGraphIds = new HashSet<PoseGraphId>();
                foreach (CharacterPoseCanvasGraph graph in reloadedGraphs)
                {
                    if (!reloadedGraphIds.Add(graph.GraphId))
                        throw new InvalidOperationException(
                            $"Reloaded Corin Pose Graph catalog contains duplicate Graph identity '{graph.GraphId}'.");
                    CharacterPoseCanvasMutationPreflight.RequireValid(graph);
                    graph.RequireValid();
                }
                JObject actual = CaptureCanonicalExpression(
                    reloaded,
                    reloaded.Graph,
                    reloadedGraphs,
                    includeNativeSerialization: true);
                if (!JToken.DeepEquals(convertedCanonical, actual))
                    throw new InvalidOperationException(
                        "Corin Pose Graph migration changed its canonical authoring expression after reload.");
                UnityEngine.Debug.Log(
                    $"Corin Pose Graph migrated to Canvas Graph catalog: {graphs.Length} graphs.");
            }
            catch (Exception exception)
            {
                try
                {
                    Rollback(
                        assetPath,
                        state,
                        originalCanvasGraphIdentities,
                        graphs);
                }
                catch (Exception rollbackException)
                {
                    throw new InvalidOperationException(
                        $"Corin Pose Graph migration rollback failed for '{assetPath}'.",
                        new AggregateException(exception, rollbackException));
                }
                throw;
            }
        }

        static JObject CaptureCanonicalExpression(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph root,
            IEnumerable<CharacterPoseCanvasGraph> graphs,
            bool includeNativeSerialization)
        {
            if (!asset || !root)
                throw new ArgumentNullException(!asset ? nameof(asset) : nameof(root));
            CharacterPoseCanvasGraph[] values = (graphs ??
                    throw new ArgumentNullException(nameof(graphs)))
                .Where(value => value)
                .OrderBy(value => value.GraphId.Value, StringComparer.Ordinal)
                .ToArray();
            if (values.Length == 0 || !values.Contains(root))
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression has no root graph in its catalog.");
            return new JObject
            {
                ["rootGraphId"] = root.GraphId.Value,
                ["graphs"] = new JArray(values.Select(value =>
                    CaptureGraphExpression(value, includeNativeSerialization))),
                ["sourceSlots"] = CaptureSourceSlots(asset),
                ["stateMachineLayouts"] = CaptureStateMachineLayouts(asset)
            };
        }

        static JObject CaptureLegacyCanonicalExpression(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationSource source)
        {
            if (!asset || source == null || source.Root == null)
                throw new InvalidOperationException(
                    "Pose Legacy canonical expression has no root graph.");
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationGraph[] graphs =
                new[] { source.Root }
                    .Concat(source.Catalog ??
                        Array.Empty<CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationGraph>())
                    .OrderBy(value => value.GraphId.Value, StringComparer.Ordinal)
                    .ToArray();
            return new JObject
            {
                ["rootGraphId"] = source.Root.GraphId.Value,
                ["graphs"] = new JArray(graphs.Select(CaptureLegacyGraphExpression)),
                ["sourceSlots"] = CaptureSourceSlots(asset),
                ["stateMachineLayouts"] = CaptureStateMachineLayouts(asset)
            };
        }

        static JObject CaptureLegacyGraphExpression(
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationGraph graph)
        {
            if (graph == null)
                throw new InvalidOperationException(
                    "Pose Legacy canonical expression contains a missing graph.");
            return new JObject
            {
                ["graphId"] = graph.GraphId.Value,
                ["contentRevision"] = graph.ContentRevision,
                ["parameters"] = new JArray(
                    graph.Parameters
                        .OrderBy(value => value == null
                            ? string.Empty
                            : value.ParameterId.Value,
                            StringComparer.Ordinal)
                        .Select(CaptureParameter)),
                ["nodes"] = new JArray(
                    graph.Nodes
                        .OrderBy(value => value.NodeId.Value, StringComparer.Ordinal)
                        .Select(CaptureLegacyNodeExpression)),
                ["edges"] = new JArray(
                    graph.Edges
                        .OrderBy(value => value.EdgeId, StringComparer.Ordinal)
                        .Select(CaptureLegacyEdge)),
                ["layout"] = new JArray(
                    graph.Layout
                        .OrderBy(value => value.NodeId.Value, StringComparer.Ordinal)
                        .Select(CaptureLayoutEntry))
            };
        }

        static JObject CaptureGraphExpression(
            CharacterPoseCanvasGraph graph,
            bool includeNativeSerialization)
        {
            if (!graph)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing graph.");
            graph.RequireValid();
            var expression = new JObject
            {
                ["graphId"] = graph.GraphId.Value,
                ["contentRevision"] = graph.ContentRevision,
                ["parameters"] = new JArray(
                    graph.Parameters
                        .OrderBy(value => value == null
                            ? string.Empty
                            : value.ParameterId.Value,
                            StringComparer.Ordinal)
                        .Select(CaptureParameter)),
                ["nodes"] = new JArray(
                    graph.Nodes
                        .OrderBy(value => value.NodeId.Value, StringComparer.Ordinal)
                        .Select(CaptureNodeExpression)),
                ["edges"] = new JArray(
                    graph.Edges
                        .OrderBy(value => value.EdgeId, StringComparer.Ordinal)
                        .Select(CaptureEdge)),
                ["layout"] = new JArray(
                    graph.Layout
                        .OrderBy(value => value.NodeId.Value, StringComparer.Ordinal)
                        .Select(CaptureLayoutEntry))
            };
            if (includeNativeSerialization)
            {
                var references = new List<UnityEngine.Object>();
                string serialized = graph.Serialize(references);
                expression["serializedGraph"] = JToken.Parse(serialized);
                expression["serializedReferences"] = new JArray(
                    references.Select(value => CaptureAssetReference(
                        value,
                        $"graph '{graph.GraphId}' serialized reference")));
            }
            return expression;
        }

        static JObject CaptureNodeExpression(CharacterPoseCanvasNode node)
        {
            if (node == null || node.Payload == null)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains an incomplete node.");
            return CaptureNodeExpression(
                node.NodeId,
                node.DisplayName,
                node.Payload,
                node.DynamicPorts);
        }

        static JObject CaptureLegacyNodeExpression(
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationNode node)
        {
            if (node == null)
                throw new InvalidOperationException(
                    "Pose Legacy canonical expression contains an incomplete node.");
            return CaptureNodeExpression(
                node.NodeId,
                node.DisplayName,
                node.Payload,
                node.DynamicPorts);
        }

        static JObject CaptureNodeExpression(
            PoseNodeId nodeId,
            string displayName,
            CharacterPoseNodePayload payload,
            IReadOnlyList<CharacterPoseDynamicPort> dynamicPorts)
        {
            if (!nodeId.IsValid || payload == null)
                throw new InvalidOperationException(
                    "Pose canonical expression contains an incomplete node.");
            CharacterPoseCanvasNode projectionNode = new CharacterPoseCanvasNode(
                nodeId,
                displayName,
                payload,
                (dynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>()).ToArray());
            CharacterPoseCanvasDefinitionProjection definition =
                CharacterPoseCanvasDefinitionProjection.For(projectionNode);
            GraphAuthoringCapabilityDescriptor capability = definition.Capability;
            var fields = new JObject();
            foreach (GraphAuthoringFieldDescriptor field in capability.Fields
                         .OrderBy(value => value.FieldId.Value, StringComparer.Ordinal))
            {
                fields[field.FieldId.Value] =
                    CharacterPoseAuthoringPayloadCodec.EncodeValue(
                        CharacterPoseAuthoringPayloadCodec.Read(
                            payload,
                            field.FieldId.Value),
                        value => CaptureAssetReference(
                            value,
                            $"node '{nodeId}' field '{field.FieldId}'"));
            }
            return new JObject
            {
                ["nodeId"] = nodeId.Value,
                ["displayName"] = displayName,
                ["kind"] = payload.Kind.ToString(),
                ["payloadType"] = payload.GetType().FullName,
                ["capability"] = capability.CapabilityId.Value,
                ["fields"] = fields,
                ["dynamicPorts"] = new JArray(
                    (dynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>())
                        .OrderBy(value => value.PortId.Value, StringComparer.Ordinal)
                        .Select(CaptureDynamicPort)),
                ["definitionPorts"] = new JArray(
                    definition.Ports
                        .OrderBy(value => value.PortId.Value, StringComparer.Ordinal)
                        .Select(CaptureProjectedPort))
            };
        }

        static JObject CaptureDynamicPort(CharacterPoseDynamicPort port)
        {
            if (port == null)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing dynamic port.");
            return new JObject
            {
                ["portId"] = port.PortId.Value,
                ["displayName"] = port.DisplayName,
                ["valueType"] = CharacterPoseCanvasGraphDocument.ValueType(port.Kind),
                ["direction"] = port.Direction.ToString(),
                ["required"] = port.Required,
                ["order"] = port.Order,
                ["interfacePortId"] = port.InterfacePortId.Value ?? string.Empty
            };
        }

        static JObject CaptureProjectedPort(
            GraphAuthoringDynamicPortProjection port) =>
            new JObject
            {
                ["portId"] = port.PortId.Value,
                ["displayName"] = port.DisplayName,
                ["valueType"] = port.ValueTypeId,
                ["direction"] = port.Direction.ToString(),
                ["capacity"] = port.Capacity.ToString(),
                ["required"] = port.Required,
                ["order"] = port.Order,
                ["interfacePortId"] = port.InterfacePortId
            };

        static JObject CaptureEdge(CharacterPoseCanvasConnection edge)
        {
            if (edge == null)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing edge.");
            return new JObject
            {
                ["edgeId"] = edge.EdgeId,
                ["sourceNodeId"] = edge.SourceNodeId.Value,
                ["sourcePortId"] = edge.SourcePortId.Value,
                ["targetNodeId"] = edge.TargetNodeId.Value,
                ["targetPortId"] = edge.TargetPortId.Value
            };
        }

        static JObject CaptureLegacyEdge(
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationEdge edge)
        {
            if (edge == null)
                throw new InvalidOperationException(
                    "Pose Legacy canonical expression contains a missing edge.");
            return new JObject
            {
                ["edgeId"] = edge.EdgeId,
                ["sourceNodeId"] = edge.SourceNodeId.Value,
                ["sourcePortId"] = edge.SourcePortId.Value,
                ["targetNodeId"] = edge.TargetNodeId.Value,
                ["targetPortId"] = edge.TargetPortId.Value
            };
        }

        static JObject CaptureParameter(CharacterPoseParameterDeclaration parameter)
        {
            if (parameter == null)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing parameter.");
            return new JObject
            {
                ["parameterId"] = parameter.ParameterId.Value,
                ["valueType"] = parameter.ValueType.ToString(),
                ["unit"] = parameter.Unit,
                ["defaultValue"] = parameter.DefaultValue
            };
        }

        static JObject CaptureLayoutEntry(CharacterPoseGraphLayoutEntry entry)
        {
            if (entry == null)
                throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing layout entry.");
            return new JObject
            {
                ["nodeId"] = entry.NodeId.Value,
                ["position"] = CaptureVector2(entry.Position)
            };
        }

        static JObject CaptureVector2(Vector2 value) =>
            new JObject
            {
                ["x"] = value.x,
                ["y"] = value.y
            };

        static JArray CaptureSourceSlots(
            CharacterPresentationPoseGraphAsset asset)
        {
            CharacterPresentationPoseSourceSlot[] slots = asset.SourceSlots
                .Select(value => value ?? throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing source slot."))
                .OrderBy(value => value.name, StringComparer.Ordinal)
                .ThenBy(value => PersistentIdentityKey(value), StringComparer.Ordinal)
                .ToArray();
            return new JArray(slots.Select(value => new JObject
            {
                ["name"] = value.name,
                ["sourceKind"] = value.SourceKind.ToString(),
                ["asset"] = CaptureAssetReference(
                    value,
                    $"source slot '{value.name}'")
            }));
        }

        static JArray CaptureStateMachineLayouts(
            CharacterPresentationPoseGraphAsset asset)
        {
            CharacterPoseStateMachineLayout[] layouts = asset.StateMachineLayouts
                .Select(value => value ?? throw new InvalidOperationException(
                    "Pose Canvas canonical expression contains a missing StateMachine layout."))
                .OrderBy(value => value.StateMachineId.Value, StringComparer.Ordinal)
                .ToArray();
            return new JArray(layouts.Select(value => new JObject
            {
                ["stateMachineId"] = value.StateMachineId.Value,
                ["elements"] = new JArray(
                    value.Elements
                        .OrderBy(element => element?.ElementId, StringComparer.Ordinal)
                        .Select(element =>
                        {
                            if (element == null)
                                throw new InvalidOperationException(
                                    "Pose Canvas canonical expression contains a missing StateMachine layout element.");
                            return new JObject
                            {
                                ["elementId"] = element.ElementId,
                                ["position"] = CaptureVector2(element.Position)
                            };
                        }))
            }));
        }

        static JToken CaptureAssetReference(
            UnityEngine.Object asset,
            string context)
        {
            if (!asset)
                return JValue.CreateNull();
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(guid) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset,
                    out string resolvedGuid,
                    out long localFileId) ||
                !string.Equals(guid, resolvedGuid, StringComparison.Ordinal) ||
                localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"Pose Canvas canonical expression reference '{context}' is not persistent.");
            }
            return new JObject
            {
                ["assetPath"] = path,
                ["assetGuid"] = guid,
                ["localFileId"] = localFileId,
                ["type"] = asset.GetType().FullName ?? asset.GetType().Name
            };
        }

        static string PersistentIdentityKey(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrWhiteSpace(path) ||
                   string.IsNullOrWhiteSpace(guid) ||
                   !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                       asset,
                       out string resolvedGuid,
                       out long localFileId) ||
                   !string.Equals(guid, resolvedGuid, StringComparison.Ordinal) ||
                   localFileId == 0
                ? string.Empty
                : $"{guid}:{localFileId}:{asset.GetType().FullName}";
        }

        static HashSet<string> CaptureGraphSubassetIdentities(
            string assetPath) =>
            AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<CharacterPoseCanvasGraph>()
                .Select(PersistentIdentityKey)
                .Where(value => !string.IsNullOrEmpty(value))
                .ToHashSet(StringComparer.Ordinal);

        static void Rollback(
            string assetPath,
            CharacterPresentationPoseGraphAsset.LegacyCanvasMigrationState state,
            HashSet<string> originalCanvasGraphIdentities,
            CharacterPoseCanvasGraph[] transientGraphs)
        {
            CharacterPresentationPoseGraphAsset target =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                    assetPath);
            if (!target)
                throw new InvalidOperationException(
                    $"Pose asset '{assetPath}' cannot be reloaded for rollback.");
            target.RestoreLegacyCanvasMigrationState(state);
            DestroyMigrationGraphs(
                transientGraphs,
                assetPath,
                originalCanvasGraphIdentities);
            RemoveNewCanvasGraphSubassets(
                assetPath,
                originalCanvasGraphIdentities);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            target = AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                assetPath);
            if (!target)
                throw new InvalidOperationException(
                    $"Pose asset '{assetPath}' cannot be reloaded after rollback.");
            target.RequireLegacyCanvasMigrationInput();
        }

        static void RemoveNewCanvasGraphSubassets(
            string assetPath,
            HashSet<string> originalCanvasGraphIdentities)
        {
            HashSet<string> original = originalCanvasGraphIdentities ??
                new HashSet<string>(StringComparer.Ordinal);
            CharacterPoseCanvasGraph[] graphs = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<CharacterPoseCanvasGraph>()
                .ToArray();
            foreach (CharacterPoseCanvasGraph graph in graphs)
            {
                string identity = PersistentIdentityKey(graph);
                if (original.Contains(identity))
                    continue;
                AssetDatabase.RemoveObjectFromAsset(graph);
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        static void DestroyMigrationGraphs(
            IEnumerable<CharacterPoseCanvasGraph> graphs,
            string assetPath,
            HashSet<string> originalCanvasGraphIdentities)
        {
            foreach (CharacterPoseCanvasGraph graph in graphs ??
                     Array.Empty<CharacterPoseCanvasGraph>())
            {
                if (!graph || (originalCanvasGraphIdentities ??
                               new HashSet<string>(StringComparer.Ordinal))
                    .Contains(PersistentIdentityKey(graph)))
                    continue;
                string graphPath = AssetDatabase.GetAssetPath(graph);
                if (string.Equals(graphPath, assetPath, StringComparison.Ordinal))
                {
                    AssetDatabase.RemoveObjectFromAsset(graph);
                    UnityEngine.Object.DestroyImmediate(graph);
                }
                else if (string.IsNullOrEmpty(graphPath))
                    UnityEngine.Object.DestroyImmediate(graph);
            }
        }
    }
}
