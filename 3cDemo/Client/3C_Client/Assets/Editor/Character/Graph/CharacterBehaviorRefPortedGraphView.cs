using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacterBehavior.Authoring;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacterBehavior.Editor.Graph
{
    public readonly struct CharacterBehaviorRefPortedGraphNodeSnapshot
    {
        public CharacterBehaviorRefPortedGraphNodeSnapshot(
            string stableId,
            string title,
            string description,
            string summary,
            Vector2 position,
            bool hasInput,
            bool hasOutput,
            bool isRoot = false,
            bool canDelete = true,
            bool canCopy = true)
        {
            StableId = stableId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Summary = summary ?? string.Empty;
            Position = position;
            HasInput = hasInput;
            HasOutput = hasOutput;
            IsRoot = isRoot;
            CanDelete = canDelete;
            CanCopy = canCopy;
        }

        public string StableId { get; }
        public string Title { get; }
        public string Description { get; }
        public string Summary { get; }
        public Vector2 Position { get; }
        public bool HasInput { get; }
        public bool HasOutput { get; }
        public bool IsRoot { get; }
        public bool CanDelete { get; }
        public bool CanCopy { get; }
    }

    public readonly struct CharacterBehaviorRefPortedGraphEdgeSnapshot
    {
        public CharacterBehaviorRefPortedGraphEdgeSnapshot(string parentNodeId, string childNodeId)
        {
            ParentNodeId = parentNodeId ?? string.Empty;
            ChildNodeId = childNodeId ?? string.Empty;
        }

        public string ParentNodeId { get; }
        public string ChildNodeId { get; }
    }

    public readonly struct CharacterBehaviorRefPortedCreateNodeOption
    {
        public CharacterBehaviorRefPortedCreateNodeOption(string id, string path)
        {
            Id = id ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public string Id { get; }
        public string Path { get; }
    }

    public readonly struct CharacterBehaviorRefPortedGraphSnapshot
    {
        public CharacterBehaviorRefPortedGraphSnapshot(
            IReadOnlyList<CharacterBehaviorRefPortedGraphNodeSnapshot> nodes,
            IReadOnlyList<CharacterBehaviorRefPortedGraphEdgeSnapshot> edges,
            string description)
        {
            Nodes = nodes ?? Array.Empty<CharacterBehaviorRefPortedGraphNodeSnapshot>();
            Edges = edges ?? Array.Empty<CharacterBehaviorRefPortedGraphEdgeSnapshot>();
            Description = description ?? string.Empty;
        }

        public IReadOnlyList<CharacterBehaviorRefPortedGraphNodeSnapshot> Nodes { get; }
        public IReadOnlyList<CharacterBehaviorRefPortedGraphEdgeSnapshot> Edges { get; }
        public string Description { get; }
    }

    public interface ICharacterBehaviorRefPortedGraphAdapter
    {
        bool IsValid { get; }
        IReadOnlyList<CharacterBehaviorRefPortedCreateNodeOption> CreateOptions { get; }
        CharacterBehaviorRefPortedGraphSnapshot Capture();
        bool AddNode(string optionId, Vector2 position, out string nodeId, out string diagnostic);
        bool Connect(string parentNodeId, string childNodeId, out string diagnostic);
        bool Disconnect(string parentNodeId, string childNodeId, out string diagnostic);
        bool MoveNode(string nodeId, Vector2 position, out string diagnostic);
        bool DeleteNode(string nodeId, out string diagnostic);
    }

    public sealed class CharacterBehaviorRefPortedGraphView : GraphView
    {
        const string BaseTreeStylePath = "Assets/Editor/Character/Graph/RefPortedResources/StyleSheet/CharacterBehaviorBaseTree.uss";
        const string BaseNodeStylePath = "Assets/Editor/Character/Graph/RefPortedResources/StyleSheet/CharacterBehaviorBaseNode.uss";

        readonly Dictionary<string, CharacterBehaviorRefPortedNodeView> nodesById =
            new Dictionary<string, CharacterBehaviorRefPortedNodeView>(StringComparer.Ordinal);
        readonly Label nodeDescription;
        readonly ToolbarSearchField nodeSearchField;
        readonly DottedRectangleSelectionElement rectangleSelectionBox;
        CharacterBehaviorRefPortedSearchWindow searchWindow;
        ICharacterBehaviorRefPortedGraphAdapter adapter;
        bool populating;
        bool rectangleSelecting;
        Vector2 rectangleSelectionStart;

        public CharacterBehaviorRefPortedGraphView()
        {
            name = "character-behavior-ref-ported-tree-view";
            style.flexGrow = 1;
            AddStyleSheet(BaseTreeStylePath);
            AddStyleSheet(BaseNodeStylePath);
            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            nodeDescription = new Label { name = "node-description", pickingMode = PickingMode.Ignore };
            Add(nodeDescription);

            nodeSearchField = new ToolbarSearchField { name = "nodeSearchContainer" };
            nodeSearchField.RegisterValueChangedCallback(evt => SelectMatchingNodes(evt.newValue));
            Add(nodeSearchField);

            rectangleSelectionBox = new DottedRectangleSelectionElement { name = "graph-rectangle-selector", pickingMode = PickingMode.Ignore };
            contentViewContainer.Add(rectangleSelectionBox);
            HideRectangleSelectionBox();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            RegisterCallback<MouseDownEvent>(BeginRectangleSelection, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(UpdateRectangleSelection);
            RegisterCallback<MouseUpEvent>(EndRectangleSelection);
            RegisterCallback<MouseMoveEvent>(evt => nodeDescription.text = ResolveDescription(evt.localMousePosition));
            graphViewChanged = OnGraphViewChanged;
            nodeCreationRequest = context => OpenSearchWindow(context.screenMousePosition);
        }

        public event Action<string> NodeSelected;
        public event Action<string> NodeOpened;
        public string SelectedNodeId { get; private set; }
        public int NodeViewCount => nodesById.Count;
        public int EdgeViewCount => edges.ToList().Count;

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .ToList()
                .Where(port => port != startPort &&
                               port.node != startPort.node &&
                               port.direction != startPort.direction)
                .ToList();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is GraphView)
            {
                evt.menu.AppendAction("Character Behavior/Create Node", _ => OpenSearchWindow(evt.mousePosition));
                evt.menu.AppendAction("Character Behavior/Frame All", _ => FrameAllNodes());
                evt.menu.AppendAction("Character Behavior/Align Selection Horizontally", _ => AlignSelectionHorizontally());
                evt.menu.AppendAction("Character Behavior/Align Selection Vertically", _ => AlignSelectionVertically());
            }
        }

        public void Populate(ICharacterBehaviorRefPortedGraphAdapter graphAdapter, string selectedNodeId = "")
        {
            adapter = graphAdapter;
            populating = true;
            DeleteElements(graphElements.ToList());
            nodesById.Clear();
            SelectedNodeId = string.Empty;
            nodeDescription.text = string.Empty;

            if (adapter == null || !adapter.IsValid)
            {
                populating = false;
                return;
            }

            CharacterBehaviorRefPortedGraphSnapshot snapshot = adapter.Capture();
            nodeDescription.text = snapshot.Description;
            for (int i = 0; i < snapshot.Nodes.Count; i++)
            {
                CharacterBehaviorRefPortedNodeView node = new CharacterBehaviorRefPortedNodeView(snapshot.Nodes[i]);
                node.NodeSelected += SelectNode;
                node.NodeOpened += OpenNode;
                nodesById.Add(node.StableId, node);
                AddElement(node);
            }

            for (int i = 0; i < snapshot.Edges.Count; i++)
            {
                CharacterBehaviorRefPortedGraphEdgeSnapshot edge = snapshot.Edges[i];
                if (!nodesById.TryGetValue(edge.ParentNodeId, out CharacterBehaviorRefPortedNodeView parent) ||
                    !nodesById.TryGetValue(edge.ChildNodeId, out CharacterBehaviorRefPortedNodeView child) ||
                    parent.Output == null ||
                    child.Input == null)
                    continue;

                AddElement(parent.Output.ConnectTo(child.Input));
            }

            populating = false;
            if (!string.IsNullOrWhiteSpace(selectedNodeId) && nodesById.ContainsKey(selectedNodeId))
                SelectNode(selectedNodeId);
        }

        public void Populate(CharacterBehaviorAuthoringAsset asset)
        {
            Populate(asset != null ? new CharacterBehaviorAuthoringGraphAdapter(asset) : null);
        }

        public void WriteTo(CharacterBehaviorAuthoringAsset asset)
        {
            if (asset == null || !(adapter is CharacterBehaviorAuthoringGraphAdapter authoringAdapter))
                return;

            authoringAdapter.WriteTo(asset);
        }

        public bool TryGetNodeView(string nodeId, out CharacterBehaviorRefPortedNodeView view)
        {
            return nodesById.TryGetValue(nodeId, out view);
        }

        public bool AddNode(string optionId, Vector2 position)
        {
            if (adapter == null ||
                !adapter.AddNode(optionId, ToGraphLocalPosition(position), out string nodeId, out _))
                return false;

            Populate(adapter, nodeId);
            return true;
        }

        public bool ConnectNodes(string parentNodeId, string childNodeId)
        {
            if (adapter == null ||
                !nodesById.TryGetValue(parentNodeId, out CharacterBehaviorRefPortedNodeView parent) ||
                !nodesById.TryGetValue(childNodeId, out CharacterBehaviorRefPortedNodeView child) ||
                parent.Output == null ||
                child.Input == null ||
                !adapter.Connect(parentNodeId, childNodeId, out _))
                return false;

            AddElement(parent.Output.ConnectTo(child.Input));
            return true;
        }

        public bool DisconnectNodes(string parentNodeId, string childNodeId)
        {
            if (adapter == null || !adapter.Disconnect(parentNodeId, childNodeId, out _))
                return false;

            Edge edge = edges.ToList().FirstOrDefault(candidate => IsEdge(candidate, parentNodeId, childNodeId));
            if (edge != null)
                RemoveElement(edge);
            return true;
        }

        public bool MoveNode(string nodeId, Vector2 position)
        {
            if (adapter == null ||
                !nodesById.TryGetValue(nodeId, out CharacterBehaviorRefPortedNodeView node))
                return false;

            node.SetContentPosition(position);
            return adapter.MoveNode(nodeId, position, out _);
        }

        public bool DeleteNode(string nodeId)
        {
            if (adapter == null ||
                !nodesById.TryGetValue(nodeId, out CharacterBehaviorRefPortedNodeView node) ||
                !node.CanDelete ||
                !adapter.DeleteNode(nodeId, out _))
                return false;

            DeleteElements(edges.ToList().Where(edge => edge.input?.node == node || edge.output?.node == node).Cast<GraphElement>().ToList());
            RemoveElement(node);
            nodesById.Remove(nodeId);
            if (string.Equals(SelectedNodeId, nodeId, StringComparison.Ordinal))
                SelectNode(string.Empty);
            return true;
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (populating || adapter == null)
                return change;

            if (change.edgesToCreate != null)
            {
                List<Edge> accepted = new List<Edge>();
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    Edge edge = change.edgesToCreate[i];
                    if (TryResolveEdge(edge, out string parentId, out string childId) &&
                        adapter.Connect(parentId, childId, out _))
                        accepted.Add(edge);
                }

                change.edgesToCreate = accepted;
            }

            if (change.elementsToRemove != null)
            {
                List<GraphElement> acceptedRemovals = new List<GraphElement>();
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    if (change.elementsToRemove[i] is Edge edge &&
                        TryResolveEdge(edge, out string parentId, out string childId))
                    {
                        adapter.Disconnect(parentId, childId, out _);
                        acceptedRemovals.Add(edge);
                    }
                    else if (change.elementsToRemove[i] is CharacterBehaviorRefPortedNodeView node)
                    {
                        if (!node.CanDelete || !adapter.DeleteNode(node.StableId, out _))
                            continue;

                        acceptedRemovals.Add(node);
                        nodesById.Remove(node.StableId);
                        if (string.Equals(SelectedNodeId, node.StableId, StringComparison.Ordinal))
                            SelectNode(string.Empty);
                    }
                }

                change.elementsToRemove = acceptedRemovals;
            }

            if (change.movedElements != null)
            {
                for (int i = 0; i < change.movedElements.Count; i++)
                {
                    if (change.movedElements[i] is CharacterBehaviorRefPortedNodeView node)
                    {
                        node.SyncContentRectFromGraphPosition();
                        adapter.MoveNode(node.StableId, node.ContentRect.position, out _);
                    }
                }
            }

            return change;
        }

        void OpenSearchWindow(Vector2 screenPosition)
        {
            if (adapter == null || adapter.CreateOptions.Count == 0)
                return;

            if (searchWindow == null)
                searchWindow = ScriptableObject.CreateInstance<CharacterBehaviorRefPortedSearchWindow>();
            searchWindow.Init(this, adapter);
            SearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
        }

        internal bool AddNodeFromSearch(string optionId, Vector2 screenPosition)
        {
            Vector2 windowMousePosition = screenPosition;
            if (EditorWindow.focusedWindow != null)
                windowMousePosition -= EditorWindow.focusedWindow.position.position;
            Vector2 graphPosition = contentViewContainer.WorldToLocal(windowMousePosition);
            return AddNode(optionId, graphPosition);
        }

        void SelectNode(string nodeId)
        {
            SelectedNodeId = nodeId ?? string.Empty;
            ClearSelection();
            if (!string.IsNullOrWhiteSpace(SelectedNodeId) &&
                nodesById.TryGetValue(SelectedNodeId, out CharacterBehaviorRefPortedNodeView node))
                AddToSelection(node);

            NodeSelected?.Invoke(SelectedNodeId);
        }

        void OpenNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !nodesById.ContainsKey(nodeId))
                return;

            NodeOpened?.Invoke(nodeId);
        }

        internal void OpenNodeForTests(string nodeId)
        {
            OpenNode(nodeId);
        }

        void SelectMatchingNodes(string query)
        {
            ClearSelection();
            if (string.IsNullOrWhiteSpace(query))
                return;

            foreach (CharacterBehaviorRefPortedNodeView node in nodesById.Values)
            {
                if (node.StableId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    node.TitleText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    AddToSelection(node);
            }
        }

        void BeginRectangleSelection(MouseDownEvent evt)
        {
            if (evt.button != 0 ||
                adapter == null ||
                !adapter.IsValid ||
                !IsBackgroundSelectionTarget(evt.target as VisualElement))
                return;

            rectangleSelecting = true;
            rectangleSelectionStart = ToContentPosition(evt.localMousePosition);
            ClearSelection();
            SelectedNodeId = string.Empty;
            NodeSelected?.Invoke(SelectedNodeId);
            rectangleSelectionBox.BringToFront();
            UpdateRectangleSelectionBox(rectangleSelectionStart);
            MouseCaptureController.CaptureMouse(this);
            evt.StopImmediatePropagation();
        }

        void UpdateRectangleSelection(MouseMoveEvent evt)
        {
            if (!rectangleSelecting)
                return;

            UpdateRectangleSelectionBox(ToContentPosition(evt.localMousePosition));
            evt.StopPropagation();
        }

        void EndRectangleSelection(MouseUpEvent evt)
        {
            if (!rectangleSelecting)
                return;

            Rect rect = CreateRect(rectangleSelectionStart, ToContentPosition(evt.localMousePosition));
            SelectNodesInContentRect(rect);
            HideRectangleSelectionBox();
            if (MouseCaptureController.HasMouseCapture(this))
                MouseCaptureController.ReleaseMouse(this);
            evt.StopPropagation();
        }

        internal int SelectNodesInContentRect(Rect contentRect)
        {
            ClearSelection();
            if (contentRect.width < 3f && contentRect.height < 3f)
            {
                SelectedNodeId = string.Empty;
                NodeSelected?.Invoke(SelectedNodeId);
                return 0;
            }

            List<CharacterBehaviorRefPortedNodeView> selected = new List<CharacterBehaviorRefPortedNodeView>();
            foreach (CharacterBehaviorRefPortedNodeView node in nodesById.Values)
            {
                if (!contentRect.Overlaps(node.ContentRect))
                    continue;

                AddToSelection(node);
                selected.Add(node);
            }

            SelectedNodeId = selected.Count == 1 ? selected[0].StableId : string.Empty;
            NodeSelected?.Invoke(SelectedNodeId);
            return selected.Count;
        }

        void UpdateRectangleSelectionBox(Vector2 end)
        {
            rectangleSelectionBox.style.display = DisplayStyle.Flex;
            rectangleSelectionBox.Start = rectangleSelectionStart;
            rectangleSelectionBox.End = end;
            rectangleSelectionBox.MarkDirtyRepaint();
        }

        void HideRectangleSelectionBox()
        {
            rectangleSelecting = false;
            rectangleSelectionBox.style.display = DisplayStyle.None;
            rectangleSelectionBox.Start = Vector2.zero;
            rectangleSelectionBox.End = Vector2.zero;
        }

        Vector2 ToContentPosition(Vector2 graphLocalPosition)
        {
            return this.ChangeCoordinatesTo(contentViewContainer, graphLocalPosition);
        }

        static bool IsBackgroundSelectionTarget(VisualElement target)
        {
            for (VisualElement current = target; current != null; current = current.parent)
            {
                if (current is GraphElement ||
                    current is ToolbarSearchField ||
                    string.Equals(current.name, "node-description", StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        static Rect CreateRect(Vector2 start, Vector2 end)
        {
            return Rect.MinMaxRect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x),
                Mathf.Max(start.y, end.y));
        }

        string ResolveDescription(Vector2 localMousePosition)
        {
            Vector2 contentMousePosition = ToContentPosition(localMousePosition);
            foreach (CharacterBehaviorRefPortedNodeView node in nodesById.Values)
            {
                if (node.ContentRect.Contains(contentMousePosition))
                    return node.Description;
            }

            return adapter?.Capture().Description ?? "Character Behavior authoring graph.";
        }

        void FrameAllNodes()
        {
            if (nodesById.Count == 0)
                return;

            ClearSelection();
            foreach (CharacterBehaviorRefPortedNodeView node in nodesById.Values)
                AddToSelection(node);
            FrameSelection();
            ClearSelection();
        }

        void AlignSelectionHorizontally()
        {
            List<CharacterBehaviorRefPortedNodeView> selected = selection.OfType<CharacterBehaviorRefPortedNodeView>().ToList();
            if (selected.Count < 2)
                return;

            float y = selected[0].ContentRect.y;
            for (int i = 1; i < selected.Count; i++)
                MoveNode(selected[i].StableId, new Vector2(selected[i].ContentRect.x, y));
        }

        void AlignSelectionVertically()
        {
            List<CharacterBehaviorRefPortedNodeView> selected = selection.OfType<CharacterBehaviorRefPortedNodeView>().ToList();
            if (selected.Count < 2)
                return;

            float x = selected[0].ContentRect.x;
            for (int i = 1; i < selected.Count; i++)
                MoveNode(selected[i].StableId, new Vector2(x, selected[i].ContentRect.y));
        }

        Vector2 ToGraphLocalPosition(Vector2 position)
        {
            return (position - new Vector2(viewTransform.position.x, viewTransform.position.y)) / scale;
        }

        static bool TryResolveEdge(Edge edge, out string parentNodeId, out string childNodeId)
        {
            parentNodeId = string.Empty;
            childNodeId = string.Empty;
            if (!(edge.output?.node is CharacterBehaviorRefPortedNodeView parent) ||
                !(edge.input?.node is CharacterBehaviorRefPortedNodeView child))
                return false;

            parentNodeId = parent.StableId;
            childNodeId = child.StableId;
            return true;
        }

        static bool IsEdge(Edge edge, string parentNodeId, string childNodeId)
        {
            return TryResolveEdge(edge, out string parent, out string child) &&
                   string.Equals(parent, parentNodeId, StringComparison.Ordinal) &&
                   string.Equals(child, childNodeId, StringComparison.Ordinal);
        }

        void AddStyleSheet(string path)
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
        }
    }

    public sealed class CharacterBehaviorRefPortedNodeView : Node
    {
        readonly CharacterBehaviorRefPortedGraphNodeSnapshot source;
        static readonly Vector2 DefaultSize = new Vector2(240, 120);

        public CharacterBehaviorRefPortedNodeView(CharacterBehaviorRefPortedGraphNodeSnapshot source)
        {
            this.source = source;
            StableId = source.StableId;
            TitleText = source.IsRoot &&
                        !string.Equals(source.StableId, "behavior.root", StringComparison.Ordinal) &&
                        source.Title.IndexOf("Root", StringComparison.Ordinal) < 0
                ? $"Root / {source.Title}"
                : source.Title;
            Description = source.Description;
            ContentRect = new Rect(source.Position, DefaultSize);
            viewDataKey = StableId;
            AddToClassList("node");
            AddToClassList("nodeState-None");
            if (source.IsRoot)
                AddToClassList("root-node");
            if (!source.CanDelete)
                capabilities &= ~Capabilities.Deletable;
            if (!source.CanCopy)
                capabilities &= ~Capabilities.Copiable;
            SetPosition(ContentRect);

            mainContainer.Clear();
            extensionContainer.Clear();

            VisualElement nodeState = new VisualElement { name = "node-state" };
            VisualElement selectionBorder = new VisualElement { name = "node-selection-border" };
            VisualElement nodeBorder = new VisualElement { name = "node-border" };
            VisualElement title = new VisualElement { name = "title" };
            VisualElement titleButtonContainer = new VisualElement { name = "title-button-container" };
            VisualElement collapseButton = new VisualElement { name = "collapse-button" };
            Label titleLabel = new Label(TitleText) { name = "title-label", tooltip = Description };
            VisualElement panelButtonContainer = new VisualElement { name = "panel-button-container" };
            VisualElement panelButton = new VisualElement { name = "panel-button" };
            VisualElement contents = new VisualElement { name = "contents" };
            VisualElement top = new VisualElement { name = "top" };
            VisualElement input = new VisualElement { name = "input" };
            VisualElement output = new VisualElement { name = "output" };

            titleButtonContainer.Add(collapseButton);
            panelButtonContainer.Add(panelButton);
            title.Add(titleButtonContainer);
            title.Add(titleLabel);
            title.Add(panelButtonContainer);

            if (source.HasInput)
            {
                Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                Input.portName = CharacterBehaviorAuthoringPortIds.Input;
                input.Add(Input);
            }

            if (source.HasOutput)
            {
                Output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                Output.portName = CharacterBehaviorAuthoringPortIds.Children;
                output.Add(Output);
            }

            top.Add(input);
            top.Add(new VisualElement { name = "space" });
            top.Add(output);
            contents.Add(top);
            nodeBorder.Add(title);
            nodeBorder.Add(contents);
            mainContainer.Add(nodeState);
            mainContainer.Add(selectionBorder);
            mainContainer.Add(nodeBorder);
            mainContainer.Add(new Label(source.Summary) { name = "node-input-field-container" });
            RegisterCallback<MouseDownEvent>(HandleMouseDown);
            RefreshExpandedState();
            RefreshPorts();
        }

        public event Action<string> NodeSelected;
        public event Action<string> NodeOpened;
        public string StableId { get; }
        public string TitleText { get; }
        public string Description { get; }
        public bool CanDelete => source.CanDelete;
        public Rect ContentRect { get; private set; }
        public Port Input { get; }
        public Port Output { get; }

        public void SetContentPosition(Vector2 position)
        {
            ContentRect = new Rect(position, ContentRect.size);
            SetPosition(ContentRect);
        }

        public void SyncContentRectFromGraphPosition()
        {
            Rect position = GetPosition();
            if (float.IsNaN(position.x) ||
                float.IsNaN(position.y) ||
                float.IsNaN(position.width) ||
                float.IsNaN(position.height))
                return;

            ContentRect = position;
        }

        void HandleMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0 || IsPortTarget(evt.target as VisualElement))
                return;

            HandleClick(evt.clickCount);
            if (evt.clickCount >= 2)
                evt.StopPropagation();
        }

        void HandleClick(int clickCount)
        {
            NodeSelected?.Invoke(StableId);
            if (clickCount >= 2)
                NodeOpened?.Invoke(StableId);
        }

        internal void HandleClickForTests(int clickCount)
        {
            HandleClick(clickCount);
        }

        static bool IsPortTarget(VisualElement element)
        {
            while (element != null)
            {
                if (element is Port)
                    return true;
                element = element.parent;
            }

            return false;
        }
    }

    sealed class DottedRectangleSelectionElement : ImmediateModeElement
    {
        const float SegmentLength = 5f;
        static Material lineMaterial;

        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public DottedRectangleSelectionElement()
        {
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
        }

        protected override void ImmediateRepaint()
        {
            if (Start == End)
                return;

            EnsureMaterial();
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(Start.x, End.x),
                Mathf.Min(Start.y, End.y),
                Mathf.Max(Start.x, End.x),
                Mathf.Max(Start.y, End.y));
            Color color = new Color(1f, 0.6f, 0f, 1f);
            Vector3[] points =
            {
                new Vector3(rect.xMin, rect.yMin, 0f),
                new Vector3(rect.xMax, rect.yMin, 0f),
                new Vector3(rect.xMax, rect.yMax, 0f),
                new Vector3(rect.xMin, rect.yMax, 0f)
            };

            GL.PushMatrix();
            lineMaterial.SetPass(0);
            DrawDottedLine(points[0], points[1], color);
            DrawDottedLine(points[1], points[2], color);
            DrawDottedLine(points[2], points[3], color);
            DrawDottedLine(points[3], points[0], color);
            GL.PopMatrix();
        }

        static void EnsureMaterial()
        {
            if (lineMaterial != null)
                return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        static void DrawDottedLine(Vector3 start, Vector3 end, Color color)
        {
            float distance = Vector3.Distance(start, end);
            if (distance <= 0f)
                return;

            int segmentCount = Mathf.CeilToInt(distance / SegmentLength);
            GL.Begin(GL.LINES);
            GL.Color(color);
            for (int i = 0; i < segmentCount; i += 2)
            {
                GL.Vertex(Vector3.Lerp(start, end, i * SegmentLength / distance));
                GL.Vertex(Vector3.Lerp(start, end, Mathf.Min(i + 1, segmentCount) * SegmentLength / distance));
            }
            GL.End();
        }
    }

    public sealed class CharacterBehaviorRefPortedSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        CharacterBehaviorRefPortedGraphView graphView;
        ICharacterBehaviorRefPortedGraphAdapter adapter;
        Texture2D indentationIcon;

        public void Init(
            CharacterBehaviorRefPortedGraphView graphView,
            ICharacterBehaviorRefPortedGraphAdapter adapter)
        {
            this.graphView = graphView;
            this.adapter = adapter;
            if (indentationIcon == null)
            {
                indentationIcon = new Texture2D(1, 1);
                indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
                indentationIcon.Apply();
            }
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Nodes"))
            };

            IReadOnlyList<CharacterBehaviorRefPortedCreateNodeOption> options =
                adapter?.CreateOptions ?? Array.Empty<CharacterBehaviorRefPortedCreateNodeOption>();
            HashSet<string> groups = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < options.Count; i++)
            {
                string[] parts = options[i].Path.Split('/');
                if (parts.Length > 1 && groups.Add(parts[0]))
                    entries.Add(new SearchTreeGroupEntry(new GUIContent(parts[0]), 1));

                entries.Add(new SearchTreeEntry(new GUIContent(parts[parts.Length - 1], indentationIcon))
                {
                    level = parts.Length > 1 ? 2 : 1,
                    userData = options[i].Id
                });
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            return graphView != null &&
                   graphView.AddNodeFromSearch(searchTreeEntry.userData as string, context.screenMousePosition);
        }
    }

    sealed class CharacterBehaviorAuthoringGraphAdapter : ICharacterBehaviorRefPortedGraphAdapter
    {
        static readonly CharacterBehaviorRefPortedCreateNodeOption[] createOptions =
        {
            new CharacterBehaviorRefPortedCreateNodeOption("Parallel", "Behavior Source/Parallel"),
            new CharacterBehaviorRefPortedCreateNodeOption("LocomotionLeaf", "Behavior Source/Locomotion Leaf"),
            new CharacterBehaviorRefPortedCreateNodeOption("CommittedActionLeaf", "Behavior Source/Committed Action Leaf")
        };

        readonly CharacterBehaviorAuthoringAsset asset;
        readonly Dictionary<string, CharacterBehaviorAuthoringNode> nodes =
            new Dictionary<string, CharacterBehaviorAuthoringNode>(StringComparer.Ordinal);
        readonly List<CharacterBehaviorAuthoringEdge> edges = new List<CharacterBehaviorAuthoringEdge>();

        public CharacterBehaviorAuthoringGraphAdapter(CharacterBehaviorAuthoringAsset asset)
        {
            this.asset = asset;
            if (asset == null)
                return;

            for (int i = 0; i < asset.Nodes.Count; i++)
                nodes[asset.Nodes[i].StableId] = asset.Nodes[i];
            for (int i = 0; i < asset.Edges.Count; i++)
                edges.Add(asset.Edges[i]);
        }

        public bool IsValid => asset != null;
        public IReadOnlyList<CharacterBehaviorRefPortedCreateNodeOption> CreateOptions => createOptions;

        public CharacterBehaviorRefPortedGraphSnapshot Capture()
        {
            CharacterBehaviorRefPortedGraphNodeSnapshot[] graphNodes = nodes.Values
                .Select(ToSnapshot)
                .ToArray();
            CharacterBehaviorRefPortedGraphEdgeSnapshot[] graphEdges = edges
                .Select(edge => new CharacterBehaviorRefPortedGraphEdgeSnapshot(edge.ParentNodeId, edge.ChildNodeId))
                .ToArray();
            return new CharacterBehaviorRefPortedGraphSnapshot(
                graphNodes,
                graphEdges,
                "Character Behavior authoring graph. Root and Parallel nodes organize submission leaves.");
        }

        public bool AddNode(string optionId, Vector2 position, out string nodeId, out string diagnostic)
        {
            nodeId = string.Empty;
            diagnostic = string.Empty;
            if (!TryResolveKind(optionId, out CharacterBehaviorAuthoringNodeKind kind))
            {
                diagnostic = $"behavior-node-kind-invalid:{optionId}";
                return false;
            }

            nodeId = $"{PrefixFor(kind)}.{Guid.NewGuid():N}";
            nodes[nodeId] = new CharacterBehaviorAuthoringNode(nodeId, kind, position);
            return true;
        }

        public bool Connect(string parentNodeId, string childNodeId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!nodes.ContainsKey(parentNodeId) || !nodes.ContainsKey(childNodeId))
            {
                diagnostic = "behavior-node-missing";
                return false;
            }

            if (edges.Any(edge => edge.ParentNodeId == parentNodeId && edge.ChildNodeId == childNodeId))
                return false;

            edges.Add(new CharacterBehaviorAuthoringEdge(
                parentNodeId,
                childNodeId,
                CharacterBehaviorAuthoringPortIds.Children,
                CharacterBehaviorAuthoringPortIds.Input));
            return true;
        }

        public bool Disconnect(string parentNodeId, string childNodeId, out string diagnostic)
        {
            diagnostic = string.Empty;
            int index = edges.FindIndex(edge => edge.ParentNodeId == parentNodeId && edge.ChildNodeId == childNodeId);
            if (index < 0)
            {
                diagnostic = "behavior-edge-missing";
                return false;
            }

            edges.RemoveAt(index);
            return true;
        }

        public bool MoveNode(string nodeId, Vector2 position, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!nodes.TryGetValue(nodeId, out CharacterBehaviorAuthoringNode node))
            {
                diagnostic = "behavior-node-missing";
                return false;
            }

            nodes[nodeId] = node.WithEditorPosition(position);
            return true;
        }

        public bool DeleteNode(string nodeId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (nodes.TryGetValue(nodeId, out CharacterBehaviorAuthoringNode node) &&
                node.Kind == CharacterBehaviorAuthoringNodeKind.Root)
            {
                diagnostic = $"behavior-root-protected:{nodeId}";
                return false;
            }

            if (!nodes.Remove(nodeId))
            {
                diagnostic = "behavior-node-missing";
                return false;
            }

            edges.RemoveAll(edge => edge.ParentNodeId == nodeId || edge.ChildNodeId == nodeId);
            return true;
        }

        public void WriteTo(CharacterBehaviorAuthoringAsset target)
        {
            target.SetGraph(nodes.Values.ToArray(), edges.ToArray());
        }

        static CharacterBehaviorRefPortedGraphNodeSnapshot ToSnapshot(CharacterBehaviorAuthoringNode node)
        {
            return new CharacterBehaviorRefPortedGraphNodeSnapshot(
                node.StableId,
                ResolveTitle(node.Kind),
                ResolveDescription(node.Kind),
                ResolveSummary(node),
                node.EditorPosition,
                node.Kind != CharacterBehaviorAuthoringNodeKind.Root,
                node.Kind == CharacterBehaviorAuthoringNodeKind.Root ||
                node.Kind == CharacterBehaviorAuthoringNodeKind.Parallel,
                node.Kind == CharacterBehaviorAuthoringNodeKind.Root,
                node.Kind != CharacterBehaviorAuthoringNodeKind.Root,
                node.Kind != CharacterBehaviorAuthoringNodeKind.Root);
        }

        static bool TryResolveKind(string optionId, out CharacterBehaviorAuthoringNodeKind kind)
        {
            if (Enum.TryParse(optionId, out kind) &&
                kind != CharacterBehaviorAuthoringNodeKind.None &&
                kind != CharacterBehaviorAuthoringNodeKind.Root)
            {
                return true;
            }

            kind = CharacterBehaviorAuthoringNodeKind.None;
            return false;
        }

        static string PrefixFor(CharacterBehaviorAuthoringNodeKind kind)
        {
            switch (kind)
            {
                case CharacterBehaviorAuthoringNodeKind.Parallel:
                    return "behavior.parallel";
                case CharacterBehaviorAuthoringNodeKind.LocomotionLeaf:
                    return "source.locomotion";
                case CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf:
                    return "source.committed-action";
                default:
                    return "behavior.node";
            }
        }

        static string ResolveTitle(CharacterBehaviorAuthoringNodeKind kind)
        {
            switch (kind)
            {
                case CharacterBehaviorAuthoringNodeKind.Root:
                    return "Behavior Root";
                case CharacterBehaviorAuthoringNodeKind.Parallel:
                    return "Parallel";
                case CharacterBehaviorAuthoringNodeKind.LocomotionLeaf:
                    return "Locomotion Leaf";
                case CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf:
                    return "Committed Action Leaf";
                default:
                    return "Unknown";
            }
        }

        static string ResolveDescription(CharacterBehaviorAuthoringNodeKind kind)
        {
            switch (kind)
            {
                case CharacterBehaviorAuthoringNodeKind.Root:
                    return "Fixed behavior entry. It is not a gameplay source and cannot be deleted.";
                case CharacterBehaviorAuthoringNodeKind.Parallel:
                    return "Composite node that preserves child order for behavior submission leaves.";
                case CharacterBehaviorAuthoringNodeKind.LocomotionLeaf:
                    return "Locomotion behavior source. It submits candidates only.";
                case CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf:
                    return "Committed action behavior source. It submits action request and output data.";
                default:
                    return string.Empty;
            }
        }

        static string ResolveSummary(CharacterBehaviorAuthoringNode node)
        {
            switch (node.Kind)
            {
                case CharacterBehaviorAuthoringNodeKind.Root:
                    return $"Fixed entry: {node.StableId}";
                case CharacterBehaviorAuthoringNodeKind.Parallel:
                    return $"Source fan-out: {node.StableId}";
                default:
                    return node.StableId;
            }
        }
    }
}
