using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterLinkedPoseProjectionCompiler
    {
        public static CharacterLinkedPoseProjectionPayload Compile(
            CharacterAnimationPresentationProfile profile,
            CharacterEquipmentProfile equipmentProfile,
            List<string> errors)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!profile.RigDefinition)
            {
                errors?.Add("Linked Pose Projection requires the Presentation Rig.");
                return null;
            }
            if (profile.LinkedPoseGroups.Count == 0 && profile.LinkedPoseImplementations.Count == 0 && profile.LinkedPoseSelectors.Count == 0)
            {
                CharacterTypedPoseNode strayCall = profile.PoseGraph?.Graph?.Nodes
                    .FirstOrDefault(value => value?.Kind == CharacterPoseNodeKind.LinkedPoseCall);
                if (strayCall != null)
                {
                    errors?.Add($"Linked Pose Call '{strayCall.NodeId}' has no Profile Group, selector or Implementation catalog.");
                    return null;
                }
                return new CharacterLinkedPoseProjectionPayload(
                    profile.RigDefinition.RigId,
                    profile.RigDefinition.Revision,
                    Array.Empty<CharacterLinkedPoseInterfaceProjectionDescriptor>(),
                    Array.Empty<CharacterLinkedPoseGroupProjectionDescriptor>(),
                    Array.Empty<CharacterLinkedPoseCompiledSelectorDescriptor>(),
                    Array.Empty<CharacterEquipmentLinkedPoseSelectorDescriptor>(),
                    Array.Empty<CharacterLinkedPoseImplementationProjectionDescriptor>(),
                    Array.Empty<CharacterLinkedPoseCallProjectionDescriptor>());
            }

            try
            {
                var groups = profile.LinkedPoseGroups.ToDictionary(value => value.GroupId);
                var selectors = profile.LinkedPoseSelectors
                    .Cast<ICharacterLinkedPoseSelectorAuthoring>()
                    .ToDictionary(value => value.GroupId);
                CharacterLinkedPoseInterfaceProjectionDescriptor[] interfaceDescriptors = groups.Values
                    .Select(value => value.Interface)
                    .Distinct()
                    .OrderBy(value => value.InterfaceId)
                    .Select(value => new CharacterLinkedPoseInterfaceProjectionDescriptor(value))
                    .ToArray();
                CharacterLinkedPoseImplementationProjectionDescriptor[] implementationDescriptors = profile.LinkedPoseImplementations
                    .OrderBy(value => value.ImplementationId)
                    .Select(value => new CharacterLinkedPoseImplementationProjectionDescriptor(value, profile.RigDefinition))
                    .ToArray();
                CharacterLinkedPoseCompiledSelectorDescriptor[] selectorDescriptors = selectors.Values
                    .OrderBy(value => value.GroupId)
                    .Select(value => value.CompileCore(groups[value.GroupId]))
                    .ToArray();
                var selectorDescriptorsByGroup = selectorDescriptors.ToDictionary(value => value.GroupId);
                CharacterLinkedPoseGroupProjectionDescriptor[] groupDescriptors = groups.Values
                    .OrderBy(value => value.GroupId)
                    .Select(value => new CharacterLinkedPoseGroupProjectionDescriptor(value, selectorDescriptorsByGroup[value.GroupId]))
                    .ToArray();
                CharacterEquipmentLinkedPoseSelectorDescriptor[] equipmentSelectors = profile.LinkedPoseSelectors
                    .OfType<CharacterEquipmentLinkedPoseSelectionBinding>()
                    .OrderBy(value => value.GroupId)
                    .Select(value => CompileEquipmentSelector(value, groups[value.GroupId], equipmentProfile))
                    .ToArray();
                CharacterLinkedPoseCallProjectionDescriptor[] calls = CompileCalls(
                    profile.PoseGraph.Graph,
                    groups);
                ValidateEntryGraphs(profile.LinkedPoseImplementations);
                return new CharacterLinkedPoseProjectionPayload(
                    profile.RigDefinition.RigId,
                    profile.RigDefinition.Revision,
                    interfaceDescriptors,
                    groupDescriptors,
                    selectorDescriptors,
                    equipmentSelectors,
                    implementationDescriptors,
                    calls);
            }
            catch (Exception exception)
            {
                errors?.Add($"Linked Pose Projection is invalid: {exception.Message}");
                return null;
            }
        }

        static CharacterLinkedPoseCallProjectionDescriptor[] CompileCalls(
            CharacterTypedPoseGraph rootGraph,
            IReadOnlyDictionary<LinkedPoseGroupId, CharacterLinkedPoseGroupBinding> groups)
        {
            if (rootGraph == null)
                throw new ArgumentNullException(nameof(rootGraph));
            var calls = new List<CharacterLinkedPoseCallProjectionDescriptor>();
            var callByEntry = new Dictionary<string, PoseNodeId>(StringComparer.Ordinal);
            for (int nodeIndex = 0; nodeIndex < rootGraph.Nodes.Count; nodeIndex++)
            {
                CharacterTypedPoseNode node = rootGraph.Nodes[nodeIndex];
                if (node?.Payload is not CharacterLinkedPoseCallPayload payload)
                    continue;
                if (!groups.TryGetValue(payload.GroupId, out CharacterLinkedPoseGroupBinding group))
                    throw new InvalidOperationException($"Linked Pose Call '{node.NodeId}' references unknown Group '{payload.GroupId}'.");
                if (payload.InterfaceId != group.Interface.InterfaceId)
                    throw new InvalidOperationException($"Linked Pose Call '{node.NodeId}' Interface '{payload.InterfaceId}' does not match Group '{payload.GroupId}'.");
                CharacterLinkedPosePortProjection.RequireCallMatch(node, group.Interface);
                string key = payload.GroupId.Value + "\0" + payload.EntryId.Value;
                if (callByEntry.TryGetValue(key, out PoseNodeId duplicate))
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.DuplicateCall,
                        $"Group '{payload.GroupId}' Entry '{payload.EntryId}' is called by both '{duplicate}' and '{node.NodeId}'."));
                callByEntry.Add(key, node.NodeId);
                calls.Add(new CharacterLinkedPoseCallProjectionDescriptor(node.NodeId, group, payload.EntryId));
            }
            foreach (CharacterLinkedPoseGroupBinding group in groups.Values)
            {
                for (int entryIndex = 0; entryIndex < group.Interface.Entries.Count; entryIndex++)
                {
                    LinkedPoseEntryId entryId = group.Interface.Entries[entryIndex].EntryId;
                    string key = group.GroupId.Value + "\0" + entryId.Value;
                    if (!callByEntry.ContainsKey(key))
                        throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                            CharacterLinkedPoseDiagnosticCode.MissingEntry,
                            $"Group '{group.GroupId}' Interface '{group.Interface.InterfaceId}' requires a root Call for missing Entry '{entryId}'."));
                }
            }
            return calls
                .OrderBy(value => value.GroupId)
                .ThenBy(value => value.EntryId)
                .ThenBy(value => value.NodeId)
                .ToArray();
        }

        static void ValidateEntryGraphs(
            IReadOnlyList<CharacterLinkedPoseImplementationAsset> implementations)
        {
            var entriesByOwner = new Dictionary<CharacterPresentationPoseGraphAsset, HashSet<PoseGraphId>>();
            for (int implementationIndex = 0; implementationIndex < implementations.Count; implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation = implementations[implementationIndex];
                implementation.RequireValid();
                for (int entryIndex = 0; entryIndex < implementation.Entries.Count; entryIndex++)
                {
                    CharacterLinkedPoseImplementationEntryBinding entry = implementation.Entries[entryIndex];
                    if (!entriesByOwner.TryGetValue(entry.GraphOwner, out HashSet<PoseGraphId> entryGraphs))
                    {
                        entryGraphs = new HashSet<PoseGraphId>();
                        entriesByOwner.Add(entry.GraphOwner, entryGraphs);
                    }
                    entryGraphs.Add(entry.GraphId);
                    CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                        entry.RequireValid(),
                        implementation.Interface,
                        entry.EntryId);
                }
            }
            foreach (KeyValuePair<CharacterPresentationPoseGraphAsset, HashSet<PoseGraphId>> owner in entriesByOwner)
            {
                IReadOnlyList<string> capabilityErrors = CharacterPoseGraphCapabilityValidator.Validate(owner.Key, owner.Value);
                if (capabilityErrors.Count != 0)
                    throw new InvalidOperationException(string.Join("\n", capabilityErrors));
            }
        }

        static CharacterEquipmentLinkedPoseSelectorDescriptor CompileEquipmentSelector(
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            CharacterLinkedPoseGroupBinding group,
            CharacterEquipmentProfile equipmentProfile)
        {
            if (!equipmentProfile)
                throw new InvalidOperationException($"Equipment Linked Pose selector '{selector.SelectorId}' requires an Equipment Profile.");
            CharacterEquipmentLinkedPoseInterface.RequireFormalContract(group.Interface);
            if (!equipmentProfile.Slots.Any(value => value != null && value.SlotId == selector.SlotId))
                throw new InvalidOperationException($"Equipment Linked Pose selector '{selector.SelectorId}' references unknown Slot '{selector.SlotId}'.");

            EquipmentId[] expected = equipmentProfile.Equipment
                .Where(value => value && value.SlotId == selector.SlotId)
                .Select(value => value.EquipmentId)
                .OrderBy(value => value)
                .ToArray();
            EquipmentId[] actual = selector.Mappings
                .Where(value => value != null)
                .Select(value => value.EquipmentId)
                .OrderBy(value => value)
                .ToArray();
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.MissingMapping,
                    $"Equipment selector '{selector.SelectorId}' does not exactly cover Slot '{selector.SlotId}' Equipment closure."));
            return selector.Compile(group);
        }
    }
}
