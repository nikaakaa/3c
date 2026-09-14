using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterLinkedPoseAuthoringService
    {
        public static CharacterLinkedPoseInterfaceAsset CreateInterface(
            CharacterAnimationPresentationProfile profile,
            string displayName)
        {
            RequireProfile(profile);
            string interfaceId = NewIdentity("character.linked-pose.interface");
            var linkedInterface = ScriptableObject.CreateInstance<CharacterLinkedPoseInterfaceAsset>();
            linkedInterface.name = RequireDisplayName(displayName, "Linked Pose Interface");
            var entries = new[]
            {
                new CharacterLinkedPoseInterfaceEntryDescriptor(
                    new LinkedPoseEntryId("pose"),
                    CharacterPoseExecutionDomain.PurePose,
                    new[]
                    {
                        new CharacterLinkedPoseInterfacePortDescriptor(
                            new PoseInterfacePortId("input.pose"),
                            CharacterPosePortDirection.Input,
                            CharacterPosePortKind.LocalPose,
                            CharacterPoseSpace.Local,
                            true,
                            0),
                        new CharacterLinkedPoseInterfacePortDescriptor(
                            new PoseInterfacePortId("output.pose"),
                            CharacterPosePortDirection.Output,
                            CharacterPosePortKind.LocalPose,
                            CharacterPoseSpace.Local,
                            true,
                            1)
                    })
            };
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Create Linked Pose Interface");
            string profileId = RequireAssetOwnerId(profile);
            transaction.Add(new CreateLinkedPoseInterfaceMutation(profileId, linkedInterface));
            transaction.Add(new ConfigureLinkedPoseInterfaceMutation(
                profileId,
                linkedInterface,
                $"linked-pose-interface/{interfaceId}",
                linkedInterface.name,
                new LinkedPoseInterfaceId(interfaceId),
                new LinkedPoseRevision(1),
                entries));
            ApplyProfile(profile, profileId, transaction, linkedInterface);
            return linkedInterface;
        }

        public static CharacterLinkedPoseGroupBinding CreateGroup(
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            string displayName)
        {
            RequireProfile(profile);
            linkedInterface?.RequireValid();
            if (!linkedInterface)
                throw new ArgumentNullException(nameof(linkedInterface));
            string groupId = NewIdentity("character.linked-pose.group");
            var binding = new CharacterLinkedPoseGroupBinding(
                new LinkedPoseGroupId(groupId),
                linkedInterface);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                $"Create Linked Pose Group: {RequireDisplayName(displayName, "Group")}");
            transaction.Add(new SetLinkedPoseGroupMutation(
                RequireAssetOwnerId(profile),
                binding));
            ApplyProfile(profile, RequireAssetOwnerId(profile), transaction);
            return binding;
        }

        public static CharacterLinkedPoseImplementationAsset CreateImplementation(
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            string displayName,
            bool emptyTemplate,
            CharacterLinkedPoseImplementationAsset source = null)
        {
            RequireProfile(profile);
            linkedInterface?.RequireValid();
            if (!linkedInterface)
                throw new ArgumentNullException(nameof(linkedInterface));
            if (source && (source.Interface != linkedInterface || source.IsStale))
                throw new InvalidOperationException("Copied Linked Pose Implementation must match the current Interface and revision.");
            string implementationId = NewIdentity("character.linked-pose.implementation");
            string ownerIdentity = $"linked-pose-implementation/{implementationId}";
            string graphOwnerIdentity = $"linked-pose-graph/{implementationId}";
            var implementation = ScriptableObject.CreateInstance<CharacterLinkedPoseImplementationAsset>();
            implementation.name = RequireDisplayName(displayName, emptyTemplate ? "Empty Linked Pose Implementation" : "Linked Pose Implementation");
            var graphOwner = ScriptableObject.CreateInstance<CharacterPresentationPoseGraphAsset>();
            graphOwner.name = $"{implementation.name} Entry Graphs";
            var entries = new List<CharacterLinkedPoseImplementationEntryMutationValue>();
            for (int index = 0; index < linkedInterface.Entries.Count; index++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry = linkedInterface.Entries[index];
                if (entry == null)
                    throw new InvalidOperationException($"Linked Pose Interface '{linkedInterface.InterfaceId}' contains a missing Entry.");
                PoseGraphId graphId = new PoseGraphId($"{implementationId}.{entry.EntryId.Value}");
                CharacterPoseCanvasGraph graph = source && !emptyTemplate
                    ? CloneEntryGraph(source, entry.EntryId, graphId)
                    : BuildEntryGraph(linkedInterface, entry, graphId, emptyTemplate);
                if (index == 0)
                    graphOwner.SetGraph(graph);
                else
                    graphOwner.AddGraph(graph);
                entries.Add(new CharacterLinkedPoseImplementationEntryMutationValue(
                    entry.EntryId,
                    graphOwnerIdentity,
                    graphOwner,
                    graphId));
            }
            string profileId = RequireAssetOwnerId(profile);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                emptyTemplate ? "Create Empty Linked Pose Implementation" : "Create Linked Pose Implementation");
            transaction.Add(new CreateLinkedPoseImplementationMutation(
                profileId,
                implementation,
                graphOwner));
            transaction.Add(new ConfigureLinkedPoseImplementationMutation(
                profileId,
                implementation,
                ownerIdentity,
                implementation.name,
                new LinkedPoseImplementationId(implementationId),
                new LinkedPoseRevision(1),
                linkedInterface,
                entries.ToArray()));
            ApplyProfile(profile, profileId, transaction, implementation, graphOwner);
            return implementation;
        }

        public static CharacterEquipmentLinkedPoseSelectionBinding CreateEquipmentSelector(
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseGroupBinding group,
            EquipmentSlotId slotId,
            LinkedPoseImplementationId emptyImplementationId)
        {
            RequireProfile(profile);
            group?.RequireValid();
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (!slotId.IsValid || !emptyImplementationId.IsValid)
                throw new ArgumentException("Equipment selector requires a Slot and Empty Implementation.");
            CharacterLinkedPoseImplementationAsset emptyImplementation =
                profile.LinkedPoseImplementations.FirstOrDefault(value =>
                    value && value.ImplementationId == emptyImplementationId);
            if (!emptyImplementation || emptyImplementation.Interface != group.Interface)
                throw new InvalidOperationException("Equipment selector Empty Implementation must belong to the selected Group Interface.");
            var selector = ScriptableObject.CreateInstance<CharacterEquipmentLinkedPoseSelectionBinding>();
            selector.name = $"{group.GroupId} Equipment Selection";
            string selectorId = NewIdentity("character.linked-pose.selector.equipment");
            selector.Configure(
                new LinkedPoseSelectorId(selectorId),
                group.GroupId,
                slotId,
                emptyImplementationId,
                Array.Empty<CharacterEquipmentLinkedPoseMapping>());
            string profileId = RequireAssetOwnerId(profile);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Create Equipment Linked Pose Selector");
            transaction.Add(new CreateEquipmentLinkedPoseSelectorMutation(profileId, selector));
            transaction.Add(new ConfigureEquipmentLinkedPoseSelectorMutation(
                profileId,
                selector,
                new LinkedPoseSelectorId(selectorId),
                group.GroupId,
                slotId,
                emptyImplementationId,
                Array.Empty<CharacterEquipmentLinkedPoseMapping>()));
            ApplyProfile(profile, profileId, transaction, selector);
            return selector;
        }

        public static void RebindCall(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset graphOwner,
            PoseGraphId graphId,
            PoseNodeId nodeId,
            CharacterLinkedPoseGroupBinding group,
            LinkedPoseEntryId entryId)
        {
            RequireProfile(profile);
            if (!graphOwner || !graphId.IsValid)
                throw new ArgumentException("Linked Pose Call graph context is incomplete.");
            group?.RequireValid();
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            CharacterLinkedPoseInterfaceEntryDescriptor entry = group.Interface.RequireEntry(entryId);
            var payload = new CharacterLinkedPoseCallPayload(
                group.GroupId,
                group.Interface.InterfaceId,
                entry.EntryId);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Rebind Linked Pose Call");
            transaction.Add(new ConfigureLinkedPoseCallMutation(
                graphId.Value,
                nodeId,
                payload,
                CharacterLinkedPosePortProjection.CreateCallPorts(group.Interface, entry.EntryId)));
            new CharacterPresentationMutationService().Apply(
                new CharacterPoseGraphAssetMutationOwner(graphOwner, profile),
                transaction);
        }

        public static int CreateMissingRequiredCalls(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset graphOwner)
        {
            RequireProfile(profile);
            if (!graphOwner || graphOwner.Graph == null)
                throw new ArgumentException("Root Pose Graph is missing.", nameof(graphOwner));
            if (profile.PoseGraph != graphOwner)
                throw new InvalidOperationException("Missing Call creation only targets the Profile root Pose Graph.");
            var existing = new HashSet<string>(
                graphOwner.Graph.Nodes
                    .Select(value => value?.Payload)
                    .OfType<CharacterLinkedPoseCallPayload>()
                    .Select(value => $"{value.GroupId}|{value.EntryId}"),
                StringComparer.Ordinal);
            var missing = new List<(CharacterLinkedPoseGroupBinding Group, CharacterLinkedPoseInterfaceEntryDescriptor Entry)>();
            foreach (CharacterLinkedPoseGroupBinding group in profile.LinkedPoseGroups.Where(value => value != null))
            {
                group.RequireValid();
                foreach (CharacterLinkedPoseInterfaceEntryDescriptor entry in group.Interface.Entries.Where(value => value != null))
                    if (existing.Add($"{group.GroupId}|{entry.EntryId}"))
                        missing.Add((group, entry));
            }
            if (missing.Count == 0)
                return 0;
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Create Missing Linked Pose Calls");
            int index = 0;
            foreach ((CharacterLinkedPoseGroupBinding group, CharacterLinkedPoseInterfaceEntryDescriptor entry) value in missing)
            {
                var payload = new CharacterLinkedPoseCallPayload(
                    value.group.GroupId,
                    value.group.Interface.InterfaceId,
                    value.entry.EntryId);
                var nodeId = new PoseNodeId($"linked-pose-call.{Guid.NewGuid():N}");
                transaction.Add(new CreatePoseNodeMutation(
                    graphOwner.Graph.GraphId.Value,
                    new CharacterPoseCanvasNode(
                        nodeId,
                        $"{value.group.GroupId} / {value.entry.EntryId}",
                        payload,
                        CharacterLinkedPosePortProjection.CreateCallPorts(
                            value.group.Interface,
                            value.entry.EntryId)),
                    new Vector2(-240f, index++ * 180f)));
            }
            new CharacterPresentationMutationService().Apply(
                new CharacterPoseGraphAssetMutationOwner(graphOwner, profile),
                transaction);
            return missing.Count;
        }

        public static string RequireAssetOwnerId(UnityEngine.Object owner)
        {
            if (!owner)
                throw new ArgumentNullException(nameof(owner));
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner));
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"Asset '{owner.name}' must be saved before authoring Linked Pose data.");
            return guid;
        }

        public static bool TryResolveProfile(
            UnityEngine.Object target,
            out CharacterAnimationPresentationProfile profile)
        {
            profile = null;
            if (!target)
                return false;
            if (target is CharacterAnimationPresentationProfile direct)
            {
                profile = direct;
                return true;
            }
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationPresentationProfile");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterAnimationPresentationProfile candidate =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(path);
                if (!candidate)
                    continue;
                if (candidate.LinkedPoseGroups.Any(value => value?.Interface == target) ||
                    candidate.LinkedPoseImplementations.Any(value => value == target) ||
                    candidate.LinkedPoseSelectors.Any(value => value == target) ||
                    EnumerateInterfaces(candidate).Contains(target as CharacterLinkedPoseInterfaceAsset))
                {
                    profile = candidate;
                    return true;
                }
            }
            return false;
        }

        public static IReadOnlyList<CharacterLinkedPoseInterfaceAsset> EnumerateInterfaces(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile)
                return Array.Empty<CharacterLinkedPoseInterfaceAsset>();
            return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(profile))
                .OfType<CharacterLinkedPoseInterfaceAsset>()
                .Where(value => value)
                .OrderBy(value => value.name, StringComparer.Ordinal)
                .ToArray();
        }

        public static void OpenWorkspace(UnityEngine.Object target)
        {
            if (!TryResolveProfile(target, out CharacterAnimationPresentationProfile profile) ||
                !profile.PoseGraph)
            {
                EditorUtility.DisplayDialog(
                    "Linked Pose Workspace",
                    "找不到拥有该 Linked Pose 资产的 Animation Presentation Profile。",
                    "确定");
                return;
            }
            CharacterPipelineDefinition definition = null;
            CharacterPresentationProjectionAsset projection = null;
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterPipelineDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterPipelineDefinition candidate =
                    AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(path);
                if (!candidate || candidate.AnimationPresentationProfile != profile)
                    continue;
                definition = candidate;
                projection = candidate.PresentationProjection;
                break;
            }
            CharacterPoseGraphWorkspace window =
                CharacterPoseGraphWorkspace.Open(
                    profile.PoseGraph,
                    profile,
                    projection,
                    definition);
            window.ShowLinkedPoseAsset(target);
        }

        static CharacterPoseCanvasGraph BuildEntryGraph(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            CharacterLinkedPoseInterfaceEntryDescriptor entry,
            PoseGraphId graphId,
            bool emptyTemplate)
        {
            PoseNodeId inputId = new PoseNodeId($"{entry.EntryId}.input");
            PoseNodeId outputId = new PoseNodeId($"{entry.EntryId}.output");
            CharacterPoseDynamicPort[] inputPorts =
                CharacterLinkedPosePortProjection.CreateGraphInputPorts(linkedInterface, entry.EntryId);
            CharacterPoseDynamicPort[] outputPorts =
                CharacterLinkedPosePortProjection.CreateGraphOutputPorts(linkedInterface, entry.EntryId);
            var nodes = new List<CharacterPoseCanvasNode>
            {
                new CharacterPoseCanvasNode(
                    inputId,
                    "Graph Input",
                    new CharacterGraphInputPosePayload(),
                    inputPorts),
                new CharacterPoseCanvasNode(
                    outputId,
                    "Graph Output",
                    new CharacterGraphOutputPosePayload(),
                    outputPorts)
            };
            var edges = new List<CharacterPoseCanvasConnection>();
            if (emptyTemplate)
                AddEmptyTemplateBody(entry, inputId, outputId, inputPorts, outputPorts, nodes, edges);
            return CharacterPoseCanvasGraph.CreateAuthoring(
                graphId,
                $"{graphId}/v1",
                Array.Empty<CharacterPoseParameterDeclaration>(),
                nodes.ToArray(),
                edges.ToArray(),
                nodes.Select((value, index) => new CharacterPoseGraphLayoutEntry(
                    value.NodeId,
                    index == 0 ? new Vector2(-360f, 0f) : new Vector2(360f, 0f))).ToArray());
        }

        static CharacterPoseCanvasGraph CloneEntryGraph(
            CharacterLinkedPoseImplementationAsset source,
            LinkedPoseEntryId entryId,
            PoseGraphId graphId)
        {
            CharacterLinkedPoseImplementationEntryBinding binding = source.Entries
                .FirstOrDefault(value => value != null && value.EntryId == entryId);
            if (binding == null || !binding.GraphOwner)
                throw new InvalidOperationException($"Source Implementation '{source.ImplementationId}' is missing Entry '{entryId}'.");
            CharacterPoseCanvasGraph current = binding.GraphOwner.RequireGraph(binding.GraphId);
            return CharacterPoseCanvasGraph.CreateAuthoring(
                graphId,
                $"{graphId}/v1",
                current.Parameters.ToArray(),
                current.Nodes.ToArray(),
                current.Edges.ToArray(),
                current.Layout.ToArray());
        }

        static void AddEmptyTemplateBody(
            CharacterLinkedPoseInterfaceEntryDescriptor entry,
            PoseNodeId inputId,
            PoseNodeId outputId,
            IReadOnlyList<CharacterPoseDynamicPort> inputPorts,
            IReadOnlyList<CharacterPoseDynamicPort> outputPorts,
            List<CharacterPoseCanvasNode> nodes,
            List<CharacterPoseCanvasConnection> edges)
        {
            CharacterLinkedPoseInterfacePortDescriptor input = entry.Ports
                .FirstOrDefault(value => value != null && value.Direction == CharacterPosePortDirection.Input);
            CharacterLinkedPoseInterfacePortDescriptor output = entry.Ports
                .FirstOrDefault(value => value != null && value.Direction == CharacterPosePortDirection.Output);
            if (input == null || output == null)
                return;
            CharacterPoseDynamicPort inputPort = inputPorts.FirstOrDefault(value => value.Kind == input.Kind);
            CharacterPoseDynamicPort outputPort = outputPorts.FirstOrDefault(value => value.Kind == output.Kind);
            if (inputPort == null || outputPort == null)
                return;
            if (input.Kind == CharacterPosePortKind.LocalPose &&
                output.Kind == CharacterPosePortKind.LocalPose)
            {
                edges.Add(new CharacterPoseCanvasConnection(
                    $"{entry.EntryId}.passthrough",
                    inputId,
                    inputPort.PortId,
                    outputId,
                    outputPort.PortId));
                return;
            }
        }

        static void ApplyProfile(
            CharacterAnimationPresentationProfile profile,
            string profileId,
            CharacterPresentationMutationTransaction transaction,
            params UnityEngine.Object[] transientObjects)
        {
            try
            {
                new CharacterPresentationMutationService().Apply(
                    new CharacterPresentationProfileMutationOwner(profile, profileId),
                    transaction);
            }
            catch
            {
                foreach (UnityEngine.Object value in transientObjects ?? Array.Empty<UnityEngine.Object>())
                    if (value && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(value)))
                        UnityEngine.Object.DestroyImmediate(value);
                throw;
            }
        }

        static void RequireProfile(CharacterAnimationPresentationProfile profile)
        {
            if (!profile || !profile.PoseGraph)
                throw new InvalidOperationException("Linked Pose authoring requires a saved Profile with a Pose Graph.");
        }

        static string NewIdentity(string prefix) =>
            $"{prefix}.{Guid.NewGuid():N}";

        static string RequireDisplayName(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
