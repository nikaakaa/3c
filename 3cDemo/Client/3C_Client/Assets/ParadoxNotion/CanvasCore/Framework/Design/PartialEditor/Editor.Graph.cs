#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


namespace NodeCanvas.Framework
{

    public interface IGraphEditorObservation
    {
        Status GetNodeStatus(string nodeId);
        string GetNodeText(string nodeId);
        Status GetConnectionStatus(string connectionId);
        string GetPortText(string nodeId, string portId);
        string GetConnectionText(string connectionId);
    }

    partial class Graph
    {
        public virtual bool persistsEditorGraph => true; // 3C: view-only document surfaces retain their original authoring owner.
        public virtual bool allowsEditorExecution => true;
        public virtual bool isEditorReadOnly => false; // 3C: observe domain instances without mutating authoring state.
        public virtual bool usesDomainAuthoring => false; // 3C: exclude generic refactoring and raw JSON import from strict domain assets.
        public virtual Object EditorUndoTarget => this;
        public IGraphEditorObservation editorObservation { get; set; }
        public virtual bool HandleEditorCommand(string command, Vector2 position) => false; // 3C: domains keep their clipboard and batch mutation contracts.

        private string _editorChildOwnerId;
        private bool _editorChildOwnerIsConnection;
        private Graph _editorChildGraph;
        public string editorTitle { get; set; }

        public IGraphElement GetCurrentChildGraphSource() {
            if (string.IsNullOrEmpty(_editorChildOwnerId)) { return null; }
            foreach (Node node in allNodes) {
                if (!_editorChildOwnerIsConnection && node.UID == _editorChildOwnerId) { return node; }
                if (_editorChildOwnerIsConnection) {
                    foreach (Connection connection in node.outConnections) {
                        if (connection.UID == _editorChildOwnerId) { return connection; }
                    }
                }
            }
            return null;
        }

        public Graph GetCurrentChildGraph() {
            IGraphElement owner = GetCurrentChildGraphSource();
            if (owner == null) { return null; }
            return owner is IGraphAssignable assignable ? assignable.subGraph : _editorChildGraph;
        }

        public void SetCurrentEditorChild(IGraphElement source, Graph child) {
            if (source == null || source.graph != this || child == null || child == this) {
                throw new System.ArgumentException("Editor child navigation requires an owned element and a different graph.");
            }
            child.SetCurrentChildGraphAssignable(null);
            _editorChildOwnerId = source.UID;
            _editorChildOwnerIsConnection = source is Connection;
            _editorChildGraph = child;
        }

        public void SetCurrentChildGraphAssignable(IGraphAssignable assignable) {
            _editorChildOwnerId = null;
            _editorChildGraph = null;
            if (assignable == null || assignable.subGraph == null) { return; }
            if (Application.isPlaying && EditorUtility.IsPersistent(assignable.subGraph)) {
                ParadoxNotion.Services.Logger.LogWarning("You can't view sub-graphs in play mode until they are initialized to avoid editing asset references accidentally", LogTag.EDITOR, this);
                return;
            }
            SetCurrentEditorChild(assignable, assignable.subGraph);
        }

        ///----------------------------------------------------------------------------------------------

        ///<summary>Editor. Returns a Generic Menu for on canvas click</summary>
        public GenericMenu CallbackOnCanvasContextMenu(GenericMenu menu, Vector2 canvasMousePos) { return OnCanvasContextMenu(menu, canvasMousePos); }
        ///<summary>Editor. Returns a Generic menu for on node click</summary>
        public GenericMenu CallbackOnNodesContextMenu(GenericMenu menu, Node[] nodes) { return OnNodesContextMenu(menu, nodes); }
        ///<summary>Editor. Invoke drag and drop on canvas for object</summary>
        public void CallbackOnObjectDropInGraph(Object o, Vector2 canvasMousePos) {
            ///<summary>for all graphs, make possible to drag and drop IGraphAssignables</summary>
            foreach ( var type in Editor.GraphEditorUtility.GetDropedReferenceNodeTypes<IGraphAssignable>(o) ) {
                if ( baseNodeType.IsAssignableFrom(type) ) {
                    var node = (IGraphAssignable)AddNode(type, canvasMousePos);
                    node.subGraph = (Graph)o;
                    return;
                }
            }
            OnObjectDropInGraph(o, canvasMousePos);
        }
        ///<summary>Editor. Invoke drag and drop on canvas for variable</summary>
        public void CallbackOnVariableDropInGraph(IBlackboard bb, Variable variable, Vector2 canvasMousePos) { OnVariableDropInGraph(bb, variable, canvasMousePos); }
        ///<summary>Editor. Allows adding more stuff in graph editor toolbar per graph instance</summary>
        public void CallbackOnGraphEditorToolbar() { OnGraphEditorToolbar(); }

        ///----------------------------------------------------------------------------------------------

        ///<summary>Editor. Override to add extra context sensitive options in the right click canvas context menu</summary>
        virtual protected GenericMenu OnCanvasContextMenu(GenericMenu menu, Vector2 canvasMousePos) { return menu; }
        ///<summary>Editor. Override to add more entries to the right click context menu when multiple nodes are selected</summary>
        virtual protected GenericMenu OnNodesContextMenu(GenericMenu menu, Node[] nodes) { return menu; }
        ///<summary>Editor. Handle drag and drop objects in the graph</summary>
        virtual protected void OnObjectDropInGraph(Object o, Vector2 canvasMousePos) { }
        ///<summary>Editor. Handle what happens when blackboard variable is drag and droped in graph</summary>
        virtual protected void OnVariableDropInGraph(IBlackboard bb, Variable variable, Vector2 canvasMousePos) { }
        ///<summary>Editor. Append stuff in graph editor toolbar</summary>
        virtual protected void OnGraphEditorToolbar() { }

        ///----------------------------------------------------------------------------------------------

    }
}

#endif
