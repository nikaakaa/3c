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
        static string ReferenceIdentity(AgentPackageObjectReference value) =>
            value == null
                ? string.Empty
                : !string.IsNullOrWhiteSpace(value.localId)
                    ? value.localId
                    : value.assetGuid + ":" + value.localFileId;

        static AnimationProducerPresentationBinding ConvertProducer(
            AgentPackageAnimationProducerBinding value,
            AgentCompileReport report)
        {
            string path =
                $"editable/presentation/profile.json.actionProducers[{ProducerKey(value)}]";
            try
            {
                return CharacterAnimationPresentationAuthoringService
                    .CreateProducerBinding(
                        new AnimationProducerId(
                            value.timelineId,
                            value.trackId));
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_producer_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static AnimationCurve ConvertCurve(AgentPackageCurve value)
        {
            var curve = new AnimationCurve(
                value.keys.Select(key => new Keyframe(
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent,
                    key.inWeight,
                    key.outWeight)
                {
                    weightedMode = Enum.Parse<WeightedMode>(
                        key.weightedMode,
                        false)
                }).ToArray())
            {
                preWrapMode = Enum.Parse<WrapMode>(
                    value.preWrapMode,
                    false),
                postWrapMode = Enum.Parse<WrapMode>(
                    value.postWrapMode,
                    false)
            };
            return curve;
        }

        object ConvertProperty(
            string capabilityId,
            string role,
            string fieldId,
            JToken token,
            Type expectedType,
            AgentCompileReport report,
            string path)
        {
            GraphAuthoringCapabilityDescriptor capability;
            GraphAuthoringFieldDescriptor field;
            try
            {
                capability =
                    CharacterPoseGraphCapabilityProjector.Catalog.Require(
                        new GraphAuthoringCapabilityId(capabilityId),
                        CharacterPoseGraphAuthoringCapabilities.Domain,
                        new GraphAuthoringDocumentRoleId(role));
                field = capability.Fields.Single(value =>
                    string.Equals(
                        value.FieldId.Value,
                        fieldId,
                        StringComparison.Ordinal) &&
                    value.AuthoringWritable);
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_property_contract_missing",
                    exception.Message);
                return null;
            }
            try
            {
                return CharacterPoseAuthoringPayloadCodec.DecodeValue(
                    field,
                    token,
                    expectedType,
                    (descriptor, value, _) => ResolvePropertyAsset(
                        descriptor,
                        value,
                        path,
                        report));
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_property_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static AgentPackagePoseStateMachineFile RequireStateMachine(
            AgentPackagePoseNode node,
            AgentDocumentPresentationEditable presentation)
        {
            AgentPackagePoseStateMachineFile[] matches =
                presentation.poseStateMachines
                    .Where(value => string.Equals(
                        value.id,
                        node.childDocumentId,
                        StringComparison.Ordinal))
                    .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Pose node '{node.id}' must reference one StateMachine document.");
        }

        UnityEngine.Object ResolvePropertyAsset(
            GraphAuthoringFieldDescriptor field,
            JToken token,
            string path,
            AgentCompileReport report)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;
            AgentPackageObjectReference reference =
                token.ToObject<AgentPackageObjectReference>();
            Type expected = field.ObjectType ??
                throw new InvalidOperationException(
                    $"Presentation asset field '{field.FieldId}' lacks a formal ObjectType.");
            if (!string.IsNullOrWhiteSpace(reference.localId))
            {
                if (m_LocalAssets.TryGetValue(reference.localId, out UnityEngine.Object local) &&
                    local && expected.IsInstanceOfType(local))
                    return local;
                report.Error(
                    path,
                    "presentation_local_asset_unresolved",
                    $"Local对象引用'{reference.localId}'没有解析到类型匹配的事务对象。");
                return null;
            }
            return Resolve(reference, expected, path, report);
        }

        static CharacterPoseNodeKind ResolveKind(
            string capability,
            GraphAuthoringDocumentRoleId role)
        {
            CharacterPoseGraphCapabilityProjector.Catalog.Require(
                new GraphAuthoringCapabilityId(capability),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                role);
            return CharacterPoseAuthoringMetadata.RequireCapability(capability)
                .Kind;
        }

        static void Normalize(
            AgentDocumentPresentationEditable value,
            IdentityMap identities,
            AgentCompileReport report)
        {
            foreach (AgentPackageAnimationProducerBinding producer in
                     value.profile.actionProducers)
            {
                producer.timelineId = identities.Map(producer.timelineId);
                producer.trackId = identities.Map(producer.trackId);
            }
            foreach (AgentPackagePoseGraphFile graph in value.poseGraphs)
            {
                graph.id = identities.Map(graph.id);
                foreach (AgentPackagePoseParameter parameter in graph.parameters)
                {
                    parameter.id = identities.Map(parameter.id);
                    parameter.owner = identities.Map(parameter.owner);
                }
                GraphAuthoringDocumentRoleId role =
                    new GraphAuthoringDocumentRoleId(graph.role);
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    node.id = identities.Map(node.id);
                    node.childDocumentId =
                        identities.MapOptional(node.childDocumentId);
                    try
                    {
                        GraphAuthoringCapabilityDescriptor capability =
                            CharacterPoseGraphCapabilityProjector.Catalog.Require(
                                new GraphAuthoringCapabilityId(node.capability),
                                CharacterPoseGraphAuthoringCapabilities.Domain,
                                role);
                        foreach (GraphAuthoringFieldDescriptor field in
                                 capability.Fields.Where(field =>
                                     field.AuthoringWritable &&
                                     field.ValueKind ==
                                     GraphAuthoringFieldValueKind
                                         .IdentityReference))
                        {
                            JToken token = node.properties[field.FieldId.Value];
                            if (token?.Type == JTokenType.String)
                                node.properties[field.FieldId.Value] =
                                    identities.Map(token.Value<string>());
                        }
                    }
                    catch (Exception exception)
                    {
                        report.Error(
                            GraphPath(graph.id) + $".nodes[{node.id}]",
                            "presentation_capability_normalize_failed",
                            exception.Message);
                    }
                    foreach (AgentPackagePoseDynamicPort port in
                             node.dynamicPorts)
                    {
                        port.id = identities.Map(port.id);
                        port.interfacePortId =
                            identities.MapOptional(port.interfacePortId);
                    }
                }
                foreach (AgentPackagePoseEdge edge in graph.edges)
                {
                    edge.id = identities.Map(edge.id);
                    edge.from.node = identities.Map(edge.from.node);
                    edge.from.port = identities.Map(edge.from.port);
                    edge.to.node = identities.Map(edge.to.node);
                    edge.to.port = identities.Map(edge.to.port);
                }
            }
            foreach (AgentPackagePoseGraphLayoutFile layout in
                     value.poseGraphLayouts)
            {
                layout.graphId = identities.Map(layout.graphId);
                foreach (AgentPackagePoseNodeLayout node in layout.nodes)
                    node.id = identities.Map(node.id);
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     value.poseStateMachines)
            {
                machine.id = identities.Map(machine.id);
                machine.entry.id = identities.Map(machine.entry.id);
                machine.entry.targetStateId =
                    identities.Map(machine.entry.targetStateId);
                foreach (AgentPackagePoseState state in machine.states)
                {
                    state.id = identities.Map(state.id);
                    state.poseGraphId = identities.Map(state.poseGraphId);
                    state.outputPoseNodeId =
                        identities.Map(state.outputPoseNodeId);
                }
                foreach (AgentPackagePoseStateAlias alias in machine.aliases)
                {
                    alias.id = identities.Map(alias.id);
                    foreach (AgentPackagePoseTransitionSource source in
                             alias.sources)
                        Normalize(source, identities);
                }
                foreach (AgentPackagePoseTransition transition in
                         machine.transitions)
                {
                    transition.id = identities.Map(transition.id);
                    Normalize(transition.source, identities);
                    transition.targetStateId =
                        identities.Map(transition.targetStateId);
                    transition.rule.id =
                        identities.Map(transition.rule.id);
                    transition.rule.outputOperationId =
                        identities.Map(
                            transition.rule.outputOperationId);
                    foreach (AgentPackagePoseTransitionRuleOperation operation
                             in transition.rule.operations)
                    {
                        operation.id = identities.Map(operation.id);
                        operation.inputA =
                            identities.MapOptional(operation.inputA);
                        operation.inputB =
                            identities.MapOptional(operation.inputB);
                    }
                }
            }
            foreach (AgentPackagePoseStateMachineLayoutFile layout in
                     value.poseStateMachineLayouts)
            {
                layout.stateMachineId = identities.Map(
                    layout.stateMachineId);
                foreach (AgentPackagePoseStateMachineLayoutElement element in
                         layout.elements)
                    element.id = identities.Map(element.id);
            }
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     value.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                implementation.ownerIdentity = identities.MapOptional(
                    implementation.ownerIdentity);
                implementation.graphOwnerIdentity = identities.MapOptional(
                    implementation.graphOwnerIdentity);
                foreach (AgentPackageLinkedPoseImplementationEntry entry in
                         implementation.entries ??
                         new List<AgentPackageLinkedPoseImplementationEntry>())
                {
                    entry.graphId = identities.Map(entry.graphId);
                }
                var closure = new AgentDocumentPresentationEditable
                {
                    profile = new AgentPackagePresentationProfileFile(),
                    poseGraphs = implementation.poseGraphs,
                    poseGraphLayouts = implementation.poseGraphLayouts,
                    poseStateMachines = implementation.poseStateMachines,
                    poseStateMachineLayouts =
                        implementation.poseStateMachineLayouts
                };
                Normalize(closure, identities, report);
                implementation.poseGraphs = closure.poseGraphs;
                implementation.poseGraphLayouts = closure.poseGraphLayouts;
                implementation.poseStateMachines = closure.poseStateMachines;
                implementation.poseStateMachineLayouts =
                    closure.poseStateMachineLayouts;
            }
        }

        static void Normalize(
            AgentPackagePoseTransitionSource value,
            IdentityMap identities)
        {
            value.stateId = identities.MapOptional(value.stateId);
            value.aliasId = identities.MapOptional(value.aliasId);
        }

        static IEnumerable<string> CurrentIdentities(
            AgentDocumentPresentationEditable value)
        {
            foreach (AgentPackagePoseSourceBinding source in
                     value.profile?.poseSources ??
                     new List<AgentPackagePoseSourceBinding>())
            {
                yield return ReferenceIdentity(source.slot);
                yield return ReferenceIdentity(source.binding);
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     value.profile?.actionProducers ??
                     new List<AgentPackageAnimationProducerBinding>())
            {
                yield return producer.timelineId;
                yield return producer.trackId;
            }
            foreach (AgentPackagePoseGraphFile graph in value.poseGraphs)
            {
                yield return graph.id;
                foreach (AgentPackagePoseParameter parameter in graph.parameters)
                    yield return parameter.id;
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    yield return node.id;
                    foreach (AgentPackagePoseDynamicPort port in
                             node.dynamicPorts)
                        yield return port.id;
                }
                foreach (AgentPackagePoseEdge edge in graph.edges)
                    yield return edge.id;
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     value.poseStateMachines)
            {
                yield return machine.id;
                yield return machine.entry.id;
                foreach (AgentPackagePoseState state in machine.states)
                    yield return state.id;
                foreach (AgentPackagePoseStateAlias alias in machine.aliases)
                    yield return alias.id;
                foreach (AgentPackagePoseTransition transition in
                         machine.transitions)
                {
                    yield return transition.id;
                    yield return transition.rule.id;
                    foreach (AgentPackagePoseTransitionRuleOperation operation
                             in transition.rule.operations)
                        yield return operation.id;
                }
            }
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     value.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                yield return implementation.ownerIdentity;
                yield return implementation.graphOwnerIdentity;
                foreach (AgentPackageLinkedPoseImplementationEntry entry in
                         implementation.entries ??
                         new List<AgentPackageLinkedPoseImplementationEntry>())
                    yield return entry.graphId;
                var closure = new AgentDocumentPresentationEditable
                {
                    profile = new AgentPackagePresentationProfileFile(),
                    poseGraphs = implementation.poseGraphs,
                    poseGraphLayouts = implementation.poseGraphLayouts,
                    poseStateMachines = implementation.poseStateMachines,
                    poseStateMachineLayouts =
                        implementation.poseStateMachineLayouts
                };
                foreach (string identity in CurrentIdentities(closure))
                    yield return identity;
            }
        }

        static T Resolve<T>(
            AgentPackageObjectReference reference,
            string path,
            AgentCompileReport report)
            where T : UnityEngine.Object =>
            Resolve(reference, typeof(T), path, report) as T;

        void InitializeBlendCatalog(
            AgentDocumentContext context,
            CharacterAnimationRigDefinition rig,
            AgentCompileReport report)
        {
            m_Rig = rig;
            m_BlendCurveCatalog = IndexBlendAssets(
                context?.presentation?.blendCurves,
                "AnimationBlendCurve",
                "context/asset-catalog.json.animationBlendCurves",
                report);
            m_BlendProfileCatalog = IndexBlendAssets(
                context?.presentation?.blendProfiles,
                "AnimationBlendProfile",
                "context/asset-catalog.json.animationBlendProfiles",
                report);
        }

        static Dictionary<string, AgentDocumentBlendAssetContext> IndexBlendAssets(
            IEnumerable<AgentDocumentBlendAssetContext> source,
            string kind,
            string path,
            AgentCompileReport report)
        {
            var result = new Dictionary<string, AgentDocumentBlendAssetContext>(StringComparer.Ordinal);
            foreach (AgentDocumentBlendAssetContext value in
                     source ?? Array.Empty<AgentDocumentBlendAssetContext>())
            {
                if (value == null || string.IsNullOrWhiteSpace(value.id) ||
                    !string.Equals(value.kind, kind, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(value.revision) ||
                    string.IsNullOrWhiteSpace(value.assetPath) ||
                    string.IsNullOrWhiteSpace(value.assetGuid) ||
                    !result.TryAdd(value.id, value))
                {
                    report.Error(
                        path,
                        "presentation_blend_asset_catalog_invalid",
                        $"Blend资产目录包含非法或重复的{kind}条目。");
                }
            }
            return result;
        }

        CharacterAnimationBlendCurveAsset ResolveBlendCurveAsset(
            string curveId)
        {
            if (string.IsNullOrWhiteSpace(curveId))
                return null;
            if (m_BlendCurveCatalog == null ||
                !m_BlendCurveCatalog.TryGetValue(curveId, out AgentDocumentBlendAssetContext entry))
                throw new InvalidOperationException(
                    $"Custom Blend Curve asset identity '{curveId}' is not in the checkout Asset Catalog.");
            CharacterAnimationBlendCurveAsset asset = ResolveBlendAsset<CharacterAnimationBlendCurveAsset>(entry);
            asset.RequireValid();
            if (!string.Equals(asset.CurveId, curveId, StringComparison.Ordinal) ||
                !string.Equals(asset.Revision, entry.revision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Custom Blend Curve asset identity or revision '{curveId}@{entry.revision}' is stale.");
            }
            return asset;
        }

        CharacterAnimationBlendProfile ResolveBlendProfileAsset(
            string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;
            if (m_BlendProfileCatalog == null ||
                !m_BlendProfileCatalog.TryGetValue(profileId, out AgentDocumentBlendAssetContext entry))
                throw new InvalidOperationException(
                    $"Animation Blend Profile asset identity '{profileId}' is not in the checkout Asset Catalog.");
            CharacterAnimationBlendProfile asset = ResolveBlendAsset<CharacterAnimationBlendProfile>(entry);
            AnimationBlendProfilePayload payload = new AnimationBlendProfilePayload(asset, m_Rig);
            string revision = StableHash.Compute(
                AnimationBlendCanonicalPayload.ProfileKey(payload)).ToString();
            if (!string.Equals(asset.ProfileId, profileId, StringComparison.Ordinal) ||
                !string.Equals(revision, entry.revision, StringComparison.Ordinal) ||
                !string.Equals(asset.RigId, entry.rigId, StringComparison.Ordinal) ||
                !string.Equals(asset.RigRevision, entry.rigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation Blend Profile asset identity, revision or Rig '{profileId}@{entry.revision}' is stale.");
            }
            return asset;
        }

        CharacterPoseResourceSlot ResolveBlendCurveSlot(
            AgentDocumentPresentationEditable presentation,
            string curveId,
            string path,
            AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(curveId))
                return null;
            AgentDocumentBlendAssetContext entry = m_BlendCurveCatalog != null &&
                                                    m_BlendCurveCatalog.TryGetValue(curveId, out AgentDocumentBlendAssetContext value)
                ? value
                : throw new InvalidOperationException(
                    $"Custom Blend Curve asset identity '{curveId}' is not in the checkout Asset Catalog.");
            ResolveBlendCurveAsset(curveId);
            return ResolvePoseResourceSlot(
                presentation,
                entry,
                CharacterPoseResourceKind.BlendCurve,
                path,
                report);
        }

        CharacterPoseResourceSlot ResolveBlendProfileSlot(
            AgentDocumentPresentationEditable presentation,
            string profileId,
            string path,
            AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;
            AgentDocumentBlendAssetContext entry = m_BlendProfileCatalog != null &&
                                                    m_BlendProfileCatalog.TryGetValue(profileId, out AgentDocumentBlendAssetContext value)
                ? value
                : throw new InvalidOperationException(
                    $"Animation Blend Profile asset identity '{profileId}' is not in the checkout Asset Catalog.");
            ResolveBlendProfileAsset(profileId);
            return ResolvePoseResourceSlot(
                presentation,
                entry,
                CharacterPoseResourceKind.BlendProfile,
                path,
                report);
        }

        CharacterPoseResourceSlot ResolvePoseResourceSlot(
            AgentDocumentPresentationEditable presentation,
            AgentDocumentBlendAssetContext resource,
            CharacterPoseResourceKind kind,
            string path,
            AgentCompileReport report)
        {
            var matches = new List<CharacterPoseResourceSlot>();
            foreach (AgentPackagePoseResourceBinding binding in
                     presentation?.profile?.poseResources ??
                     new List<AgentPackagePoseResourceBinding>())
            {
                if (!string.Equals(binding?.kind, kind.ToString(), StringComparison.Ordinal) ||
                    !string.Equals(binding.resource?.assetGuid, resource.assetGuid, StringComparison.Ordinal))
                    continue;
                CharacterPoseResourceSlot slot;
                if (!string.IsNullOrWhiteSpace(binding.slot?.localId))
                {
                    m_LocalAssets.TryGetValue(
                        binding.slot.localId,
                        out UnityEngine.Object localAsset);
                    slot = localAsset as CharacterPoseResourceSlot;
                    if (!slot)
                    {
                        report.Error(
                            path + ".slot",
                            "presentation_local_asset_unresolved",
                            $"Local Pose Resource Slot '{binding.slot.localId}'没有解析到当前事务对象。");
                        continue;
                    }
                }
                else
                {
                    slot = Resolve<CharacterPoseResourceSlot>(
                        binding.slot,
                        path + ".slot",
                        report);
                }
                if (slot && slot.Kind == kind)
                    matches.Add(slot);
                else if (slot)
                    report.Error(
                        path + ".slot",
                        "presentation_pose_resource_slot_kind_mismatch",
                        "Blend资源对应的Resource Slot kind不匹配。");
            }
            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Pose Resource '{resource.id}' of kind '{kind}' must have exactly one Profile Resource Slot binding.");
            }
            return matches[0];
        }

        static T ResolveBlendAsset<T>(AgentDocumentBlendAssetContext entry)
            where T : UnityEngine.Object
        {
            string resolvedPath = AssetDatabase.GUIDToAssetPath(entry.assetGuid);
            if (!string.Equals(resolvedPath, entry.assetPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Blend asset '{entry.id}' path and GUID do not resolve to the same asset.");
            T asset = AssetDatabase.LoadAssetAtPath<T>(resolvedPath);
            return asset
                ? asset
                : throw new InvalidOperationException(
                    $"Blend asset '{entry.id}' is missing or not a {typeof(T).Name}.");
        }

        static T ResolveOptional<T>(
            AgentPackageObjectReference reference,
            string path,
            AgentCompileReport report)
            where T : UnityEngine.Object =>
            reference == null
                ? null
                : Resolve<T>(reference, path, report);

        static UnityEngine.Object Resolve(
            AgentPackageObjectReference reference,
            Type expected,
            string path,
            AgentCompileReport report)
        {
            if (reference == null)
                return null;
            if (!string.IsNullOrWhiteSpace(reference.localId))
            {
                report.Error(
                    path,
                    "presentation_local_asset_unresolved",
                    $"Local子资产引用'{reference.localId}'没有解析到当前事务的正式symbol。");
                return null;
            }
            string resolvedPath =
                AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (!string.Equals(
                    resolvedPath,
                    reference.assetPath,
                    StringComparison.Ordinal))
            {
                report.Error(
                    path,
                    "presentation_asset_identity_mismatch",
                    "Presentation资源的assetGuid与assetPath没有指向同一正式资产。");
                return null;
            }
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath(
                    resolvedPath)
                .FirstOrDefault(candidate =>
                    candidate && expected.IsInstanceOfType(candidate) &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        candidate,
                        out string guid,
                        out long localFileId) &&
                    string.Equals(
                        guid,
                        reference.assetGuid,
                        StringComparison.Ordinal) &&
                    localFileId == reference.localFileId);
            if (!asset)
            {
                report.Error(
                    path,
                    "presentation_asset_missing",
                    $"Presentation资源不存在或类型不是{expected.Name}。");
            }
            return asset;
        }

        static bool Matches(
            AgentPackageObjectReference reference,
            UnityEngine.Object asset,
            string guid) =>
            reference != null &&
            asset &&
            string.Equals(
                reference.assetGuid,
                guid,
                StringComparison.Ordinal) &&
            string.Equals(
                reference.assetPath,
                AssetDatabase.GetAssetPath(asset),
                StringComparison.Ordinal) &&
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                asset,
                out _,
                out long localFileId) &&
            localFileId == reference.localFileId;

        static Dictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> identity)
            where T : class =>
            (values ?? Array.Empty<T>())
            .Where(value => value != null)
            .ToDictionary(identity, StringComparer.Ordinal);

        static string ProducerKey(
            AgentPackageAnimationProducerBinding value) =>
            value.timelineId + ":" + value.trackId;

        static bool Same(object left, object right) =>
            string.Equals(
                AgentAuthoringDocumentCodec.Hash(left),
                AgentAuthoringDocumentCodec.Hash(right),
                StringComparison.Ordinal);

        static bool SameTransition(
            AgentPackagePoseTransition left,
            AgentPackagePoseTransition right)
        {
            if (left == null || right == null)
                return left == right;
            return Text(left.id, right.id) &&
                   SameSource(left.source, right.source) &&
                   Text(left.targetStateId, right.targetStateId) &&
                   left.priority == right.priority &&
                   SameRule(left.rule, right.rule) &&
                   Text(left.blendLogic, right.blendLogic) &&
                   left.durationSeconds.Equals(right.durationSeconds) &&
                   Text(left.blendMode, right.blendMode) &&
                   OptionalText(
                       left.customBlendCurveAssetId,
                       right.customBlendCurveAssetId) &&
                   OptionalText(
                       left.blendProfileAssetId,
                       right.blendProfileAssetId);
        }

        static bool SameStateStructure(
            AgentPackagePoseState left,
            AgentPackagePoseState right) =>
            left != null && right != null &&
            Text(left.id, right.id) &&
            Text(left.name, right.name) &&
            Text(left.poseGraphId, right.poseGraphId) &&
            Text(left.outputPoseNodeId, right.outputPoseNodeId);

        static bool SameSource(
            AgentPackagePoseTransitionSource left,
            AgentPackagePoseTransitionSource right) =>
            left != null && right != null &&
            Text(left.kind, right.kind) &&
            OptionalText(left.stateId, right.stateId) &&
            OptionalText(left.aliasId, right.aliasId);

        static bool SameRule(
            AgentPackagePoseTransitionRule left,
            AgentPackagePoseTransitionRule right)
        {
            if (left == null || right == null ||
                !Text(left.id, right.id) ||
                !Text(left.outputOperationId, right.outputOperationId))
                return false;
            AgentPackagePoseTransitionRuleOperation[] leftOperations =
                (left.operations ?? new List<AgentPackagePoseTransitionRuleOperation>())
                .OrderBy(value => value.id, StringComparer.Ordinal)
                .ToArray();
            AgentPackagePoseTransitionRuleOperation[] rightOperations =
                (right.operations ?? new List<AgentPackagePoseTransitionRuleOperation>())
                .OrderBy(value => value.id, StringComparer.Ordinal)
                .ToArray();
            return leftOperations.Length == rightOperations.Length &&
                   leftOperations.Zip(rightOperations, SameOperation).All(value => value);
        }

        static bool SameOperation(
            AgentPackagePoseTransitionRuleOperation left,
            AgentPackagePoseTransitionRuleOperation right) =>
            left != null && right != null &&
            Text(left.id, right.id) &&
            Text(left.kind, right.kind) &&
            OptionalText(left.inputA, right.inputA) &&
            OptionalText(left.inputB, right.inputB) &&
            OptionalText(left.factId, right.factId) &&
            left.boolLiteral == right.boolLiteral &&
            left.floatLiteral.Equals(right.floatLiteral) &&
            OptionalText(left.enumTypeId, right.enumTypeId) &&
            left.enumLiteral == right.enumLiteral &&
            OptionalText(left.identityLiteral, right.identityLiteral);

        static bool Text(string left, string right) =>
            string.Equals(left, right, StringComparison.Ordinal);

        static bool OptionalText(string left, string right) =>
            string.Equals(
                left ?? string.Empty,
                right ?? string.Empty,
                StringComparison.Ordinal);

        static bool SamePoseField(JToken left, JToken right)
        {
            if (left == null || right == null)
                return left == null && right == null;
            if (IsNumber(left.Type) && IsNumber(right.Type))
                return left.Value<float>().Equals(right.Value<float>());
            if (left is JArray leftArray && right is JArray rightArray)
            {
                return leftArray.Count == rightArray.Count &&
                       leftArray.Zip(
                               rightArray,
                               SamePoseField)
                           .All(value => value);
            }
            if (left is JObject leftObject && right is JObject rightObject)
            {
                List<JProperty> leftProperties = leftObject.Properties()
                    .OrderBy(value => value.Name, StringComparer.Ordinal)
                    .ToList();
                List<JProperty> rightProperties = rightObject.Properties()
                    .OrderBy(value => value.Name, StringComparer.Ordinal)
                    .ToList();
                return leftProperties.Count == rightProperties.Count &&
                       leftProperties.Zip(
                               rightProperties,
                               (leftProperty, rightProperty) =>
                                   string.Equals(
                                       leftProperty.Name,
                                       rightProperty.Name,
                                       StringComparison.Ordinal) &&
                                   SamePoseField(
                                       leftProperty.Value,
                                       rightProperty.Value))
                           .All(value => value);
            }
            return JToken.DeepEquals(left, right);
        }

        static bool IsNumber(JTokenType type) =>
            type == JTokenType.Integer ||
            type == JTokenType.Float;

        static string GraphPath(string id) =>
            $"editable/presentation/pose-graphs/{id}/graph.json";

        static string StateMachinePath(string id) =>
            $"editable/presentation/pose-state-machines/{id}/state-machine.json";

        static string StateMachineLayoutPath(string id) =>
            $"editable/presentation/pose-state-machines/{id}/layout.json";

        static string DescribeTransition(
            AgentPackagePoseStateMachineFile machine,
            AgentPackagePoseTransition transition,
            string action)
        {
            string source = transition.source == null
                ? "Missing Source"
                : string.Equals(transition.source.kind, "State", StringComparison.Ordinal)
                    ? StateName(machine, transition.source.stateId)
                    : AliasName(machine, transition.source.aliasId);
            string target = StateName(machine, transition.targetStateId);
            string curve = string.IsNullOrWhiteSpace(
                transition.customBlendCurveAssetId)
                ? string.Empty
                : $"; Custom Curve={transition.customBlendCurveAssetId}";
            return FormattableString.Invariant(
                $"{action} {source} -> {target}; Logic={transition.blendLogic}; Duration={transition.durationSeconds:R}s; Mode={transition.blendMode}{curve}; Profile={transition.blendProfileAssetId}");
        }

        static string StateName(
            AgentPackagePoseStateMachineFile machine,
            string id)
        {
            AgentPackagePoseState state = machine.states.FirstOrDefault(
                value => string.Equals(value.id, id, StringComparison.Ordinal));
            return state == null || string.IsNullOrWhiteSpace(state.name)
                ? id ?? string.Empty
                : state.name;
        }

        static string AliasName(
            AgentPackagePoseStateMachineFile machine,
            string id)
        {
            AgentPackagePoseStateAlias alias = machine.aliases.FirstOrDefault(
                value => string.Equals(value.id, id, StringComparison.Ordinal));
            return alias == null || string.IsNullOrWhiteSpace(alias.name)
                ? id ?? string.Empty
                : alias.name;
        }

        sealed class PlanBuilder
        {
            readonly CharacterPresentationMutationTransaction
                m_GraphTransaction;
            readonly CharacterPresentationMutationTransaction
                m_ProfileTransaction;
            readonly AgentCompileReport m_Report;
            readonly DiffSequence m_Sequence;

            public PlanBuilder(
                CharacterPresentationMutationTransaction graphTransaction,
                CharacterPresentationMutationTransaction profileTransaction,
                AgentCompileReport report,
                DiffSequence sequence = null)
            {
                m_GraphTransaction = graphTransaction;
                m_ProfileTransaction = profileTransaction;
                m_Report = report;
                m_Sequence = sequence ?? new DiffSequence();
            }

            public PlanBuilder ForGraph(
                CharacterPresentationMutationTransaction graphTransaction) =>
                new PlanBuilder(
                    graphTransaction,
                    m_ProfileTransaction,
                    m_Report,
                    m_Sequence);

            public void Graph(
                string path,
                CharacterPresentationMutation mutation,
                string detail = null)
            {
                m_GraphTransaction.Add(mutation);
                Add(path, mutation, detail);
            }

            public void Profile(
                string path,
                CharacterPresentationMutation mutation)
            {
                m_ProfileTransaction.Add(mutation);
                Add(path, mutation, null);
            }

            public void Direct(
                string action,
                string owner,
                string path,
                string detail)
            {
                m_Report.plannedDiff.Add(new AgentCompileDiffEntry
                {
                    mutationId =
                        "presentation-" +
                        m_Sequence.Index++.ToString("D4"),
                    action = action,
                    graph = owner,
                    target = path,
                    detail = detail ?? string.Empty
                });
            }

            void Add(
                string path,
                CharacterPresentationMutation mutation,
                string detail)
            {
                m_Report.plannedDiff.Add(new AgentCompileDiffEntry
                {
                    mutationId =
                        "presentation-" +
                        m_Sequence.Index++.ToString("D4"),
                    action = mutation.Kind.ToString(),
                    graph = mutation.OwnerId,
                    target = path,
                    detail = string.IsNullOrWhiteSpace(detail)
                        ? mutation.GetType().Name
                        : detail
                });
            }

            public sealed class DiffSequence
            {
                public int Index;
            }
        }

        sealed class IdentityMap
        {
            readonly string m_RootIdentity;
            readonly HashSet<string> m_Current;
            readonly Dictionary<string, string> m_Values =
                new Dictionary<string, string>(StringComparer.Ordinal);

            public IdentityMap(
                string rootIdentity,
                IEnumerable<string> current)
            {
                m_RootIdentity = rootIdentity ?? string.Empty;
                m_Current = new HashSet<string>(
                    current.Where(value => !string.IsNullOrWhiteSpace(value)),
                    StringComparer.Ordinal);
            }

            public string MapOptional(string value) =>
                string.IsNullOrWhiteSpace(value) ? string.Empty : Map(value);

            public string Map(string value)
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    !value.StartsWith("local:", StringComparison.Ordinal))
                    return value;
                if (m_Values.TryGetValue(value, out string mapped))
                    return mapped;
                mapped = AgentAuthoringDocumentCodec.Hash(
                        m_RootIdentity + "\n" + value)
                    .Substring(0, 32);
                if (!m_Current.Add(mapped) ||
                    m_Values.Values.Contains(
                        mapped,
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Presentation local identity冲突：{value}");
                }
                m_Values.Add(value, mapped);
                return mapped;
            }
        }
    }
}
