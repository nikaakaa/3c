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

    [Serializable]
    internal sealed class TreeWindowNavigationController
    {
        [SerializeField]
        UnityEngine.Object m_CurrentTreeSerializedOwner;
        [SerializeField]
        string m_CurrentTreeSerializedPropertyPath;
        [SerializeField]
        string m_CurrentTreeAuthoringId;

        [NonSerialized]
        BaseTreeWindow m_Window;
        [NonSerialized]
        Button m_BackButton;
        [NonSerialized]
        VisualElement m_BreadcrumbContainer;
        [NonSerialized]
        List<BaseTree> m_OpenedTrees;
        [NonSerialized]
        List<AuthoringPageEntry> m_Stack;
        [NonSerialized]
        object m_AuthoringContext;

        public object AuthoringContext => m_AuthoringContext;
        public UnityEngine.Object CurrentPageSerializedOwner => m_Stack != null && m_Stack.Count > 0
            ? m_Stack[m_Stack.Count - 1].SerializedOwner
            : m_Window?.Tree?.SerializedOwner;
        public string CurrentPageSerializedPropertyPath => m_Stack != null && m_Stack.Count > 0
            ? m_Stack[m_Stack.Count - 1].SerializedPropertyPath
            : m_Window?.Tree?.SerializedPropertyPath ?? string.Empty;

        public IReadOnlyList<BaseTree> VisibleTrees
        {
            get
            {
                List<BaseTree> trees = new List<BaseTree>();
                if (m_Stack != null)
                {
                    for (int i = 0; i < m_Stack.Count; i++)
                    {
                        BaseTree tree = m_Stack[i].Tree;
                        if (tree != null && !trees.Contains(tree))
                            trees.Add(tree);
                    }
                }
                if (m_Window?.Tree != null && !trees.Contains(m_Window.Tree))
                    trees.Add(m_Window.Tree);
                return trees;
            }
        }

        public void Initialize(BaseTreeWindow window, Button backButton, VisualElement breadcrumbContainer)
        {
            m_Window = window;
            m_BackButton = backButton;
            m_BreadcrumbContainer = breadcrumbContainer;
            m_OpenedTrees = new List<BaseTree>();
            m_Stack = new List<AuthoringPageEntry>();
            m_BackButton.clicked += Pop;
        }

        public void Dispose()
        {
            if (m_OpenedTrees != null)
            {
                foreach (BaseTree tree in m_OpenedTrees)
                {
                    if (tree != null && tree.User == null)
                        tree.DisposeTree();
                }
                m_OpenedTrees.Clear();
            }
            m_Stack?.Clear();
            m_Window = null;
            m_BackButton = null;
            m_BreadcrumbContainer = null;
        }

        public void ReplaceRoot(BaseTree tree, object authoringContext)
        {
            if (tree == null)
                return;

            SetAuthoringContext(authoringContext);
            m_Stack.Clear();
            m_Stack.Add(new AuthoringPageEntry(tree, tree.SerializedOwner as BaseTreeAsset, tree.name, null, string.Empty, string.Empty, m_AuthoringContext));
            SelectPage(m_Stack[0], false);
        }

        public void ReplaceRoot(BaseTreeAsset treeAsset, object authoringContext)
        {
            if (!treeAsset)
                return;

            SetAuthoringContext(authoringContext);
            BaseTree tree = treeAsset.Tree;
            m_Stack.Clear();
            m_Stack.Add(new AuthoringPageEntry(tree, treeAsset, treeAsset.name, null, string.Empty, string.Empty, m_AuthoringContext));
            SelectPage(m_Stack[0], false);
        }

        public void SetAuthoringContext(object authoringContext)
        {
            if (ReferenceEquals(m_AuthoringContext, authoringContext))
                return;

            m_AuthoringContext = authoringContext;
            m_Window.ApplyAuthoringContext(authoringContext);
        }

        public void Push(BaseNode sourceNode, NodeGraphReference reference)
        {
            if (reference.Tree == null)
                return;

            string displayName = GetReferenceDisplayName(sourceNode, reference);
            string sourceNodeGuid = sourceNode != null ? sourceNode.GUID : string.Empty;
            m_Stack.Add(new AuthoringPageEntry(reference.Tree, reference.SharedAsset, displayName, m_Window.Tree, sourceNodeGuid, reference.Key, m_AuthoringContext));
            SelectPage(m_Stack[m_Stack.Count - 1], true);
        }

        public void Push(BaseEdge sourceEdge, BaseTree tree, string label)
        {
            if (tree == null)
                return;

            string edgeGuid = sourceEdge != null ? sourceEdge.GUID : string.Empty;
            string displayName = string.IsNullOrEmpty(label) ? tree.name : label;
            m_Stack.Add(new AuthoringPageEntry(tree, null, displayName, m_Window.Tree, edgeGuid, "ConditionRuleGraph", m_AuthoringContext));
            SelectPage(m_Stack[m_Stack.Count - 1], true);
        }

        public void Push(
            BaseTree tree,
            BaseTreeAsset treeAsset,
            string displayName,
            string sourceIdentity,
            string referenceKey,
            AuthoringPageKind pageKind)
        {
            if (tree == null)
                return;
            m_Stack.Add(new AuthoringPageEntry(
                tree,
                treeAsset,
                string.IsNullOrEmpty(displayName) ? tree.name : displayName,
                m_Window.Tree,
                sourceIdentity ?? string.Empty,
                referenceKey ?? string.Empty,
                m_AuthoringContext,
                pageKind));
            SelectPage(m_Stack[m_Stack.Count - 1], true);
        }

        public void Pop()
        {
            if (m_Stack.Count <= 1)
                return;

            m_Stack.RemoveAt(m_Stack.Count - 1);
            SelectPage(m_Stack[m_Stack.Count - 1], true);
        }

        public void PopTo(int index)
        {
            if (index < 0 || index >= m_Stack.Count - 1)
                return;

            m_Stack.RemoveRange(index + 1, m_Stack.Count - index - 1);
            SelectPage(m_Stack[index], true);
        }

        public void SelectTree(BaseTree tree, BaseTreeAsset treeAsset = null)
        {
            if (tree == null)
                return;

            if (!m_OpenedTrees.Contains(tree))
                m_OpenedTrees.Add(tree);
            BaseTreeAsset pageAsset = treeAsset;
            if (!pageAsset && m_Stack.Count > 0 && ReferenceEquals(m_Stack[m_Stack.Count - 1].Tree, tree))
                pageAsset = m_Stack[m_Stack.Count - 1].TreeAsset;
            if (!pageAsset)
                pageAsset = tree.SerializedOwner as BaseTreeAsset;
            m_Window.ApplyNavigationTree(tree, pageAsset);
            CaptureRestoreState(tree);
            RefreshToolbar();
        }

        public IReadOnlyList<BaseTree> ResolveVisibleTrees()
        {
            List<BaseTree> trees = VisibleTrees.ToList();
            if (m_AuthoringContext is ITreeInspectorBlackboardAuthoringContext source)
            {
                IEnumerable<BaseTree> additional = source.GetAdditionalVisibleBlackboardSources(m_Window.Tree);
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

        public void TryRestore()
        {
            bool hasOwner = m_CurrentTreeSerializedOwner;
            bool hasPath = !string.IsNullOrEmpty(m_CurrentTreeSerializedPropertyPath);
            bool hasAuthoringId = !string.IsNullOrEmpty(m_CurrentTreeAuthoringId);
            if (!hasOwner && !hasPath && !hasAuthoringId)
                return;
            if (!hasOwner || !hasPath || !hasAuthoringId)
            {
                ClearRestoreState();
                m_Window.ReportNavigationRestoreError("Current Graph locator is incomplete after domain reload.");
                return;
            }

            SerializedObject serializedObject = new SerializedObject(m_CurrentTreeSerializedOwner);
            SerializedProperty property = serializedObject.FindProperty(m_CurrentTreeSerializedPropertyPath);
            BaseTree tree = property?.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue as BaseTree
                : null;
            if (tree == null ||
                !string.Equals(tree.GraphAuthoringId, m_CurrentTreeAuthoringId, StringComparison.Ordinal))
            {
                ClearRestoreState();
                m_Window.ReportNavigationRestoreError("Current Graph source could not be restored after domain reload.");
                return;
            }

            tree.BindSerializedOwner(m_CurrentTreeSerializedOwner, m_CurrentTreeSerializedPropertyPath);
            m_Stack.Clear();
            m_Stack.Add(new AuthoringPageEntry(
                tree,
                m_CurrentTreeSerializedOwner as BaseTreeAsset,
                tree.name,
                null,
                string.Empty,
                string.Empty,
                m_AuthoringContext));
            SelectPage(m_Stack[0], false);
        }

        public void RefreshToolbar()
        {
            if (m_BackButton == null || m_BreadcrumbContainer == null)
                return;

            m_BackButton.SetEnabled(m_Stack.Count > 1);
            m_BreadcrumbContainer.Clear();
            if (m_Stack.Count == 0)
            {
                if (m_Window.Tree != null)
                    AddCurrentBreadcrumb(m_Window.Tree.name, AuthoringPageKind.Graph);
                return;
            }

            for (int i = 0; i < m_Stack.Count; i++)
            {
                if (i > 0)
                    AddBreadcrumbSeparator();

                AuthoringPageEntry entry = m_Stack[i];
                string displayName = string.IsNullOrEmpty(entry.DisplayName) && entry.Tree != null ? entry.Tree.name : entry.DisplayName;
                if (i == m_Stack.Count - 1)
                    AddCurrentBreadcrumb(displayName, entry.PageKind);
                else
                    AddBreadcrumbButton(displayName, i, entry.PageKind);
            }
        }

        void SelectPage(AuthoringPageEntry entry, bool notifyOpened)
        {
            SetAuthoringContext(entry.AuthoringContext);
            SelectTree(entry.Tree, entry.TreeAsset);
            if (notifyOpened && entry.Tree != null)
                TreeWindowUtility.NotifyOpened(m_Window, entry.Tree);
        }

        void CaptureRestoreState(BaseTree tree)
        {
            if (tree == null || !tree.SerializedOwner ||
                string.IsNullOrEmpty(tree.SerializedPropertyPath) ||
                string.IsNullOrEmpty(tree.GraphAuthoringId))
            {
                ClearRestoreState();
                return;
            }

            m_CurrentTreeSerializedOwner = tree.SerializedOwner;
            m_CurrentTreeSerializedPropertyPath = tree.SerializedPropertyPath;
            m_CurrentTreeAuthoringId = tree.GraphAuthoringId;
        }

        void ClearRestoreState()
        {
            m_CurrentTreeSerializedOwner = null;
            m_CurrentTreeSerializedPropertyPath = string.Empty;
            m_CurrentTreeAuthoringId = string.Empty;
        }

        void AddBreadcrumbButton(string text, int index, AuthoringPageKind pageKind)
        {
            Button button = new Button(() => PopTo(index))
            {
                text = text,
                tooltip = pageKind.ToString()
            };
            button.AddToClassList("tree-navigation-segment");
            m_BreadcrumbContainer.Add(button);
        }

        void AddCurrentBreadcrumb(string text, AuthoringPageKind pageKind)
        {
            Label label = new Label(text) { tooltip = pageKind.ToString() };
            label.AddToClassList("tree-navigation-current-segment");
            m_BreadcrumbContainer.Add(label);
        }

        void AddBreadcrumbSeparator()
        {
            Label separator = new Label("/");
            separator.AddToClassList("tree-navigation-separator");
            m_BreadcrumbContainer.Add(separator);
        }

        static string GetReferenceDisplayName(BaseNode sourceNode, NodeGraphReference reference)
        {
            string nodeName = GetNodeDisplayName(sourceNode);
            if (!string.IsNullOrEmpty(nodeName))
                return nodeName;
            if (!string.IsNullOrEmpty(reference.Label))
                return reference.Label;
            return reference.Tree != null ? reference.Tree.name : "Graph";
        }

        static string GetNodeDisplayName(BaseNode sourceNode)
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

        readonly struct AuthoringPageEntry
        {
            public readonly BaseTree Tree;
            public readonly BaseTreeAsset TreeAsset;
            public readonly string DisplayName;
            public readonly BaseTree SourceTree;
            public readonly string SourceIdentity;
            public readonly string ReferenceKey;
            public readonly AuthoringPageKind PageKind;
            public readonly UnityEngine.Object SerializedOwner;
            public readonly string SerializedPropertyPath;
            public readonly object AuthoringContext;

            public AuthoringPageEntry(
                BaseTree tree,
                BaseTreeAsset treeAsset,
                string displayName,
                BaseTree sourceTree,
                string sourceIdentity,
                string referenceKey,
                object authoringContext,
                AuthoringPageKind pageKind = AuthoringPageKind.Graph)
            {
                Tree = tree;
                TreeAsset = treeAsset;
                DisplayName = displayName;
                SourceTree = sourceTree;
                SourceIdentity = sourceIdentity;
                ReferenceKey = referenceKey;
                PageKind = pageKind;
                SerializedOwner = treeAsset ? treeAsset : tree?.SerializedOwner;
                SerializedPropertyPath = tree?.SerializedPropertyPath ?? string.Empty;
                AuthoringContext = authoringContext;
            }
        }
    }

    [Serializable]
    internal sealed class TreeWindowRuntimeOverlayController
    {
        [SerializeField]
        TreeWindowMode m_WindowMode;

        [NonSerialized]
        BaseTreeWindow m_Window;
        [NonSerialized]
        BaseTreeView m_TreeView;
        [NonSerialized]
        BaseTreeInspectorView m_InspectorView;
        [NonSerialized]
        Dictionary<string, RuntimeDebugViewBinding> m_Bindings;
        [NonSerialized]
        Dictionary<string, RuntimeDebugTargetRequest> m_Requests;
        [NonSerialized]
        VisualElement m_Toolbar;
        [NonSerialized]
        ToolbarToggle m_AuthoringModeToggle;
        [NonSerialized]
        ToolbarToggle m_LiveDebugModeToggle;
        [NonSerialized]
        ToolbarMenu m_TargetMenu;
        [NonSerialized]
        ToolbarMenu m_InstanceMenu;
        [NonSerialized]
        ToolbarToggle m_FollowToggle;
        [NonSerialized]
        ToolbarToggle m_LiveToggle;
        [NonSerialized]
        ToolbarButton m_CaptureButton;
        [NonSerialized]
        SliderInt m_HistorySlider;
        [NonSerialized]
        Label m_Status;
        [NonSerialized]
        RuntimeDebugViewModel m_LastView;
        [NonSerialized]
        RuntimeInstanceKey m_LastInstance;
        [NonSerialized]
        string m_LastGraphAuthoringId = string.Empty;
        [NonSerialized]
        long m_LastRevision = -1;
        [NonSerialized]
        long m_LastMenuTargetRevision = -1;
        [NonSerialized]
        long m_LastGraphInstanceRevision = -1;

        public bool IsLiveDebug => m_WindowMode == TreeWindowMode.LiveDebug;

        public void Initialize(
            BaseTreeWindow window,
            VisualElement navigationToolbar,
            BaseTreeView treeView,
            BaseTreeInspectorView inspectorView)
        {
            m_Window = window;
            m_TreeView = treeView;
            m_InspectorView = inspectorView;
            m_Bindings = new Dictionary<string, RuntimeDebugViewBinding>(StringComparer.Ordinal);
            m_Requests = new Dictionary<string, RuntimeDebugTargetRequest>(StringComparer.Ordinal);
            CreateToolbar(navigationToolbar);
            RuntimeDebugSession.Shared.Changed -= OnSessionChanged;
            RuntimeDebugSession.Shared.Changed += OnSessionChanged;
            SetMode(m_WindowMode);
        }

        public void Dispose()
        {
            RuntimeDebugSession.Shared.Changed -= OnSessionChanged;
            ReleaseInterests();
            m_Bindings?.Clear();
            m_Requests?.Clear();
            m_Window = null;
            m_TreeView = null;
            m_InspectorView = null;
        }

        public void SetMode(TreeWindowMode mode)
        {
            m_WindowMode = mode;
            bool liveDebug = mode == TreeWindowMode.LiveDebug;
            m_AuthoringModeToggle?.SetValueWithoutNotify(!liveDebug);
            m_LiveDebugModeToggle?.SetValueWithoutNotify(liveDebug);
            if (m_TargetMenu != null)
                m_TargetMenu.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_InstanceMenu != null)
                m_InstanceMenu.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_FollowToggle != null)
                m_FollowToggle.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_LiveToggle != null)
                m_LiveToggle.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_CaptureButton != null)
                m_CaptureButton.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_HistorySlider != null)
                m_HistorySlider.style.display = DisplayStyle.None;
            if (m_Status != null)
                m_Status.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;

            m_TreeView?.SetRuntimeReadOnly(liveDebug);
            m_InspectorView?.SetEnabled(!liveDebug);
            if (liveDebug)
                Refresh();
            else
            {
                ReleaseInterests();
                ClearOverlay();
                InvalidateOverlay();
            }
        }

        public void OnTreeChanged()
        {
            InvalidateOverlay();
            if (IsLiveDebug)
                Refresh();
        }

        public void Refresh()
        {
            BaseTree tree = m_Window?.Tree;
            if (tree == null || m_TreeView == null)
                return;

            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewBinding binding = GetBinding(tree);
            if (binding == null)
                return;
            RuntimeDebugTargetResolution resolution = binding.Refresh(session, RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine);
            RuntimeDebugViewModel view = session.ViewModel;
            RefreshMenus(tree, view, binding);
            RefreshControls(session);
            m_FollowToggle.SetValueWithoutNotify(binding.Following);
            m_TreeView.SetRuntimeReadOnly(true);
            m_InspectorView.SetEnabled(false);

            if (!resolution.CanReadSnapshot)
            {
                ClearOverlay();
                InvalidateOverlay();
                SetStatus(resolution.Message);
                return;
            }

            if (!view.Valid)
            {
                ClearOverlay();
                InvalidateOverlay();
                SetStatus(!string.IsNullOrEmpty(view.Error) ? view.Error : binding.StatusMessage);
                return;
            }

            RuntimeInstanceKey instance = binding.SelectedInstance;
            if (!instance.IsValid)
            {
                ClearOverlay();
                InvalidateOverlay();
                SetStatus(binding.StatusMessage);
                return;
            }

            bool resetOverlay = !ReferenceEquals(m_LastView, view) ||
                                !m_LastInstance.Equals(instance) ||
                                !string.Equals(m_LastGraphAuthoringId, tree.GraphAuthoringId, StringComparison.Ordinal) ||
                                view.Changes.FullSync;
            if (resetOverlay)
                ClearOverlay();

            if (resetOverlay || m_LastRevision != view.Revision)
                ApplyStates(view.GetGraphStates(tree.GraphAuthoringId, instance, !resetOverlay));

            m_LastView = view;
            m_LastInstance = instance;
            m_LastGraphAuthoringId = tree.GraphAuthoringId;
            m_LastRevision = view.Revision;
            string prefix = session.AttachmentState == RuntimeDebugAttachmentState.Ended ? "Ended | " :
                session.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory ? "Capture | " :
                session.AttachmentState == RuntimeDebugAttachmentState.Frozen ? "Frozen | " : string.Empty;
            SetStatus($"{prefix}{view.Target.DisplayName} | {ShortInstance(instance)} | L{view.LatestLogicTick} P{view.LatestPresentationFrame} | {view.Target.Revision.ProgramHash}");
        }

        public void InvalidateRequests()
        {
            m_Requests?.Clear();
            InvalidateOverlay();
        }

        public void InvalidateOverlay()
        {
            m_LastView = null;
            m_LastInstance = default;
            m_LastGraphAuthoringId = string.Empty;
            m_LastRevision = -1;
            m_LastMenuTargetRevision = -1;
            m_LastGraphInstanceRevision = -1;
        }

        public void SetStatus(string text)
        {
            if (m_Status == null)
                return;
            m_Status.text = text ?? string.Empty;
            m_Status.tooltip = text ?? string.Empty;
        }

        void CreateToolbar(VisualElement navigationToolbar)
        {
            m_Toolbar = new Toolbar();
            m_Toolbar.style.flexGrow = 1;
            m_AuthoringModeToggle = new ToolbarToggle { text = "Authoring", value = true };
            m_LiveDebugModeToggle = new ToolbarToggle { text = "Live Debug" };
            m_AuthoringModeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetMode(TreeWindowMode.Authoring);
                else if (!IsLiveDebug)
                    m_AuthoringModeToggle.SetValueWithoutNotify(true);
            });
            m_LiveDebugModeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetMode(TreeWindowMode.LiveDebug);
                else if (IsLiveDebug)
                    m_LiveDebugModeToggle.SetValueWithoutNotify(true);
            });
            m_Toolbar.Add(m_AuthoringModeToggle);
            m_Toolbar.Add(m_LiveDebugModeToggle);

            m_TargetMenu = new ToolbarMenu { text = "Target" };
            m_InstanceMenu = new ToolbarMenu { text = "Instance" };
            m_FollowToggle = new ToolbarToggle { text = "Follow" };
            m_LiveToggle = new ToolbarToggle { text = "Freeze" };
            m_CaptureButton = new ToolbarButton(() =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.IsCaptureRecording)
                    session.EndCapture();
                else
                    session.BeginCapture(RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine, RuntimeDiagnosticsCaptureDetail.Evaluation);
            }) { text = "Capture" };
            m_HistorySlider = new SliderInt(0, 511) { value = 0 };
            m_HistorySlider.style.width = 110;
            m_Status = new Label();
            m_Status.style.marginLeft = 6;
            m_Status.style.flexGrow = 1;

            m_FollowToggle.RegisterValueChangedCallback(evt =>
            {
                RuntimeDebugViewBinding binding = GetBinding(m_Window?.Tree);
                if (binding == null)
                    return;
                if (evt.newValue)
                    binding.Follow();
                else
                    binding.Clear();
                Refresh();
            });
            m_LiveToggle.RegisterValueChangedCallback(_ =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.CanControlLiveTarget)
                    session.FreezeLive();
                else if (session.CanResumeLiveTarget)
                    session.ResumeLive();
            });
            m_HistorySlider.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != RuntimeDebugSession.Shared.HistoryOffset)
                    RuntimeDebugSession.Shared.SetHistoryOffset(evt.newValue);
            });

            m_Toolbar.Add(m_TargetMenu);
            m_Toolbar.Add(m_InstanceMenu);
            m_Toolbar.Add(m_FollowToggle);
            m_Toolbar.Add(m_LiveToggle);
            m_Toolbar.Add(m_CaptureButton);
            m_Toolbar.Add(m_HistorySlider);
            m_Toolbar.Add(m_Status);
            navigationToolbar.Add(m_Toolbar);
        }

        void ApplyStates(IReadOnlyList<RuntimeElementDebugState> states)
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

        void RefreshControls(RuntimeDebugSession session)
        {
            bool canResume = session.CanControlLiveTarget || session.CanResumeLiveTarget;
            m_LiveToggle.text = session.CanControlLiveTarget ? "Freeze" : "Resume";
            m_LiveToggle.SetValueWithoutNotify(session.AttachmentState == RuntimeDebugAttachmentState.Frozen);
            m_LiveToggle.SetEnabled(canResume);
            m_CaptureButton.text = session.IsCaptureRecording ? "Stop Capture" : "Capture";
            m_CaptureButton.SetEnabled(session.CanStartCapture || session.CanStopCapture);
            bool showHistory = session.HasCaptureHistory;
            m_HistorySlider.style.display = showHistory ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showHistory)
                return;

            m_HistorySlider.highValue = Math.Max(0, session.CaptureSnapshot.SegmentCount - 1);
            m_HistorySlider.SetValueWithoutNotify(Math.Min(session.HistoryOffset, m_HistorySlider.highValue));
            m_HistorySlider.SetEnabled(true);
        }

        RuntimeDebugViewBinding GetBinding(BaseTree tree)
        {
            if (tree == null || string.IsNullOrEmpty(tree.GraphAuthoringId))
                return null;

            if (!m_Bindings.TryGetValue(tree.GraphAuthoringId, out RuntimeDebugViewBinding binding))
            {
                binding = new RuntimeDebugViewBinding(RuntimeDebugViewKind.Graph);
                m_Bindings.Add(tree.GraphAuthoringId, binding);
            }

            if (!m_Requests.TryGetValue(tree.GraphAuthoringId, out RuntimeDebugTargetRequest request))
            {
                request = new RuntimeDebugTargetRequest(
                    RuntimeSourceElementKey.Graph(tree.GraphAuthoringId),
                    GraphAuthoringFingerprint.Compute(tree));
                m_Requests.Add(tree.GraphAuthoringId, request);
            }
            binding.Configure(request);
            return binding;
        }

        void RefreshMenus(BaseTree tree, RuntimeDebugViewModel view, RuntimeDebugViewBinding binding)
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            bool rebuild = m_LastMenuTargetRevision != session.TargetRevision ||
                           m_LastGraphInstanceRevision != view.GetGraphInstanceRevision(tree.GraphAuthoringId) ||
                           view.Changes.FullSync;
            if (rebuild)
            {
                m_TargetMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeDebugTargetCandidate> candidates = session.GetTargetCandidates(binding.Request);
                for (int i = 0; i < candidates.Count; i++)
                {
                    RuntimeDebugTargetCandidate candidate = candidates[i];
                    RuntimeDebugTargetInfo target = candidate.Target;
                    m_TargetMenu.menu.AppendAction(
                        TargetLabel(target, candidate.Match),
                        _ => session.AttachToTarget(target.CharacterRuntimeId),
                        _ => candidate.IsExact ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                m_InstanceMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeInstanceKey> instances = view.Attached
                    ? view.GetGraphInstances(tree.GraphAuthoringId)
                    : Array.Empty<RuntimeInstanceKey>();
                for (int i = 0; i < instances.Count; i++)
                {
                    RuntimeInstanceKey instance = instances[i];
                    m_InstanceMenu.menu.AppendAction(ShortInstance(instance), _ =>
                    {
                        if (binding.Pin(instance))
                            Refresh();
                    });
                }

                m_LastMenuTargetRevision = session.TargetRevision;
                m_LastGraphInstanceRevision = view.GetGraphInstanceRevision(tree.GraphAuthoringId);
            }
            m_TargetMenu.text = view.Attached
                ? view.Target.DisplayName + (session.AttachmentState == RuntimeDebugAttachmentState.Ended ? " (Ended)" : string.Empty)
                : "Target";
            m_InstanceMenu.text = binding.SelectedInstance.IsValid
                ? ShortInstance(binding.SelectedInstance)
                : "Instance";
        }

        void ClearOverlay()
        {
            if (m_TreeView == null)
                return;
            for (int i = 0; i < m_TreeView.NodeViews.Count; i++)
                m_TreeView.NodeViews[i].ClearRuntimeDebugState();
            foreach (BaseEdgeView edgeView in m_TreeView.edges.ToList().OfType<BaseEdgeView>())
                edgeView.ClearRuntimeDebugState();
        }

        void ReleaseInterests()
        {
            if (m_Bindings == null)
                return;
            foreach (RuntimeDebugViewBinding binding in m_Bindings.Values)
                binding.Dispose(RuntimeDebugSession.Shared);
        }

        void OnSessionChanged()
        {
            if (IsLiveDebug)
                Refresh();
            m_Window?.Repaint();
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

    public class BaseTreeWindow : EditorWindow
    {
        [SerializeField]
        TreeWindowNavigationController m_Navigation = new TreeWindowNavigationController();

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;

        protected BaseTreeAsset m_TreeAsset;
        public BaseTreeAsset TreeAsset => m_TreeAsset;

        public object AuthoringContext => m_Navigation.AuthoringContext;
        
        protected BaseTreeView m_TreeView;
        public BaseTreeView TreeView => m_TreeView;

        protected VisualElement m_LeftPanel;
        protected VisualElement m_RightPanel;
        protected VisualElement m_NavigationToolbar;
        protected Label m_TreeTitle;
        [SerializeField]
        TreeWindowRuntimeOverlayController m_RuntimeOverlay = new TreeWindowRuntimeOverlayController();
        public bool IsLiveDebug => m_RuntimeOverlay.IsLiveDebug;
        protected BaseTreeInspectorView m_TreeInspectorView;
        public UnityEngine.Object CurrentPageSerializedOwner => m_Navigation.CurrentPageSerializedOwner;
        public string CurrentPageSerializedPropertyPath => m_Navigation.CurrentPageSerializedPropertyPath;
        public IReadOnlyList<BaseTree> VisibleTrees => m_Navigation.VisibleTrees;

        public IEnumerable<BaseExposedProperty> GetVisibleExposedProperties()
        {
            return ResolveVisibleTrees().SelectMany(i => i.ExposedProperties);
        }

        public IReadOnlyList<BaseTree> ResolveVisibleTrees()
        {
            return m_Navigation.ResolveVisibleTrees();
        }

        public bool TryResolveExposedProperty(PipelineBlackboardVariableReference reference, out BaseExposedProperty declaration)
        {
            declaration = GetVisibleExposedProperties().FirstOrDefault(i =>
                string.Equals(i.DeclarationId, reference.DeclarationId, StringComparison.Ordinal) &&
                string.Equals(i.DeclarationOwnerId, reference.DeclarationOwnerId, StringComparison.Ordinal));
            return declaration != null;
        }

        public bool FocusBlackboardDeclaration(string graphAuthoringId, string declarationId)
        {
            return m_TreeInspectorView != null &&
                   m_TreeInspectorView.FocusBlackboardDeclaration(graphAuthoringId, declarationId);
        }

        protected virtual Type m_TreeViewType => typeof(BaseTreeView);
        protected virtual Type m_TreeInspectorViewType => typeof(BaseTreeInspectorView);

        public Action OnClosedCallback;
        public Action OnFocusCallback;
        public Action OnLostFocusCallback;

        protected bool m_Docking;
        public bool Docking => m_Docking;

        public virtual void CreateGUI()
        {
            m_Tree = null;
            if (m_Navigation == null)
                m_Navigation = new TreeWindowNavigationController();
            if (m_RuntimeOverlay == null)
                m_RuntimeOverlay = new TreeWindowRuntimeOverlayController();

            VisualElement root = rootVisualElement;
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            visualTree.CloneTree(root);

            m_LeftPanel = root.Q("left-panel");
            m_RightPanel = root.Q("right-panel");
            m_NavigationToolbar = root.Q("tree-navigation-toolbar");
            Button backButton = root.Q<Button>("tree-navigation-back-button");
            VisualElement breadcrumbContainer = root.Q("tree-navigation-breadcrumb");

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
            m_Navigation.Initialize(this, backButton, breadcrumbContainer);
            m_RuntimeOverlay.Initialize(this, m_NavigationToolbar, m_TreeView, m_TreeInspectorView);

            Undo.undoRedoPerformed += OnUndoRedo;
            m_Navigation.TryRestore();
            OnClosedCallback?.Invoke();
            m_Navigation.RefreshToolbar();
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

            m_Navigation.Dispose();
            m_RuntimeOverlay.Dispose();

            Undo.undoRedoPerformed -= OnUndoRedo;
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
            m_Navigation.ReplaceRoot(tree, authoringContext);
        }

        public void ReplaceNavigationRoot(BaseTreeAsset treeAsset, object authoringContext = null)
        {
            m_Navigation.ReplaceRoot(treeAsset, authoringContext);
        }

        public void SetAuthoringContext(object authoringContext)
        {
            m_Navigation.SetAuthoringContext(authoringContext);
        }

        public void PushReferencedTree(BaseNode sourceNode, NodeGraphReference reference)
        {
            m_Navigation.Push(sourceNode, reference);
        }

        public void PushReferencedTree(BaseEdge sourceEdge, BaseTree tree, string label)
        {
            m_Navigation.Push(sourceEdge, tree, label);
        }

        public void PushTreePage(
            BaseTree tree,
            BaseTreeAsset treeAsset,
            string displayName,
            string sourceIdentity,
            string referenceKey,
            AuthoringPageKind pageKind = AuthoringPageKind.Graph)
        {
            m_Navigation.Push(tree, treeAsset, displayName, sourceIdentity, referenceKey, pageKind);
        }

        public void PopNavigationPage()
        {
            m_Navigation.Pop();
        }

        public void PopNavigationTo(int index)
        {
            m_Navigation.PopTo(index);
        }

        public void SelectTree(BaseTree tree)
        {
            m_Navigation.SelectTree(tree);
        }

        internal void ApplyAuthoringContext(object authoringContext)
        {
            m_TreeInspectorView?.SetAuthoringContext(authoringContext);
            if (m_Tree != null)
                m_TreeInspectorView?.PopulateView(m_Tree);
        }

        internal void ApplyNavigationTree(BaseTree tree, BaseTreeAsset treeAsset)
        {
            if (tree == null)
                return;

            if (!ReferenceEquals(tree, m_Tree))
            {
                m_Tree = tree;
                m_TreeAsset = treeAsset;
                m_TreeTitle.text = tree.name;

                if (Application.isPlaying)
                {
                    tree.RebindReadOnlyViewReferences();
                }
                else if (!IsLiveDebug)
                {
                    if (tree.Refresh())
                        SetCurrentTreeDirty();
                    if (tree.CheckInit())
                        SetCurrentTreeDirty();
                }
                m_TreeView.PopulateView(tree);
                m_TreeInspectorView.SetAuthoringContext(AuthoringContext);
                m_TreeInspectorView.SetVisibleBlackboardSources(ResolveVisibleTrees());
                m_TreeInspectorView.PopulateView(tree);
                m_TreeView.SetRuntimeReadOnly(IsLiveDebug);
                m_TreeInspectorView.SetEnabled(!IsLiveDebug);
                m_RuntimeOverlay.OnTreeChanged();
                TreeWindowUtility.SelectTree(tree);
            }

            if (m_TreeTitle != null)
                m_TreeTitle.text = tree.name;
        }

        internal void ReportNavigationRestoreError(string error)
        {
            m_RuntimeOverlay.SetStatus(error);
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
                m_RuntimeOverlay.InvalidateRequests();
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
            m_RuntimeOverlay.InvalidateRequests();
            if (CurrentPageSerializedOwner)
                EditorUtility.SetDirty(CurrentPageSerializedOwner);
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
