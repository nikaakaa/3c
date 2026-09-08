using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseCanvasBinding : VisualElement, IDisposable
    {
        NodeCanvas.Framework.Graph m_Graph;
        readonly Dictionary<string, CharacterPoseDocumentCanvas> m_DocumentCanvases = new Dictionary<string, CharacterPoseDocumentCanvas>(StringComparer.Ordinal);
        bool m_RuntimeReadOnly;
        readonly CharacterPoseCanvasObservation m_Observation = new CharacterPoseCanvasObservation();
        public GraphAuthoringProjectionCanvasBinding ProjectionBinding { get; private set; }
        public GraphAuthoringStateMachineBinding StateMachineBinding { get; private set; }
        internal bool PersistsLayout => StateMachineBinding?.Policy.PersistsLayout ?? ProjectionBinding.PersistsLayout;
        public event Action<Vector2, IReadOnlyList<GraphAuthoringCapabilityDescriptor>> NodeCreationRequested;
        public event Action<Vector2> StateMachineNodeCreationRequested;
        public event Action<GraphAuthoringNodeProjection, GraphAuthoringChildSurfaceDescriptor> ChildSurfaceRequested;
        internal NodeCanvas.Framework.Graph Graph => m_Graph;

        public void BindProjection(GraphAuthoringProjectionCanvasBinding binding)
        {
            ProjectionBinding = binding ?? throw new ArgumentNullException(nameof(binding));
            StateMachineBinding = null;
            PopulateProjection();
        }

        public void BindStateMachine(GraphAuthoringStateMachineBinding binding)
        {
            StateMachineBinding = binding ?? throw new ArgumentNullException(nameof(binding));
            ProjectionBinding = null;
            PopulateStateMachine();
        }

        public void PopulateProjection()
        {
            if (ProjectionBinding.Document is CharacterPoseCanvasGraphDocument pose)
                SetGraph(pose.Graph);
            else
                RefreshDocumentCanvas();
            Activate();
        }

        public void PopulateStateMachine()
        {
            RefreshDocumentCanvas();
            Activate();
        }

        void RefreshDocumentCanvas()
        {
            string id = StateMachineBinding?.Document.DocumentId ?? ProjectionBinding.Document.DocumentId;
            if (!m_DocumentCanvases.TryGetValue(id, out CharacterPoseDocumentCanvas canvas))
            {
                canvas = ScriptableObject.CreateInstance<CharacterPoseDocumentCanvas>();
                canvas.hideFlags = HideFlags.HideAndDontSave;
                canvas.Binding = this;
                m_DocumentCanvases.Add(id, canvas);
            }
            canvas.StateMachineBinding = StateMachineBinding;
            canvas.ProjectionBinding = ProjectionBinding;
            SetGraph(canvas);
            canvas.name = StateMachineBinding?.Document.DisplayName ?? ProjectionBinding.Document.DisplayName;
            canvas.RefreshDocument();
        }

        void SetGraph(NodeCanvas.Framework.Graph graph)
        {
            if (m_Graph != null && ReferenceEquals(m_Graph.editorObservation, m_Observation)) m_Graph.editorObservation = null;
            m_Graph = graph;
        }

        void Activate()
        {
            if (GraphEditor.current == null)
                GraphEditor.OpenWindow(m_Graph);
            else if (GraphEditor.currentGraph != m_Graph)
                GraphEditor.SetReferences(m_Graph);
            SetRuntimeReadOnly(m_RuntimeReadOnly);
        }

        internal bool HandleCommand(string command, Vector2 position)
        {
            if (StateMachineBinding == null)
                return CharacterPoseCanvasCommands.Handle(ProjectionBinding, m_Graph, command, position);
            if (command is not ("Copy" or "Cut" or "Paste" or "Duplicate" or "Delete" or "SoftDelete"))
                return false;
            CharacterPoseCanvasInteraction.Apply(() =>
            {
                if (command is not ("Delete" or "SoftDelete"))
                    throw new InvalidOperationException("State machine elements use their dedicated create and configure commands.");
                IReadOnlyList<GraphAuthoringSelection> selection = GetStableSelection();
                var requests = new List<GraphAuthoringMutationRequest>();
                foreach (GraphAuthoringSelection item in selection)
                {
                    if (item.ElementId.Equals(StateMachineBinding.Document.Entry.ElementId))
                        throw new InvalidOperationException("Entry cannot be deleted.");
                    GraphAuthoringMutationKind kind = item.Kind == GraphAuthoringSelectionKind.Transition
                        ? GraphAuthoringMutationKind.DeleteTransition
                        : StateMachineBinding.Document.Aliases.Any(value => value.AliasId.Equals(item.ElementId))
                            ? GraphAuthoringMutationKind.DeleteStateAlias : GraphAuthoringMutationKind.DeleteState;
                    requests.Add(new GraphAuthoringMutationRequest(kind, item.ElementId));
                }
                if (requests.Count != 0)
                    StateMachineBinding.Mutation.Apply(StateMachineBinding.Document, requests);
                RefreshDocumentCanvas();
            });
            return true;
        }

        public void CreateNode(GraphAuthoringCapabilityId capabilityId, object typedPayload, Vector2 position)
        {
            ProjectionBinding.Mutation.Apply(ProjectionBinding.Document,
                new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.CreateNode,
                    capabilityId: capabilityId, value: typedPayload, position: position));
            PopulateProjection();
        }

        internal void RequestCreation(Vector2 position)
        {
            if (StateMachineBinding != null)
                StateMachineNodeCreationRequested?.Invoke(position);
            else
                NodeCreationRequested?.Invoke(position, ProjectionBinding.Capabilities.GetAllowed(
                    ProjectionBinding.Document.DomainId, ProjectionBinding.Document.DocumentRoleId));
        }

        internal void OpenChild(GraphAuthoringElementId id)
        {
            if (StateMachineBinding != null)
            {
                GraphAuthoringStateProjection state = StateMachineBinding.Document.States.Single(value => value.StateId.Equals(id));
                StateMachineBinding.Policy.OpenStateChildGraph(StateMachineBinding.Document, state.StateId);
                return;
            }
            GraphAuthoringNodeProjection node = ProjectionBinding.Document.Nodes.Single(value => value.NodeId.Equals(id));
            GraphAuthoringCapabilityDescriptor capability = ProjectionBinding.Capabilities.Require(node.CapabilityId,
                ProjectionBinding.Document.DomainId, ProjectionBinding.Document.DocumentRoleId);
            foreach (GraphAuthoringChildSurfaceDescriptor child in capability.ChildSurfaces)
                ChildSurfaceRequested?.Invoke(node, child);
        }

        public IReadOnlyList<GraphAuthoringSelection> GetStableSelection() => CharacterPoseCanvasCommands.Selection(m_Graph);

        internal void RestoreSelection(IReadOnlyList<GraphAuthoringSelection> selection)
        {
            GraphEditorUtility.activeElement = null;
            GraphEditorUtility.activeElements = selection.Select(value => Find(value.ElementId)).Where(value => value != null).ToList();
        }

        IGraphElement Find(GraphAuthoringElementId id)
        {
            foreach (Node node in m_Graph.allNodes)
            {
                if (node is CharacterPoseCanvasNode pose && pose.NodeId.Value == id.Value ||
                    node is CharacterPoseDocumentCanvasNode document && document.ElementId.Equals(id))
                    return node;
                foreach (Connection connection in node.outConnections)
                    if (connection is CharacterPoseCanvasConnection edge && edge.EdgeId == id.Value ||
                        connection is CharacterPoseDocumentCanvasConnection projected && projected.ElementId.Equals(id))
                        return connection;
            }
            return null;
        }

        public void FocusElement(GraphAuthoringElementId id)
        {
            IGraphElement element = Find(id);
            if (element != null)
                GraphEditor.FocusElement(element, true);
        }

        public void FrameAll()
        {
            GraphEditorUtility.activeElements = null;
            GraphEditorUtility.activeElement = null;
            GraphEditor.FocusSelection();
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            if (m_Graph != null) m_Graph.editorObservation = readOnly ? m_Observation : null;
            if (m_Graph is CharacterPoseCanvasGraph graph && graph.EditorWriteRouter is CharacterPoseCanvasEditorWriteSession session)
                session.ReadOnly = readOnly;
        }

        public void Highlight(GraphAuthoringElementId id, string port = "")
        {
            FocusElement(id);
            GraphEditor.current?.ShowNotification(new GUIContent(string.IsNullOrEmpty(port) ? id.Value : $"{id.Value} · {port}"));
        }

        public void ClearHighlights() => GraphEditor.current?.RemoveNotification();

        public void UpdateObservation(IReadOnlyDictionary<string, GraphAuthoringRuntimeTraceProjection> nodes,
            IReadOnlyDictionary<string, string> ports, ISet<string> active) => m_Observation.Update(nodes, ports, active);

        public void Dispose()
        {
            SetGraph(null);
            foreach (CharacterPoseDocumentCanvas canvas in m_DocumentCanvases.Values)
                UnityEngine.Object.DestroyImmediate(canvas);
            m_DocumentCanvases.Clear();
            m_Graph = null;
        }
    }
}
