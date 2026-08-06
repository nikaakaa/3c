using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class CharacterLinkedPoseEditorDetails
    {
        public static void DrawInterface(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            bool drawEntries)
        {
            EditorGUILayout.LabelField("Authoring Contract", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Owner", linkedInterface.OwnerIdentity);
            EditorGUILayout.LabelField("Interface", linkedInterface.InterfaceId.ToString());
            EditorGUILayout.LabelField("Revision", linkedInterface.Revision.ToString());

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Derived Identity", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Signature", linkedInterface.SignatureHash.ToString());
            EditorGUILayout.LabelField("Fact Contract", linkedInterface.FactContractIdentity.ToString());
            EditorGUILayout.LabelField("Execution Contract", linkedInterface.ExecutionContract);
            EditorGUILayout.LabelField("Status", linkedInterface.IsStale ? "Stale" : "Current");

            if (!drawEntries)
                return;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Entries ({linkedInterface.Entries.Count})", EditorStyles.boldLabel);
            for (int entryIndex = 0; entryIndex < linkedInterface.Entries.Count; entryIndex++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry =
                    linkedInterface.Entries[entryIndex];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    entry == null ? "Missing Entry" : entry.EntryId.ToString(),
                    EditorStyles.boldLabel);
                if (entry != null)
                {
                    EditorGUILayout.LabelField("Execution Domain", entry.ExecutionDomain.ToString());
                    for (int portIndex = 0; portIndex < entry.Ports.Count; portIndex++)
                    {
                        CharacterLinkedPoseInterfacePortDescriptor port =
                            entry.Ports[portIndex];
                        string label = port == null
                            ? "Missing Port"
                            : $"{port.Direction} {port.Kind} · {port.Space}";
                        string value = port == null
                            ? string.Empty
                            : $"{port.PortId} · Order {port.Order} · {(port.Required ? "Required" : "Optional")}";
                        EditorGUILayout.LabelField(label, value);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        public static void RunValidation(
            Action validate,
            bool stale,
            out string message,
            out MessageType type)
        {
            try
            {
                validate();
                message = stale ? "Authoring contract is stale." : "Authoring contract is valid.";
                type = stale ? MessageType.Warning : MessageType.Info;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                type = MessageType.Error;
            }
        }

        public static void DrawObject(string label, UnityEngine.Object value)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(label, value, value ? value.GetType() : typeof(UnityEngine.Object), false);
        }

    }

    [CustomEditor(typeof(CharacterLinkedPoseInterfaceAsset))]
    public sealed class CharacterLinkedPoseInterfaceAssetEditor : UnityEditor.Editor
    {
        string m_ValidationMessage = string.Empty;
        MessageType m_ValidationType;

        public override void OnInspectorGUI()
        {
            var linkedInterface = (CharacterLinkedPoseInterfaceAsset)target;
            EditorGUILayout.LabelField("Linked Pose Interface", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This surface is read-only. It exposes the authored typed contract and derived identities, never generated offsets or runtime handles.",
                MessageType.Info);
            if (GUILayout.Button("Open in Animation Workspace"))
                CharacterLinkedPoseAuthoringService.OpenWorkspace(linkedInterface);
            CharacterLinkedPoseEditorDetails.DrawInterface(linkedInterface, true);
            if (GUILayout.Button("Validate Authoring Contract"))
            {
                CharacterLinkedPoseEditorDetails.RunValidation(
                    linkedInterface.RequireValid,
                    linkedInterface.IsStale,
                    out m_ValidationMessage,
                    out m_ValidationType);
            }
            if (!string.IsNullOrEmpty(m_ValidationMessage))
                EditorGUILayout.HelpBox(m_ValidationMessage, m_ValidationType);
        }
    }

    [CustomEditor(typeof(CharacterLinkedPoseImplementationAsset))]
    public sealed class CharacterLinkedPoseImplementationAssetEditor : UnityEditor.Editor
    {
        string m_ValidationMessage = string.Empty;
        MessageType m_ValidationType;

        public override void OnInspectorGUI()
        {
            var implementation = (CharacterLinkedPoseImplementationAsset)target;
            EditorGUILayout.LabelField("Linked Pose Implementation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This surface is read-only. Entry mappings are authoring data; content hash, captured signature and stale state are derived diagnostics.",
                MessageType.Info);
            if (GUILayout.Button("Open in Animation Workspace"))
                CharacterLinkedPoseAuthoringService.OpenWorkspace(implementation);

            EditorGUILayout.LabelField("Authoring Contract", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Owner", implementation.OwnerIdentity);
            EditorGUILayout.LabelField("Implementation", implementation.ImplementationId.ToString());
            EditorGUILayout.LabelField("Revision", implementation.Revision.ToString());
            CharacterLinkedPoseEditorDetails.DrawObject("Interface", implementation.Interface);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Derived Identity", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Captured Signature", implementation.CapturedInterfaceSignature);
            EditorGUILayout.LabelField("Current Signature", implementation.Interface
                ? implementation.Interface.SignatureHash.ToString()
                : "Unavailable");
            string contentHash;
            try
            {
                contentHash = implementation.ContentHash.ToString();
            }
            catch (Exception exception)
            {
                contentHash = $"Unavailable: {exception.Message}";
            }
            EditorGUILayout.LabelField("Content Hash", contentHash);
            EditorGUILayout.LabelField("Status", implementation.IsStale ? "Stale" : "Current");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Entry Graphs ({implementation.Entries.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < implementation.Entries.Count; i++)
            {
                CharacterLinkedPoseImplementationEntryBinding entry = implementation.Entries[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    entry == null ? "Missing Entry" : entry.EntryId.ToString(),
                    EditorStyles.boldLabel);
                if (entry != null)
                {
                    CharacterLinkedPoseEditorDetails.DrawObject("Graph Owner", entry.GraphOwner);
                    EditorGUILayout.LabelField("Graph", entry.GraphId.ToString());
                    EditorGUILayout.LabelField("Graph Owner Identity", entry.GraphOwnerIdentity);
                    using (new EditorGUI.DisabledScope(!entry.GraphOwner))
                    {
                        if (GUILayout.Button("Open Entry in Animation Workspace"))
                            CharacterLinkedPoseAuthoringService.OpenWorkspace(implementation);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Validate Authoring Contract"))
            {
                CharacterLinkedPoseEditorDetails.RunValidation(
                    implementation.RequireValid,
                    implementation.IsStale,
                    out m_ValidationMessage,
                    out m_ValidationType);
            }
            if (!string.IsNullOrEmpty(m_ValidationMessage))
                EditorGUILayout.HelpBox(m_ValidationMessage, m_ValidationType);
        }
    }

    [CustomEditor(typeof(CharacterEquipmentLinkedPoseSelectionBinding))]
    public sealed class CharacterEquipmentLinkedPoseSelectionBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var selector = (CharacterEquipmentLinkedPoseSelectionBinding)target;
            EditorGUILayout.LabelField("Equipment Linked Pose Selector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This surface is read-only. The selector maps committed Equipment identity to the generic Linked Pose selection contract.",
                MessageType.Info);
            if (GUILayout.Button("Open in Animation Workspace"))
                CharacterLinkedPoseAuthoringService.OpenWorkspace(selector);

            EditorGUILayout.LabelField("Selector", selector.SelectorId.ToString());
            EditorGUILayout.LabelField("Group", selector.GroupId.ToString());
            EditorGUILayout.LabelField("Equipment Slot", selector.SlotId.ToString());
            EditorGUILayout.LabelField("Empty Equipment", selector.EmptyImplementationId.ToString());

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Exact Mappings ({selector.Mappings.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < selector.Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseMapping mapping = selector.Mappings[i];
                EditorGUILayout.LabelField(
                    mapping == null ? "Missing Equipment" : mapping.EquipmentId.ToString(),
                    mapping == null ? "Missing Implementation" : mapping.ImplementationId.ToString());
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Derived Candidate Closure ({selector.CandidateImplementationIds.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < selector.CandidateImplementationIds.Count; i++)
                EditorGUILayout.LabelField(selector.CandidateImplementationIds[i].ToString());
        }
    }
}
