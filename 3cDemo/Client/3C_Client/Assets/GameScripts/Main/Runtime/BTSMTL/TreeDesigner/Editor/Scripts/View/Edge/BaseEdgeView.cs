using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public class BaseEdgeView : Edge
    {
        protected BaseEdge m_Edge;
        readonly Label m_EdgeSummary;
        Capabilities m_AuthoringCapabilities;
        bool m_RuntimeReadOnly;

        public BaseEdge Edge
        {
            get => m_Edge;
            set
            {
                m_Edge = value;
                UpdateEdgeSummary();
            }
        }

        public BasePortView StartPortView => output as BasePortView;
        public BasePortView EndPortView => input as BasePortView;
        public BaseNodeView StartNodeView => StartPortView.NodeView;
        public BaseNodeView EndNodeView => EndPortView.NodeView;
        public bool IsStateMachineTransitionEdge => IsStateMachineTransition();
        public bool IsBTConditionEdge => IsBTCondition();
        public bool HasConditionRuleEditor => IsStateMachineTransitionEdge || IsBTConditionEdge;
        public string ConditionSummary => ResolveConditionLabel();
        public string EdgeSummary => BuildSummaryText();
        public ConditionRuleGraphOwnership RuleGraphOwnership => GetRuleGraphOwnership();
        public string RuleGraphOwnershipLabel => RuleGraphOwnership switch
        {
            ConditionRuleGraphOwnership.Inline => "Inline",
            ConditionRuleGraphOwnership.Shared => "Shared Asset",
            _ => IsBTConditionEdge ? "Unconditional" : "Unspecified (Invalid)"
        };

        public BaseEdgeView()
        {
            m_EdgeSummary = new Label();
            m_EdgeSummary.pickingMode = PickingMode.Ignore;
            m_EdgeSummary.style.position = Position.Absolute;
            m_EdgeSummary.style.left = 12;
            m_EdgeSummary.style.top = -10;
            m_EdgeSummary.style.paddingLeft = 4;
            m_EdgeSummary.style.paddingRight = 4;
            m_EdgeSummary.style.paddingTop = 1;
            m_EdgeSummary.style.paddingBottom = 1;
            m_EdgeSummary.style.fontSize = 10;
            m_EdgeSummary.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.82f);
            m_EdgeSummary.style.color = new Color(0.86f, 0.9f, 0.95f, 1f);
            m_EdgeSummary.style.display = DisplayStyle.None;
            Add(m_EdgeSummary);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (StartNodeView?.TreeView?.TreeWindow?.IsLiveDebug == true)
                return;
            if (!HasConditionRuleEditor)
                return;

            evt.menu.AppendSeparator();
            if (IsStateMachineTransitionEdge)
            {
                for (int i = 0; i <= 5; i++)
                {
                    int priority = i;
                    evt.menu.AppendAction($"Transition/Priority/{priority}", _ => SetTransitionPriority(priority));
                }
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("Condition Rule/Open", _ => OpenConditionRuleGraph(), _ =>
                CanOpenConditionRuleGraph() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (m_Edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.ResolvedInline)
                evt.menu.AppendAction("Condition Rule/Extract Shared", _ => ExtractSharedConditionRuleGraph());

            if (m_Edge.ConditionRuleGraphReferenceStatus != ConditionRuleGraphReferenceStatus.ResolvedInline)
                evt.menu.AppendAction("Condition Rule/Use Inline Rule", _ => UseInlineConditionRuleGraph());

            if (IsBTConditionEdge)
            {
                evt.menu.AppendSeparator();
                foreach (BTAbortPolicy policy in System.Enum.GetValues(typeof(BTAbortPolicy)))
                {
                    BTAbortPolicy captured = policy;
                    evt.menu.AppendAction($"Abort Policy/{captured}", _ => SetAbortPolicy(captured), _ =>
                        m_Edge.AbortPolicy == captured ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
            }
        }

        bool IsStateMachineTransition()
        {
            return IsStateMachineTransition(m_Edge);
        }

        bool IsBTCondition()
        {
            return IsBTConditionFlowEdge(m_Edge);
        }

        void UpdateEdgeSummary()
        {
            if (m_EdgeSummary == null)
                return;

            string summary = BuildSummaryText();
            if (string.IsNullOrEmpty(summary))
            {
                m_EdgeSummary.style.display = DisplayStyle.None;
                return;
            }

            m_EdgeSummary.text = summary;
            m_EdgeSummary.tooltip = summary;
            m_EdgeSummary.style.display = DisplayStyle.Flex;
        }

        public void SetRuntimeDebugState(string status, bool selected)
        {
            Color color = selected ? new Color(0.25f, 0.9f, 0.55f, 1f) : new Color(0.95f, 0.76f, 0.2f, 1f);
            edgeControl.inputColor = color;
            edgeControl.outputColor = color;
            edgeControl.edgeWidth = selected ? 4 : 3;
            tooltip = status ?? string.Empty;
            MarkDirtyRepaint();
        }

        public void ClearRuntimeDebugState()
        {
            edgeControl.inputColor = Color.gray;
            edgeControl.outputColor = Color.gray;
            edgeControl.edgeWidth = 2;
            tooltip = string.Empty;
            MarkDirtyRepaint();
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            if (m_RuntimeReadOnly == readOnly)
                return;
            m_RuntimeReadOnly = readOnly;
            if (readOnly)
            {
                m_AuthoringCapabilities = capabilities;
                capabilities &= Capabilities.Selectable | Capabilities.Ascendable;
            }
            else
            {
                capabilities = m_AuthoringCapabilities;
            }
        }

        string BuildSummaryText()
        {
            if (m_Edge == null)
                return string.Empty;

            if (IsStateMachineTransitionEdge)
            {
                string condition = ResolveConditionLabel();
                return string.Join(" | ", new[] { $"P{m_Edge.TransitionPriority}", condition }
                    .Where(i => !string.IsNullOrEmpty(i)));
            }

            if (IsBTConditionEdge)
            {
                string condition = ResolveConditionLabel();
                string abort = m_Edge.AbortPolicy == BTAbortPolicy.None ? string.Empty : m_Edge.AbortPolicy.ToString();
                return string.Join(" | ", new[] { abort, condition }.Where(i => !string.IsNullOrEmpty(i)));
            }

            return string.Empty;
        }

        string ResolveConditionLabel()
        {
            if (m_Edge == null)
                return string.Empty;

            if (m_Edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified)
                return IsStateMachineTransitionEdge ? "Invalid Rule: Unspecified" : string.Empty;

            return m_Edge.ConditionRuleGraph
                ? m_Edge.ConditionRuleGraph.name
                : $"Invalid Rule: {m_Edge.ConditionRuleGraphReferenceStatus}";
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount != 2 || !HasConditionRuleEditor)
                return;

            OpenConditionRuleGraph();
            evt.StopPropagation();
        }

        public void SetTransitionPriority(int priority)
        {
            if (!IsStateMachineTransitionEdge)
                return;

            m_Edge.Owner.ApplyModify("Set Transition Priority", () =>
            {
                m_Edge.TransitionPriority = priority;
            });
            UpdateEdgeSummary();
        }

        public void SetAbortPolicy(BTAbortPolicy policy)
        {
            if (!IsBTConditionEdge)
                return;

            m_Edge.Owner.ApplyModify("Set BT Abort Policy", () =>
            {
                m_Edge.AbortPolicy = policy;
            });
            UpdateEdgeSummary();
        }

        public void SetConditionRuleGraph(ConditionRuleGraph graph)
        {
            if (!HasConditionRuleEditor)
                return;

            m_Edge.Owner.ApplyModify("Set Condition Rule Graph", () =>
            {
                m_Edge.SetConditionRuleGraph(graph);
            });
            UpdateEdgeSummary();
        }

        public bool ReplaceConditionRuleGraph(ConditionRuleGraph graph)
        {
            if (!HasConditionRuleEditor || graph == null)
                return false;

            SetConditionRuleGraph(graph);
            return true;
        }

        public bool ReplaceConditionRuleGraphAsset(BaseTreeAsset asset)
        {
            if (!HasConditionRuleEditor)
                return false;

            if (!asset || !(asset.Tree is ConditionRuleGraph))
                return false;

            if (m_Edge.ConditionRuleGraphOwnership == ConditionRuleGraphOwnership.Shared &&
                m_Edge.HasResolvedConditionRuleGraph &&
                m_Edge.SharedConditionRuleGraphAsset == asset)
                return true;

            ConditionRuleGraph oldGraph = m_Edge.ConditionRuleGraph;
            ConditionRuleGraphOwnership oldOwnership = RuleGraphOwnership;
            if (!ConfirmRuleGraphRemoval(oldGraph, oldOwnership))
                return false;

            m_Edge.Owner.ApplyModify("Set Shared Condition Rule Graph", () =>
            {
                m_Edge.SetConditionRuleGraphAsset(asset);
            });
            UpdateEdgeSummary();
            return true;
        }

        public bool UseInlineConditionRuleGraph()
        {
            if (!HasConditionRuleEditor)
                return false;

            ConditionRuleGraph oldGraph = m_Edge.ConditionRuleGraph;
            if (!ConfirmRuleGraphRemoval(oldGraph, RuleGraphOwnership))
                return false;

            ConditionRuleGraph graph = CreateInlineConditionRuleGraph();
            if (graph == null || m_Edge.Owner == null)
                return false;

            m_Edge.Owner.ApplyModify("Use Inline Condition Rule Graph", () =>
            {
                m_Edge.SetConditionRuleGraph(graph);
            });
            UpdateEdgeSummary();
            return true;
        }

        public void OpenConditionRuleGraph()
        {
            if (!EnsureConditionRuleGraph())
            {
                if (m_Edge != null)
                {
                    Debug.LogError($"Cannot open ConditionRuleGraph: owner={m_Edge.Owner?.name}/{m_Edge.Owner?.GraphAuthoringId} edge={m_Edge.GUID} ownership={m_Edge.ConditionRuleGraphOwnership} reason={m_Edge.ConditionRuleGraphReferenceError}", m_Edge.Owner?.SerializedOwner);
                }
                SelectEdgeInInspector();
                return;
            }

            StartNodeView.TreeView.TreeWindow.PushReferencedTree(m_Edge, m_Edge.ConditionRuleGraph, "Condition Rule");
        }

        bool CanOpenConditionRuleGraph()
        {
            if (m_Edge == null || !HasConditionRuleEditor)
                return false;

            if (m_Edge.ConditionRuleGraph)
                return true;

            return IsBTConditionEdge && m_Edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified;
        }

        bool EnsureConditionRuleGraph()
        {
            if (!CanOpenConditionRuleGraph())
                return false;

            if (m_Edge.ConditionRuleGraph)
                return true;

            return UseInlineConditionRuleGraph();
        }

        ConditionRuleGraph CreateInlineConditionRuleGraph()
        {
            if (!(m_Edge.Owner is BaseTree owner))
                return null;

            return ConditionRuleGraph.CreateDefaultGraph(
                UniqueInlineRuleGraphName(owner, RuleGraphBaseName()),
                owner.AuthoringRole);
        }

        public void ExtractSharedConditionRuleGraph()
        {
            if (m_Edge.ConditionRuleGraphReferenceStatus != ConditionRuleGraphReferenceStatus.ResolvedInline)
                return;

            ConditionRuleGraph inlineGraph = m_Edge.InlineConditionRuleGraph;
            if (inlineGraph == null || !(m_Edge.Owner is BaseTree owner))
                return;

            string ownerPath = AssetDatabase.GetAssetPath(owner.SerializedOwner);
            if (string.IsNullOrEmpty(ownerPath))
                return;

            string directory = Path.GetDirectoryName(ownerPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory))
                return;

            string folderName = $"{SanitizeFileName(owner.name)}.SharedRules";
            string sharedFolder = $"{directory}/{folderName}";
            if (!AssetDatabase.IsValidFolder(sharedFolder))
                AssetDatabase.CreateFolder(directory, folderName);

            ConditionRuleGraph sharedGraph = inlineGraph.Clone();
            sharedGraph.name = inlineGraph.name;
            BaseTreeAsset sharedAsset = ScriptableObject.CreateInstance<BaseTreeAsset>();
            sharedAsset.SetTree(sharedGraph);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{sharedFolder}/{SanitizeFileName(sharedGraph.name)}.asset");
            AssetDatabase.CreateAsset(sharedAsset, assetPath);
            EditorUtility.SetDirty(sharedAsset);
            AssetDatabase.SaveAssets();

            m_Edge.Owner.ApplyModify("Extract Shared Condition Rule Graph", () =>
            {
                m_Edge.SetConditionRuleGraphAsset(sharedAsset);
            });
            UpdateEdgeSummary();
        }

        ConditionRuleGraphOwnership GetRuleGraphOwnership()
        {
            return GetRuleGraphOwnership(m_Edge);
        }

        public static bool IsStateMachineTransition(BaseEdge edge)
        {
            return edge != null &&
                   !(edge is PropertyEdge) &&
                   edge.Owner is StateMachineGraph &&
                   IsValidTransitionStart(edge.StartNode) &&
                   IsValidTransitionEnd(edge.EndNode);
        }

        public static bool IsBTConditionFlowEdge(BaseEdge edge)
        {
            return edge != null &&
                   !(edge is PropertyEdge) &&
                   !(edge.Owner is StateMachineGraph) &&
                   edge.StartNode is CompositeNode &&
                   edge.EndNode is RunnableNode &&
                   edge.StartPortName == "Output" &&
                   edge.EndPortName == "Input";
        }

        static bool IsValidTransitionStart(BaseNode node)
        {
            return node is StateMachineEnterNode ||
                   node is StateMachineAnyStateNode ||
                   node is StateNode;
        }

        static bool IsValidTransitionEnd(BaseNode node)
        {
            return node is StateNode ||
                   node is StateMachineExitNode;
        }

        public static ConditionRuleGraphOwnership GetRuleGraphOwnership(BaseEdge edge)
        {
            return edge?.ConditionRuleGraphOwnership ?? ConditionRuleGraphOwnership.Unspecified;
        }

        public static bool ConfirmConditionEdgeDeletion(BaseEdge edge)
        {
            ConditionRuleGraph graph = edge?.ConditionRuleGraph;
            ConditionRuleGraphOwnership ownership = GetRuleGraphOwnership(edge);
            return ConfirmRuleGraphRemoval(graph, ownership);
        }

        public static void DeleteOwnedConditionRuleGraphForEdgeDelete(BaseEdge edge)
        {
            if (GetRuleGraphOwnership(edge) != ConditionRuleGraphOwnership.Inline)
                return;

            edge.ClearConditionRuleGraph();
        }

        static bool ConfirmRuleGraphRemoval(ConditionRuleGraph graph, ConditionRuleGraphOwnership ownership)
        {
            if (ownership != ConditionRuleGraphOwnership.Inline || !HasAuthoredRuleGraphContent(graph))
                return true;

            return EditorUtility.DisplayDialog(
                "Clear Inline Condition Rule",
                $"Condition rule '{graph.name}' contains authored content. Clear it from this edge?",
                "Clear",
                "Cancel");
        }

        static bool HasAuthoredRuleGraphContent(ConditionRuleGraph graph)
        {
            if (graph == null)
                return false;

            int nodeCount = graph.Nodes?.Count(i => i) ?? 0;
            int resultNodeCount = graph.Nodes?.Count(i => i is ConditionRuleResultNode) ?? 0;
            if (nodeCount != 1 || resultNodeCount != 1)
                return true;

            if ((graph.Edges?.Count ?? 0) > 0)
                return true;
            if ((graph.PropertyEdges?.Count ?? 0) > 0)
                return true;
            return (graph.ExposedProperties?.Count ?? 0) > 0;
        }

        void SelectEdgeInInspector()
        {
            BaseTreeView treeView = StartNodeView?.TreeView;
            if (treeView == null)
                return;

            treeView.ClearSelection();
            treeView.AddToSelection(this);
            treeView.TreeWindow.PopulateSelectionInspector(treeView.selection);
        }

        string RuleGraphBaseName()
        {
            return $"{SanitizeFileName(NodeLabel(m_Edge.StartNode))}_To_{SanitizeFileName(NodeLabel(m_Edge.EndNode))}_Rule";
        }

        static string UniqueInlineRuleGraphName(BaseTree owner, string baseName)
        {
            HashSet<string> existingNames = owner.Edges
                .Select(i => i?.ConditionRuleGraph?.name)
                .Where(i => !string.IsNullOrEmpty(i))
                .ToHashSet();

            if (!existingNames.Contains(baseName))
                return baseName;

            for (int i = 1; ; i++)
            {
                string candidate = $"{baseName} {i}";
                if (!existingNames.Contains(candidate))
                    return candidate;
            }
        }

        static string NodeLabel(BaseNode node)
        {
            return node == null ? "Node" : node.ResolvedDisplayName;
        }

        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Condition";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '_');
            return name.Replace(' ', '_');
        }
    }
}
