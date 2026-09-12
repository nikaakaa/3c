using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;


using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation
{
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        CharacterPoseCanvasGraph ConvertGraph(
            AgentPackagePoseGraphFile graph,
            AgentPackagePoseGraphLayoutFile layout,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report,
            string path)
        {
            var nodes = new List<CharacterPoseCanvasNode>();
            foreach (AgentPackagePoseNode node in graph.nodes)
            {
                CharacterPoseCanvasNode converted = ConvertNode(
                    node,
                    graph.role,
                    presentation,
                    report,
                    path + $".nodes[{node.id}]");
                if (converted != null)
                    nodes.Add(converted);
            }
            if (report.HasErrors())
                return null;
            Dictionary<string, AgentPackagePoseNodeLayout> positions =
                Index(layout.nodes, value => value.id);
            return CharacterPoseCanvasGraph.CreateAuthoring(
                new PoseGraphId(graph.id),
                graph.contentRevision,
                graph.parameters.Select(ConvertParameter).ToArray(),
                nodes.ToArray(),
                graph.edges.Select(value => new CharacterPoseCanvasConnection(
                    value.id,
                    new PoseNodeId(value.from.node),
                    new PosePortId(value.from.port),
                    new PoseNodeId(value.to.node),
                    new PosePortId(value.to.port))).ToArray(),
                graph.nodes.Select(value =>
                {
                    AgentPackagePoseNodeLayout position = positions[value.id];
                    return new CharacterPoseGraphLayoutEntry(
                        new PoseNodeId(value.id),
                        new Vector2(position.x, position.y));
                }).ToArray(),
                ResolveAuthoringRole(graph.role));
        }

        static CharacterPoseAuthoringGraphRole ResolveAuthoringRole(string role)
        {
            return CharacterPoseGraphAuthoringCapabilities.TryResolveRole(
                    role,
                    out CharacterPoseAuthoringGraphRole resolved)
                ? resolved
                : throw new InvalidOperationException(
                    $"Pose Graph role '{role}' has no typed authoring mapping.");
        }

        CharacterPoseCanvasNode ConvertNode(
            AgentPackagePoseNode node,
            string role,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report,
            string path)
        {
            try
            {
                CharacterPoseNodeKind kind = ResolveKind(
                    node.capability,
                    new GraphAuthoringDocumentRoleId(role));
                CharacterPoseNodePayload payload =
                    CharacterPoseAuthoringPayloadCodec.Create(
                        kind,
                        new CharacterPoseAuthoringPayloadInput(
                            (field, expectedType) => ConvertProperty(
                                node.capability,
                                role,
                                field,
                                node.properties[field],
                                expectedType,
                                report,
                                path + ".properties." + field),
                            CharacterPoseAuthoringMetadata
                                .Require(kind).OperationFamily ==
                            CharacterPoseOperationFamily.StateMachine
                                ? new Func<CharacterPoseStateMachineDefinition>(() =>
                                    ConvertStateMachine(
                                        RequireStateMachine(
                                            node,
                                            presentation),
                                        presentation,
                                        report))
                                : null));
                return new CharacterPoseCanvasNode(
                    new PoseNodeId(node.id),
                    node.name,
                    payload,
                    node.dynamicPorts.Select(ConvertPort).ToArray());
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_pose_node_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static CharacterPoseParameterDeclaration ConvertParameter(
            AgentPackagePoseParameter value) =>
            new CharacterPoseParameterDeclaration(
                new PoseParameterId(value.id),
                Enum.Parse<PoseParameterValueType>(value.valueType, false),
                value.defaultValue,
                value.unit,
                Enum.Parse<CharacterPoseParameterUsage>(value.usage, false),
                value.displayName);

        static CharacterPoseDynamicPort ConvertPort(
            AgentPackagePoseDynamicPort value) =>
            CharacterPoseAuthoringPortProjection.CreateDynamicPort(
                value.id,
                value.name,
                value.valueType,
                value.direction,
                value.required,
                value.order,
                value.interfacePortId);

        static CharacterPoseStateDefinition ConvertState(
            AgentPackagePoseState value) =>
            new CharacterPoseStateDefinition(
                new PoseStateId(value.id),
                value.name,
                new PoseGraphId(value.poseGraphId),
                new PoseNodeId(value.outputPoseNodeId),
                value.alwaysResetOnEntry.Value);

        static CharacterPoseStateEntry ConvertEntry(
            AgentPackagePoseStateEntry value) =>
            new CharacterPoseStateEntry(
                new PoseStateEntryId(value.id),
                new PoseStateId(value.targetStateId));

        static CharacterPoseStateAlias ConvertAlias(
            AgentPackagePoseStateAlias value) =>
            new CharacterPoseStateAlias(
                new PoseStateAliasId(value.id),
                value.name,
                value.sources.Select(ConvertSource).ToArray());

        static CharacterPoseStateTransitionSource ConvertSource(
            AgentPackagePoseTransitionSource value) =>
            Enum.Parse<PoseStateTransitionSourceKind>(
                value.kind,
                false) == PoseStateTransitionSourceKind.State
                ? CharacterPoseStateTransitionSource.FromState(
                    new PoseStateId(value.stateId))
                : CharacterPoseStateTransitionSource.FromAlias(
                    new PoseStateAliasId(value.aliasId));

        CharacterPoseStateTransition ConvertTransition(
            AgentPackagePoseTransition value,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report) =>
            new CharacterPoseStateTransition(
                new PoseStateTransitionId(value.id),
                ConvertSource(value.source),
                new PoseStateId(value.targetStateId),
                value.priority,
                ConvertRule(value.rule),
                Enum.Parse<AnimationTransitionBlendLogic>(
                    value.blendLogic,
                    false),
                value.durationSeconds,
                Enum.Parse<CharacterAnimationBlendMode>(
                    value.blendMode,
                    false),
                ResolveBlendCurveSlot(
                    presentation,
                    value.customBlendCurveAssetId,
                    $"editable/presentation/pose-state-machines[{value.id}].customBlendCurveAssetId",
                    report),
                ResolveBlendProfileSlot(
                    presentation,
                    value.blendProfileAssetId,
                    $"editable/presentation/pose-state-machines[{value.id}].blendProfileAssetId",
                    report));

        internal CharacterPoseStateMachineDefinition
            ConvertStateMachine(
                AgentPackagePoseStateMachineFile value,
                AgentDocumentPresentationEditable presentation,
                AgentCompileReport report) =>
            new CharacterPoseStateMachineDefinition(
                new PoseStateMachineId(value.id),
                value.contentRevision,
                ConvertEntry(value.entry),
                value.states.Select(ConvertState).ToArray(),
                value.transitions
                    .Select(transition => ConvertTransition(transition, presentation, report))
                    .ToArray(),
                value.aliases.Select(ConvertAlias).ToArray(),
                value.maxTransitionsPerFrame);

        static CharacterPoseTransitionRuleGraph ConvertRule(
            AgentPackagePoseTransitionRule value) =>
            new CharacterPoseTransitionRuleGraph(
                new PoseTransitionRuleGraphId(value.id),
                value.contentRevision,
                value.operations.Select(operation =>
                    new CharacterPoseTransitionRuleOperation(
                        new PoseTransitionRuleOperationId(operation.id),
                        Enum.Parse<PoseTransitionRuleOperationKind>(
                            operation.kind,
                            false),
                        Optional(
                            operation.inputA,
                            text =>
                                new PoseTransitionRuleOperationId(text)),
                        Optional(
                            operation.inputB,
                            text =>
                                new PoseTransitionRuleOperationId(text)),
                        Optional(
                            operation.factId,
                            text => new PresentationFactId(text)),
                        operation.boolLiteral,
                        operation.floatLiteral,
                        operation.enumTypeId,
                        operation.enumLiteral,
                        operation.identityLiteral)).ToArray(),
                new PoseTransitionRuleOperationId(value.outputOperationId));

        static T Optional<T>(string value, Func<string, T> create) =>
            string.IsNullOrWhiteSpace(value) ? default : create(value);

        CharacterPresentationPoseSourceBinding ConvertSource(
            AgentPackagePoseSourceBinding value,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition profileRig,
            AgentCompileReport report)
        {
            string path =
                $"editable/presentation/profile.json.poseSources[{value.name}]";
            CharacterAnimationRigDefinition rig = profileRig;
            if (!rig)
                return null;
            try
            {
                PresentationPoseSourceKind kind =
                    Enum.Parse<PresentationPoseSourceKind>(
                        value.kind,
                        false);
                CharacterPresentationPoseSourceSlot slot = ResolveSourceSlot(
                    poseGraph,
                    value.slot,
                    kind,
                    path,
                    report);
                if (!slot)
                    return null;
                UnityEngine.Object source = Resolve(
                    value.source,
                    CharacterAnimationPresentationAuthoringService.SourceType(kind),
                    path + ".source",
                    report);
                if (!source)
                    return null;
                CharacterMotionMatchingDatabaseDefinition[] databases =
                    kind == PresentationPoseSourceKind.MotionMatching
                        ? (value.databases ?? new List<AgentPackageObjectReference>())
                            .Select((reference, index) =>
                                Resolve<CharacterMotionMatchingDatabaseDefinition>(
                                    reference,
                                    path + $".databases[{index}]",
                                    report))
                            .Where(database => database)
                            .ToArray()
                        : Array.Empty<CharacterMotionMatchingDatabaseDefinition>();
                if (kind == PresentationPoseSourceKind.MotionMatching &&
                    databases.Length != (value.databases?.Count ?? 0))
                    return null;
                CharacterPresentationPoseSourceBinding binding =
                    CharacterAnimationPresentationAuthoringService.CreateSourceBinding(
                        kind,
                        slot,
                        source,
                        rig,
                        value.footAnalysisIdentity,
                        value.searchDomainId,
                        databases);
                return RegisterLocalBinding(value.binding, binding, path, report);
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_pose_source_lower_failed",
                    exception.Message);
                return null;
            }
        }

        CharacterPoseResourceBinding[] ConvertResourceBindings(
            IReadOnlyList<AgentPackagePoseResourceBinding> values,
            CharacterPresentationPoseGraphAsset poseGraph,
            string path,
            AgentCompileReport report)
        {
            var result = new List<CharacterPoseResourceBinding>();
            foreach (AgentPackagePoseResourceBinding value in values ??
                     Array.Empty<AgentPackagePoseResourceBinding>())
            {
                try
                {
                    string slotPath =
                        path + $"[{ReferenceIdentity(value.slot)}].slot";
                    CharacterPoseResourceSlot slot;
                    bool local = !string.IsNullOrWhiteSpace(value.slot?.localId);
                    if (local)
                    {
                        m_LocalAssets.TryGetValue(
                            value.slot.localId,
                            out UnityEngine.Object localAsset);
                        slot = localAsset as CharacterPoseResourceSlot;
                        if (!slot)
                        {
                            report.Error(
                                slotPath,
                                "presentation_local_asset_unresolved",
                                $"Local Pose Resource Slot '{value.slot.localId}'没有解析到当前事务对象。");
                            continue;
                        }
                    }
                    else
                    {
                        slot = Resolve<CharacterPoseResourceSlot>(
                            value.slot,
                            slotPath,
                            report);
                    }
                    UnityEngine.Object resource = Resolve<UnityEngine.Object>(
                        value.resource,
                        path + $"[{ReferenceIdentity(value.slot)}].resource",
                        report);
                    CharacterPoseResourceKind kind = Enum.Parse<CharacterPoseResourceKind>(
                        value.kind,
                        false);
                    CharacterPoseResourceBinding binding = local
                        ? CreateTransientResourceBinding(slot, kind, resource)
                        : CharacterAnimationPresentationAuthoringService
                            .CreateResourceBinding(
                                poseGraph,
                                slot,
                                kind,
                                resource);
                    result.Add(binding);
                }
                catch (Exception exception)
                {
                    report.Error(
                        path + $"[{ReferenceIdentity(value?.slot)}]",
                        "presentation_pose_resource_lower_failed",
                        exception.Message);
                }
            }
            return result.ToArray();
        }

        static CharacterPoseResourceBinding CreateTransientResourceBinding(
            CharacterPoseResourceSlot slot,
            CharacterPoseResourceKind kind,
            UnityEngine.Object resource)
            => CharacterAnimationPresentationAuthoringService
                .CreateResourceBinding(slot, kind, resource);

        CharacterPresentationPoseSourceSlot ResolveSourceSlot(
            CharacterPresentationPoseGraphAsset poseGraph,
            AgentPackageObjectReference reference,
            PresentationPoseSourceKind kind,
            string path,
            AgentCompileReport report)
        {
            CharacterPresentationPoseSourceSlot slot;
            bool local = !string.IsNullOrWhiteSpace(reference?.localId);
            if (local)
            {
                m_LocalAssets.TryGetValue(reference.localId, out UnityEngine.Object value);
                slot = value as CharacterPresentationPoseSourceSlot;
            }
            else
            {
                slot = Resolve<CharacterPresentationPoseSourceSlot>(
                    reference,
                    path + ".slot",
                    report);
            }
            if (!slot || !local && !poseGraph.SourceSlots.Contains(slot) ||
                slot.SourceKind != kind)
            {
                report.Error(
                    path,
                    "presentation_pose_source_slot_unresolved",
                    "Pose source必须解析到当前Pose Graph中唯一且类型匹配的Source Slot对象。");
                return null;
            }
            return slot;
        }

        CharacterPresentationPoseSourceBinding RegisterLocalBinding(
            AgentPackageObjectReference reference,
            CharacterPresentationPoseSourceBinding binding,
            string path,
            AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(reference?.localId))
                return binding;
            if (m_LocalAssets.TryAdd(reference.localId, binding))
                return binding;
            report.Error(
                path + ".binding.localId",
                "presentation_local_asset_identity_duplicate",
                "Profile binding local identity重复。");
            UnityEngine.Object.DestroyImmediate(binding);
            return null;
        }
    }
}
