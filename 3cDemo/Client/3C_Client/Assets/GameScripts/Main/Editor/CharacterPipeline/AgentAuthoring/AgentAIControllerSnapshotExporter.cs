using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.AI.Editor;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAIControllerSnapshotExporter
    {
        public AgentGraphSnapshot Export(AIControllerDefinition definition, AgentSnapshotExportMode mode)
        {
            string definitionPath = definition ? AssetDatabase.GetAssetPath(definition) : string.Empty;
            var snapshot = new AgentGraphSnapshot
            {
                domain = AgentAuthoringSchema.AIControllerDomain,
                exportMode = mode.ToString(),
                definitionName = definition ? definition.name : string.Empty,
                definitionAssetPath = definitionPath,
                rootAssetPath = definitionPath,
                rootIdentity = definition ? definition.ControllerId : string.Empty,
                rootTreeAssetPath = definition?.RootTreeAsset ? AssetDatabase.GetAssetPath(definition.RootTreeAsset) : string.Empty
            };
            if (!definition || definition.RootTreeAsset?.Tree is not AIControllerTree root)
                return snapshot;
            root.RebindReadOnlyViewReferences();
            snapshot.rootGraphAuthoringId = root.GraphAuthoringId;
            snapshot.aiController = ExportController(definition, root);
            snapshot.sourceRevision = snapshot.aiController.sourceRevision;
            ExportGraph(root, mode, snapshot);
            return snapshot;
        }

        static AgentSnapshotAIController ExportController(AIControllerDefinition definition, AIControllerTree root)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string treePath = AssetDatabase.GetAssetPath(definition.RootTreeAsset);
            string perceptionPath = definition.PerceptionProfile ? AssetDatabase.GetAssetPath(definition.PerceptionProfile) : string.Empty;
            string characterPath = definition.ControlledCharacter ? AssetDatabase.GetAssetPath(definition.ControlledCharacter) : string.Empty;
            var result = new AgentSnapshotAIController
            {
                controllerId = definition.ControllerId,
                definitionAssetPath = definitionPath,
                definitionAssetGuid = AssetDatabase.AssetPathToGUID(definitionPath),
                treeAssetPath = treePath,
                treeAssetGuid = AssetDatabase.AssetPathToGUID(treePath),
                graphAuthoringId = root.GraphAuthoringId,
                authoringRole = root.AuthoringRole.ToString(),
                perceptionAssetPath = perceptionPath,
                perceptionAssetGuid = AssetDatabase.AssetPathToGUID(perceptionPath),
                candidateOrdering = definition.PerceptionProfile ? definition.PerceptionProfile.Ordering.ToString() : string.Empty,
                controlledCharacterAssetPath = characterPath,
                controlledCharacterAssetGuid = AssetDatabase.AssetPathToGUID(characterPath)
            };
            if (definition.PerceptionProfile)
                result.candidateActorIds.AddRange(definition.PerceptionProfile.CandidateActorIds);
            if (definition.ControlledCharacter?.SimulationProgram && definition.PerceptionProfile)
            {
                CharacterSimulationProgram characterProgram = definition.ControlledCharacter.SimulationProgram.Load();
                result.characterProgramId = characterProgram.Manifest.ProgramId.Value;
                result.characterProgramHash = characterProgram.ProgramHash.ToString();
                ExportInputCatalog(characterProgram, result);
                var candidates = result.candidateActorIds.Select(value => new ActorId(value)).ToArray();
                var perception = new AIPerceptionDescriptor(candidates, definition.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
                result.sourceRevision = AIControllerSourceRevision.Compute(definition, characterProgram.Manifest.ProgramId, characterProgram.ProgramHash, perception.SchemaHash);
            }
            for (int i = 0; i < root.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = root.ExposedProperties[i];
                if (declaration == null)
                    continue;
                result.blackboardDeclarations.Add(new AgentSnapshotAIBlackboardDeclaration
                {
                    declarationAuthoringId = declaration.DeclarationId,
                    ownerGraphAuthoringId = declaration.DeclarationOwnerId,
                    displayName = declaration.BlackboardKey,
                    valueType = declaration.ValueType?.FullName ?? string.Empty,
                    scope = declaration.BlackboardScope.ToString(),
                    lifetime = declaration.BlackboardLifetime.ToString(),
                    authority = declaration.BlackboardAuthority.ToString(),
                    syncPolicy = declaration.BlackboardSyncPolicy.ToString(),
                    defaultValue = Convert.ToString(declaration.GetValue(), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                });
            }
            var capabilityCatalog =
                new BtsmtlGraphAuthoringCapabilities();
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                BaseNode node = root.Nodes[i];
                if (node == null)
                    continue;
                if (capabilityCatalog.TryGetAnchor(node.GetType().FullName, out _))
                    continue;
                NodeAuthoringCapabilityPolicy.TryGetCapability(node.GetType(), out NodeAuthoringCapability capability);
                var entry = new AgentSnapshotAINode
                {
                    graphAuthoringId = root.GraphAuthoringId,
                    nodeAuthoringId = node.GUID,
                    nodeType = node.GetType().FullName,
                    capability = capability.ToString()
                };
                switch (node)
                {
                    case ReadAIMemoryNode read:
                        entry.memoryDeclarationAuthoringId = read.BlackboardVariable.DeclarationId;
                        entry.memoryValueKind = read.ValueKind.ToString();
                        break;
                    case WriteAIMemoryNode write:
                        entry.memoryDeclarationAuthoringId = write.BlackboardVariable.DeclarationId;
                        entry.memoryValueKind = write.ValueKind.ToString();
                        break;
                    case WriteContinuousInputNode continuous:
                        entry.inputId = continuous.InputId;
                        break;
                    case WriteActionTargetSnapshotNode target:
                        entry.inputId = target.InputId;
                        break;
                    case SubmitActionRequestNode request:
                        entry.requestId = request.RequestId;
                        entry.requestBufferSeconds = request.BufferSeconds;
                        entry.requestPriority = request.Priority;
                        entry.requestRepeatPolicy = request.RepeatPolicy.ToString();
                        break;
                }
                result.nodes.Add(entry);
            }
            if (definition.IntentProgram)
            {
                string programPath = AssetDatabase.GetAssetPath(definition.IntentProgram);
                result.intentProgramAssetPath = programPath;
                result.intentProgramAssetGuid = AssetDatabase.AssetPathToGUID(programPath);
                result.intentProgramId = definition.IntentProgram.ProgramId;
                result.intentProgramHash = definition.IntentProgram.ProgramHash;
                result.intentProgramSourceRevision = definition.IntentProgram.SourceRevision;
                result.intentProgramStale = !AIIntentProgramBuildService.IsCurrent(definition, out _);
            }
            else
            {
                result.intentProgramStale = true;
            }
            return result;
        }

        static void ExportGraph(AIControllerTree root, AgentSnapshotExportMode mode, AgentGraphSnapshot snapshot)
        {
            ExportGraph(root, mode, snapshot, "root", AgentGraphOwnership.RootAsset, string.Empty,
                string.Empty, string.Empty, new HashSet<BaseTree>());
        }

        static void ExportGraph(
            BaseTree tree,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot,
            string path,
            AgentGraphOwnership ownership,
            string ownerElementAuthoringId,
            string referenceKey,
            string sharedAssetPath,
            HashSet<BaseTree> exported)
        {
            if (tree == null || !exported.Add(tree))
                return;

            tree.CheckInit();
            string kind = (tree is ConditionRuleGraph ? AgentGraphKind.ConditionRuleGraph : AgentGraphKind.BaseTree).ToString();
            var graph = new AgentSnapshotGraph
            {
                graphAuthoringId = tree.GraphAuthoringId,
                path = path,
                name = tree.name,
                kind = kind,
                ownership = ownership.ToString(),
                ownerElementAuthoringId = ownerElementAuthoringId,
                referenceKey = referenceKey,
                sharedAssetPath = sharedAssetPath
            };
            snapshot.graphs.Add(graph);
            snapshot.graphSummaries.Add(new AgentSnapshotGraphSummary
            {
                graphAuthoringId = tree.GraphAuthoringId,
                path = path,
                name = tree.name,
                kind = kind,
                ownership = ownership.ToString(),
                ownerNode = ownerElementAuthoringId,
                referenceKey = referenceKey
            });

            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;
                var entry = new AgentSnapshotNode
                {
                    elementAuthoringId = node.GUID,
                    typeName = node.GetType().FullName,
                    displayName = node.ResolvedDisplayName,
                    nodeTypeDisplayName = node.NodeTypeDisplayName,
                    position = new AgentSnapshotVector2 { x = node.Position.x, y = node.Position.y }
                };
                if (node is LoopNode loopNode)
                    entry.loopStopType = loopNode.LoopStopType.ToString();
                else if (node is CompareNode compareNode)
                    entry.compareType = compareNode.Comparison.ToString();
                if (mode == AgentSnapshotExportMode.Full)
                {
                    foreach (PropertyPort port in node.PropertyPortMap.Values.OrderBy(value => value.PortId, StringComparer.Ordinal))
                    {
                        entry.propertyPorts.Add(new AgentSnapshotPropertyPort
                        {
                            portId = port.PortId,
                            displayName = port.DisplayName,
                            direction = port.Direction.ToString(),
                            valueType = port.ValueType?.FullName ?? string.Empty
                        });
                    }
                }
                graph.nodes.Add(entry);
            }

            for (int i = 0; i < tree.Edges.Count; i++)
            {
                BaseEdge edge = tree.Edges[i];
                if (edge == null)
                    continue;

                ConditionRuleGraph condition = edge.ConditionRuleGraph;
                string conditionPath = condition
                    ? $"{path}/ConditionRule:Edge:{edge.GUID}/conditionRule/{condition.GraphAuthoringId}"
                    : string.Empty;
                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = edge.GUID,
                    startElementAuthoringId = edge.StartNodeGUID,
                    endElementAuthoringId = edge.EndNodeGUID,
                    startPort = edge.StartPortName,
                    endPort = edge.EndPortName,
                    flowOrder = edge.FlowOrder,
                    transitionPriority = edge.TransitionPriority,
                    abortPolicy = edge.AbortPolicy.ToString(),
                    conditionRuleGraphAuthoringId = condition ? condition.GraphAuthoringId : string.Empty,
                    conditionRuleGraphPath = conditionPath
                });
                if (condition)
                {
                    ExportGraph(condition, mode, snapshot, conditionPath, AgentGraphOwnership.Inline, edge.GUID,
                        "conditionRule", string.Empty, exported);
                }
            }

            for (int i = 0; i < tree.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = tree.PropertyEdges[i];
                if (edge == null)
                    continue;
                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = edge.GUID,
                    startElementAuthoringId = edge.StartNodeGUID,
                    endElementAuthoringId = edge.EndNodeGUID,
                    startPortId = edge.StartPortName,
                    endPortId = edge.EndPortName
                });
            }
        }

        static void ExportInputCatalog(CharacterSimulationProgram program, AgentSnapshotAIController result)
        {
            foreach (ProgramCatalogEntry entry in program.CatalogEntries.OrderBy(value => value.Identity, StringComparer.Ordinal))
            {
                if (entry.Kind == ProgramCatalogEntryKind.InputValue)
                {
                    result.inputValues.Add(new AgentSnapshotInputValue
                    {
                        inputValueId = StripCatalogPrefix(entry.Identity, "input:value:"),
                        valueType = ReadCatalogEnum<ProgramInputValueKind>(program, entry, "ValueType").ToString()
                    });
                }
                else if (entry.Kind == ProgramCatalogEntryKind.InputRequest)
                {
                    result.actionRequests.Add(new AgentSnapshotActionRequest
                    {
                        requestId = StripCatalogPrefix(entry.Identity, "input:request:"),
                        timingClass = ReadCatalogEnum<CharacterActionRequestTimingClass>(program, entry, "TimingClass").ToString()
                    });
                }
            }
        }

        static T ReadCatalogEnum<T>(CharacterSimulationProgram program, ProgramCatalogEntry entry, string fieldName) where T : struct, Enum
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, fieldName, StringComparison.Ordinal) || field.Kind != ProgramCatalogFieldKind.Constant)
                    continue;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                object candidate = Enum.ToObject(typeof(T), value.Int32);
                if (value.Kind == ProgramConstantKind.Int32 && Enum.IsDefined(typeof(T), candidate))
                    return (T)candidate;
            }
            throw new InvalidOperationException($"Catalog entry '{entry.Identity}' lacks {fieldName}.");
        }

        static string StripCatalogPrefix(string value, string prefix)
        {
            return value.StartsWith(prefix, StringComparison.Ordinal) ? value.Substring(prefix.Length) : value;
        }
    }
}
