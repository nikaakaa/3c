using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior.Editor.ActionTimeline;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacterBehavior.Editor.ActionBranch
{
    public readonly struct CommittedActionBranchNodeEditorSnapshot
    {
        public CommittedActionBranchNodeEditorSnapshot(
            string nodeId,
            CommittedActionNodeKind kind,
            string propertyPath,
            Vector2 editorPosition,
            IReadOnlyList<string> childNodeIds,
            CommittedActionConditionKind conditionKind,
            InputRequestKind requestKind,
            string requiredFactId,
            CharacterStateVariant expectedVariant,
            string timelineNodeId,
            float durationSeconds,
            string primaryAnimationKey,
            bool isRoot)
        {
            NodeId = nodeId ?? string.Empty;
            Kind = kind;
            PropertyPath = propertyPath ?? string.Empty;
            EditorPosition = editorPosition;
            ChildNodeIds = childNodeIds ?? Array.Empty<string>();
            ConditionKind = conditionKind;
            RequestKind = requestKind;
            RequiredFactId = requiredFactId ?? string.Empty;
            ExpectedVariant = expectedVariant;
            TimelineNodeId = timelineNodeId ?? string.Empty;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            PrimaryAnimationKey = primaryAnimationKey ?? string.Empty;
            IsRoot = isRoot;
        }

        public string NodeId { get; }
        public CommittedActionNodeKind Kind { get; }
        public string PropertyPath { get; }
        public Vector2 EditorPosition { get; }
        public IReadOnlyList<string> ChildNodeIds { get; }
        public CommittedActionConditionKind ConditionKind { get; }
        public InputRequestKind RequestKind { get; }
        public string RequiredFactId { get; }
        public CharacterStateVariant ExpectedVariant { get; }
        public string TimelineNodeId { get; }
        public float DurationSeconds { get; }
        public string PrimaryAnimationKey { get; }
        public bool IsRoot { get; }
        public bool IsProtected => IsRoot;
        public bool CanDelete => !IsRoot;
        public bool IsTimeline => Kind == CommittedActionNodeKind.Timeline;
    }

    public sealed class CommittedActionBranchEditorSnapshot
    {
        readonly CommittedActionBranchNodeEditorSnapshot[] nodes;

        public CommittedActionBranchEditorSnapshot(
            string branchId,
            string rootNodeId,
            CommittedActionBranchNodeEditorSnapshot[] nodes)
        {
            BranchId = branchId ?? string.Empty;
            RootNodeId = rootNodeId ?? string.Empty;
            this.nodes = nodes ?? Array.Empty<CommittedActionBranchNodeEditorSnapshot>();
        }

        public string BranchId { get; }
        public string RootNodeId { get; }
        public IReadOnlyList<CommittedActionBranchNodeEditorSnapshot> Nodes => nodes;
    }

    public sealed class CommittedActionBranchSerializedAdapter
    {
        readonly CharacterActionDefinitionSO actionDefinition;
        readonly SerializedObject serializedObject;

        public CommittedActionBranchSerializedAdapter(CharacterActionDefinitionSO actionDefinition)
            : this(actionDefinition, actionDefinition != null ? new SerializedObject(actionDefinition) : null)
        {
        }

        public CommittedActionBranchSerializedAdapter(
            CharacterActionDefinitionSO actionDefinition,
            SerializedObject serializedObject)
        {
            this.actionDefinition = actionDefinition;
            this.serializedObject = serializedObject;
        }

        public CharacterActionDefinitionSO ActionDefinition => actionDefinition;
        public SerializedObject SerializedObject => serializedObject;
        public bool IsValid => actionDefinition != null && serializedObject != null;

        public bool TryGetBranchProperty(out SerializedProperty branch, out string diagnostic)
        {
            branch = null;
            diagnostic = string.Empty;
            if (!IsValid)
            {
                diagnostic = "action-definition-missing";
                return false;
            }

            serializedObject.UpdateIfRequiredOrScript();
            branch = serializedObject.FindProperty("committedActionBranch");
            if (branch == null)
                diagnostic = "committed-action-branch-property-missing";
            return branch != null;
        }

        public CommittedActionBranchEditorSnapshot Capture()
        {
            if (!TryGetBranchProperty(out SerializedProperty branch, out _))
                return new CommittedActionBranchEditorSnapshot(string.Empty, string.Empty, Array.Empty<CommittedActionBranchNodeEditorSnapshot>());

            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            string rootNodeId = branch.FindPropertyRelative("rootNodeId").stringValue;
            List<CommittedActionBranchNodeEditorSnapshot> snapshots = new List<CommittedActionBranchNodeEditorSnapshot>();
            for (int i = 0; nodes != null && i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                string nodeId = node.FindPropertyRelative("nodeId").stringValue;
                snapshots.Add(new CommittedActionBranchNodeEditorSnapshot(
                    nodeId,
                    (CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex,
                    node.propertyPath,
                    node.FindPropertyRelative("editorPosition").vector2Value,
                    ReadChildren(node),
                    (CommittedActionConditionKind)node.FindPropertyRelative("condition").FindPropertyRelative("kind").enumValueIndex,
                    (InputRequestKind)node.FindPropertyRelative("condition").FindPropertyRelative("requestKind").enumValueIndex,
                    node.FindPropertyRelative("condition").FindPropertyRelative("requiredFactId").stringValue,
                    (CharacterStateVariant)node.FindPropertyRelative("condition").FindPropertyRelative("expectedVariant").enumValueIndex,
                    node.FindPropertyRelative("timeline").FindPropertyRelative("timelineNodeId").stringValue,
                    node.FindPropertyRelative("timeline").FindPropertyRelative("durationSeconds").floatValue,
                    ReadPrimaryAnimationKey(node.FindPropertyRelative("timeline")),
                    string.Equals(nodeId, rootNodeId, StringComparison.Ordinal)));
            }

            return new CommittedActionBranchEditorSnapshot(
                branch.FindPropertyRelative("branchId").stringValue,
                branch.FindPropertyRelative("rootNodeId").stringValue,
                snapshots.ToArray());
        }

        public bool TryGetNodeProperty(string nodeId, out SerializedProperty node, out string diagnostic)
        {
            node = null;
            diagnostic = string.Empty;
            if (!TryGetNodesProperty(out SerializedProperty nodes, out diagnostic))
                return false;

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty candidate = nodes.GetArrayElementAtIndex(i);
                if (string.Equals(candidate.FindPropertyRelative("nodeId").stringValue, nodeId, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            diagnostic = $"node-missing:{nodeId}";
            return false;
        }

        public bool TryGetTimelineNodeId(CommittedActionTimelineVariant variant, out string nodeId)
        {
            nodeId = string.Empty;
            if (!TryGetNodesProperty(out SerializedProperty nodes, out _))
                return false;

            string expected = variant == CommittedActionTimelineVariant.Backstep ? "backstep" : "directional";
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                if ((CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex != CommittedActionNodeKind.Timeline)
                    continue;

                string candidate = node.FindPropertyRelative("nodeId").stringValue;
                if (candidate.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                nodeId = candidate;
                return true;
            }

            return false;
        }

        public bool AddNode(CommittedActionNodeKind kind, string nodeId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (kind == CommittedActionNodeKind.None || kind == CommittedActionNodeKind.Root)
            {
                diagnostic = "node-kind-invalid";
                return false;
            }
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                diagnostic = "node-id-missing";
                return false;
            }
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;
            if (TryGetNodeProperty(nodeId, out _, out _))
            {
                diagnostic = $"node-id-duplicate:{nodeId}";
                return false;
            }

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Add Node");
            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            int index = nodes.arraySize;
            nodes.InsertArrayElementAtIndex(index);
            InitializeNode(nodes.GetArrayElementAtIndex(index), kind, nodeId);
            EndEdit();
            return true;
        }

        public bool RemoveNode(string nodeId, out string diagnostic)
        {
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;

            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            if (nodes == null)
            {
                diagnostic = "branch-nodes-missing";
                return false;
            }

            string rootNodeId = branch.FindPropertyRelative("rootNodeId").stringValue;
            if (string.Equals(rootNodeId, nodeId, StringComparison.Ordinal))
            {
                diagnostic = $"root-node-protected:{nodeId}";
                return false;
            }

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                if (!string.Equals(node.FindPropertyRelative("nodeId").stringValue, nodeId, StringComparison.Ordinal))
                    continue;

                Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Remove Node");
                RemoveChildReferences(nodes, nodeId);
                nodes.DeleteArrayElementAtIndex(i);
                EndEdit();
                return true;
            }

            diagnostic = $"node-missing:{nodeId}";
            return false;
        }

        public bool RenameNode(string oldNodeId, string newNodeId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (string.IsNullOrWhiteSpace(newNodeId))
            {
                diagnostic = "node-id-missing";
                return false;
            }
            if (!TryGetNodeProperty(oldNodeId, out SerializedProperty node, out diagnostic))
                return false;
            if ((CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex == CommittedActionNodeKind.Root)
            {
                diagnostic = $"root-node-protected:{oldNodeId}";
                return false;
            }
            if (!string.Equals(oldNodeId, newNodeId, StringComparison.Ordinal) &&
                TryGetNodeProperty(newNodeId, out _, out _))
            {
                diagnostic = $"node-id-duplicate:{newNodeId}";
                return false;
            }

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Rename Node");
            node.FindPropertyRelative("nodeId").stringValue = newNodeId;
            serializedObject.ApplyModifiedProperties();
            if (TryGetBranchProperty(out SerializedProperty branch, out _) &&
                string.Equals(branch.FindPropertyRelative("rootNodeId").stringValue, oldNodeId, StringComparison.Ordinal))
            {
                branch.FindPropertyRelative("rootNodeId").stringValue = newNodeId;
            }

            ReplaceChildReferences(oldNodeId, newNodeId);
            EndEdit();
            return true;
        }

        public bool SetRootNode(string nodeId, out string diagnostic)
        {
            if (!TryGetNodeProperty(nodeId, out SerializedProperty node, out diagnostic))
                return false;
            if ((CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex != CommittedActionNodeKind.Root)
            {
                diagnostic = $"root-node-kind-invalid:{nodeId}";
                return false;
            }
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Root Node");
            branch.FindPropertyRelative("rootNodeId").stringValue = nodeId;
            EndEdit();
            return true;
        }

        public bool AppendChild(string parentNodeId, string childNodeId, out string diagnostic)
        {
            if (!TryGetNodeProperty(parentNodeId, out SerializedProperty parent, out diagnostic))
                return false;
            if (!TryGetNodeProperty(childNodeId, out _, out diagnostic))
                return false;

            SerializedProperty children = parent.FindPropertyRelative("childNodeIds");
            for (int i = 0; children != null && i < children.arraySize; i++)
            {
                if (string.Equals(children.GetArrayElementAtIndex(i).stringValue, childNodeId, StringComparison.Ordinal))
                {
                    diagnostic = $"child-duplicate:{parentNodeId}:{childNodeId}";
                    return false;
                }
            }

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Append Child");
            int index = children.arraySize;
            children.InsertArrayElementAtIndex(index);
            children.GetArrayElementAtIndex(index).stringValue = childNodeId;
            EndEdit();
            return true;
        }

        public bool SetConditionKind(string nodeId, CommittedActionConditionKind kind, out string diagnostic)
        {
            if (!TryGetConditionProperty(nodeId, out SerializedProperty condition, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Condition Kind");
            condition.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            EndEdit();
            return true;
        }

        public bool SetConditionRequestKind(string nodeId, InputRequestKind requestKind, out string diagnostic)
        {
            if (!TryGetConditionProperty(nodeId, out SerializedProperty condition, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Condition Request Kind");
            condition.FindPropertyRelative("requestKind").enumValueIndex = (int)requestKind;
            EndEdit();
            return true;
        }

        public bool SetConditionRequiredFactId(string nodeId, string requiredFactId, out string diagnostic)
        {
            if (!TryGetConditionProperty(nodeId, out SerializedProperty condition, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Condition Required Fact");
            condition.FindPropertyRelative("requiredFactId").stringValue = requiredFactId ?? string.Empty;
            EndEdit();
            return true;
        }

        public bool SetConditionActionVariant(string nodeId, CharacterStateVariant variant, out string diagnostic)
        {
            if (!TryGetConditionProperty(nodeId, out SerializedProperty condition, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Condition Variant");
            condition.FindPropertyRelative("expectedVariant").enumValueIndex = (int)variant;
            EndEdit();
            return true;
        }

        public bool SetConditionExpectedVariant(string nodeId, CharacterStateVariant variant, out string diagnostic)
        {
            return SetConditionActionVariant(nodeId, variant, out diagnostic);
        }

        public bool InitializeMinimalBranchTemplate(out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;

            CharacterActionDefinition definition = actionDefinition.ToDefinition(
                ActionTimelineCompileContext.FromTickRate(ThirdPersonSimulation.SimulationTickRate.Default));
            string branchId = BranchIdFor(definition.ActionState.Value);
            string rootNodeId = $"branch.root.{branchId}";
            string timelineNodeId = $"timeline.{branchId}.main";
            float duration = Mathf.Max(0.1f, definition.DirectionalDodge.Duration > 0f ? definition.DirectionalDodge.Duration : 0.1f);

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Initialize Branch");
            WriteBranchHeader(branch, branchId, rootNodeId);
            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            nodes.arraySize = 0;
            SerializedProperty root = AppendNode(nodes, CommittedActionNodeKind.Root, rootNodeId, new Vector2(0f, 0f));
            SetChildren(root, timelineNodeId);
            SerializedProperty timeline = AppendNode(nodes, CommittedActionNodeKind.Timeline, timelineNodeId, new Vector2(360f, 0f));
            ConfigureTimelineNode(timeline, $"{branchId}.main", timelineNodeId, duration);
            EndEdit();

            CharacterStateId sourceState = definition.MotionSourceState.IsValid
                ? definition.MotionSourceState
                : new CharacterStateId(definition.ActionState.Value);
            ActionAnimationKey animationKey = definition.DirectionalDodge.AnimationKey.IsValid
                ? definition.DirectionalDodge.AnimationKey
                : new ActionAnimationKey($"{definition.ActionState.Value}.Main");
            return AddAnimationMotionTimeline(
                timelineNodeId,
                CommittedActionTimelineVariant.Generic,
                animationKey,
                sourceState,
                CharacterStateVariant.None,
                duration,
                Mathf.Max(0f, definition.DirectionalDodge.Distance),
                definition.DirectionalDodge.RotateToDirection,
                out diagnostic);
        }

        public bool InitializeDodgeBranchTemplate(out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;

            CharacterActionDefinition definition = actionDefinition.ToDefinition(
                ActionTimelineCompileContext.FromTickRate(ThirdPersonSimulation.SimulationTickRate.Default));
            DodgeActionVariantDefinition directional = definition.DirectionalDodge.HasDefinition
                ? definition.DirectionalDodge
                : new DodgeActionVariantDefinition(
                    DodgeActionVariant.Directional,
                    0.35f,
                    4f,
                    true,
                    ActionAnimationKeys.DodgeDirectional);
            DodgeActionVariantDefinition backstep = definition.BackstepDodge.HasDefinition
                ? definition.BackstepDodge
                : new DodgeActionVariantDefinition(
                    DodgeActionVariant.Backstep,
                    0.35f,
                    3f,
                    false,
                    ActionAnimationKeys.DodgeBackstep);
            CharacterStateId sourceState = definition.MotionSourceState.IsValid
                ? definition.MotionSourceState
                : CharacterStateIds.Dodge;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Initialize Dodge Branch");
            WriteBranchHeader(branch, "action.dodge", "branch.root.action.dodge");
            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            nodes.arraySize = 0;
            SerializedProperty root = AppendNode(nodes, CommittedActionNodeKind.Root, "branch.root.action.dodge", new Vector2(0f, 0f));
            SetChildren(root, "selector.dodge");
            SerializedProperty selector = AppendNode(nodes, CommittedActionNodeKind.Selector, "selector.dodge", new Vector2(320f, 0f));
            SetChildren(selector, "condition.dodge.directional", "condition.dodge.backstep");
            SerializedProperty directionalCondition = AppendNode(nodes, CommittedActionNodeKind.Condition, "condition.dodge.directional", new Vector2(660f, -120f));
            ConfigureConditionNode(directionalCondition, CharacterStateVariant.Directional, "timeline.dodge.directional");
            SerializedProperty backstepCondition = AppendNode(nodes, CommittedActionNodeKind.Condition, "condition.dodge.backstep", new Vector2(660f, 120f));
            ConfigureConditionNode(backstepCondition, CharacterStateVariant.Backstep, "timeline.dodge.backstep");
            SerializedProperty directionalTimeline = AppendNode(nodes, CommittedActionNodeKind.Timeline, "timeline.dodge.directional", new Vector2(1000f, -120f));
            ConfigureTimelineNode(directionalTimeline, "action.dodge.directional", "timeline.dodge.directional", directional.Duration);
            SerializedProperty backstepTimeline = AppendNode(nodes, CommittedActionNodeKind.Timeline, "timeline.dodge.backstep", new Vector2(1000f, 120f));
            ConfigureTimelineNode(backstepTimeline, "action.dodge.backstep", "timeline.dodge.backstep", backstep.Duration);
            EndEdit();

            if (!AddAnimationMotionTimeline(
                    "timeline.dodge.directional",
                    CommittedActionTimelineVariant.Directional,
                    directional.AnimationKey,
                    sourceState,
                    CharacterStateVariant.Directional,
                    directional.Duration,
                    directional.Distance,
                    directional.RotateToDirection,
                    out diagnostic))
                return false;

            return AddAnimationMotionTimeline(
                "timeline.dodge.backstep",
                CommittedActionTimelineVariant.Backstep,
                backstep.AnimationKey,
                sourceState,
                CharacterStateVariant.Backstep,
                backstep.Duration,
                backstep.Distance,
                backstep.RotateToDirection,
                out diagnostic);
        }

        public bool RemoveChild(string parentNodeId, string childNodeId, out string diagnostic)
        {
            if (!TryGetNodeProperty(parentNodeId, out SerializedProperty parent, out diagnostic))
                return false;

            SerializedProperty children = parent.FindPropertyRelative("childNodeIds");
            for (int i = 0; children != null && i < children.arraySize; i++)
            {
                if (!string.Equals(children.GetArrayElementAtIndex(i).stringValue, childNodeId, StringComparison.Ordinal))
                    continue;

                Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Remove Child");
                children.DeleteArrayElementAtIndex(i);
                EndEdit();
                return true;
            }

            diagnostic = $"child-missing:{parentNodeId}:{childNodeId}";
            return false;
        }

        public bool ReorderChild(string parentNodeId, int fromIndex, int toIndex, out string diagnostic)
        {
            if (!TryGetNodeProperty(parentNodeId, out SerializedProperty node, out diagnostic))
                return false;

            SerializedProperty children = node.FindPropertyRelative("childNodeIds");
            if (children == null || fromIndex < 0 || fromIndex >= children.arraySize)
            {
                diagnostic = $"child-index-invalid:{fromIndex}";
                return false;
            }

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Reorder Child");
            children.MoveArrayElement(fromIndex, Mathf.Clamp(toIndex, 0, children.arraySize - 1));
            EndEdit();
            return true;
        }

        public bool SetNodePosition(string nodeId, Vector2 position, out string diagnostic)
        {
            if (!TryGetNodeProperty(nodeId, out SerializedProperty node, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, "Committed Action Branch Editor: Set Node Position");
            node.FindPropertyRelative("editorPosition").vector2Value = position;
            EndEdit();
            return true;
        }

        public bool Save(out CharacterActionCatalogValidationResult validation)
        {
            validation = new CharacterActionCatalogValidationResult();
            if (!IsValid)
                return false;

            serializedObject.ApplyModifiedProperties();
            validation = actionDefinition.Validate(ActionTimelineCompileContext.FromTickRate(ThirdPersonSimulation.SimulationTickRate.Default));
            EditorUtility.SetDirty(actionDefinition);
            AssetDatabase.SaveAssets();
            return !validation.HasErrors;
        }

        bool TryGetNodesProperty(out SerializedProperty nodes, out string diagnostic)
        {
            nodes = null;
            if (!TryGetBranchProperty(out SerializedProperty branch, out diagnostic))
                return false;

            nodes = branch.FindPropertyRelative("nodes");
            if (nodes == null)
                diagnostic = "branch-nodes-missing";
            return nodes != null;
        }

        bool TryGetConditionProperty(string nodeId, out SerializedProperty condition, out string diagnostic)
        {
            condition = null;
            if (!TryGetNodeProperty(nodeId, out SerializedProperty node, out diagnostic))
                return false;

            condition = node.FindPropertyRelative("condition");
            if (condition == null)
                diagnostic = $"node-condition-missing:{nodeId}";
            return condition != null;
        }

        static void RemoveChildReferences(SerializedProperty nodes, string nodeId)
        {
            for (int i = 0; i < nodes.arraySize; i++)
                RemoveChildReference(nodes.GetArrayElementAtIndex(i), nodeId);
        }

        void ReplaceChildReferences(string oldNodeId, string newNodeId)
        {
            if (!TryGetNodesProperty(out SerializedProperty nodes, out _))
                return;

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty children = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("childNodeIds");
                for (int childIndex = 0; children != null && childIndex < children.arraySize; childIndex++)
                {
                    SerializedProperty child = children.GetArrayElementAtIndex(childIndex);
                    if (string.Equals(child.stringValue, oldNodeId, StringComparison.Ordinal))
                        child.stringValue = newNodeId;
                }
            }
        }

        static void RemoveChildReference(SerializedProperty node, string nodeId)
        {
            SerializedProperty children = node.FindPropertyRelative("childNodeIds");
            for (int i = children != null ? children.arraySize - 1 : -1; i >= 0; i--)
            {
                if (string.Equals(children.GetArrayElementAtIndex(i).stringValue, nodeId, StringComparison.Ordinal))
                    children.DeleteArrayElementAtIndex(i);
            }
        }

        static void InitializeNode(SerializedProperty node, CommittedActionNodeKind kind, string nodeId)
        {
            node.FindPropertyRelative("nodeId").stringValue = nodeId;
            node.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            node.FindPropertyRelative("childNodeIds").arraySize = 0;
            node.FindPropertyRelative("editorPosition").vector2Value = Vector2.zero;
            SerializedProperty condition = node.FindPropertyRelative("condition");
            condition.FindPropertyRelative("kind").enumValueIndex = (int)CommittedActionConditionKind.None;
            condition.FindPropertyRelative("expectedVariant").enumValueIndex = (int)CharacterStateVariant.None;
            condition.FindPropertyRelative("expectedBool").boolValue = false;
            condition.FindPropertyRelative("requestKind").enumValueIndex = 0;
            condition.FindPropertyRelative("requiredFactId").stringValue = string.Empty;
            SerializedProperty timeline = node.FindPropertyRelative("timeline");
            timeline.FindPropertyRelative("required").boolValue = kind == CommittedActionNodeKind.Timeline;
            timeline.FindPropertyRelative("branchId").stringValue = nodeId;
            timeline.FindPropertyRelative("timelineNodeId").stringValue = nodeId;
            timeline.FindPropertyRelative("durationSeconds").floatValue = 0f;
            timeline.FindPropertyRelative("defaultBodyKind").enumValueIndex = (int)BodyOccupancyKind.FullBody;
            timeline.FindPropertyRelative("defaultChannels").intValue =
                (int)(CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation);
            timeline.FindPropertyRelative("tracks").arraySize = 0;
        }

        static SerializedProperty AppendNode(
            SerializedProperty nodes,
            CommittedActionNodeKind kind,
            string nodeId,
            Vector2 position)
        {
            int index = nodes.arraySize;
            nodes.InsertArrayElementAtIndex(index);
            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            InitializeNode(node, kind, nodeId);
            node.FindPropertyRelative("editorPosition").vector2Value = position;
            return node;
        }

        static void WriteBranchHeader(SerializedProperty branch, string branchId, string rootNodeId)
        {
            branch.FindPropertyRelative("schemaVersion").intValue = 1;
            branch.FindPropertyRelative("required").boolValue = true;
            branch.FindPropertyRelative("branchId").stringValue = branchId;
            branch.FindPropertyRelative("rootNodeId").stringValue = rootNodeId;
            branch.FindPropertyRelative("defaultBodyKind").enumValueIndex = (int)BodyOccupancyKind.FullBody;
            branch.FindPropertyRelative("defaultChannels").intValue =
                (int)(CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation);
        }

        static void SetChildren(SerializedProperty node, params string[] childNodeIds)
        {
            SerializedProperty children = node.FindPropertyRelative("childNodeIds");
            children.arraySize = childNodeIds?.Length ?? 0;
            for (int i = 0; childNodeIds != null && i < childNodeIds.Length; i++)
                children.GetArrayElementAtIndex(i).stringValue = childNodeIds[i] ?? string.Empty;
        }

        static void ConfigureConditionNode(
            SerializedProperty node,
            CharacterStateVariant expectedVariant,
            string childNodeId)
        {
            SerializedProperty condition = node.FindPropertyRelative("condition");
            condition.FindPropertyRelative("kind").enumValueIndex = (int)CommittedActionConditionKind.ActionVariantEquals;
            condition.FindPropertyRelative("expectedVariant").enumValueIndex = (int)expectedVariant;
            condition.FindPropertyRelative("expectedBool").boolValue = false;
            condition.FindPropertyRelative("requestKind").enumValueIndex = 0;
            condition.FindPropertyRelative("requiredFactId").stringValue = string.Empty;
            SetChildren(node, childNodeId);
        }

        static void ConfigureTimelineNode(
            SerializedProperty node,
            string branchId,
            string timelineNodeId,
            float durationSeconds)
        {
            SerializedProperty timeline = node.FindPropertyRelative("timeline");
            timeline.FindPropertyRelative("required").boolValue = true;
            timeline.FindPropertyRelative("branchId").stringValue = branchId;
            timeline.FindPropertyRelative("timelineNodeId").stringValue = timelineNodeId;
            timeline.FindPropertyRelative("durationSeconds").floatValue = Mathf.Max(0f, durationSeconds);
            timeline.FindPropertyRelative("defaultBodyKind").enumValueIndex = (int)BodyOccupancyKind.FullBody;
            timeline.FindPropertyRelative("defaultChannels").intValue =
                (int)(CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation);
            timeline.FindPropertyRelative("tracks").arraySize = 0;
        }

        bool AddAnimationMotionTimeline(
            string timelineNodeId,
            CommittedActionTimelineVariant variant,
            ActionAnimationKey animationKey,
            CharacterStateId sourceState,
            CharacterStateVariant motionVariant,
            float durationSeconds,
            float distance,
            bool rotateToDirection,
            out string diagnostic)
        {
            CommittedActionTimelineSerializedAdapter timelineAdapter =
                new CommittedActionTimelineSerializedAdapter(actionDefinition, serializedObject, timelineNodeId);
            float duration = Mathf.Max(0.01f, durationSeconds);
            if (!timelineAdapter.AddTrack(variant, ActionTimelineTrackKind.Animation, out diagnostic))
                return false;
            if (!timelineAdapter.AddClip(variant, 0, ActionTimelineClipKind.AnimationKey, 0f, duration, out diagnostic))
                return false;
            if (!timelineAdapter.SetAnimationKey(variant, 0, 0, animationKey, out diagnostic))
                return false;
            if (!timelineAdapter.AddTrack(variant, ActionTimelineTrackKind.Motion, out diagnostic))
                return false;
            if (!timelineAdapter.AddClip(variant, 1, ActionTimelineClipKind.Motion, 0f, duration, out diagnostic))
                return false;
            return timelineAdapter.SetMotionPayload(
                variant,
                1,
                0,
                sourceState,
                motionVariant,
                duration,
                distance,
                rotateToDirection,
                false,
                out diagnostic);
        }

        static string BranchIdFor(string actionStateId)
        {
            string value = string.IsNullOrWhiteSpace(actionStateId) ? "action.branch" : actionStateId.Trim();
            return value.Replace('/', '.').Replace(' ', '.').ToLowerInvariant();
        }

        static IReadOnlyList<string> ReadChildren(SerializedProperty node)
        {
            SerializedProperty children = node.FindPropertyRelative("childNodeIds");
            if (children == null || children.arraySize == 0)
                return Array.Empty<string>();

            string[] result = new string[children.arraySize];
            for (int i = 0; i < children.arraySize; i++)
                result[i] = children.GetArrayElementAtIndex(i).stringValue;
            return result;
        }

        static string ReadPrimaryAnimationKey(SerializedProperty timeline)
        {
            SerializedProperty tracks = timeline?.FindPropertyRelative("tracks");
            for (int trackIndex = 0; tracks != null && trackIndex < tracks.arraySize; trackIndex++)
            {
                SerializedProperty clips = tracks.GetArrayElementAtIndex(trackIndex).FindPropertyRelative("clips");
                for (int clipIndex = 0; clips != null && clipIndex < clips.arraySize; clipIndex++)
                {
                    SerializedProperty clip = clips.GetArrayElementAtIndex(clipIndex);
                    if ((ActionTimelineClipKind)clip.FindPropertyRelative("kind").enumValueIndex != ActionTimelineClipKind.AnimationKey)
                        continue;

                    return clip.FindPropertyRelative("payload").FindPropertyRelative("animationKey").stringValue;
                }
            }

            return string.Empty;
        }

        void EndEdit()
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(actionDefinition);
        }
    }

    public readonly struct ActionTransitionPolicyMatrixRowEditorSnapshot
    {
        public ActionTransitionPolicyMatrixRowEditorSnapshot(
            int index,
            ActionTransitionPolicyRowDefinition row)
        {
            Index = index;
            Row = row;
        }

        public int Index { get; }
        public ActionTransitionPolicyRowDefinition Row { get; }
    }

    public sealed class ActionTransitionPolicyMatrixEditorSnapshot
    {
        readonly ActionTransitionPolicyMatrixRowEditorSnapshot[] rows;

        public ActionTransitionPolicyMatrixEditorSnapshot(ActionTransitionPolicyMatrixRowEditorSnapshot[] rows)
        {
            this.rows = rows ?? Array.Empty<ActionTransitionPolicyMatrixRowEditorSnapshot>();
        }

        public IReadOnlyList<ActionTransitionPolicyMatrixRowEditorSnapshot> Rows => rows;
    }

    public sealed class ActionTransitionPolicyMatrixSerializedAdapter
    {
        readonly ActionInterruptPolicySetSO policySet;
        readonly SerializedObject serializedObject;

        public ActionTransitionPolicyMatrixSerializedAdapter(ActionInterruptPolicySetSO policySet)
            : this(policySet, policySet != null ? new SerializedObject(policySet) : null)
        {
        }

        public ActionTransitionPolicyMatrixSerializedAdapter(
            ActionInterruptPolicySetSO policySet,
            SerializedObject serializedObject)
        {
            this.policySet = policySet;
            this.serializedObject = serializedObject;
        }

        public bool IsValid => policySet != null && serializedObject != null;

        public ActionTransitionPolicyMatrixEditorSnapshot Capture()
        {
            if (!TryGetRowsProperty(out SerializedProperty policies, out _))
                return new ActionTransitionPolicyMatrixEditorSnapshot(Array.Empty<ActionTransitionPolicyMatrixRowEditorSnapshot>());

            ActionTransitionPolicyMatrixRowEditorSnapshot[] result =
                new ActionTransitionPolicyMatrixRowEditorSnapshot[policies.arraySize];
            for (int i = 0; i < policies.arraySize; i++)
                result[i] = new ActionTransitionPolicyMatrixRowEditorSnapshot(i, ReadRow(policies.GetArrayElementAtIndex(i)));

            return new ActionTransitionPolicyMatrixEditorSnapshot(result);
        }

        public ActionTransitionPolicyMatrixDefinition ToMatrix()
        {
            ActionTransitionPolicyMatrixEditorSnapshot snapshot = Capture();
            ActionTransitionPolicyRowDefinition[] rows = new ActionTransitionPolicyRowDefinition[snapshot.Rows.Count];
            for (int i = 0; i < snapshot.Rows.Count; i++)
                rows[i] = snapshot.Rows[i].Row;
            return new ActionTransitionPolicyMatrixDefinition(rows);
        }

        public bool AddRow(ActionTransitionPolicyRowDefinition row, out string diagnostic)
        {
            if (!TryGetRowsProperty(out SerializedProperty policies, out diagnostic))
                return false;

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Add Row");
            int index = policies.arraySize;
            policies.InsertArrayElementAtIndex(index);
            WriteRow(policies.GetArrayElementAtIndex(index), row);
            EndEdit();
            return true;
        }

        public bool RemoveRow(int index, out string diagnostic)
        {
            if (!TryGetRowsProperty(out SerializedProperty policies, out diagnostic))
                return false;
            if (index < 0 || index >= policies.arraySize)
            {
                diagnostic = $"matrix-row-index-invalid:{index}";
                return false;
            }

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Remove Row");
            policies.DeleteArrayElementAtIndex(index);
            EndEdit();
            return true;
        }

        public bool MoveRow(int fromIndex, int toIndex, out string diagnostic)
        {
            if (!TryGetRowsProperty(out SerializedProperty policies, out diagnostic))
                return false;
            if (fromIndex < 0 || fromIndex >= policies.arraySize)
            {
                diagnostic = $"matrix-row-index-invalid:{fromIndex}";
                return false;
            }

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Move Row");
            policies.MoveArrayElement(fromIndex, Mathf.Clamp(toIndex, 0, policies.arraySize - 1));
            EndEdit();
            return true;
        }

        public bool SetFromActionId(int index, string value, out string diagnostic)
        {
            return SetString(index, "fromStateId", value, "Set From Action", out diagnostic);
        }

        public bool SetToActionId(int index, string value, out string diagnostic)
        {
            return SetString(index, "targetStateId", value, "Set To Action", out diagnostic);
        }

        public bool SetRequestType(int index, ActionRequestType requestType, out string diagnostic)
        {
            if (!TryGetRowProperty(index, out SerializedProperty row, out diagnostic))
                return false;

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Set Request");
            row.FindPropertyRelative("requestType").intValue = (int)requestType;
            EndEdit();
            return true;
        }

        public bool SetRequiredFactId(int index, string value, out string diagnostic)
        {
            return SetString(index, "requiredFactId", value, "Set Required Fact", out diagnostic);
        }

        public bool SetMinPriority(int index, int value, out string diagnostic)
        {
            if (!TryGetRowProperty(index, out SerializedProperty row, out diagnostic))
                return false;

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Set Min Priority");
            row.FindPropertyRelative("minPriority").intValue = value;
            EndEdit();
            return true;
        }

        public bool SetForce(int index, bool value, out string diagnostic)
        {
            if (!TryGetRowProperty(index, out SerializedProperty row, out diagnostic))
                return false;

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Set Force");
            row.FindPropertyRelative("force").boolValue = value;
            EndEdit();
            return true;
        }

        public bool SetResistanceRule(int index, ActionTransitionResistanceRule value, out string diagnostic)
        {
            if (!TryGetRowProperty(index, out SerializedProperty row, out diagnostic))
                return false;

            Undo.RecordObject(policySet, "Action Transition Policy Matrix: Set Resistance");
            row.FindPropertyRelative("resistanceRule").intValue = (int)value;
            EndEdit();
            return true;
        }

        public bool Save(ActionFactCompileContext factContext, out ActionInterruptPolicyValidationResult validation)
        {
            validation = new ActionInterruptPolicyValidationResult();
            if (!IsValid)
                return false;

            serializedObject.ApplyModifiedProperties();
            validation = ActionTransitionPolicyMatrixValidator.Validate(ToMatrix(), factContext);
            EditorUtility.SetDirty(policySet);
            AssetDatabase.SaveAssets();
            return !validation.HasErrors;
        }

        bool SetString(int index, string propertyName, string value, string undoLabel, out string diagnostic)
        {
            if (!TryGetRowProperty(index, out SerializedProperty row, out diagnostic))
                return false;

            Undo.RecordObject(policySet, $"Action Transition Policy Matrix: {undoLabel}");
            row.FindPropertyRelative(propertyName).stringValue = value ?? string.Empty;
            EndEdit();
            return true;
        }

        bool TryGetRowsProperty(out SerializedProperty policies, out string diagnostic)
        {
            policies = null;
            diagnostic = string.Empty;
            if (!IsValid)
            {
                diagnostic = "policy-set-missing";
                return false;
            }

            serializedObject.UpdateIfRequiredOrScript();
            policies = serializedObject.FindProperty("policies");
            if (policies == null)
                diagnostic = "policy-rows-missing";
            return policies != null;
        }

        bool TryGetRowProperty(int index, out SerializedProperty row, out string diagnostic)
        {
            row = null;
            if (!TryGetRowsProperty(out SerializedProperty rows, out diagnostic))
                return false;
            if (index < 0 || index >= rows.arraySize)
            {
                diagnostic = $"matrix-row-index-invalid:{index}";
                return false;
            }

            row = rows.GetArrayElementAtIndex(index);
            return true;
        }

        static ActionTransitionPolicyRowDefinition ReadRow(SerializedProperty row)
        {
            return new ActionTransitionPolicyRowDefinition(
                row.FindPropertyRelative("fromStateId").stringValue,
                row.FindPropertyRelative("targetStateId").stringValue,
                (ActionRequestType)row.FindPropertyRelative("requestType").intValue,
                row.FindPropertyRelative("requiredFactId").stringValue,
                row.FindPropertyRelative("minPriority").intValue,
                row.FindPropertyRelative("force").boolValue,
                (ActionTransitionResistanceRule)row.FindPropertyRelative("resistanceRule").intValue,
                row.FindPropertyRelative("note").stringValue);
        }

        static void WriteRow(SerializedProperty target, ActionTransitionPolicyRowDefinition row)
        {
            target.FindPropertyRelative("fromStateId").stringValue = row.FromActionId;
            target.FindPropertyRelative("targetStateId").stringValue = row.ToActionId;
            target.FindPropertyRelative("requestType").intValue = (int)row.RequestType;
            target.FindPropertyRelative("minPriority").intValue = row.MinPriority;
            target.FindPropertyRelative("timingRule").enumValueIndex = (int)ActionInterruptTimingRule.Always;
            target.FindPropertyRelative("windowStart").floatValue = 0f;
            target.FindPropertyRelative("windowEnd").floatValue = 0f;
            target.FindPropertyRelative("windowId").stringValue = string.Empty;
            target.FindPropertyRelative("requiredFactId").stringValue = row.RequiredFactId;
            target.FindPropertyRelative("force").boolValue = row.Force;
            target.FindPropertyRelative("resistanceRule").intValue = (int)row.ResistanceRule;
            target.FindPropertyRelative("note").stringValue = row.DiagnosticsLabel;
        }

        void EndEdit()
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(policySet);
        }
    }
}
