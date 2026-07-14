using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using BTSMTL;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using GraphSelectable = UnityEditor.Experimental.GraphView.ISelectable;

namespace TreeDesigner.Editor
{
    public enum AuthoringPageKind
    {
        Graph,
        TreeClip
    }

    public enum TreeWindowMode
    {
        Authoring,
        LiveDebug
    }

    public class BaseTreeWindow : EditorWindow
    {
        [SerializeField]
        UnityEngine.Object m_CurrentTreeSerializedOwner;

        [SerializeField]
        string m_CurrentTreeSerializedPropertyPath;

        [SerializeField]
        string m_CurrentTreeAuthoringId;

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;

        protected BaseTreeAsset m_TreeAsset;
        public BaseTreeAsset TreeAsset => m_TreeAsset;

        protected object m_AuthoringContext;
        public object AuthoringContext => m_AuthoringContext;
        
        protected BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        protected VisualElement m_LeftPanel;
        protected VisualElement m_RightPanel;
        protected VisualElement m_NavigationToolbar;
        protected Button m_BackButton;
        protected VisualElement m_BreadcrumbContainer;
        protected Label m_TreeTitle;
        protected VisualElement m_DebugToolbar;
        protected ToolbarToggle m_AuthoringModeToggle;
        protected ToolbarToggle m_LiveDebugModeToggle;
        protected ToolbarMenu m_DebugTargetMenu;
        protected ToolbarMenu m_DebugInstanceMenu;
        protected ToolbarToggle m_DebugFollowToggle;
        protected ToolbarToggle m_DebugLiveToggle;
        protected ToolbarButton m_DebugCaptureButton;
        protected SliderInt m_DebugHistorySlider;
        protected Label m_DebugStatus;
        [SerializeField]
        protected TreeWindowMode m_WindowMode;
        protected readonly Dictionary<string, RuntimeDebugViewBinding> m_RuntimeDebugBindings = new Dictionary<string, RuntimeDebugViewBinding>(StringComparer.Ordinal);
        protected readonly Dictionary<string, RuntimeDebugTargetRequest> m_RuntimeDebugRequests = new Dictionary<string, RuntimeDebugTargetRequest>(StringComparer.Ordinal);
        RuntimeDebugViewModel m_LastRuntimeDebugView;
        RuntimeInstanceKey m_LastRuntimeDebugInstance;
        string m_LastRuntimeDebugGraphAuthoringId = string.Empty;
        long m_LastRuntimeDebugRevision = -1;
        long m_LastRuntimeDebugMenuTargetRevision = -1;
        long m_LastRuntimeDebugGraphInstanceRevision = -1;
        public bool IsLiveDebug => m_WindowMode == TreeWindowMode.LiveDebug;
        protected BaseTreeInspectorView m_TreeInspectorView;
        protected List<BaseTree> m_OpenedTrees = new List<BaseTree>();
        protected List<AuthoringPageEntry> m_NavigationStack = new List<AuthoringPageEntry>();
        public UnityEngine.Object CurrentPageSerializedOwner => m_NavigationStack.Count > 0
            ? m_NavigationStack[m_NavigationStack.Count - 1].SerializedOwner
            : m_Tree?.SerializedOwner;
        public string CurrentPageSerializedPropertyPath => m_NavigationStack.Count > 0
            ? m_NavigationStack[m_NavigationStack.Count - 1].SerializedPropertyPath
            : m_Tree?.SerializedPropertyPath ?? string.Empty;

        public IReadOnlyList<BaseTree> VisibleTrees
        {
            get
            {
                List<BaseTree> trees = new List<BaseTree>();
                for (int i = 0; i < m_NavigationStack.Count; i++)
                {
                    BaseTree tree = m_NavigationStack[i].Tree;
                    if (tree != null && !trees.Contains(tree))
                        trees.Add(tree);
                }
                if (m_Tree != null && !trees.Contains(m_Tree))
                    trees.Add(m_Tree);
                return trees;
            }
        }

        public IEnumerable<BaseExposedProperty> GetVisibleExposedProperties()
        {
            return ResolveVisibleTrees().SelectMany(i => i.ExposedProperties);
        }

        public IReadOnlyList<BaseTree> ResolveVisibleTrees()
        {
            List<BaseTree> trees = VisibleTrees.ToList();
            if (m_AuthoringContext is ITreeInspectorBlackboardAuthoringContext source)
            {
                IEnumerable<BaseTree> additional = source.GetAdditionalVisibleBlackboardSources(m_Tree);
                if (additional != null)
                {
                    foreach (BaseTree tree in additional)
                    {
                        if (tree != null && !trees.Contains(tree))
                            trees.Add(tree);
                    }
                }
            }
            return trees;
        }

        public bool TryResolveExposedProperty(PipelineBlackboardVariableReference reference, out BaseExposedProperty declaration)
        {
            declaration = GetVisibleExposedProperties().FirstOrDefault(i =>
                string.Equals(i.DeclarationId, reference.DeclarationId, StringComparison.Ordinal) &&
                string.Equals(i.DeclarationOwnerId, reference.DeclarationOwnerId, StringComparison.Ordinal));
            return declaration != null;
        }

        protected virtual Type m_TreeViewType => typeof(BaseTreeView);
        protected virtual Type m_TreeInspectorViewType => typeof(BaseTreeInspectorView);

        public Action OnClosedCallback;
        public Action OnFocusCallback;
        public Action OnLostFocusCallback;

        protected bool m_Docking;
        public bool Docking => m_Docking;

        protected readonly struct AuthoringPageEntry
        {
            public readonly BaseTree Tree;
            public readonly BaseTreeAsset TreeAsset;
            public readonly string DisplayName;
            public readonly BaseTree SourceTree;
            public readonly string SourceNodeGuid;
            public readonly string ReferenceKey;
            public readonly AuthoringPageKind PageKind;
            public readonly UnityEngine.Object SerializedOwner;
            public readonly string SerializedPropertyPath;
            public readonly object AuthoringContext;
            public AuthoringPageEntry(BaseTree tree, BaseTreeAsset treeAsset, string displayName, BaseTree sourceTree, string sourceNodeGuid, string referenceKey, object authoringContext, AuthoringPageKind pageKind = AuthoringPageKind.Graph)
            {
                Tree = tree;
                TreeAsset = treeAsset;
                DisplayName = displayName;
                SourceTree = sourceTree;
                SourceNodeGuid = sourceNodeGuid;
                ReferenceKey = referenceKey;
                PageKind = pageKind;
                SerializedOwner = treeAsset ? treeAsset : tree?.SerializedOwner;
                SerializedPropertyPath = tree?.SerializedPropertyPath ?? string.Empty;
                AuthoringContext = authoringContext;
            }
        }

        public virtual void CreateGUI()
        {
            m_Tree = null;

            VisualElement root = rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            visualTree.CloneTree(root);

            m_LeftPanel = root.Q("left-panel");
            m_RightPanel = root.Q("right-panel");
            m_NavigationToolbar = root.Q("tree-navigation-toolbar");
            m_BackButton = root.Q<Button>("tree-navigation-back-button");
            m_BreadcrumbContainer = root.Q("tree-navigation-breadcrumb");

            m_BackButton.clicked += PopNavigationPage;
            CreateRuntimeDebugToolbar();

            m_TreeView = Activator.CreateInstance(m_TreeViewType) as BaseTreeView;
            m_TreeView.Init(this);
            m_TreeView.name = "tree-view";
            m_RightPanel.Add(m_TreeView);

            m_TreeTitle = new Label();
            m_TreeTitle.name = "tree-title";
            m_RightPanel.Add(m_TreeTitle);

            m_TreeInspectorView = Activator.CreateInstance(m_TreeInspectorViewType) as BaseTreeInspectorView;
            m_TreeInspectorView.name = "tree-inspector";
            m_LeftPanel.Add(m_TreeInspectorView);

            Undo.undoRedoPerformed += OnUndoRedo;
            RuntimeDebugSession.Shared.Changed += OnRuntimeDebugSessionChanged;
            TryRestoreCurrentTree();
            OnClosedCallback?.Invoke();
            RefreshNavigationToolbar();
        }

        public virtual void OnFocus()
        {
            TreeWindowUtility.SelectTree(m_Tree);
            OnFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Bind(m_Tree.GetSerializedTree());
            //    }
            //}
        }
        public virtual void OnLostFocus()
        {
            OnLostFocusCallback?.Invoke();

            //if (Application.isPlaying && m_Tree)
            //{
            //    foreach (var propertyField in rootVisualElement.Query<PropertyField>().ToList())
            //    {
            //        propertyField.Unbind();
            //    }
            //}
        }
        public virtual void OnDisable()
        {
            m_TreeView?.ClearView();
            m_TreeInspectorView?.ClearView();

            m_OpenedTrees.ForEach(i => 
            {
                if (i.User == null)
                    i.DisposeTree();
            });
            m_OpenedTrees.Clear();
            m_NavigationStack.Clear();
            foreach (RuntimeDebugViewBinding binding in m_RuntimeDebugBindings.Values)
                binding.Dispose(RuntimeDebugSession.Shared);
            m_RuntimeDebugBindings.Clear();
            m_RuntimeDebugRequests.Clear();

            Undo.undoRedoPerformed -= OnUndoRedo;
            RuntimeDebugSession.Shared.Changed -= OnRuntimeDebugSessionChanged;
            TreeWindowUtility.OnWindowClosed(this);
            
            OnClosedCallback?.Invoke();
            OnClosedCallback = null;
        }
        public virtual void Update()
        {
            if (m_Tree != null)
            {
                if (!Application.isPlaying)
                {
                    m_TreeTitle.text = m_Tree.name;
                    //m_TreeView.NodeViews.ForEach(i => i.Update());
                }
                m_TreeView.NodeViews.ForEach(i => i.Animation());
            }
            else if(m_Tree == null && !m_TreeView.Empty)
            {
                m_TreeView.ClearView();
                m_TreeInspectorView.ClearView();
            }

            if (EditorApplication.isCompiling)
                Close();
        }
        public void ReplaceNavigationRoot(BaseTree tree, object authoringContext = null)
        {
            if (tree == null)
                return;

            SetAuthoringContext(authoringContext);
            m_NavigationStack.Clear();
            m_NavigationStack.Add(new AuthoringPageEntry(tree, tree.SerializedOwner as BaseTreeAsset, tree.name, null, string.Empty, string.Empty, m_AuthoringContext));
            SelectNavigationPage(m_NavigationStack[0], false);
        }
        public void ReplaceNavigationRoot(BaseTreeAsset treeAsset, object authoringContext = null)
        {
            if (!treeAsset)
                return;

            SetAuthoringContext(authoringContext);
            BaseTree tree = treeAsset.Tree;
            m_NavigationStack.Clear();
            m_NavigationStack.Add(new AuthoringPageEntry(tree, treeAsset, treeAsset.name, null, string.Empty, string.Empty, m_AuthoringContext));
            SelectNavigationPage(m_NavigationStack[0], false);
        }
        public void SetAuthoringContext(object authoringContext)
        {
            if (ReferenceEquals(m_AuthoringContext, authoringContext))
                return;

            m_AuthoringContext = authoringContext;
            m_TreeInspectorView?.SetAuthoringContext(m_AuthoringContext);
            if (m_Tree != null)
                m_TreeInspectorView?.PopulateView(m_Tree);
        }
        public void PushReferencedTree(BaseNode sourceNode, NodeGraphReference reference)
        {
            if (reference.Tree == null)
                return;

            string displayName = GetReferenceDisplayName(sourceNode, reference);
            string sourceNodeGuid = sourceNode != null ? sourceNode.GUID : string.Empty;
            m_NavigationStack.Add(new AuthoringPageEntry(reference.Tree, reference.SharedAsset, displayName, m_Tree, sourceNodeGuid, reference.Key, m_AuthoringContext));
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PushReferencedTree(BaseEdge sourceEdge, BaseTree tree, string label)
        {
            if (tree == null)
                return;

            string edgeGuid = sourceEdge != null ? sourceEdge.GUID : string.Empty;
            string displayName = string.IsNullOrEmpty(label) ? tree.name : label;
            m_NavigationStack.Add(new AuthoringPageEntry(tree, null, displayName, m_Tree, edgeGuid, "ConditionRuleGraph", m_AuthoringContext));
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PushTreePage(BaseTree tree, BaseTreeAsset treeAsset, string displayName, string sourceIdentity, string referenceKey, AuthoringPageKind pageKind = AuthoringPageKind.Graph)
        {
            if (tree == null)
                return;
            m_NavigationStack.Add(new AuthoringPageEntry(tree, treeAsset, string.IsNullOrEmpty(displayName) ? tree.name : displayName, m_Tree, sourceIdentity ?? string.Empty, referenceKey ?? string.Empty, m_AuthoringContext, pageKind));
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PopNavigationPage()
        {
            if (m_NavigationStack.Count <= 1)
                return;

            m_NavigationStack.RemoveAt(m_NavigationStack.Count - 1);
            SelectNavigationPage(m_NavigationStack[m_NavigationStack.Count - 1], true);
        }
        public void PopNavigationTo(int index)
        {
            if (index < 0 || index >= m_NavigationStack.Count - 1)
                return;

            m_NavigationStack.RemoveRange(index + 1, m_NavigationStack.Count - index - 1);
            SelectNavigationPage(m_NavigationStack[index], true);
        }
        protected void SelectNavigationPage(AuthoringPageEntry entry, bool notifyOpened)
        {
            SetAuthoringContext(entry.AuthoringContext);
            SelectTree(entry.Tree);
            RefreshNavigationToolbar();

            if (notifyOpened && entry.Tree != null)
                TreeWindowUtility.NotifyOpened(this, entry.Tree);
        }
        protected string GetReferenceDisplayName(BaseNode sourceNode, NodeGraphReference reference)
        {
            string nodeName = GetNodeDisplayName(sourceNode);
            if (!string.IsNullOrEmpty(nodeName))
                return nodeName;

            if (!string.IsNullOrEmpty(reference.Label))
                return reference.Label;

            return reference.Tree != null ? reference.Tree.name : "Graph";
        }
        protected string GetNodeDisplayName(BaseNode sourceNode)
        {
            if (sourceNode == null)
                return string.Empty;

            NodeNameAttribute nodeNameAttribute = sourceNode.GetAttribute<NodeNameAttribute>();
            if (nodeNameAttribute != null)
            {
                MethodInfo methodInfo = sourceNode.GetMethod(nodeNameAttribute.Name);
                if (methodInfo != null)
                    return methodInfo.Invoke(sourceNode, null) as string;

                return nodeNameAttribute.Name;
            }

            return sourceNode.GetType().Name;
        }
        protected void RefreshNavigationToolbar()
        {
            if (m_BackButton == null || m_BreadcrumbContainer == null)
                return;

            m_BackButton.SetEnabled(m_NavigationStack.Count > 1);
            m_BreadcrumbContainer.Clear();

            if (m_NavigationStack.Count == 0)
            {
                if (m_Tree != null)
                    AddCurrentBreadcrumb(m_Tree.name);
                return;
            }

            for (int i = 0; i < m_NavigationStack.Count; i++)
            {
                if (i > 0)
                    AddBreadcrumbSeparator();

                AuthoringPageEntry entry = m_NavigationStack[i];
                string displayName = string.IsNullOrEmpty(entry.DisplayName) && entry.Tree != null ? entry.Tree.name : entry.DisplayName;
                if (i == m_NavigationStack.Count - 1)
                    AddCurrentBreadcrumb(displayName, entry.PageKind);
                else
                    AddBreadcrumbButton(displayName, i, entry.PageKind);
            }
        }
        void AddBreadcrumbButton(string text, int index, AuthoringPageKind pageKind)
        {
            Button button = new Button(() => PopNavigationTo(index));
            button.text = text;
            button.tooltip = pageKind.ToString();
            button.AddToClassList("tree-navigation-segment");
            m_BreadcrumbContainer.Add(button);
        }
        void AddCurrentBreadcrumb(string text, AuthoringPageKind pageKind = AuthoringPageKind.Graph)
        {
            Label label = new Label(text);
            label.tooltip = pageKind.ToString();
            label.AddToClassList("tree-navigation-current-segment");
            m_BreadcrumbContainer.Add(label);
        }
        void AddBreadcrumbSeparator()
        {
            Label separator = new Label("/");
            separator.AddToClassList("tree-navigation-separator");
            m_BreadcrumbContainer.Add(separator);
        }
        public void SelectTree(BaseTree tree)
        {
            if (tree == null)
                return;

            if (tree != m_Tree)
            {
                //m_Tree?.Dispose();
                m_Tree = tree;
                m_TreeAsset = m_NavigationStack.Count > 0 ? m_NavigationStack[m_NavigationStack.Count - 1].TreeAsset : tree.SerializedOwner as BaseTreeAsset;
                m_TreeTitle.text = m_Tree.name;

                if (!m_OpenedTrees.Contains(tree))
                    m_OpenedTrees.Add(tree);
                if (Application.isPlaying)
                {
                    m_Tree.RebindReadOnlyViewReferences();
                }
                else if (!IsLiveDebug)
                {
                    if (m_Tree.Refresh())
                        SetCurrentTreeDirty();
                    if (m_Tree.CheckInit())
                        SetCurrentTreeDirty();
                }
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.SetAuthoringContext(m_AuthoringContext);
                m_TreeInspectorView.SetVisibleBlackboardSources(ResolveVisibleTrees());
                m_TreeInspectorView.PopulateView(m_Tree);
                m_TreeView.SetRuntimeReadOnly(IsLiveDebug);
                m_TreeInspectorView.SetEnabled(!IsLiveDebug);
                InvalidateRuntimeDebugOverlay();
                TreeWindowUtility.SelectTree(m_Tree);
            }

            if (m_TreeTitle != null)
                m_TreeTitle.text = m_Tree.name;

            CaptureCurrentTreeRestoreState(m_Tree);
            RefreshNavigationToolbar();
            if (IsLiveDebug)
                RefreshRuntimeDebugOverlay();
        }

        void TryRestoreCurrentTree()
        {
            if (m_Tree != null || !m_CurrentTreeSerializedOwner ||
                string.IsNullOrEmpty(m_CurrentTreeSerializedPropertyPath) ||
                string.IsNullOrEmpty(m_CurrentTreeAuthoringId))
                return;

            SerializedObject serializedObject = new SerializedObject(m_CurrentTreeSerializedOwner);
            SerializedProperty property = serializedObject.FindProperty(m_CurrentTreeSerializedPropertyPath);
            BaseTree tree = property?.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue as BaseTree
                : null;
            if (tree == null ||
                !string.Equals(tree.GraphAuthoringId, m_CurrentTreeAuthoringId, StringComparison.Ordinal))
            {
                ClearCurrentTreeRestoreState();
                SetRuntimeDebugStatus("Current Graph source could not be restored after domain reload.");
                return;
            }

            tree.BindSerializedOwner(m_CurrentTreeSerializedOwner, m_CurrentTreeSerializedPropertyPath);
            m_NavigationStack.Clear();
            m_NavigationStack.Add(new AuthoringPageEntry(
                tree,
                m_CurrentTreeSerializedOwner as BaseTreeAsset,
                tree.name,
                null,
                string.Empty,
                string.Empty,
                m_AuthoringContext));
            SelectNavigationPage(m_NavigationStack[0], false);
        }

        void CaptureCurrentTreeRestoreState(BaseTree tree)
        {
            if (tree == null || !tree.SerializedOwner ||
                string.IsNullOrEmpty(tree.SerializedPropertyPath) ||
                string.IsNullOrEmpty(tree.GraphAuthoringId))
            {
                ClearCurrentTreeRestoreState();
                return;
            }

            m_CurrentTreeSerializedOwner = tree.SerializedOwner;
            m_CurrentTreeSerializedPropertyPath = tree.SerializedPropertyPath;
            m_CurrentTreeAuthoringId = tree.GraphAuthoringId;
        }

        void ClearCurrentTreeRestoreState()
        {
            m_CurrentTreeSerializedOwner = null;
            m_CurrentTreeSerializedPropertyPath = string.Empty;
            m_CurrentTreeAuthoringId = string.Empty;
        }

        public void PopulateSelectionInspector(IEnumerable<GraphSelectable> selection)
        {
            m_TreeInspectorView?.PopulateSelection(selection);
            m_TreeInspectorView?.SetEnabled(!IsLiveDebug);
        }
        void OnUndoRedo()
        {
            if (IsLiveDebug)
                return;
            if (m_Tree != null)
            {
                //m_Tree.DisposeTree();

                m_Tree.GetNewSerializedTree();
                if (m_Tree.Refresh())
                    SetCurrentTreeDirty();
                if (m_Tree.CheckInit())
                    SetCurrentTreeDirty();
                m_TreeView.PopulateView(m_Tree);
                m_TreeInspectorView.SetVisibleBlackboardSources(ResolveVisibleTrees());
                m_TreeInspectorView.PopulateView(m_Tree);
                InvalidateRuntimeDebugRequests();
            }
        }

        void OnBeforeRemovedAsTab()
        {
            m_Docking = true;
        }
        void OnAddedAsTab()
        {
            m_Docking = false;
        }

        void SetCurrentTreeDirty()
        {
            InvalidateRuntimeDebugRequests();
            if (CurrentPageSerializedOwner)
                EditorUtility.SetDirty(CurrentPageSerializedOwner);
        }

        void CreateRuntimeDebugToolbar()
        {
            m_DebugToolbar = new Toolbar();
            m_DebugToolbar.style.flexGrow = 1;
            m_AuthoringModeToggle = new ToolbarToggle { text = "Authoring", value = true };
            m_LiveDebugModeToggle = new ToolbarToggle { text = "Live Debug" };
            m_AuthoringModeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetWindowMode(TreeWindowMode.Authoring);
                else if (!IsLiveDebug)
                    m_AuthoringModeToggle.SetValueWithoutNotify(true);
            });
            m_LiveDebugModeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetWindowMode(TreeWindowMode.LiveDebug);
                else if (IsLiveDebug)
                    m_LiveDebugModeToggle.SetValueWithoutNotify(true);
            });
            m_DebugToolbar.Add(m_AuthoringModeToggle);
            m_DebugToolbar.Add(m_LiveDebugModeToggle);

            m_DebugTargetMenu = new ToolbarMenu { text = "Target" };
            m_DebugInstanceMenu = new ToolbarMenu { text = "Instance" };
            m_DebugFollowToggle = new ToolbarToggle { text = "Follow" };
            m_DebugLiveToggle = new ToolbarToggle { text = "Freeze" };
            m_DebugCaptureButton = new ToolbarButton(() =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.IsCaptureRecording)
                    session.EndCapture();
                else
                    session.BeginCapture(RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine, RuntimeDiagnosticsCaptureDetail.Evaluation);
            }) { text = "Capture" };
            m_DebugHistorySlider = new SliderInt(0, 511) { value = 0 };
            m_DebugHistorySlider.style.width = 110;
            m_DebugStatus = new Label();
            m_DebugStatus.style.marginLeft = 6;
            m_DebugStatus.style.flexGrow = 1;

            m_DebugFollowToggle.RegisterValueChangedCallback(evt =>
            {
                RuntimeDebugViewBinding binding = GetRuntimeDebugBinding();
                if (binding == null)
                    return;
                if (evt.newValue)
                    binding.Follow();
                else
                    binding.Clear();
                RefreshRuntimeDebugOverlay();
            });
            m_DebugLiveToggle.RegisterValueChangedCallback(evt =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.CanControlLiveTarget)
                    session.FreezeLive();
                else if (session.CanResumeLiveTarget)
                    session.ResumeLive();
            });
            m_DebugHistorySlider.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != RuntimeDebugSession.Shared.HistoryOffset)
                    RuntimeDebugSession.Shared.SetHistoryOffset(evt.newValue);
            });

            m_DebugToolbar.Add(m_DebugTargetMenu);
            m_DebugToolbar.Add(m_DebugInstanceMenu);
            m_DebugToolbar.Add(m_DebugFollowToggle);
            m_DebugToolbar.Add(m_DebugLiveToggle);
            m_DebugToolbar.Add(m_DebugCaptureButton);
            m_DebugToolbar.Add(m_DebugHistorySlider);
            m_DebugToolbar.Add(m_DebugStatus);
            m_NavigationToolbar.Add(m_DebugToolbar);
            SetWindowMode(TreeWindowMode.Authoring);
        }

        void SetWindowMode(TreeWindowMode mode)
        {
            m_WindowMode = mode;
            bool liveDebug = mode == TreeWindowMode.LiveDebug;
            m_AuthoringModeToggle?.SetValueWithoutNotify(!liveDebug);
            m_LiveDebugModeToggle?.SetValueWithoutNotify(liveDebug);
            if (m_DebugTargetMenu != null)
                m_DebugTargetMenu.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_DebugInstanceMenu != null)
                m_DebugInstanceMenu.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_DebugFollowToggle != null)
                m_DebugFollowToggle.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_DebugLiveToggle != null)
                m_DebugLiveToggle.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_DebugCaptureButton != null)
                m_DebugCaptureButton.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_DebugHistorySlider != null)
                m_DebugHistorySlider.style.display = DisplayStyle.None;
            if (m_DebugStatus != null)
                m_DebugStatus.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;

            m_TreeView?.SetRuntimeReadOnly(liveDebug);
            m_TreeInspectorView?.SetEnabled(!liveDebug);
            if (liveDebug)
                RefreshRuntimeDebugOverlay();
            else
            {
                ReleaseRuntimeDebugInterests();
                ClearRuntimeDebugOverlay();
                InvalidateRuntimeDebugOverlay();
            }
        }

        void RefreshRuntimeDebugOverlay()
        {
            if (m_Tree == null || m_TreeView == null)
                return;

            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewBinding binding = GetRuntimeDebugBinding();
            if (binding == null)
                return;
            RuntimeDebugTargetResolution resolution = binding.Refresh(session, RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine);
            RuntimeDebugViewModel view = session.ViewModel;
            RefreshRuntimeDebugMenus(view, binding);
            RefreshRuntimeDebugControls(session);
            m_DebugFollowToggle.SetValueWithoutNotify(binding.Following);
            m_TreeView.SetRuntimeReadOnly(true);
            m_TreeInspectorView.SetEnabled(false);

            if (!resolution.CanReadSnapshot)
            {
                ClearRuntimeDebugOverlay();
                InvalidateRuntimeDebugOverlay();
                SetRuntimeDebugStatus(resolution.Message);
                return;
            }

            if (!view.Valid)
            {
                ClearRuntimeDebugOverlay();
                InvalidateRuntimeDebugOverlay();
                SetRuntimeDebugStatus(!string.IsNullOrEmpty(view.Error) ? view.Error : binding.StatusMessage);
                return;
            }

            RuntimeInstanceKey instance = binding.SelectedInstance;
            if (!instance.IsValid)
            {
                ClearRuntimeDebugOverlay();
                InvalidateRuntimeDebugOverlay();
                SetRuntimeDebugStatus(binding.StatusMessage);
                return;
            }

            bool resetOverlay = !ReferenceEquals(m_LastRuntimeDebugView, view) ||
                                !m_LastRuntimeDebugInstance.Equals(instance) ||
                                !string.Equals(m_LastRuntimeDebugGraphAuthoringId, m_Tree.GraphAuthoringId, StringComparison.Ordinal) ||
                                view.Changes.FullSync;
            if (resetOverlay)
                ClearRuntimeDebugOverlay();

            if (resetOverlay || m_LastRuntimeDebugRevision != view.Revision)
                ApplyRuntimeDebugStates(view.GetGraphStates(m_Tree.GraphAuthoringId, instance, !resetOverlay));

            m_LastRuntimeDebugView = view;
            m_LastRuntimeDebugInstance = instance;
            m_LastRuntimeDebugGraphAuthoringId = m_Tree.GraphAuthoringId;
            m_LastRuntimeDebugRevision = view.Revision;
            string prefix = session.AttachmentState == RuntimeDebugAttachmentState.Ended ? "Ended | " :
                session.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory ? "Capture | " :
                session.AttachmentState == RuntimeDebugAttachmentState.Frozen ? "Frozen | " : string.Empty;
            SetRuntimeDebugStatus($"{prefix}{view.Target.DisplayName} | {ShortInstance(instance)} | L{view.LatestLogicTick} P{view.LatestPresentationFrame} | r{view.Target.Revision.CompilationRevision}");
        }

        void ApplyRuntimeDebugStates(IReadOnlyList<RuntimeElementDebugState> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                RuntimeElementDebugState state = states[i];
                if (state.Source.Kind == RuntimeSourceElementKind.Node)
                {
                    for (int nodeIndex = 0; nodeIndex < m_TreeView.NodeViews.Count; nodeIndex++)
                    {
                        BaseNodeView nodeView = m_TreeView.NodeViews[nodeIndex];
                        if (!string.Equals(nodeView.Node.GUID, state.Source.ElementAuthoringId, StringComparison.Ordinal))
                            continue;
                        nodeView.SetRuntimeDebugState(state.Status, $"{state.Kind} | {state.Domain} {state.Position} | seq {state.Sequence}");
                        break;
                    }
                }
                else if (state.Source.Kind == RuntimeSourceElementKind.Edge)
                {
                    foreach (BaseEdgeView edgeView in m_TreeView.edges.ToList().OfType<BaseEdgeView>())
                    {
                        if (!string.Equals(edgeView.Edge.GUID, state.Source.ElementAuthoringId, StringComparison.Ordinal))
                            continue;
                        edgeView.SetRuntimeDebugState(
                            state.Status,
                            state.Kind == RuntimeTraceEventKind.EdgeSelected ||
                            state.Kind == RuntimeTraceEventKind.StateTransitionSelected);
                        break;
                    }
                }
            }
        }

        void RefreshRuntimeDebugControls(RuntimeDebugSession session)
        {
            bool canResume = session.CanControlLiveTarget || session.CanResumeLiveTarget;
            m_DebugLiveToggle.text = session.CanControlLiveTarget ? "Freeze" : "Resume";
            m_DebugLiveToggle.SetValueWithoutNotify(session.AttachmentState == RuntimeDebugAttachmentState.Frozen);
            m_DebugLiveToggle.SetEnabled(canResume);
            m_DebugCaptureButton.text = session.IsCaptureRecording ? "Stop Capture" : "Capture";
            m_DebugCaptureButton.SetEnabled(session.CanStartCapture || session.CanStopCapture);
            bool showHistory = session.HasCaptureHistory;
            m_DebugHistorySlider.style.display = showHistory ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showHistory)
                return;

            m_DebugHistorySlider.highValue = Math.Max(0, session.CaptureSnapshot.SegmentCount - 1);
            m_DebugHistorySlider.SetValueWithoutNotify(Math.Min(session.HistoryOffset, m_DebugHistorySlider.highValue));
            m_DebugHistorySlider.SetEnabled(true);
        }

        RuntimeDebugViewBinding GetRuntimeDebugBinding()
        {
            if (m_Tree == null || string.IsNullOrEmpty(m_Tree.GraphAuthoringId))
                return null;

            if (!m_RuntimeDebugBindings.TryGetValue(m_Tree.GraphAuthoringId, out RuntimeDebugViewBinding binding))
            {
                binding = new RuntimeDebugViewBinding(RuntimeDebugViewKind.Graph);
                m_RuntimeDebugBindings.Add(m_Tree.GraphAuthoringId, binding);
            }

            if (!m_RuntimeDebugRequests.TryGetValue(m_Tree.GraphAuthoringId, out RuntimeDebugTargetRequest request))
            {
                request = new RuntimeDebugTargetRequest(
                    RuntimeSourceElementKey.Graph(m_Tree.GraphAuthoringId),
                    GraphAuthoringFingerprint.Compute(m_Tree));
                m_RuntimeDebugRequests.Add(m_Tree.GraphAuthoringId, request);
            }
            binding.Configure(request);
            return binding;
        }

        void RefreshRuntimeDebugMenus(RuntimeDebugViewModel view, RuntimeDebugViewBinding binding)
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            bool rebuild = m_LastRuntimeDebugMenuTargetRevision != session.TargetRevision ||
                           m_LastRuntimeDebugGraphInstanceRevision != view.GetGraphInstanceRevision(m_Tree.GraphAuthoringId) ||
                           view.Changes.FullSync;
            if (rebuild)
            {
                m_DebugTargetMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeDebugTargetCandidate> candidates = session.GetTargetCandidates(binding.Request);
                for (int i = 0; i < candidates.Count; i++)
                {
                    RuntimeDebugTargetCandidate candidate = candidates[i];
                    RuntimeDebugTargetInfo target = candidate.Target;
                    m_DebugTargetMenu.menu.AppendAction(
                        TargetLabel(target, candidate.Match),
                        _ => session.AttachToTarget(target.CharacterRuntimeId),
                        _ => candidate.IsExact ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                m_DebugInstanceMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeInstanceKey> instances = m_Tree != null && view.Attached
                    ? view.GetGraphInstances(m_Tree.GraphAuthoringId)
                    : Array.Empty<RuntimeInstanceKey>();
                for (int i = 0; i < instances.Count; i++)
                {
                    RuntimeInstanceKey instance = instances[i];
                    m_DebugInstanceMenu.menu.AppendAction(ShortInstance(instance), _ =>
                    {
                        if (binding.Pin(instance))
                            RefreshRuntimeDebugOverlay();
                    });
                }

                m_LastRuntimeDebugMenuTargetRevision = session.TargetRevision;
                m_LastRuntimeDebugGraphInstanceRevision = view.GetGraphInstanceRevision(m_Tree.GraphAuthoringId);
            }
            m_DebugTargetMenu.text = view.Attached
                ? view.Target.DisplayName + (session.AttachmentState == RuntimeDebugAttachmentState.Ended ? " (Ended)" : string.Empty)
                : "Target";
            m_DebugInstanceMenu.text = binding.SelectedInstance.IsValid
                ? ShortInstance(binding.SelectedInstance)
                : "Instance";
        }

        static string TargetLabel(RuntimeDebugTargetInfo target, RuntimeDebugTargetMatch match)
        {
            return match switch
            {
                RuntimeDebugTargetMatch.Exact => target.DisplayName,
                RuntimeDebugTargetMatch.SourceMissing => $"{target.DisplayName} (source missing)",
                RuntimeDebugTargetMatch.RevisionMismatch => $"{target.DisplayName} (revision mismatch)",
                _ => target.DisplayName
            };
        }

        void ClearRuntimeDebugOverlay()
        {
            if (m_TreeView == null)
                return;
            for (int i = 0; i < m_TreeView.NodeViews.Count; i++)
                m_TreeView.NodeViews[i].ClearRuntimeDebugState();
            foreach (BaseEdgeView edgeView in m_TreeView.edges.ToList().OfType<BaseEdgeView>())
                edgeView.ClearRuntimeDebugState();
        }

        void ReleaseRuntimeDebugInterests()
        {
            foreach (RuntimeDebugViewBinding binding in m_RuntimeDebugBindings.Values)
                binding.Dispose(RuntimeDebugSession.Shared);
        }

        void InvalidateRuntimeDebugRequests()
        {
            m_RuntimeDebugRequests.Clear();
            InvalidateRuntimeDebugOverlay();
        }

        void InvalidateRuntimeDebugOverlay()
        {
            m_LastRuntimeDebugView = null;
            m_LastRuntimeDebugInstance = default;
            m_LastRuntimeDebugGraphAuthoringId = string.Empty;
            m_LastRuntimeDebugRevision = -1;
            m_LastRuntimeDebugMenuTargetRevision = -1;
            m_LastRuntimeDebugGraphInstanceRevision = -1;
        }

        void SetRuntimeDebugStatus(string text)
        {
            if (m_DebugStatus == null)
                return;
            m_DebugStatus.text = text ?? string.Empty;
            m_DebugStatus.tooltip = text ?? string.Empty;
        }

        void OnRuntimeDebugSessionChanged()
        {
            if (IsLiveDebug)
                RefreshRuntimeDebugOverlay();
            Repaint();
        }

        static string ShortInstance(RuntimeInstanceKey instance)
        {
            return instance.Kind switch
            {
                RuntimeInstanceKind.StateActivation => $"State {instance.StateId} #{instance.ActivationGeneration}",
                RuntimeInstanceKind.TimelinePlayback => $"Timeline #{instance.TimelinePlaybackId}",
                RuntimeInstanceKind.TreeClip => $"TreeClip #{instance.TimelinePlaybackId}/{instance.TreeClipCycle}",
                RuntimeInstanceKind.Graph => $"Graph {instance.GraphRuntimeId.ToString("N").Substring(0, 8)}",
                _ => instance.Kind.ToString()
            };
        }
    }

    public static class AuthoringPageOpenRegistry
    {
        static readonly Dictionary<Type, Action<BaseTreeWindow, BaseNode>> s_OpenHandlers = new Dictionary<Type, Action<BaseTreeWindow, BaseNode>>();
        static readonly Dictionary<Type, Action<BaseTreeInspectorView, BaseNodeView, BaseNode>> s_InspectorHandlers = new Dictionary<Type, Action<BaseTreeInspectorView, BaseNodeView, BaseNode>>();

        public static void Register<TNode>(Action<BaseTreeWindow, TNode> open, Action<BaseTreeInspectorView, BaseNodeView, TNode> populateInspector = null) where TNode : BaseNode
        {
            if (open != null)
                s_OpenHandlers[typeof(TNode)] = (window, node) => open(window, (TNode)node);
            if (populateInspector != null)
                s_InspectorHandlers[typeof(TNode)] = (inspector, view, node) => populateInspector(inspector, view, (TNode)node);
        }

        public static bool CanOpen(BaseNode node)
        {
            return node != null && s_OpenHandlers.ContainsKey(node.GetType());
        }

        public static bool TryOpen(BaseTreeWindow window, BaseNode node)
        {
            if (window == null || node == null || !s_OpenHandlers.TryGetValue(node.GetType(), out Action<BaseTreeWindow, BaseNode> open))
                return false;
            open(window, node);
            return true;
        }

        public static void PopulateInspector(BaseTreeInspectorView inspector, BaseNodeView view, BaseNode node)
        {
            if (node != null && s_InspectorHandlers.TryGetValue(node.GetType(), out Action<BaseTreeInspectorView, BaseNodeView, BaseNode> populate))
                populate(inspector, view, node);
        }
    }
}
