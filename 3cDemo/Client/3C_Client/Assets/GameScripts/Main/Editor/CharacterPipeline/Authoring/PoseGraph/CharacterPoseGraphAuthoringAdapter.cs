using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPresentationProfileMutationOwner : ICharacterPresentationMutationOwner
    {
        readonly CharacterAnimationPresentationProfile m_Profile;
        readonly string m_ProfileId;

        public CharacterPresentationProfileMutationOwner(
            CharacterAnimationPresentationProfile profile,
            string profileId)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_ProfileId = string.IsNullOrWhiteSpace(profileId)
                ? throw new ArgumentException("Presentation Profile identity is missing.", nameof(profileId))
                : profileId.Trim();
        }

        public UnityEngine.Object SerializedOwner => m_Profile;

        public CharacterTypedPoseGraph RequirePoseGraph(string graphId) =>
            m_Profile.PoseGraph
                ? m_Profile.PoseGraph.RequireGraph(new PoseGraphId(graphId))
                : throw new InvalidOperationException(
                    $"Presentation Profile '{m_ProfileId}' has no Pose Graph.");

        public void ReplacePoseGraph(CharacterTypedPoseGraph graph) =>
            throw new InvalidOperationException(
                "Presentation Profile owner cannot mutate Pose Graph content.");

        public void ApplyGraphCatalogMutation(
            CharacterPresentationMutation mutation) =>
            throw new InvalidOperationException(
                "Presentation Profile owner cannot mutate the Pose Graph catalog.");

        public void ApplyStateMachineMutation(CharacterPresentationMutation mutation) =>
            throw new InvalidOperationException(
                "Presentation Profile owner cannot mutate Pose StateMachine content.");

        public void ApplyProfileMutation(CharacterPresentationMutation mutation)
        {
            if (mutation == null ||
                !string.Equals(mutation.OwnerId, m_ProfileId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Presentation Profile mutation does not target '{m_ProfileId}'.");
            }
            switch (mutation)
            {
                case SetPresentationGraphMutation graph:
                    m_Profile.SetPresentationGraph(graph.PoseGraph, graph.Rig);
                    break;
                case SetMotionMatchingProfileMutation motionMatching:
                    m_Profile.SetMotionMatchingProfile(motionMatching.Profile);
                    break;
                case SetFootPlacementAnalysisMutation footPlacement:
                    m_Profile.SetFootPlacementAnalysis(
                        footPlacement.Mode,
                        footPlacement.SourceAssetGuid);
                    break;
                case CreateProfileSourceBindingMutation source:
                    SetSourceBinding(source.Binding, false);
                    break;
                case SetProfileSourceBindingMutation source:
                    SetSourceBinding(source.Binding, true);
                    break;
                case RenameProfileSourceBindingMutation source:
                    RenameSourceBinding(source.Binding, source.DisplayName);
                    break;
                case RemoveProfileSourceBindingMutation source:
                    RemoveSourceBinding(source.Binding);
                    break;
                case SetProfileProducerBindingMutation producer:
                {
                    List<AnimationProducerPresentationBinding> bindings =
                        m_Profile.ProducerBindings
                            .Where(value => value != null &&
                                            !value.ProducerId.Equals(
                                                producer.Binding.ProducerId))
                            .ToList();
                    bindings.Add(producer.Binding);
                    m_Profile.SetProducerBindings(
                        bindings
                            .OrderBy(
                                value => value.ProducerId.TimelineAuthoringId,
                                StringComparer.Ordinal)
                            .ThenBy(
                                value => value.ProducerId.TrackAuthoringId,
                                StringComparer.Ordinal)
                            .ToArray());
                    break;
                }
                case RemoveProfileProducerBindingMutation producer:
                    m_Profile.SetProducerBindings(
                        m_Profile.ProducerBindings
                            .Where(value => value != null &&
                                            !value.ProducerId.Equals(
                                                producer.ProducerId))
                            .OrderBy(
                                value => value.ProducerId.TimelineAuthoringId,
                                StringComparer.Ordinal)
                            .ThenBy(
                                value => value.ProducerId.TrackAuthoringId,
                                StringComparer.Ordinal)
                            .ToArray());
                    break;
                case CreateLinkedPoseImplementationMutation implementation:
                    CreateLinkedPoseImplementation(
                        implementation.Implementation,
                        implementation.GraphOwner);
                    break;
                case ConfigureLinkedPoseImplementationMutation implementation:
                    RequireLinkedPoseImplementation(implementation.Implementation);
                    Undo.RegisterCompleteObjectUndo(
                        implementation.Implementation,
                        "Configure Linked Pose Implementation");
                    implementation.Implementation.name =
                        implementation.DisplayName;
                    implementation.Implementation.Configure(
                        implementation.OwnerIdentity,
                        implementation.ImplementationId,
                        implementation.Revision,
                        implementation.Interface,
                        implementation.Entries.Select(entry =>
                            new CharacterLinkedPoseImplementationEntryBinding(
                                entry.EntryId,
                                entry.GraphOwnerIdentity,
                                entry.GraphOwner,
                                entry.GraphId)).ToArray());
                    EditorUtility.SetDirty(implementation.Implementation);
                    break;
                case RemoveLinkedPoseImplementationMutation implementation:
                    RemoveLinkedPoseImplementation(implementation.Implementation);
                    break;
                case CreateLinkedPoseInterfaceMutation linkedInterface:
                    CreateLinkedPoseInterface(linkedInterface.Interface);
                    break;
                case ConfigureLinkedPoseInterfaceMutation linkedInterface:
                    ConfigureLinkedPoseInterface(linkedInterface);
                    break;
                case RemoveLinkedPoseInterfaceMutation linkedInterface:
                    RemoveLinkedPoseInterface(linkedInterface.Interface);
                    break;
                case SetLinkedPoseGroupMutation group:
                    SetLinkedPoseGroup(group.Binding);
                    break;
                case RemoveLinkedPoseGroupMutation group:
                    RemoveLinkedPoseGroup(group.GroupId);
                    break;
                case CreateEquipmentLinkedPoseSelectorMutation selector:
                    CreateLinkedPoseSelector(selector.Selector);
                    break;
                case ConfigureEquipmentLinkedPoseSelectorMutation selector:
                    RequireLinkedPoseSelector(selector.Selector);
                    Undo.RegisterCompleteObjectUndo(
                        selector.Selector,
                        "Configure Equipment Linked Pose Selector");
                    selector.Selector.Configure(
                        selector.SelectorId,
                        selector.GroupId,
                        selector.SlotId,
                        selector.EmptyImplementationId,
                        selector.Mappings.ToArray());
                    EditorUtility.SetDirty(selector.Selector);
                    break;
                case RemoveLinkedPoseSelectorMutation selector:
                    RemoveLinkedPoseSelector(selector.Selector);
                    break;
                case SetEquipmentLinkedPoseMappingMutation mapping:
                    SetEquipmentLinkedPoseMapping(mapping.Selector, mapping.Mapping);
                    break;
                case RemoveEquipmentLinkedPoseMappingMutation mapping:
                    RemoveEquipmentLinkedPoseMapping(
                        mapping.Selector,
                        mapping.EquipmentId);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Mutation '{mutation.Kind}' is not owned by the Presentation Profile surface.");
            }
        }

        void CreateLinkedPoseImplementation(
            CharacterLinkedPoseImplementationAsset implementation,
            CharacterPresentationPoseGraphAsset graphOwner)
        {
            if (m_Profile.LinkedPoseImplementations.Contains(implementation))
                throw new InvalidOperationException(
                    $"Linked Pose Implementation '{implementation.name}' already belongs to the Profile.");
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(implementation)) ||
                !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graphOwner)))
                throw new InvalidOperationException(
                    "New Linked Pose Implementation and graph owner must be transient objects.");
            Undo.RegisterCreatedObjectUndo(
                graphOwner,
                "Create Linked Pose Graph Owner");
            AssetDatabase.AddObjectToAsset(graphOwner, m_Profile);
            Undo.RegisterCreatedObjectUndo(
                implementation,
                "Create Linked Pose Implementation");
            AssetDatabase.AddObjectToAsset(implementation, m_Profile);
            m_Profile.SetLinkedPoseBindings(
                m_Profile.LinkedPoseGroups.ToArray(),
                m_Profile.LinkedPoseImplementations.Append(implementation)
                    .ToArray(),
                m_Profile.LinkedPoseSelectors.ToArray());
            EditorUtility.SetDirty(graphOwner);
            EditorUtility.SetDirty(implementation);
        }

        void CreateLinkedPoseInterface(
            CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            if (!linkedInterface ||
                !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(linkedInterface)))
                throw new InvalidOperationException(
                    "New Linked Pose Interface must be a transient object.");
            Undo.RegisterCreatedObjectUndo(
                linkedInterface,
                "Create Linked Pose Interface");
            AssetDatabase.AddObjectToAsset(linkedInterface, m_Profile);
            EditorUtility.SetDirty(linkedInterface);
        }

        void ConfigureLinkedPoseInterface(
            ConfigureLinkedPoseInterfaceMutation mutation)
        {
            if (!mutation.Interface ||
                !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mutation.Interface)) &&
                !AssetDatabase.GetAssetPath(mutation.Interface).Equals(
                    AssetDatabase.GetAssetPath(m_Profile),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Linked Pose Interface must belong to the current Profile or be transient.");
            Undo.RegisterCompleteObjectUndo(
                mutation.Interface,
                "Configure Linked Pose Interface");
            mutation.Interface.name = mutation.DisplayName;
            mutation.Interface.Configure(
                mutation.OwnerIdentity,
                mutation.InterfaceId,
                mutation.Revision,
                mutation.Entries.ToArray());
            EditorUtility.SetDirty(mutation.Interface);
        }

        void RemoveLinkedPoseInterface(
            CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            if (!linkedInterface ||
                !IsOwnedSubAsset(linkedInterface))
                throw new InvalidOperationException(
                    "Linked Pose Interface does not belong to this Profile.");
            if (m_Profile.LinkedPoseGroups.Any(value =>
                    value != null && value.Interface == linkedInterface))
                throw new InvalidOperationException(
                    $"Linked Pose Interface '{linkedInterface.InterfaceId}' is still used by a Group.");
            if (m_Profile.LinkedPoseImplementations.Any(value =>
                    value != null && value.Interface == linkedInterface))
                throw new InvalidOperationException(
                    $"Linked Pose Interface '{linkedInterface.InterfaceId}' is still used by an Implementation.");
            if (m_Profile.PoseGraph && m_Profile.PoseGraph.EnumerateGraphs()
                    .SelectMany(value => value.Nodes)
                    .Any(value => value?.Payload is CharacterLinkedPoseCallPayload call &&
                                  call.InterfaceId == linkedInterface.InterfaceId))
                throw new InvalidOperationException(
                    $"Linked Pose Interface '{linkedInterface.InterfaceId}' is still used by a root Call.");
            Undo.DestroyObjectImmediate(linkedInterface);
        }

        bool IsOwnedSubAsset(UnityEngine.Object value) =>
            value &&
            string.Equals(
                AssetDatabase.GetAssetPath(value),
                AssetDatabase.GetAssetPath(m_Profile),
                StringComparison.Ordinal);

        void RemoveLinkedPoseImplementation(
            CharacterLinkedPoseImplementationAsset implementation)
        {
            RequireLinkedPoseImplementation(implementation);
            if (m_Profile.LinkedPoseSelectors.Any(selector =>
                    selector is CharacterEquipmentLinkedPoseSelectionBinding equipmentSelector &&
                    (equipmentSelector.EmptyImplementationId == implementation.ImplementationId ||
                     equipmentSelector.CandidateImplementationIds.Contains(implementation.ImplementationId))))
                throw new InvalidOperationException(
                    $"Linked Pose Implementation '{implementation.ImplementationId}' is still used by a selector.");
            CharacterPresentationPoseGraphAsset[] graphOwners = implementation
                .Entries
                .Where(value => value != null && value.GraphOwner)
                .Select(value => value.GraphOwner)
                .Distinct()
                .ToArray();
            m_Profile.SetLinkedPoseBindings(
                m_Profile.LinkedPoseGroups.ToArray(),
                m_Profile.LinkedPoseImplementations
                    .Where(value => value != implementation)
                    .ToArray(),
                m_Profile.LinkedPoseSelectors.ToArray());
            Undo.DestroyObjectImmediate(implementation);
            foreach (CharacterPresentationPoseGraphAsset graphOwner in graphOwners)
            {
                bool shared = m_Profile.LinkedPoseImplementations
                    .Where(value => value)
                    .SelectMany(value => value.Entries)
                    .Any(value => value != null && value.GraphOwner == graphOwner);
                if (!shared)
                    Undo.DestroyObjectImmediate(graphOwner);
            }
        }

        void SetLinkedPoseGroup(CharacterLinkedPoseGroupBinding binding)
        {
            binding.RequireValid();
            CharacterLinkedPoseGroupBinding[] groups = m_Profile.LinkedPoseGroups
                .Where(value => value != null && value.GroupId != binding.GroupId)
                .Append(binding)
                .OrderBy(value => value.GroupId)
                .ToArray();
            m_Profile.SetLinkedPoseBindings(
                groups,
                m_Profile.LinkedPoseImplementations.ToArray(),
                m_Profile.LinkedPoseSelectors.ToArray());
        }

        void RemoveLinkedPoseGroup(LinkedPoseGroupId groupId)
        {
            if (!m_Profile.LinkedPoseGroups.Any(value =>
                    value != null && value.GroupId == groupId))
                throw new InvalidOperationException(
                    $"Linked Pose Group '{groupId}' does not exist.");
            if (m_Profile.LinkedPoseSelectors.Any(value =>
                    value != null && value.GroupId == groupId))
                throw new InvalidOperationException(
                    $"Linked Pose Group '{groupId}' is still used by a selector.");
            if (m_Profile.PoseGraph && m_Profile.PoseGraph.EnumerateGraphs()
                    .SelectMany(value => value.Nodes)
                    .Any(value => value?.Payload is CharacterLinkedPoseCallPayload call &&
                                  call.GroupId == groupId))
                throw new InvalidOperationException(
                    $"Linked Pose Group '{groupId}' is still used by a root Call.");
            m_Profile.SetLinkedPoseBindings(
                m_Profile.LinkedPoseGroups
                    .Where(value => value != null && value.GroupId != groupId)
                    .ToArray(),
                m_Profile.LinkedPoseImplementations.ToArray(),
                m_Profile.LinkedPoseSelectors.ToArray());
        }

        void CreateLinkedPoseSelector(
            CharacterEquipmentLinkedPoseSelectionBinding selector)
        {
            if (m_Profile.LinkedPoseSelectors.Contains(selector))
                throw new InvalidOperationException(
                    $"Linked Pose selector '{selector.name}' already belongs to the Profile.");
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selector)))
                throw new InvalidOperationException(
                    "New Linked Pose selector must be a transient object.");
            Undo.RegisterCreatedObjectUndo(selector, "Create Linked Pose Selector");
            AssetDatabase.AddObjectToAsset(selector, m_Profile);
            m_Profile.SetLinkedPoseBindings(
                m_Profile.LinkedPoseGroups.ToArray(),
                m_Profile.LinkedPoseImplementations.ToArray(),
                m_Profile.LinkedPoseSelectors.Append(selector).ToArray());
            EditorUtility.SetDirty(selector);
        }

        void RemoveLinkedPoseSelector(
            CharacterLinkedPoseSelectorBindingAsset selector)
        {
            RequireLinkedPoseSelector(selector);
            m_Profile.SetLinkedPoseBindings(
                m_Profile.LinkedPoseGroups.ToArray(),
                m_Profile.LinkedPoseImplementations.ToArray(),
                m_Profile.LinkedPoseSelectors
                    .Where(value => value != selector)
                    .ToArray());
            Undo.DestroyObjectImmediate(selector);
        }

        void SetEquipmentLinkedPoseMapping(
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            CharacterEquipmentLinkedPoseMapping mapping)
        {
            RequireLinkedPoseSelector(selector);
            mapping.RequireValid();
            CharacterEquipmentLinkedPoseMapping[] mappings = selector.Mappings
                .Where(value => value != null &&
                                value.EquipmentId != mapping.EquipmentId)
                .Append(mapping)
                .OrderBy(value => value.EquipmentId)
                .ToArray();
            Undo.RegisterCompleteObjectUndo(
                selector,
                "Set Equipment Linked Pose Mapping");
            selector.Configure(
                selector.SelectorId,
                selector.GroupId,
                selector.SlotId,
                selector.EmptyImplementationId,
                mappings);
            EditorUtility.SetDirty(selector);
        }

        void RemoveEquipmentLinkedPoseMapping(
            CharacterEquipmentLinkedPoseSelectionBinding selector,
            EquipmentId equipmentId)
        {
            RequireLinkedPoseSelector(selector);
            if (!selector.Mappings.Any(value =>
                    value != null && value.EquipmentId == equipmentId))
                throw new InvalidOperationException(
                    $"Equipment Linked Pose mapping '{equipmentId}' does not exist.");
            Undo.RegisterCompleteObjectUndo(
                selector,
                "Remove Equipment Linked Pose Mapping");
            selector.Configure(
                selector.SelectorId,
                selector.GroupId,
                selector.SlotId,
                selector.EmptyImplementationId,
                selector.Mappings
                    .Where(value => value != null &&
                                    value.EquipmentId != equipmentId)
                    .ToArray());
            EditorUtility.SetDirty(selector);
        }

        void RequireLinkedPoseImplementation(
            CharacterLinkedPoseImplementationAsset implementation)
        {
            if (!implementation ||
                !m_Profile.LinkedPoseImplementations.Contains(implementation))
                throw new InvalidOperationException(
                    "Linked Pose Implementation does not belong to this Profile.");
        }

        void RequireLinkedPoseSelector(
            CharacterLinkedPoseSelectorBindingAsset selector)
        {
            if (!selector || !m_Profile.LinkedPoseSelectors.Contains(selector))
                throw new InvalidOperationException(
                    "Linked Pose selector does not belong to this Profile.");
        }

        void SetSourceBinding(
            CharacterPresentationPoseSourceBinding binding,
            bool replace)
        {
            if (!m_Profile.PoseGraph ||
                !m_Profile.PoseGraph.SourceSlots.Contains(binding.Slot))
                throw new InvalidOperationException("Pose source binding references a Slot outside the Profile Pose Graph.");
            binding.RequireValid(m_Profile.RigDefinition);
            CharacterPresentationPoseSourceBinding existing =
                m_Profile.FindPoseSourceBinding(binding.Slot);
            if (replace != (existing != null))
                throw new InvalidOperationException(replace
                    ? $"Pose Source Slot '{binding.Slot.name}' has no binding to replace."
                    : $"Pose Source Slot '{binding.Slot.name}' already has a binding.");
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(binding)))
                throw new InvalidOperationException($"Pose source binding '{binding.name}' already belongs to an asset.");
            Undo.RegisterCreatedObjectUndo(binding, replace
                ? "Replace Pose Source Binding"
                : "Create Pose Source Binding");
            AssetDatabase.AddObjectToAsset(binding, m_Profile);
            CharacterPresentationPoseSourceBinding[] bindings = m_Profile.PoseGraph.SourceSlots
                .Select(slot => slot == binding.Slot
                    ? binding
                    : m_Profile.FindPoseSourceBinding(slot))
                .Where(value => value != null)
                .ToArray();
            m_Profile.SetPoseSourceBindings(bindings);
            EditorUtility.SetDirty(binding);
            if (existing)
                Undo.DestroyObjectImmediate(existing);
        }

        void RemoveSourceBinding(CharacterPresentationPoseSourceBinding binding)
        {
            if (!m_Profile.PoseSourceBindings.Contains(binding))
                throw new InvalidOperationException("Pose source binding does not belong to this Profile.");
            if (m_Profile.PoseGraph && m_Profile.PoseGraph.SourceSlots.Contains(binding.Slot))
                throw new InvalidOperationException($"Pose Source Slot '{binding.Slot.name}' still belongs to the Profile Pose Graph.");
            m_Profile.SetPoseSourceBindings(
                m_Profile.PoseSourceBindings.Where(value => value != binding).ToArray());
            Undo.DestroyObjectImmediate(binding);
        }

        void RenameSourceBinding(
            CharacterPresentationPoseSourceBinding binding,
            string displayName)
        {
            if (!m_Profile.PoseSourceBindings.Contains(binding))
                throw new InvalidOperationException("Pose source binding does not belong to this Profile.");
            Undo.RegisterCompleteObjectUndo(binding, "Rename Pose Source Binding");
            binding.name = displayName;
            EditorUtility.SetDirty(binding);
        }
    }

    public sealed class CharacterPoseGraphAssetMutationOwner : ICharacterPresentationMutationOwner
    {
        readonly CharacterPresentationPoseGraphAsset m_Asset;
        readonly CharacterAnimationPresentationProfile m_Profile;

        public CharacterPoseGraphAssetMutationOwner(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile = null)
        {
            m_Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            m_Profile = profile;
        }

        public UnityEngine.Object SerializedOwner => m_Asset;
        internal CharacterAnimationPresentationProfile Profile => m_Profile;
        public CharacterTypedPoseGraph RequirePoseGraph(string graphId) => m_Asset.RequireGraph(new PoseGraphId(graphId));
        public void ReplacePoseGraph(CharacterTypedPoseGraph graph) => m_Asset.ReplaceGraph(graph);

        public void ApplyGraphCatalogMutation(
            CharacterPresentationMutation mutation)
        {
            switch (mutation)
            {
                case CreatePoseGraphMutation create:
                    if (m_Asset.Graph == null)
                        m_Asset.SetGraph(create.Graph);
                    else
                        m_Asset.AddGraph(create.Graph);
                    break;
                case DeletePoseGraphMutation delete:
                    m_Asset.RemoveGraph(delete.GraphId);
                    break;
                case CreatePoseSourceSlotMutation create:
                    CreateSourceSlot(create.Slot);
                    break;
                case RenamePoseSourceSlotMutation rename:
                    RenameSourceSlot(rename.Slot, rename.DisplayName);
                    break;
                case DeletePoseSourceSlotMutation delete:
                    DeleteSourceSlot(delete.Slot);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Mutation '{mutation?.Kind}' is not a Pose Graph catalog command.");
            }
        }

        void CreateSourceSlot(CharacterPresentationPoseSourceSlot slot)
        {
            if (m_Asset.SourceSlots.Contains(slot))
                throw new InvalidOperationException($"Pose Source Slot '{slot.name}' already exists.");
            slot.RequireValid();
            CharacterPresentationPoseSourceSlot[] replaced = m_Asset.SourceSlots
                .Where(value => value && string.Equals(value.name, slot.name, StringComparison.Ordinal))
                .ToArray();
            string ownerPath = AssetDatabase.GetAssetPath(m_Asset);
            if (string.IsNullOrWhiteSpace(ownerPath))
                throw new InvalidOperationException("Pose Graph asset must be saved before adding a Source Slot.");
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(slot)))
                throw new InvalidOperationException($"Pose Source Slot '{slot.name}' already belongs to an asset.");
            Undo.RegisterCreatedObjectUndo(slot, "Create Pose Source Slot");
            AssetDatabase.AddObjectToAsset(slot, m_Asset);
            m_Asset.SetSourceSlots(m_Asset.SourceSlots
                .Where(value => value && !replaced.Contains(value))
                .Append(slot)
                .ToArray());
            for (int i = 0; i < replaced.Length; i++)
                Undo.DestroyObjectImmediate(replaced[i]);
            EditorUtility.SetDirty(slot);
        }

        void RenameSourceSlot(CharacterPresentationPoseSourceSlot slot, string displayName)
        {
            if (!m_Asset.SourceSlots.Contains(slot))
                throw new InvalidOperationException("Pose Source Slot does not belong to this Pose Graph.");
            Undo.RegisterCompleteObjectUndo(slot, "Rename Pose Source Slot");
            slot.name = displayName;
            slot.RequireValid();
            EditorUtility.SetDirty(slot);
        }

        void DeleteSourceSlot(CharacterPresentationPoseSourceSlot slot)
        {
            CharacterPresentationPoseSourceSlot owned = m_Asset.SourceSlots.FirstOrDefault(
                value => value == slot ||
                         SameAssetIdentity(value, slot) ||
                         value && slot && string.Equals(value.name, slot.name, StringComparison.Ordinal));
            if (!owned)
            {
                if (!slot)
                    return;
                if (slot && string.Equals(
                        AssetDatabase.GetAssetPath(slot),
                        AssetDatabase.GetAssetPath(m_Asset),
                        StringComparison.Ordinal))
                {
                    Undo.DestroyObjectImmediate(slot);
                    return;
                }
                throw new InvalidOperationException("Pose Source Slot does not belong to this Pose Graph.");
            }
            slot = owned;
            CharacterTypedPoseNode consumer = m_Asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .FirstOrDefault(value => value?.PresentationPoseSourceSlot == slot);
            if (consumer != null)
                throw new InvalidOperationException($"Pose Source Slot '{slot.name}' is still used by node '{consumer.DisplayName}'.");
            m_Asset.SetSourceSlots(m_Asset.SourceSlots.Where(value => value != slot).ToArray());
            Undo.DestroyObjectImmediate(slot);
        }

        static bool SameAssetIdentity(UnityEngine.Object left, UnityEngine.Object right)
        {
            return left && right &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(left, out string leftGuid, out long leftFileId) &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(right, out string rightGuid, out long rightFileId) &&
                   leftFileId == rightFileId &&
                   string.Equals(leftGuid, rightGuid, StringComparison.Ordinal);
        }

        public void ApplyStateMachineMutation(
            CharacterPresentationMutation mutation)
        {
            CharacterPoseStateMachineDefinition machine =
                FindStateMachine(mutation.OwnerId);
            switch (mutation)
            {
                case SetPoseStateMachineLayoutElementMutation setLayout:
                    m_Asset.SetStateMachineLayoutElement(
                        machine.StateMachineId,
                        setLayout.ElementId,
                        setLayout.Position);
                    return;
                case RemovePoseStateMachineLayoutElementMutation removeLayout:
                    m_Asset.RemoveStateMachineLayoutElement(
                        machine.StateMachineId,
                        removeLayout.ElementId);
                    return;
            }
            var states = machine.States.ToList();
            var transitions = machine.Transitions.ToList();
            CharacterPoseStateEntry entry = machine.Entry;
            CharacterPoseStateAlias[] aliases = machine.Aliases.ToArray();
            int maxTransitionsPerFrame = machine.MaxTransitionsPerFrame;
            switch (mutation)
            {
                case CreatePoseStateMachineMutation create:
                    if (!ReferenceEquals(machine, create.StateMachine))
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{mutation.OwnerId}' already exists.");
                    return;
                case CreatePoseStateMutation create:
                    if (states.Any(value => value.StateId == create.State.StateId))
                        throw new InvalidOperationException(
                            $"Pose State '{create.State.StateId}' already exists.");
                    states.Add(create.State);
                    break;
                case DeletePoseStateMutation delete:
                    if (states.RemoveAll(value => value.StateId == delete.StateId) != 1)
                        throw new InvalidOperationException(
                            $"Pose State '{delete.StateId}' does not exist.");
                    break;
                case SetPoseStateFieldMutation set:
                {
                    int index = states.FindIndex(value => value.StateId == set.StateId);
                    if (index < 0)
                        throw new InvalidOperationException(
                            $"Pose State '{set.StateId}' does not exist.");
                    CharacterPoseStateDefinition current = states[index];
                    bool alwaysResetOnEntry = set.FieldId == "always-reset-on-entry"
                        ? Convert.ToBoolean(set.Value)
                        : throw new InvalidOperationException(
                            $"Pose State does not declare field '{set.FieldId}'.");
                    states[index] = new CharacterPoseStateDefinition(
                        current.StateId,
                        current.DisplayName,
                        current.PoseGraphId,
                        current.OutputPoseNodeId,
                        alwaysResetOnEntry);
                    break;
                }
                case CreatePoseTransitionMutation create:
                    if (transitions.Any(value =>
                            value.TransitionId.Equals(
                                create.Transition.TransitionId)))
                        throw new InvalidOperationException(
                            $"Pose Transition '{create.Transition.TransitionId}' already exists.");
                    transitions.Add(create.Transition);
                    break;
                case DeletePoseTransitionMutation delete:
                    if (transitions.RemoveAll(value =>
                            value.TransitionId.Equals(
                                delete.TransitionId)) != 1)
                        throw new InvalidOperationException(
                            $"Pose Transition '{delete.TransitionId}' does not exist.");
                    break;
                case SetPoseTransitionFieldMutation set:
                {
                    int index = transitions.FindIndex(value =>
                        value.TransitionId.Equals(set.TransitionId));
                    if (index < 0)
                        throw new InvalidOperationException(
                            $"Pose Transition '{set.TransitionId}' does not exist.");
                    transitions[index] =
                        CharacterPoseTransitionFieldMutation.Set(
                            transitions[index],
                            set.FieldId,
                            set.Value);
                    break;
                }
                case ConfigurePoseStateMachineMutation configure:
                    entry = configure.Entry;
                    aliases = configure.Aliases.ToArray();
                    maxTransitionsPerFrame =
                        configure.MaxTransitionsPerFrame;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Mutation '{mutation.Kind}' is not a Pose StateMachine command.");
            }
            machine.Configure(
                entry,
                states.OrderBy(value => value.StateId).ToArray(),
                transitions.OrderBy(value => value.Priority)
                    .ThenBy(value => value.TransitionId)
                    .ToArray(),
                aliases.OrderBy(value => value.AliasId).ToArray(),
                maxTransitionsPerFrame);
            if (mutation is CreatePoseTransitionMutation created)
            {
                CharacterPoseStateTransition applied = machine.Transitions.Single(value =>
                    value.TransitionId.Equals(created.Transition.TransitionId));
                if (applied.BlendProfile != created.Transition.BlendProfile)
                    throw new InvalidOperationException(
                        $"Pose Transition '{applied.TransitionId}' did not retain its Blend Profile during mutation.");
            }
        }

        public void ApplyProfileMutation(CharacterPresentationMutation mutation) =>
            throw new InvalidOperationException("Presentation Profile mutations require the Profile owner surface.");

        CharacterPoseStateMachineDefinition FindStateMachine(string id)
        {
            CharacterPoseStateMachineDefinition[] matches = m_Asset
                .EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Select(value => value.StateMachine)
                .Where(value => value != null &&
                                string.Equals(
                                    value.StateMachineId.Value,
                                    id,
                                    StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Pose StateMachine '{id}' must have exactly one owning node.");
        }
    }

    public sealed class CharacterTypedPoseGraphDocument : IGraphAuthoringDocumentProjection
    {
        readonly ICharacterPresentationMutationOwner m_Owner;
        readonly string m_GraphId;
        readonly GraphAuthoringDocumentRoleId m_Role;
        readonly string m_DisplayName;

        public CharacterTypedPoseGraphDocument(
            ICharacterPresentationMutationOwner owner,
            string graphId,
            GraphAuthoringDocumentRoleId role,
            string displayName)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_GraphId = string.IsNullOrWhiteSpace(graphId) ? throw new ArgumentException("Pose Graph identity is missing.", nameof(graphId)) : graphId;
            m_Role = role.IsValid ? role : throw new ArgumentException("Pose Graph document role is missing.", nameof(role));
            m_DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException(
                    "Pose Graph display name is missing.",
                    nameof(displayName))
                : displayName;
        }

        public GraphAuthoringDomainId DomainId => CharacterPoseGraphAuthoringCapabilities.Domain;
        public GraphAuthoringDocumentRoleId DocumentRoleId => m_Role;
        public string DocumentId => m_GraphId;
        public string DisplayName => m_DisplayName;
        public string ContentRevision => Graph.ContentRevision;
        public UnityEngine.Object SerializedOwner => m_Owner.SerializedOwner;
        public IReadOnlyList<GraphAuthoringPageProjection> Pages => new[] { new GraphAuthoringPageProjection(new GraphAuthoringElementId(m_GraphId), m_DisplayName, m_Role.Value) };
        public IReadOnlyList<GraphAuthoringNodeProjection> Nodes => ProjectNodes();
        public IReadOnlyList<GraphAuthoringEdgeProjection> Edges => ProjectEdges();
        internal CharacterTypedPoseGraph Graph => m_Owner.RequirePoseGraph(m_GraphId);
        internal ICharacterPresentationMutationOwner Owner => m_Owner;

        IReadOnlyList<GraphAuthoringNodeProjection> ProjectNodes()
        {
            CharacterTypedPoseGraph graph = Graph;
            Dictionary<PoseNodeId, Vector2> positions = graph.Layout
                .Where(value => value != null && value.NodeId.IsValid)
                .GroupBy(value => value.NodeId)
                .ToDictionary(value => value.Key, value => value.Last().Position);
            CharacterAnimationPresentationProfile profile =
                (m_Owner as CharacterPoseGraphAssetMutationOwner)?.Profile;
            return graph.Nodes.Select(node => new GraphAuthoringNodeProjection(
                new GraphAuthoringElementId(node.NodeId.Value),
                CharacterPoseGraphAuthoringCapabilities.Get(node.Kind),
                node.DisplayName,
                positions.TryGetValue(node.NodeId, out Vector2 position) ? position : Vector2.zero,
                node.DynamicPorts.Select(ProjectDynamicPort).ToArray(),
                SourceSubtitle(node, profile))).ToArray();
        }

        static string SourceSubtitle(
            CharacterTypedPoseNode node,
            CharacterAnimationPresentationProfile profile)
        {
            CharacterPresentationPoseSourceSlot slot = node?.PresentationPoseSourceSlot;
            if (!slot)
                return string.Empty;
            if (!profile)
                return slot.name;
            CharacterPresentationPoseSourceBinding binding =
                profile.FindPoseSourceBinding(slot);
            return binding && binding.SourceAsset
                ? $"{slot.name} → {binding.SourceAsset.name}"
                : $"{slot.name} → Missing Binding";
        }

        static GraphAuthoringDynamicPortProjection ProjectDynamicPort(CharacterPoseDynamicPort port) => new GraphAuthoringDynamicPortProjection(
            new GraphAuthoringPortId(port.PortId.Value),
            port.DisplayName,
            ValueType(port.Kind),
            port.Direction == CharacterPosePortDirection.Input ? GraphAuthoringPortDirection.Input : GraphAuthoringPortDirection.Output,
            port.Direction == CharacterPosePortDirection.Input ? GraphAuthoringPortCapacity.Single : GraphAuthoringPortCapacity.Multiple,
            port.Required,
            port.Order);

        IReadOnlyList<GraphAuthoringEdgeProjection> ProjectEdges() => Graph.Edges.Select(edge => new GraphAuthoringEdgeProjection(
            new GraphAuthoringElementId(edge.EdgeId),
            new GraphAuthoringElementId(edge.SourceNodeId.Value),
            new GraphAuthoringPortId(edge.SourcePortId.Value),
            new GraphAuthoringElementId(edge.TargetNodeId.Value),
            new GraphAuthoringPortId(edge.TargetPortId.Value))).ToArray();

        internal static string ValueType(CharacterPosePortKind kind) => kind switch
        {
            CharacterPosePortKind.LocalPose => "pose.local",
            CharacterPosePortKind.ComponentPose => "pose.component",
            CharacterPosePortKind.Parameter => "pose.parameter",
            CharacterPosePortKind.PoseDiscontinuity => "pose.discontinuity",
            CharacterPosePortKind.ActionPlayback => "pose.action-playback",
            CharacterPosePortKind.FullBodyIkGoals => "component.full-body-ik-goals",
            CharacterPosePortKind.FullBodyIkGoalContribution => "component.full-body-ik-goal-contribution",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public sealed class CharacterTypedPoseGraphMutationAdapter : IGraphAuthoringDomainMutation
    {
        readonly CharacterPresentationMutationService m_Service = new CharacterPresentationMutationService();
        public bool ReadOnly { get; set; }

        public void Apply(IGraphAuthoringDocumentProjection document, GraphAuthoringMutationRequest request) => Apply(document, new[] { request });

        public void Apply(IGraphAuthoringDocumentProjection document, IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (ReadOnly)
                throw new InvalidOperationException("Pose Graph document is read-only.");
            if (!(document is CharacterTypedPoseGraphDocument poseDocument))
                throw new ArgumentException("Pose mutation requires a typed Pose Graph document.", nameof(document));
            var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Edit Pose Graph");
            foreach (GraphAuthoringMutationRequest request in requests ?? throw new ArgumentNullException(nameof(requests)))
                transaction.Add(Convert(poseDocument, request));
            m_Service.Apply(poseDocument.Owner, transaction);
        }

        static CharacterPresentationMutation Convert(CharacterTypedPoseGraphDocument document, GraphAuthoringMutationRequest request)
        {
            string graphId = document.DocumentId;
            return request.Kind switch
            {
                GraphAuthoringMutationKind.CreateNode => request.Value is CharacterTypedPoseNode node
                    ? new CreatePoseNodeMutation(graphId, node, request.Position)
                    : throw new InvalidOperationException("Create Pose Node requires a complete typed payload."),
                GraphAuthoringMutationKind.DeleteElement => new DeletePoseNodeMutation(graphId, new PoseNodeId(request.TargetId.Value)),
                GraphAuthoringMutationKind.MoveElement => new MovePoseNodeMutation(graphId, new PoseNodeId(request.TargetId.Value), request.Position),
                GraphAuthoringMutationKind.ConnectPorts => new ConnectPosePortMutation(
                    graphId,
                    Guid.NewGuid().ToString("N"),
                    new PoseNodeId(request.SourceNodeId.Value),
                    new PosePortId(request.SourcePortId.Value),
                    new PoseNodeId(request.TargetNodeId.Value),
                    new PosePortId(request.TargetPortId.Value)),
                GraphAuthoringMutationKind.DisconnectEdge => new DisconnectPosePortMutation(graphId, request.TargetId.Value),
                GraphAuthoringMutationKind.SetField => new SetPoseNodeFieldMutation(graphId, new PoseNodeId(request.TargetId.Value), request.FieldId.Value, request.Value),
                GraphAuthoringMutationKind.SetDisplayName => new SetPoseNodeNameMutation(
                    graphId,
                    new PoseNodeId(request.TargetId.Value),
                    request.Value?.ToString() ?? string.Empty),
                GraphAuthoringMutationKind.AddDynamicPort => request.Value is CharacterPoseDynamicPort port
                    ? new AddDynamicPosePortMutation(graphId, new PoseNodeId(request.TargetId.Value), port)
                    : throw new InvalidOperationException("Add Dynamic Pose Port requires a typed port payload."),
                GraphAuthoringMutationKind.RemoveDynamicPort => new RemoveDynamicPosePortMutation(graphId, new PoseNodeId(request.TargetId.Value), new PosePortId(request.Value?.ToString() ?? string.Empty)),
                _ => throw new InvalidOperationException($"Shared Graph command '{request.Kind}' is not valid for a Pose Graph.")
            };
        }
    }

    public sealed class CharacterTypedPoseConnectionPolicy : IGraphAuthoringConnectionPolicy
    {
        public bool CanConnect(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringNodeProjection sourceNode,
            GraphAuthoringPortId sourcePortId,
            GraphAuthoringNodeProjection targetNode,
            GraphAuthoringPortId targetPortId)
        {
            if (!(document is CharacterTypedPoseGraphDocument poseDocument) || sourceNode == null || targetNode == null || sourceNode.NodeId.Equals(targetNode.NodeId))
                return false;
            CharacterTypedPoseNode source = poseDocument.Graph.Nodes.Single(value => value.NodeId.Value == sourceNode.NodeId.Value);
            CharacterTypedPoseNode target = poseDocument.Graph.Nodes.Single(value => value.NodeId.Value == targetNode.NodeId.Value);
            PortInfo output = Resolve(source, sourcePortId.Value);
            PortInfo input = Resolve(target, targetPortId.Value);
            if (output.Direction != CharacterPosePortDirection.Output || input.Direction != CharacterPosePortDirection.Input || output.Kind != input.Kind)
                return false;
            return !poseDocument.Graph.Edges.Any(value => value.TargetNodeId == target.NodeId && value.TargetPortId.Value == targetPortId.Value);
        }

        static PortInfo Resolve(CharacterTypedPoseNode node, string portId)
        {
            foreach (CharacterPosePortDefinition port in
                     CharacterPoseAuthoringPortProjection.GetFixed(
                         node.Kind))
                if (port.PortId.Value == portId)
                    return new PortInfo(
                        port.Kind,
                        port.Direction);
            CharacterPoseDynamicPort dynamic = node.DynamicPorts.SingleOrDefault(value => value.PortId.Value == portId);
            return dynamic == null
                ? throw new InvalidOperationException($"Pose node '{node.NodeId}' does not declare port '{portId}'.")
                : new PortInfo(dynamic.Kind, dynamic.Direction);
        }

        readonly struct PortInfo
        {
            public PortInfo(CharacterPosePortKind kind, CharacterPosePortDirection direction)
            {
                Kind = kind;
                Direction = direction;
            }
            public CharacterPosePortKind Kind { get; }
            public CharacterPosePortDirection Direction { get; }
        }
    }

    public sealed class CharacterTypedPoseDetailsDataSource :
        IGraphAuthoringDetailsDataSource,
        IGraphAuthoringAppliedValuesDataSource,
        IGraphAuthoringFieldOptionSource
    {
        readonly IGraphAuthoringDomainDiagnostics m_Diagnostics;
        readonly CharacterAnimationRigDefinition m_Rig;
        readonly CharacterAnimationPresentationProfile m_Profile;
        readonly Func<GraphAuthoringSelection, IReadOnlyList<GraphAuthoringReadOnlyDetail>> m_AppliedValues;

        public CharacterTypedPoseDetailsDataSource(
            IGraphAuthoringDomainDiagnostics diagnostics = null,
            CharacterAnimationRigDefinition rig = null,
            CharacterAnimationPresentationProfile profile = null,
            Func<GraphAuthoringSelection, IReadOnlyList<GraphAuthoringReadOnlyDetail>> appliedValues = null)
        {
            m_Diagnostics = diagnostics;
            m_Rig = rig;
            m_Profile = profile;
            m_AppliedValues = appliedValues;
        }

        public object ReadField(IGraphAuthoringDocumentProjection document, GraphAuthoringElementId elementId, GraphAuthoringFieldDescriptor field)
        {
            CharacterTypedPoseNode node = ((CharacterTypedPoseGraphDocument)document).Graph.Nodes.Single(value => value.NodeId.Value == elementId.Value);
            return CharacterPoseAuthoringPayloadCodec.Read(
                node.Payload,
                field.FieldId.Value);
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetLive(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection)
        {
            if (m_Diagnostics == null ||
                selection.Kind != GraphAuthoringSelectionKind.Node)
            {
                return Array.Empty<GraphAuthoringReadOnlyDetail>();
            }
            GraphAuthoringRuntimeTraceProjection trace =
                m_Diagnostics.GetRuntimeTrace(document)
                    .FirstOrDefault(value =>
                        value.ElementId.Equals(selection.ElementId));
            if (!trace.ElementId.IsValid)
                return Array.Empty<GraphAuthoringReadOnlyDetail>();
            return new[]
            {
                new GraphAuthoringReadOnlyDetail("Status", trace.Status),
                new GraphAuthoringReadOnlyDetail("Result", trace.Detail)
            };
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetAppliedValues(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection) =>
            m_AppliedValues?.Invoke(selection) ?? new[]
            {
                new GraphAuthoringReadOnlyDetail(
                    "Target",
                    "Select an exact Preview or Live target to inspect applied values.")
            };

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetDiagnostics(IGraphAuthoringDocumentProjection document, GraphAuthoringSelection selection) => Array.Empty<GraphAuthoringReadOnlyDetail>();

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetReferences(IGraphAuthoringDocumentProjection document, GraphAuthoringSelection selection)
        {
            if (selection.Kind != GraphAuthoringSelectionKind.Node)
                return Array.Empty<GraphAuthoringReadOnlyDetail>();
            CharacterTypedPoseNode node = ((CharacterTypedPoseGraphDocument)document)
                .Graph.Nodes.Single(value => value.NodeId.Value == selection.ElementId.Value);
            CharacterPresentationPoseSourceSlot slot = node.PresentationPoseSourceSlot;
            if (!slot)
                return Array.Empty<GraphAuthoringReadOnlyDetail>();
            var result = new List<GraphAuthoringReadOnlyDetail>
            {
                new GraphAuthoringReadOnlyDetail("Source Slot", slot.name),
                new GraphAuthoringReadOnlyDetail("Source Type", slot.SourceKind.ToString())
            };
            if (!m_Profile)
            {
                result.Add(new GraphAuthoringReadOnlyDetail(
                    "Profile Binding",
                    "Unavailable: exact Profile context required"));
                return result;
            }
            CharacterPresentationPoseSourceBinding binding =
                m_Profile.FindPoseSourceBinding(slot);
            if (!binding)
            {
                result.Add(new GraphAuthoringReadOnlyDetail("Profile Binding", "Missing"));
                return result;
            }
            result.Add(new GraphAuthoringReadOnlyDetail("Resource", binding.SourceAsset.name));
            result.Add(new GraphAuthoringReadOnlyDetail("Profile Owner", m_Profile.name));
            result.Add(new GraphAuthoringReadOnlyDetail("Rig", m_Profile.RigDefinition ? m_Profile.RigDefinition.name : "Missing"));
            if (binding is CharacterClipPoseSourceBinding clipBinding)
            {
                result.Add(new GraphAuthoringReadOnlyDetail("Duration", $"{clipBinding.Clip.length:0.###} s"));
                result.Add(new GraphAuthoringReadOnlyDetail("Loop", clipBinding.Clip.isLooping ? "Yes" : "No"));
            }
            result.Add(new GraphAuthoringReadOnlyDetail("Foot Analysis", "Configured"));
            return result;
        }

        public bool TryGetFieldOptions(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field,
            out IReadOnlyList<GraphAuthoringFieldOption> options)
        {
            bool physicalOnly = string.Equals(
                field.PickerKind,
                "rig-physical-bone",
                StringComparison.Ordinal);
            bool poseBone = string.Equals(
                                field.PickerKind,
                                "rig-pose-bone",
                                StringComparison.Ordinal) ||
                            string.Equals(
                                field.PickerKind,
                                "rig-bone",
                                StringComparison.Ordinal);
            if (!physicalOnly && !poseBone)
            {
                options = Array.Empty<GraphAuthoringFieldOption>();
                return false;
            }
            if (!m_Rig)
            {
                options = Array.Empty<GraphAuthoringFieldOption>();
                return true;
            }

            var result = new List<GraphAuthoringFieldOption>(
                physicalOnly
                    ? m_Rig.PhysicalBoneCount
                    : m_Rig.PoseBoneCount);
            for (int i = 0; i < m_Rig.PhysicalBones.Count; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone =
                    m_Rig.PhysicalBones[i];
                if (bone != null && bone.BoneId.IsValid)
                {
                    result.Add(new GraphAuthoringFieldOption(
                        bone.BoneId.Value,
                        $"{bone.BoneId.Value} (Physical)"));
                }
            }
            if (!physicalOnly)
            {
                for (int i = 0; i < m_Rig.VirtualBones.Count; i++)
                {
                    CharacterAnimationVirtualBoneDefinition bone =
                        m_Rig.VirtualBones[i];
                    if (bone != null &&
                        bone.VirtualBoneId.IsValid)
                    {
                        result.Add(new GraphAuthoringFieldOption(
                            bone.VirtualBoneId.Value,
                            $"{bone.DisplayName} (Virtual)"));
                    }
                }
            }
            options = result;
            return true;
        }
    }

    internal static class CharacterPoseGraphCapabilityValidator
    {
        public static IReadOnlyList<string> Validate(CharacterPresentationPoseGraphAsset asset)
        {
            return Validate(asset, Array.Empty<PoseGraphId>());
        }

        public static IReadOnlyList<string> Validate(
            CharacterPresentationPoseGraphAsset asset,
            IReadOnlyCollection<PoseGraphId> linkedPoseEntryGraphs)
        {
            var errors = new List<string>();
            if (!asset || asset.Graph == null)
            {
                errors.Add("Pose capability validation requires one typed Pose Graph asset.");
                return errors;
            }

            GraphAuthoringCapabilityCatalog catalog = CharacterPoseGraphAuthoringCapabilities.Catalog;
            HashSet<PoseGraphId> stateGraphs = asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Where(value => value.StateMachine != null)
                .SelectMany(value => value.StateMachine.States)
                .Where(value => value != null && value.PoseGraphId.IsValid)
                .Select(value => value.PoseGraphId)
                .ToHashSet();
            var linkedEntries = new HashSet<PoseGraphId>(linkedPoseEntryGraphs ?? Array.Empty<PoseGraphId>());
            HashSet<PoseGraphId> linkedClosure = CollectLinkedPoseClosure(asset, linkedEntries);
            foreach (CharacterTypedPoseGraph graph in asset.EnumerateGraphs())
            {
                if (graph == null)
                    continue;
                GraphAuthoringDocumentRoleId role = linkedEntries.Contains(graph.GraphId)
                    ? CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
                    : ReferenceEquals(graph, asset.Graph)
                    ? CharacterPoseGraphAuthoringCapabilities.RootGraph
                    : stateGraphs.Contains(graph.GraphId)
                        ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                        : CharacterPoseGraphAuthoringCapabilities.Subgraph;
                for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
                {
                    CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                    if (node?.Payload == null)
                    {
                        errors.Add($"Pose Graph '{graph.GraphId}' node #{nodeIndex} has no typed payload.");
                        continue;
                    }
                    try
                    {
                        GraphAuthoringCapabilityDescriptor capability = catalog.Require(
                            CharacterPoseGraphAuthoringCapabilities.Get(node.Kind),
                            CharacterPoseGraphAuthoringCapabilities.Domain,
                            role);
                        ValidatePayload(node, capability);
                        ValidateFields(node, capability);
                        ValidatePorts(node, capability);
                        if (linkedClosure.Contains(graph.GraphId))
                            ValidateLinkedPoseEntryContext(node, role);
                        if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                        {
                            CharacterPoseSubgraphSignatureValidator.RequireMatch(
                                node,
                                asset.RequireGraph(node.Subgraph.PoseGraphId));
                        }
                    }
                    catch (Exception exception)
                    {
                        errors.Add($"Pose Graph '{graph.GraphId}' node '{node.NodeId}': {exception.Message}");
                    }
                }
            }
            return errors;
        }

        static HashSet<PoseGraphId> CollectLinkedPoseClosure(
            CharacterPresentationPoseGraphAsset asset,
            IReadOnlyCollection<PoseGraphId> roots)
        {
            var result = new HashSet<PoseGraphId>();
            var pending = new Stack<PoseGraphId>(roots.Reverse());
            while (pending.Count > 0)
            {
                PoseGraphId graphId = pending.Pop();
                if (!result.Add(graphId))
                    continue;
                CharacterTypedPoseGraph graph = asset.RequireGraph(graphId);
                for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
                {
                    CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                    if (node?.Payload is CharacterPoseSubgraphPayload subgraph && subgraph.Subgraph != null && subgraph.Subgraph.PoseGraphId.IsValid)
                        pending.Push(subgraph.Subgraph.PoseGraphId);
                    if (node?.Payload is not CharacterPoseStateMachineNodePayload stateMachine || stateMachine.StateMachine == null)
                        continue;
                    for (int stateIndex = 0; stateIndex < stateMachine.StateMachine.States.Count; stateIndex++)
                    {
                        CharacterPoseStateDefinition state = stateMachine.StateMachine.States[stateIndex];
                        if (state != null && state.PoseGraphId.IsValid)
                            pending.Push(state.PoseGraphId);
                    }
                }
            }
            return result;
        }

        static void ValidateLinkedPoseEntryContext(
            CharacterTypedPoseNode node,
            GraphAuthoringDocumentRoleId role)
        {
            switch (node.Kind)
            {
                case CharacterPoseNodeKind.ActionPlaybackInput:
                case CharacterPoseNodeKind.AnimationSlot:
                case CharacterPoseNodeKind.ModifyBone:
                case CharacterPoseNodeKind.FootPlacement:
                case CharacterPoseNodeKind.FullBodyIkGoalAssembler:
                case CharacterPoseNodeKind.FullBodyIK:
                case CharacterPoseNodeKind.LocalToComponentPose:
                case CharacterPoseNodeKind.ComponentToLocalPose:
                case CharacterPoseNodeKind.LinkedPoseCall:
                    throw new InvalidOperationException($"Linked Pose Entry context forbids '{node.Kind}'.");
                case CharacterPoseNodeKind.OutputPose when !role.Equals(CharacterPoseGraphAuthoringCapabilities.StatePoseGraph):
                    throw new InvalidOperationException("Linked Pose Entry context only permits OutputPose as a StateMachine state boundary.");
            }
        }

        static void ValidatePayload(
            CharacterTypedPoseNode node,
            GraphAuthoringCapabilityDescriptor capability)
        {
            Type expected =
                CharacterPoseGraphAuthoringCapabilities.RequirePayloadType(
                    node.Kind);
            if (node.Payload.GetType() != expected ||
                !string.Equals(
                    capability.CompilerBindingId,
                    "presentation.pose-node." + ToKebabCase(node.Kind.ToString()),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"capability '{capability.CapabilityId}' does not own payload '{node.Payload.GetType().Name}'.");
            }
        }

        static void ValidateFields(
            CharacterTypedPoseNode node,
            GraphAuthoringCapabilityDescriptor capability)
        {
            foreach (GraphAuthoringFieldDescriptor field in
                     capability.Fields.Where(value => value.AuthoringWritable))
            {
                object value = CharacterPoseAuthoringPayloadCodec.Read(
                    node.Payload,
                    field.FieldId.Value);
                GraphAuthoringFieldConstraint constraint = field.Constraint;
                if (constraint.NonEmpty &&
                    (value == null || value is string text && string.IsNullOrWhiteSpace(text)))
                {
                    throw new InvalidOperationException(
                        $"field '{field.FieldId}' requires a value.");
                }
                if (value is string enumName &&
                    constraint.AllowedValues.Count > 0 &&
                    !constraint.AllowedValues.Contains(enumName))
                {
                    throw new InvalidOperationException(
                        $"field '{field.FieldId}' contains unsupported value '{enumName}'.");
                }
                if (!TryNumber(value, out double number))
                    continue;
                if (constraint.Finite && (double.IsNaN(number) || double.IsInfinity(number)) ||
                    constraint.Minimum.HasValue && number < constraint.Minimum.Value ||
                    constraint.Maximum.HasValue && number > constraint.Maximum.Value)
                {
                    throw new InvalidOperationException(
                        $"field '{field.FieldId}' violates its capability constraint.");
                }
            }
        }

        static void ValidatePorts(
            CharacterTypedPoseNode node,
            GraphAuthoringCapabilityDescriptor capability)
        {
            Dictionary<string, CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node)
                .Where(value => value != null)
                .ToDictionary(value => value.PortId.Value, StringComparer.Ordinal);
            foreach (GraphAuthoringPortDescriptor expected in capability.FixedPorts)
            {
                if (!ports.TryGetValue(expected.PortId.Value, out CharacterPosePortDefinition actual) ||
                    CharacterTypedPoseGraphDocument.ValueType(actual.Kind) != expected.ValueTypeId ||
                    (actual.Direction == CharacterPosePortDirection.Input
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output) != expected.Direction ||
                    actual.Required != expected.Required)
                {
                    throw new InvalidOperationException(
                        $"fixed port '{expected.PortId}' does not match the shared capability.");
                }
                ports.Remove(expected.PortId.Value);
            }

            foreach (CharacterPoseDynamicPort dynamic in node.DynamicPorts)
            {
                if (dynamic == null || !ports.Remove(dynamic.PortId.Value))
                    throw new InvalidOperationException("dynamic port identity is missing or duplicated.");
                if (capability.DynamicPortPolicy == GraphAuthoringDynamicPortPolicy.None ||
                    capability.DynamicPortPolicy == GraphAuthoringDynamicPortPolicy.OrderedInputs &&
                    dynamic.Direction != CharacterPosePortDirection.Input ||
                    capability.DynamicPortPolicy == GraphAuthoringDynamicPortPolicy.OrderedOutputs &&
                    dynamic.Direction != CharacterPosePortDirection.Output)
                {
                    throw new InvalidOperationException(
                        $"dynamic port '{dynamic.PortId}' is not allowed by capability '{capability.CapabilityId}'.");
                }
            }
            if (ports.Count != 0)
                throw new InvalidOperationException("node publishes ports not declared by its capability.");
        }

        static bool TryNumber(object value, out double number)
        {
            switch (value)
            {
                case byte byteValue:
                    number = byteValue;
                    return true;
                case short shortValue:
                    number = shortValue;
                    return true;
                case int intValue:
                    number = intValue;
                    return true;
                case long longValue:
                    number = longValue;
                    return true;
                case float floatValue:
                    number = floatValue;
                    return true;
                case double doubleValue:
                    number = doubleValue;
                    return true;
                default:
                    number = 0d;
                    return false;
            }
        }

        static string ToKebabCase(string value)
        {
            var characters = new List<char>(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current))
                    characters.Add('-');
                characters.Add(char.ToLowerInvariant(current));
            }
            return new string(characters.ToArray());
        }
    }
}
