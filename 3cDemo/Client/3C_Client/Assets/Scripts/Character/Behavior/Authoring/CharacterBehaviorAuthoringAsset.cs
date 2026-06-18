using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacterBehavior.Authoring
{
    public static class CharacterBehaviorAuthoringPortIds
    {
        public const string Input = "in";
        public const string Children = "children";
    }

    public enum CharacterBehaviorAuthoringNodeKind
    {
        None = 0,
        Root = 1,
        Parallel = 2,
        LocomotionLeaf = 3,
        CommittedActionLeaf = 4
    }

    [Serializable]
    public struct CharacterBehaviorAuthoringNode
    {
        [SerializeField] string stableId;
        [SerializeField] CharacterBehaviorAuthoringNodeKind kind;
        [SerializeField] Vector2 editorPosition;

        public CharacterBehaviorAuthoringNode(
            string stableId,
            CharacterBehaviorAuthoringNodeKind kind,
            Vector2 editorPosition)
        {
            this.stableId = stableId ?? string.Empty;
            this.kind = kind;
            this.editorPosition = editorPosition;
        }

        public string StableId => stableId ?? string.Empty;
        public CharacterBehaviorAuthoringNodeKind Kind => kind;
        public Vector2 EditorPosition => editorPosition;
        public bool IsValid => !string.IsNullOrWhiteSpace(StableId) && Kind != CharacterBehaviorAuthoringNodeKind.None;

        public CharacterBehaviorAuthoringNode WithEditorPosition(Vector2 position)
        {
            return new CharacterBehaviorAuthoringNode(StableId, Kind, position);
        }
    }

    [Serializable]
    public struct CharacterBehaviorAuthoringEdge
    {
        [SerializeField] string parentNodeId;
        [SerializeField] string childNodeId;
        [SerializeField] string outputPortId;
        [SerializeField] string inputPortId;

        public CharacterBehaviorAuthoringEdge(
            string parentNodeId,
            string childNodeId,
            string outputPortId,
            string inputPortId)
        {
            this.parentNodeId = parentNodeId ?? string.Empty;
            this.childNodeId = childNodeId ?? string.Empty;
            this.outputPortId = outputPortId ?? string.Empty;
            this.inputPortId = inputPortId ?? string.Empty;
        }

        public string ParentNodeId => parentNodeId ?? string.Empty;
        public string ChildNodeId => childNodeId ?? string.Empty;
        public string OutputPortId => outputPortId ?? string.Empty;
        public string InputPortId => inputPortId ?? string.Empty;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ParentNodeId) &&
            !string.IsNullOrWhiteSpace(ChildNodeId) &&
            !string.IsNullOrWhiteSpace(OutputPortId) &&
            !string.IsNullOrWhiteSpace(InputPortId);
    }

    [CreateAssetMenu(fileName = "CharacterBehaviorAuthoring", menuName = "3C/Behavior/Character Behavior Authoring")]
    public sealed class CharacterBehaviorAuthoringAsset : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField, Min(0)] int schemaVersion = CurrentSchemaVersion;
        [SerializeField] string stableAssetId = "character.behavior.authoring";
        [SerializeField] CharacterBehaviorAuthoringNode[] nodes = Array.Empty<CharacterBehaviorAuthoringNode>();
        [SerializeField] CharacterBehaviorAuthoringEdge[] edges = Array.Empty<CharacterBehaviorAuthoringEdge>();

        public int SchemaVersion => schemaVersion;
        public string StableAssetId => stableAssetId ?? string.Empty;
        public IReadOnlyList<CharacterBehaviorAuthoringNode> Nodes => nodes ?? Array.Empty<CharacterBehaviorAuthoringNode>();
        public IReadOnlyList<CharacterBehaviorAuthoringEdge> Edges => edges ?? Array.Empty<CharacterBehaviorAuthoringEdge>();

        public void SetSchemaVersion(int value)
        {
            schemaVersion = Mathf.Max(0, value);
        }

        public void SetStableAssetId(string value)
        {
            stableAssetId = value ?? string.Empty;
        }

        public void SetGraph(
            CharacterBehaviorAuthoringNode[] newNodes,
            CharacterBehaviorAuthoringEdge[] newEdges)
        {
            nodes = newNodes != null ? (CharacterBehaviorAuthoringNode[])newNodes.Clone() : Array.Empty<CharacterBehaviorAuthoringNode>();
            edges = newEdges != null ? (CharacterBehaviorAuthoringEdge[])newEdges.Clone() : Array.Empty<CharacterBehaviorAuthoringEdge>();
        }

        void Reset()
        {
            EnsureStableAssetId();
        }

        void OnValidate()
        {
            if (schemaVersion == 0)
                return;

            EnsureStableAssetId();
        }

        void EnsureStableAssetId()
        {
            if (string.IsNullOrWhiteSpace(stableAssetId))
                stableAssetId = $"character.behavior.{Guid.NewGuid():N}";
        }
    }
}
