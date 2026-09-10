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


namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        void BuildLinkedPosePlan(
            AgentDocumentPresentationEditable current,
            AgentDocumentPresentationEditable target,
            AgentDocumentContext context,
            CharacterAnimationPresentationProfile profile,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report,
            out IReadOnlyList<AgentLinkedPoseGraphMutationPlan> graphPlans)
        {
            var plans = new List<AgentLinkedPoseGraphMutationPlan>();
            graphPlans = plans;
            var currentImplementations = Index(
                current.linkedPoseImplementations,
                value => value.id);
            var targetImplementations = Index(
                target.linkedPoseImplementations,
                value => value.id);
            var contextInterfaces = Index(
                context?.presentation?.linkedPoseInterfaces,
                value => ReferenceIdentity(value.asset));

            foreach (AgentPackageLinkedPoseImplementationFile value in
                     target.linkedPoseImplementations
                         .OrderBy(item => item.implementationId, StringComparer.Ordinal))
            {
                string path =
                    "editable/presentation/linked-pose-implementations/" +
                    AgentAuthoringPackageMapper.Segment(value.id) +
                    "/implementation.json";
                CharacterLinkedPoseInterfaceAsset linkedInterface =
                    Resolve<CharacterLinkedPoseInterfaceAsset>(
                        value.interfaceAsset,
                        path + ".interfaceAsset",
                        report);
                string interfaceKey = ReferenceIdentity(value.interfaceAsset);
                if (!linkedInterface ||
                    !contextInterfaces.TryGetValue(
                        interfaceKey,
                        out AgentPackageLinkedPoseInterfaceFile interfaceContext) ||
                    !InterfaceContextMatches(interfaceContext, linkedInterface))
                {
                    report.Error(
                        path + ".interfaceAsset",
                        "linked_pose_interface_context_mismatch",
                        "Implementation必须引用checkout readonly context中的同一Interface revision与signature。");
                    continue;
                }
                if (!ValidateLinkedTargetEntries(
                        value,
                        linkedInterface,
                        interfaceContext,
                        path,
                        report))
                    continue;

                bool created = !string.IsNullOrWhiteSpace(value.asset?.localId);
                CharacterLinkedPoseImplementationAsset implementation;
                CharacterPresentationPoseGraphAsset graphOwner;
                AgentPackageLinkedPoseImplementationFile previous = null;
                if (created)
                {
                    if (!string.Equals(
                            value.id,
                            value.asset.localId,
                            StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(value.graphOwner?.localId))
                    {
                        report.Error(
                            path,
                            "linked_pose_local_identity_invalid",
                            "新增Implementation的object key、asset localId与Graph owner localId不完整。");
                        continue;
                    }
                    implementation = ScriptableObject.CreateInstance<
                        CharacterLinkedPoseImplementationAsset>();
                    implementation.name = value.name;
                    graphOwner = ScriptableObject.CreateInstance<
                        CharacterPresentationPoseGraphAsset>();
                    graphOwner.name = value.name + " Graphs";
                    if (!m_LocalAssets.TryAdd(value.asset.localId, implementation) ||
                        !m_LocalAssets.TryAdd(value.graphOwner.localId, graphOwner))
                    {
                        report.Error(
                            path,
                            "linked_pose_local_identity_duplicate",
                            "Implementation或Graph owner local identity重复。");
                        UnityEngine.Object.DestroyImmediate(implementation);
                        UnityEngine.Object.DestroyImmediate(graphOwner);
                        continue;
                    }
                    builder.Profile(
                        path,
                        new CreateLinkedPoseImplementationMutation(
                            profileId,
                            implementation,
                            graphOwner));
                }
                else
                {
                    implementation = Resolve<CharacterLinkedPoseImplementationAsset>(
                        value.asset,
                        path + ".asset",
                        report);
                    graphOwner = Resolve<CharacterPresentationPoseGraphAsset>(
                        value.graphOwner,
                        path + ".graphOwner",
                        report);
                    if (!implementation || !graphOwner ||
                        !profile.LinkedPoseImplementations.Contains(implementation) ||
                        !currentImplementations.TryGetValue(value.id, out previous) ||
                        !Same(previous.graphOwner, value.graphOwner) ||
                        !string.Equals(
                            previous.ownerIdentity,
                            value.ownerIdentity,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            previous.graphOwnerIdentity,
                            value.graphOwnerIdentity,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            previous.implementationId,
                            value.implementationId,
                            StringComparison.Ordinal))
                    {
                        report.Error(
                            path,
                            "linked_pose_implementation_owner_mismatch",
                            "既有Implementation必须保持Profile成员身份与同一Graph owner对象。");
                        continue;
                    }
                }

                if (!created &&
                    (value.revision < previous.revision ||
                     !SameImplementationSemantic(previous, value) &&
                     value.revision <= previous.revision))
                {
                    report.Error(
                        path + ".revision",
                        "linked_pose_implementation_revision_stale",
                        "Implementation语义变化必须显式提高revision，revision不得回退。");
                    continue;
                }

                var graphTransaction =
                    new CharacterPresentationMutationTransaction(
                        "document-linked-pose-graph-" +
                        AgentAuthoringPackageMapper.Segment(value.id),
                        "Apply Linked Pose Entry Graph Document");
                PlanBuilder graphBuilder = builder.ForGraph(graphTransaction);
                AgentDocumentPresentationEditable previousClosure =
                    LinkedClosure(previous);
                AgentDocumentPresentationEditable targetClosure =
                    LinkedClosure(value);
                BuildGraphPlan(
                    previousClosure,
                    targetClosure,
                    graphOwner,
                    value.graphOwnerIdentity,
                    graphBuilder,
                    report,
                    false);
                BuildStateMachinePlan(
                    previousClosure,
                    targetClosure,
                    graphBuilder,
                    report);

                CharacterLinkedPoseImplementationEntryMutationValue[] entries =
                    value.entries.Select(entry =>
                        new CharacterLinkedPoseImplementationEntryMutationValue(
                            new LinkedPoseEntryId(entry.entryId),
                            value.graphOwnerIdentity,
                            graphOwner,
                            new PoseGraphId(entry.graphId)))
                    .OrderBy(entry => entry.EntryId)
                    .ToArray();
                if (created || !SameImplementationHeader(previous, value))
                {
                    builder.Profile(
                        path,
                        new ConfigureLinkedPoseImplementationMutation(
                            profileId,
                            implementation,
                            value.ownerIdentity,
                            value.name,
                            new LinkedPoseImplementationId(
                                value.implementationId),
                            new LinkedPoseRevision(value.revision),
                            linkedInterface,
                            entries));
                }
                plans.Add(new AgentLinkedPoseGraphMutationPlan(
                    implementation,
                    graphOwner,
                    value.graphOwnerIdentity,
                    graphTransaction));
            }

            BuildLinkedPoseGroups(
                current.profile,
                target.profile,
                profileId,
                builder,
                report);
            BuildLinkedPoseSelectors(
                current.profile,
                target.profile,
                profile,
                profileId,
                builder,
                report);

            foreach (AgentPackageLinkedPoseImplementationFile removed in
                     current.linkedPoseImplementations
                         .Where(value => !targetImplementations.ContainsKey(value.id))
                         .OrderBy(value => value.implementationId, StringComparer.Ordinal))
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    Resolve<CharacterLinkedPoseImplementationAsset>(
                        removed.asset,
                        "editable/presentation/linked-pose-implementations/" +
                        AgentAuthoringPackageMapper.Segment(removed.id),
                        report);
                if (implementation)
                {
                    builder.Profile(
                        "editable/presentation/linked-pose-implementations/" +
                        AgentAuthoringPackageMapper.Segment(removed.id),
                        new RemoveLinkedPoseImplementationMutation(
                            profileId,
                            implementation));
                }
            }
        }

        void BuildLinkedPoseGroups(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            var oldGroups = Index(current.linkedPoseGroups, value => value.groupId);
            var newGroups = Index(target.linkedPoseGroups, value => value.groupId);
            foreach (AgentPackageLinkedPoseGroupBinding value in
                     target.linkedPoseGroups.OrderBy(
                         item => item.groupId,
                         StringComparer.Ordinal))
            {
                if (oldGroups.TryGetValue(value.groupId, out AgentPackageLinkedPoseGroupBinding previous) &&
                    Same(previous, value))
                    continue;
                CharacterLinkedPoseInterfaceAsset linkedInterface =
                    Resolve<CharacterLinkedPoseInterfaceAsset>(
                        value.interfaceAsset,
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        value.groupId + "]",
                        report);
                if (linkedInterface)
                {
                    builder.Profile(
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        value.groupId + "]",
                        new SetLinkedPoseGroupMutation(
                            profileId,
                            new CharacterLinkedPoseGroupBinding(
                                new LinkedPoseGroupId(value.groupId),
                                linkedInterface)));
                }
            }
            foreach (AgentPackageLinkedPoseGroupBinding removed in
                     current.linkedPoseGroups.Where(value =>
                         !newGroups.ContainsKey(value.groupId)))
            {
                builder.Profile(
                    "editable/presentation/profile.json.linkedPoseGroups[" +
                    removed.groupId + "]",
                    new RemoveLinkedPoseGroupMutation(
                        profileId,
                        new LinkedPoseGroupId(removed.groupId)));
            }
        }

        void BuildLinkedPoseSelectors(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            CharacterAnimationPresentationProfile profile,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            var oldSelectors = Index(current.linkedPoseSelectors, value => value.id);
            var newSelectors = Index(target.linkedPoseSelectors, value => value.id);
            foreach (AgentPackageLinkedPoseSelectorBinding value in
                     target.linkedPoseSelectors.OrderBy(
                         item => item.selectorId,
                         StringComparer.Ordinal))
            {
                string path =
                    "editable/presentation/profile.json.linkedPoseSelectors[" +
                    value.selectorId + "]";
                bool created = !string.IsNullOrWhiteSpace(value.asset?.localId);
                CharacterEquipmentLinkedPoseSelectionBinding selector;
                AgentPackageLinkedPoseSelectorBinding previous = null;
                if (created)
                {
                    if (!string.Equals(value.id, value.asset.localId, StringComparison.Ordinal))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_local_identity_invalid",
                            "新增selector的object key必须等于asset localId。");
                        continue;
                    }
                    selector = ScriptableObject.CreateInstance<
                        CharacterEquipmentLinkedPoseSelectionBinding>();
                    selector.name = value.selectorId;
                    if (!m_LocalAssets.TryAdd(value.asset.localId, selector))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_local_identity_duplicate",
                            "selector local identity重复。");
                        UnityEngine.Object.DestroyImmediate(selector);
                        continue;
                    }
                    builder.Profile(
                        path,
                        new CreateEquipmentLinkedPoseSelectorMutation(
                            profileId,
                            selector));
                }
                else
                {
                    selector = Resolve<CharacterEquipmentLinkedPoseSelectionBinding>(
                        value.asset,
                        path + ".asset",
                        report);
                    if (!selector || !profile.LinkedPoseSelectors.Contains(selector) ||
                        !oldSelectors.TryGetValue(value.id, out previous))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_owner_mismatch",
                            "既有selector必须保持Profile成员身份与稳定对象identity。");
                        continue;
                    }
                }

                CharacterEquipmentLinkedPoseMapping[] mappings =
                    (value.equipment?.mappings ??
                     new List<AgentPackageEquipmentLinkedPoseMapping>())
                    .Select(mapping => new CharacterEquipmentLinkedPoseMapping(
                        new EquipmentId(mapping.equipmentId),
                        new LinkedPoseImplementationId(mapping.implementationId)))
                    .OrderBy(mapping => mapping.EquipmentId)
                    .ToArray();
                bool envelopeChanged = created || previous == null ||
                    !string.Equals(previous.selectorId, value.selectorId, StringComparison.Ordinal) ||
                    !string.Equals(previous.groupId, value.groupId, StringComparison.Ordinal) ||
                    !string.Equals(previous.kind, value.kind, StringComparison.Ordinal) ||
                    !string.Equals(previous.equipment?.slotId, value.equipment?.slotId, StringComparison.Ordinal) ||
                    !string.Equals(
                        previous.equipment?.emptyImplementationId,
                        value.equipment?.emptyImplementationId,
                        StringComparison.Ordinal);
                if (envelopeChanged)
                {
                    builder.Profile(
                        path,
                        new ConfigureEquipmentLinkedPoseSelectorMutation(
                            profileId,
                            selector,
                            new LinkedPoseSelectorId(value.selectorId),
                            new LinkedPoseGroupId(value.groupId),
                            new EquipmentSlotId(value.equipment.slotId),
                            new LinkedPoseImplementationId(
                                value.equipment.emptyImplementationId),
                            mappings));
                    continue;
                }

                var oldMappings = Index(
                    previous.equipment.mappings,
                    mapping => mapping.equipmentId);
                var newMappings = Index(
                    value.equipment.mappings,
                    mapping => mapping.equipmentId);
                foreach (AgentPackageEquipmentLinkedPoseMapping mapping in
                         previous.equipment.mappings.Where(item =>
                             !newMappings.ContainsKey(item.equipmentId)))
                {
                    builder.Profile(
                        path + ".equipment.mappings[" + mapping.equipmentId + "]",
                        new RemoveEquipmentLinkedPoseMappingMutation(
                            profileId,
                            selector,
                            new EquipmentId(mapping.equipmentId)));
                }
                foreach (AgentPackageEquipmentLinkedPoseMapping mapping in
                         value.equipment.mappings.Where(item =>
                             !oldMappings.TryGetValue(
                                 item.equipmentId,
                                 out AgentPackageEquipmentLinkedPoseMapping old) ||
                             !Same(old, item)))
                {
                    builder.Profile(
                        path + ".equipment.mappings[" + mapping.equipmentId + "]",
                        new SetEquipmentLinkedPoseMappingMutation(
                            profileId,
                            selector,
                            new CharacterEquipmentLinkedPoseMapping(
                                new EquipmentId(mapping.equipmentId),
                                new LinkedPoseImplementationId(
                                    mapping.implementationId))));
                }
            }
            foreach (AgentPackageLinkedPoseSelectorBinding removed in
                     current.linkedPoseSelectors.Where(value =>
                         !newSelectors.ContainsKey(value.id)))
            {
                CharacterLinkedPoseSelectorBindingAsset selector =
                    Resolve<CharacterLinkedPoseSelectorBindingAsset>(
                        removed.asset,
                        "editable/presentation/profile.json.linkedPoseSelectors[" +
                        removed.selectorId + "]",
                        report);
                if (selector)
                {
                    builder.Profile(
                        "editable/presentation/profile.json.linkedPoseSelectors[" +
                        removed.selectorId + "]",
                        new RemoveLinkedPoseSelectorMutation(
                            profileId,
                            selector));
                }
            }
        }

        static AgentDocumentPresentationEditable LinkedClosure(
            AgentPackageLinkedPoseImplementationFile value) =>
            new AgentDocumentPresentationEditable
            {
                profile = new AgentPackagePresentationProfileFile(),
                poseGraphs = value?.poseGraphs ??
                             new List<AgentPackagePoseGraphFile>(),
                poseGraphLayouts = value?.poseGraphLayouts ??
                                   new List<AgentPackagePoseGraphLayoutFile>(),
                poseStateMachines = value?.poseStateMachines ??
                                    new List<AgentPackagePoseStateMachineFile>(),
                poseStateMachineLayouts = value?.poseStateMachineLayouts ??
                    new List<AgentPackagePoseStateMachineLayoutFile>()
            };

        static bool SameImplementationHeader(
            AgentPackageLinkedPoseImplementationFile left,
            AgentPackageLinkedPoseImplementationFile right) =>
            left != null && right != null &&
            string.Equals(left.name, right.name, StringComparison.Ordinal) &&
            string.Equals(left.ownerIdentity, right.ownerIdentity, StringComparison.Ordinal) &&
            string.Equals(
                left.implementationId,
                right.implementationId,
                StringComparison.Ordinal) &&
            left.revision == right.revision &&
            Same(left.interfaceAsset, right.interfaceAsset) &&
            Same(left.graphOwner, right.graphOwner) &&
            string.Equals(
                left.graphOwnerIdentity,
                right.graphOwnerIdentity,
                StringComparison.Ordinal) &&
            Same(left.entries, right.entries);

        static bool SameImplementationSemantic(
            AgentPackageLinkedPoseImplementationFile left,
            AgentPackageLinkedPoseImplementationFile right) =>
            left != null && right != null &&
            string.Equals(
                left.ownerIdentity,
                right.ownerIdentity,
                StringComparison.Ordinal) &&
            string.Equals(
                left.implementationId,
                right.implementationId,
                StringComparison.Ordinal) &&
            Same(left.interfaceAsset, right.interfaceAsset) &&
            Same(left.graphOwner, right.graphOwner) &&
            string.Equals(
                left.graphOwnerIdentity,
                right.graphOwnerIdentity,
                StringComparison.Ordinal) &&
            Same(left.entries, right.entries) &&
            Same(left.poseGraphs, right.poseGraphs) &&
            Same(left.poseStateMachines, right.poseStateMachines);

        static bool InterfaceContextMatches(
            AgentPackageLinkedPoseInterfaceFile context,
            CharacterLinkedPoseInterfaceAsset asset) =>
            context != null && asset &&
            string.Equals(
                context.interfaceId,
                asset.InterfaceId.Value,
                StringComparison.Ordinal) &&
            context.revision == asset.Revision.Value &&
            string.Equals(
                context.signatureHash,
                asset.SignatureHash.ToString(),
                StringComparison.Ordinal) &&
            string.Equals(
                context.factContractIdentity,
                asset.FactContractIdentity.ToString(),
                StringComparison.Ordinal) &&
            string.Equals(
                context.executionContract,
                asset.ExecutionContract,
                StringComparison.Ordinal);

        bool ValidateLinkedTargetEntries(
            AgentPackageLinkedPoseImplementationFile implementation,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            AgentPackageLinkedPoseInterfaceFile interfaceContext,
            string path,
            AgentCompileReport report)
        {
            HashSet<string> expected = interfaceContext.entries
                .Select(value => value.entryId)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> actual = implementation.entries
                .Select(value => value.entryId)
                .ToHashSet(StringComparer.Ordinal);
            if (!expected.SetEquals(actual))
            {
                report.Error(
                    path + ".entries",
                    "linked_pose_entry_coverage_mismatch",
                    "Implementation Entry映射必须精确覆盖Interface全部Entry。");
                return false;
            }
            var graphs = Index(implementation.poseGraphs, value => value.id);
            var layouts = Index(
                implementation.poseGraphLayouts,
                value => value.graphId);
            AgentDocumentPresentationEditable closure =
                LinkedClosure(implementation);
            bool valid = true;
            foreach (AgentPackageLinkedPoseImplementationEntry entry in
                     implementation.entries)
            {
                if (!graphs.TryGetValue(
                        entry.graphId,
                        out AgentPackagePoseGraphFile graph) ||
                    !layouts.TryGetValue(
                        entry.graphId,
                        out AgentPackagePoseGraphLayoutFile layout))
                {
                    report.Error(
                        path + $".entries[{entry.entryId}]",
                        "linked_pose_entry_graph_missing",
                        "Implementation Entry缺少同owner下的Graph或layout。");
                    valid = false;
                    continue;
                }
                CharacterPoseCanvasGraph typed = ConvertGraph(
                    graph,
                    layout,
                    closure,
                    report,
                    path + $".entries[{entry.entryId}]");
                if (typed == null)
                {
                    valid = false;
                    continue;
                }
                try
                {
                    CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                        typed,
                        linkedInterface,
                        new LinkedPoseEntryId(entry.entryId));
                }
                catch (Exception exception)
                {
                    report.Error(
                        path + $".entries[{entry.entryId}]",
                        "linked_pose_entry_signature_mismatch",
                        exception.Message);
                    valid = false;
                }
            }
            return valid;
        }
    }
}
