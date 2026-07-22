using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    sealed class BtsmtlGraphAuthoringDocumentAdapter : IGraphAuthoringDocument
    {
        readonly BaseTreeWindow m_Window;

        public BtsmtlGraphAuthoringDocumentAdapter(BaseTreeWindow window)
        {
            m_Window = window;
        }

        public string DomainId => "btsmtl";
        public string DocumentId => m_Window.Tree?.GraphAuthoringId ?? string.Empty;
        public string DisplayName => m_Window.Tree != null ? m_Window.Tree.name : "BTSMTL Graph";
        public string ContentRevision => m_Window.Tree != null ? GraphAuthoringFingerprint.Compute(m_Window.Tree) : string.Empty;
        public UnityEngine.Object SerializedOwner => m_Window.CurrentPageSerializedOwner;
    }

    sealed class BtsmtlGraphAuthoringNodeCatalogAdapter : IGraphAuthoringNodeCatalog
    {
        readonly BaseTreeWindow m_Window;

        public BtsmtlGraphAuthoringNodeCatalogAdapter(BaseTreeWindow window)
        {
            m_Window = window;
        }

        public IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetEntries(IGraphAuthoringDocument document)
        {
            BaseTree tree = m_Window.Tree;
            if (tree == null)
                return Array.Empty<GraphAuthoringNodeCatalogEntry>();
            var entries = new List<GraphAuthoringNodeCatalogEntry>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            AcceptableNodePathsAttribute[] attributes = tree.GetAttributes<AcceptableNodePathsAttribute>();
            for (int attributeIndex = 0; attributeIndex < attributes.Length; attributeIndex++)
            {
                foreach (string rootPath in attributes[attributeIndex].AcceptableNodePaths)
                {
                    foreach ((Type type, string path) in TreeDesignerUtility.GetNodePathPairs(rootPath))
                    {
                        if (type == null || !tree.CanCreateNodeType(type) || !paths.Add(path))
                            continue;
                        entries.Add(new GraphAuthoringNodeCatalogEntry(path, type.AssemblyQualifiedName));
                    }
                }
            }
            entries.Sort((left, right) => string.CompareOrdinal(left.Path, right.Path));
            return entries;
        }
    }

    sealed class BtsmtlGraphAuthoringPortPolicyAdapter : IGraphAuthoringPortPolicy
    {
        readonly BaseTreeWindow m_Window;

        public BtsmtlGraphAuthoringPortPolicyAdapter(BaseTreeWindow window)
        {
            m_Window = window;
        }

        public bool CanConnect(IGraphAuthoringDocument document, Port startPort, Port endPort)
        {
            if (!(startPort is BasePortView start) || !(endPort is BasePortView end) ||
                start.NodeView == end.NodeView || end.direction == start.direction ||
                start.portType == null || end.portType == null || start.portType == typeof(object))
                return false;
            if (!IsCompatibleStateMachineFlowPort(start, end))
                return false;
            if (end is VariablePropertyPortView variable && variable.PropertyPort.ValueType == null)
                return variable.AcceptableTypes.Any(type => start.portType.IsSubClassOfRawGeneric(type));
            if (start.portType == end.portType || start.portType.IsSubclassOf(end.portType))
                return true;
            return end is PropertyPortView property &&
                   property.PropertyPort.GetAttribute<CompatiblePortsAttribute>() is CompatiblePortsAttribute compatibility &&
                   compatibility.CompatibleTypes.Contains(start.portType);
        }

        bool IsCompatibleStateMachineFlowPort(BasePortView start, BasePortView end)
        {
            if (!(m_Window.Tree is StateMachineGraph) || IsPropertyPort(start) || IsPropertyPort(end))
                return true;
            BasePortView output = start.direction == Direction.Output ? start : end;
            BasePortView input = start.direction == Direction.Input ? start : end;
            if (output.Name != StateMachinePorts.StateOut || input.Name != StateMachinePorts.StateIn)
                return false;
            BaseNode startNode = output.NodeView?.Node;
            BaseNode endNode = input.NodeView?.Node;
            return startNode is StateMachineEnterNode && endNode is StateNode ||
                   startNode is StateMachineAnyStateNode && (endNode is StateNode || endNode is StateMachineExitNode) ||
                   startNode is StateNode && (endNode is StateNode || endNode is StateMachineExitNode);
        }

        static bool IsPropertyPort(BasePortView port)
        {
            return port is PropertyPortView || port is VariablePropertyPortView;
        }
    }

    sealed class BtsmtlGraphAuthoringMutationAdapter : IGraphAuthoringMutationAdapter
    {
        readonly BaseTreeWindow m_Window;

        public BtsmtlGraphAuthoringMutationAdapter(BaseTreeWindow window)
        {
            m_Window = window;
        }

        public bool ReadOnly => !m_Window.CanMutateCurrentDocument;

        public void CreateNode(IGraphAuthoringDocument document, string typeId, Vector2 graphPosition)
        {
            RequireWritable();
            Type type = Type.GetType(typeId, false);
            if (type == null)
                throw new InvalidOperationException($"BTSMTL node type '{typeId}' cannot be resolved.");
            m_Window.TreeView.CreateNode(type, graphPosition);
        }

        public GraphViewChange ApplyGraphViewChange(IGraphAuthoringDocument document, GraphViewChange change)
        {
            if (ReadOnly)
            {
                change.edgesToCreate = null;
                change.elementsToRemove = new List<GraphElement>();
                change.movedElements = null;
                return change;
            }
            return m_Window.TreeView.ApplyDomainGraphViewChange(change);
        }

        public string SerializeSelection(IGraphAuthoringDocument document, IEnumerable<GraphElement> elements)
        {
            return m_Window.TreeView.SerializeGraphElements(elements);
        }

        public bool CanPaste(IGraphAuthoringDocument document, string payload)
        {
            return m_Window.TreeView.CanPasteGraphElements(payload);
        }

        public void Paste(IGraphAuthoringDocument document, string operationName, string payload)
        {
            RequireWritable();
            m_Window.TreeView.PasteGraphElements(operationName, payload);
        }

        public void Reload(IGraphAuthoringDocument document)
        {
            m_Window.ReloadCurrentTreeFromSerializedState();
        }

        void RequireWritable()
        {
            if (ReadOnly)
                throw new InvalidOperationException("BTSMTL Graph document has no writable serialized owner.");
        }
    }

    sealed class BtsmtlGraphAuthoringInspectorAdapter : IGraphAuthoringInspectorAdapter
    {
        readonly BaseTreeWindow m_Window;
        readonly BaseTreeInspectorView m_View;

        public BtsmtlGraphAuthoringInspectorAdapter(BaseTreeWindow window, BaseTreeInspectorView view)
        {
            m_Window = window;
            m_View = view;
        }

        public VisualElement View => m_View;

        public void Bind(IGraphAuthoringDocument document)
        {
            m_View.SetAuthoringContext(m_Window.AuthoringContext);
            m_View.SetEnabled(m_Window.CanMutateCurrentDocument);
            if (m_Window.Tree == null)
                return;
            m_View.SetVisibleBlackboardSources(m_Window.ResolveVisibleTrees());
            m_View.PopulateView(m_Window.Tree);
        }

        public void Inspect(IReadOnlyList<ISelectable> selection)
        {
            m_Window.PopulateSelectionInspector(selection);
        }

        public void Clear()
        {
            m_View.ClearView();
        }
    }

    sealed class BtsmtlGraphAuthoringDiagnosticsAdapter : IGraphAuthoringDiagnosticsAdapter
    {
        readonly BaseTreeWindow m_Window;

        public BtsmtlGraphAuthoringDiagnosticsAdapter(BaseTreeWindow window)
        {
            m_Window = window;
        }

        public void Bind(IGraphAuthoringDocument document, GraphView graphView, VisualElement toolbar)
        {
            m_Window.BindRuntimeDiagnostics(graphView, toolbar);
        }

        public void Refresh()
        {
            m_Window.RefreshRuntimeDiagnostics();
        }

        public void Clear()
        {
            m_Window.ClearRuntimeDiagnostics();
        }
    }
}
