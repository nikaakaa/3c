#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FlowCanvas;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using NodeCanvas.StateMachines;
using NodeCanvas.Editor;
using ParadoxNotion.Design;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillNativeStateMachine : FSM, IBtsmtlSkillAuthoringGraph, IBlackboardEditorAdapter
    {
        [Serializable]
        sealed class AuthoringData
        {
            public string Identity;
            public string OwnerGraphId;
            public string OwnerNodeId;
        }

        [SerializeField, HideInInspector] string m_AuthoringId = Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] string m_OwnerGraphId = string.Empty;
        [SerializeField, HideInInspector] string m_OwnerNodeId = string.Empty;

        public string AuthoringId => m_AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => BtsmtlSkillFlowGraphRole.StateMachine;
        public string OwnerGraphId => m_OwnerGraphId;
        public string OwnerNodeId => m_OwnerNodeId;
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;

        public void ConfigureIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("技能原生状态机必须有稳定身份。", nameof(identity));
            m_AuthoringId = identity;
        }

        public void ConfigureOwner(string graphId, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(graphId) || string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("技能原生状态机必须绑定调用图和调用节点。", nameof(graphId));
            m_OwnerGraphId = graphId;
            m_OwnerNodeId = nodeId;
        }

        public override object OnDerivedDataSerialization() => new AuthoringData
        {
            Identity = m_AuthoringId,
            OwnerGraphId = m_OwnerGraphId,
            OwnerNodeId = m_OwnerNodeId
        };

        public override void OnDerivedDataDeserialization(object data)
        {
            if (data is not AuthoringData authoring)
                throw new InvalidOperationException("技能原生状态机缺少作者身份数据。");
            ConfigureIdentity(authoring.Identity);
            ConfigureOwner(authoring.OwnerGraphId, authoring.OwnerNodeId);
        }

        protected override void OnGraphInitialize() =>
            throw new InvalidOperationException("技能原生状态机只用于作者和编译，禁止启动NodeCanvas运行时。");

#if UNITY_EDITOR
        public override bool allowsEditorExecution => false;
        public override bool usesDomainAuthoring => true;
        public override bool isEditorReadOnly => Application.isPlaying;
        public override IBlackboard editorBlackboard => OwnerGraph?.editorBlackboard;
        public override UnityEngine.Object EditorUndoTarget => BtsmtlSkillFlowEditorMutation.UndoTarget(this);
        public override bool HandleEditorCommand(string command, Vector2 position) =>
            BtsmtlSkillNativeStateMachineCopy.HandleCommand(this, command, position);

        public override Node AddNode(Type nodeType, Vector2 position = default)
        {
            if (!CanAuthorNodeType(nodeType))
                throw new InvalidOperationException("Skill FSM只允许登记的状态适配器。");
            return BtsmtlSkillFlowEditorMutation.Execute(this, "创建技能FSM状态", () =>
            {
                BtsmtlSkillNativeState state = (BtsmtlSkillNativeState)base.AddNode(nodeType, position);
                if (state is not BtsmtlSkillNativeEntryState && state is not BtsmtlSkillNativeAnyState &&
                    state is not BtsmtlSkillNativeExitState)
                    BtsmtlSkillGraphAssetFactory.CreatePrivateStateBody(this, state);
                return state;
            });
        }

        public override Connection ConnectNodes(Node sourceNode, Node targetNode, int sourceIndex = -1, int targetIndex = -1)
        {
            ValidateConnectionEndpoints(sourceNode, targetNode);
            return BtsmtlSkillFlowEditorMutation.Execute(this, "连接技能FSM转移", () =>
                base.ConnectNodes(sourceNode, targetNode, sourceIndex, targetIndex));
        }

        public override void RemoveNode(Node node, bool recordUndo = true, bool force = false)
        {
            if (node is BtsmtlSkillNativeEntryState || node is BtsmtlSkillNativeAnyState ||
                node is BtsmtlSkillNativeExitState)
                throw new InvalidOperationException("Skill FSM系统锚点不能单独删除。");
            BtsmtlSkillFlowEditorMutation.Execute(this, "删除技能FSM状态", () => base.RemoveNode(node, false, force));
        }

        public override void RemoveConnection(Connection connection, bool recordUndo = true) =>
            BtsmtlSkillFlowEditorMutation.Execute(this, "删除技能FSM转移", () => base.RemoveConnection(connection, false));

        public override List<Node> DuplicateNodes(List<Node> originalNodes, Vector2 originPosition = default) =>
            BtsmtlSkillGraphCopy.CopyNative(this, originalNodes, originPosition);

        internal List<Node> DuplicateNodesDirect(List<Node> originalNodes, Vector2 originPosition = default) =>
            base.DuplicateNodes(originalNodes, originPosition);

        public override GenericMenu GetNodeSelectionMenu(NodeCreationRequestContext request)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("BTSMTL/技能状态"), false, () =>
            {
                BtsmtlSkillFlowEditorMutation.Execute(this, "创建技能FSM状态", () =>
                {
                    BtsmtlSkillNativeState state = (BtsmtlSkillNativeState)AddNode(
                        typeof(BtsmtlSkillNativeState), request.position);
                    if (request.connectSource != null)
                        ConnectNodes(request.connectSource, state, request.connectSourcePortIndex);
                });
            });
            return menu;
        }

        public bool CanAuthorNodeType(Type nodeType) =>
            nodeType != null && typeof(BtsmtlSkillNativeState).IsAssignableFrom(nodeType);

        static void ValidateConnectionEndpoints(Node sourceNode, Node targetNode)
            => BtsmtlSkillNativeStateMachineAuthoring.RequireConnectionEndpoints(sourceNode, targetNode);

        public bool IsReadOnly => isEditorReadOnly;
        public bool AllowVariablePick => !IsReadOnly;
        public void DrawBlackboardExtensions(IBlackboard blackboard, UnityEngine.Object contextObject) =>
            BtsmtlSkillBlackboardEditorAdapter.Draw(OwnerGraph, blackboard);
        public GenericMenu GetAddVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject) =>
            BtsmtlSkillBlackboardEditorAdapter.GetAddVariableMenu(OwnerGraph, blackboard);
        public GenericMenu GetVariableMenu(
            IBlackboard blackboard,
            UnityEngine.Object contextObject,
            Variable variable,
            int index) =>
            BtsmtlSkillBlackboardEditorAdapter.GetVariableMenu(OwnerGraph, blackboard, variable);
        public void ExecuteMutation(string title, Action mutation) =>
            BtsmtlSkillBlackboardEditorAdapter.ExecuteMutation(OwnerGraph, title, mutation);
        public void ApplyVariableList(IBlackboard blackboard, IReadOnlyList<Variable> variables) =>
            BtsmtlSkillBlackboardEditorAdapter.ApplyVariableList(OwnerGraph, blackboard, variables);

        BtsmtlSkillFlowGraph OwnerGraph
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(this);
                return string.IsNullOrEmpty(path)
                    ? null
                    : AssetDatabase.LoadMainAssetAtPath(path) as BtsmtlSkillFlowGraph;
            }
        }
#endif
    }

    [Name("技能状态")]
    [Color("ff6d53")]
    public class BtsmtlSkillNativeState : FSMState, IGraphAssignable
    {
        [SerializeField] BtsmtlSkillFlowGraph m_Body;

        public string AuthoringId => UID;
        public BtsmtlSkillFlowGraph Body => m_Body;
        public override Type outConnectionType => typeof(BtsmtlSkillNativeConnection);

        protected override bool CanConnectFromSource(Node sourceNode) => true;
        protected override bool CanConnectToTarget(Node targetNode) => true;

        public void SetBody(BtsmtlSkillFlowGraph graph)
        {
            if (graph != null && graph.Role != BtsmtlSkillFlowGraphRole.StateBody)
                throw new ArgumentException("原生技能状态只能引用StateBody图。", nameof(graph));
            m_Body = graph;
        }

        Graph IGraphAssignable.subGraph { get => m_Body; set => SetBody((BtsmtlSkillFlowGraph)value); }
        Graph IGraphAssignable.currentInstance { get => null; set => throw new InvalidOperationException("技能StateBody不创建作者运行实例。"); }
        BBParameter IGraphAssignable.subGraphParameter => null;
        List<BBMappingParameter> IGraphAssignable.variablesMap
        {
            get => null;
            set
            {
                if (value != null)
                    throw new InvalidOperationException("技能StateBody使用正式owner和编译合同。");
            }
        }
        Dictionary<Graph, Graph> IGraphAssignable.instances
        {
            get => new();
            set => throw new InvalidOperationException("技能StateBody不创建作者运行实例。");
        }

#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI()
        {
            if (this is BtsmtlSkillNativeEntryState || this is BtsmtlSkillNativeAnyState ||
                this is BtsmtlSkillNativeExitState)
                return;
            var value = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField(
                "StateBody", Body, typeof(BtsmtlSkillFlowGraph), false);
            if (value == Body)
                return;
            try
            {
                BtsmtlSkillFlowEditorMutation.Apply(
                    (BtsmtlSkillNativeStateMachine)graph,
                    "修改StateBody引用",
                    () => SetBody(value));
            }
            catch (InvalidOperationException error)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }
#endif
    }

    [Name("技能入口")]
    [DoNotList]
    public sealed class BtsmtlSkillNativeEntryState : BtsmtlSkillNativeState
    {
        public override int maxInConnections => 0;
    }

    [Name("任意状态")]
    [DoNotList]
    public sealed class BtsmtlSkillNativeAnyState : BtsmtlSkillNativeState
    {
        public override int maxInConnections => 0;
    }

    [Name("技能出口")]
    [DoNotList]
    public sealed class BtsmtlSkillNativeExitState : BtsmtlSkillNativeState
    {
        public override int maxOutConnections => 0;
    }

    [Serializable]
    public sealed class BtsmtlSkillNativeConnection : FSMConnection
    {
        [SerializeField] BtsmtlSkillTransferPayload m_Payload = new();

        public BtsmtlSkillTransferPayload Payload => m_Payload;
        public BtsmtlSkillFlowGraph Condition => Payload.Condition;
        public int Priority => Payload.Priority;
        public ProgramAbortPolicy AbortPolicy => Payload.AbortPolicy;
        public int Order => Payload.Order;

        public void Configure(
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order)
        {
            Payload.Configure(condition, priority, abortPolicy, order);
        }

        public override void OnCreate(int sourceIndex, int targetIndex)
        {
            base.OnCreate(sourceIndex, targetIndex);
            IEnumerable<int> orders = sourceNode == null
                ? Array.Empty<int>()
                : sourceNode.outConnections
                    .OfType<BtsmtlSkillNativeConnection>()
                    .Where(value => value != this)
                    .Select(value => value.Order);
            int order = orders.DefaultIfEmpty(-1).Max() + 1;
            Configure(null, 0, ProgramAbortPolicy.None, order);
        }

#if UNITY_EDITOR
        protected override string GetConnectionInfo()
        {
            string condition = Condition ? Condition.name : "无条件";
            string priority = Priority == 0 ? string.Empty : $" · 优先级 {Priority}";
            return $"{condition} · 顺序 {Order}{priority}";
        }

        protected override void OnConnectionInspectorGUI()
        {
            BtsmtlSkillNativeStateMachine machine = graph as BtsmtlSkillNativeStateMachine;
            if (machine == null)
                return;
            EditorGUILayout.HelpBox("BTSMTL转移参数由Skill领域适配器管理，NodeCanvas ConditionTask、Stacked和Clean不开放。", MessageType.None);
            if (Condition == null && GUILayout.Button("新建私有条件图"))
            {
                try
                {
                    BtsmtlSkillFlowEditorMutation.Apply(
                        machine,
                        "创建技能转移条件",
                        () => BtsmtlSkillGraphAssetFactory.CreatePrivateCondition(machine, this));
                }
                catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
                {
                    GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
                }
                return;
            }
            var condition = (BtsmtlSkillFlowGraph)EditorGUILayout.ObjectField(
                "条件图", Condition, typeof(BtsmtlSkillFlowGraph), false);
            int priority = EditorGUILayout.DelayedIntField("优先级", Priority);
            ProgramAbortPolicy abortPolicy = (ProgramAbortPolicy)EditorGUILayout.EnumPopup("中断策略", AbortPolicy);
            int order = EditorGUILayout.DelayedIntField("并列顺序", Order);
            if (condition == Condition && priority == Priority && abortPolicy == AbortPolicy && order == Order)
                return;
            try
            {
                BtsmtlSkillFlowEditorMutation.Apply(
                    machine,
                    "修改技能FSM转移",
                    () => Configure(condition, priority, abortPolicy, order));
            }
            catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
            {
                GraphEditor.current?.ShowNotification(new GUIContent(error.Message));
            }
        }

        public override bool TryOpenEditorChild()
        {
            if (Condition == null)
                return false;
            graph.SetCurrentEditorChild(this, Condition);
            return true;
        }
#endif
    }

    public static class BtsmtlSkillNativeStateMachineAuthoring
    {
        public static BtsmtlSkillNativeState ResolveState(
            BtsmtlSkillNativeStateMachine machine,
            string identity)
        {
            if (machine == null || string.IsNullOrWhiteSpace(identity))
                return null;
            return machine.allNodes
                .OfType<BtsmtlSkillNativeState>()
                .SingleOrDefault(value => string.Equals(value.UID, identity, StringComparison.Ordinal));
        }

        public static BtsmtlSkillNativeState ResolveAnchor(
            BtsmtlSkillNativeStateMachine machine,
            string kind)
        {
            if (machine == null)
                return null;
            return kind switch
            {
                "@enter" => machine.allNodes.OfType<BtsmtlSkillNativeEntryState>().SingleOrDefault(),
                "@any" => machine.allNodes.OfType<BtsmtlSkillNativeAnyState>().SingleOrDefault(),
                "@exit" => machine.allNodes.OfType<BtsmtlSkillNativeExitState>().SingleOrDefault(),
                _ => null
            };
        }

        public static Type AnchorType(string kind) => kind switch
        {
            "@enter" => typeof(BtsmtlSkillNativeEntryState),
            "@any" => typeof(BtsmtlSkillNativeAnyState),
            "@exit" => typeof(BtsmtlSkillNativeExitState),
            _ => null
        };

        public static BtsmtlSkillNativeConnection ResolveConnection(
            BtsmtlSkillNativeStateMachine machine,
            string identity)
        {
            if (machine == null || string.IsNullOrWhiteSpace(identity))
                return null;
            return machine.allNodes
                .OfType<BtsmtlSkillNativeState>()
                .SelectMany(value => value.outConnections.OfType<BtsmtlSkillNativeConnection>())
                .SingleOrDefault(value => string.Equals(value.UID, identity, StringComparison.Ordinal));
        }

        public static BtsmtlSkillNativeState EnsureState(
            BtsmtlSkillNativeStateMachine machine,
            Type stateType,
            string identity,
            string name,
            Vector2 position)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            if (stateType == null || !typeof(BtsmtlSkillNativeState).IsAssignableFrom(stateType) || stateType.IsAbstract)
                throw new ArgumentException("技能FSM状态类型无效。", nameof(stateType));
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("技能FSM状态identity不能为空。", nameof(identity));
            return BtsmtlSkillFlowEditorMutation.Execute(machine, "配置技能FSM状态", () =>
            {
                BtsmtlSkillNativeState state = ResolveState(machine, identity);
                if (state == null && IsSystemAnchor(stateType))
                    state = ResolveAnchor(machine, AnchorKind(stateType));
                if (state == null)
                    state = CreateState(machine, stateType, name, position);
                else if (state.GetType() != stateType)
                    throw new InvalidOperationException($"技能FSM状态identity '{identity}'的类型不一致。");
                state.ConfigureAuthoringIdentity(identity);
                state.name = name;
                state.position = position;
                if (state is BtsmtlSkillNativeEntryState)
                    machine.primeNode = state;
                return state;
            });
        }

        public static BtsmtlSkillNativeState CreateState(
            BtsmtlSkillNativeStateMachine machine,
            Type stateType,
            string name,
            Vector2 position)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            if (stateType == null || !typeof(BtsmtlSkillNativeState).IsAssignableFrom(stateType) || stateType.IsAbstract)
                throw new ArgumentException("技能FSM状态类型无效。", nameof(stateType));
            return BtsmtlSkillFlowEditorMutation.Execute(machine, "创建技能FSM状态", () =>
            {
                BtsmtlSkillNativeState state = (BtsmtlSkillNativeState)machine.AddNode(stateType, position);
                state.name = name;
                state.position = position;
                if (state is BtsmtlSkillNativeEntryState)
                    machine.primeNode = state;
                return state;
            });
        }

        public static void ConfigureState(
            BtsmtlSkillNativeState state,
            string name,
            Vector2 position,
            BtsmtlSkillFlowGraph body)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.graph is not BtsmtlSkillNativeStateMachine machine)
                throw new InvalidOperationException("技能FSM状态没有正式状态机owner。");
            BtsmtlSkillFlowEditorMutation.Execute(machine, "配置技能FSM状态内容", () =>
            {
                if (state is BtsmtlSkillNativeEntryState || state is BtsmtlSkillNativeAnyState ||
                    state is BtsmtlSkillNativeExitState)
                {
                    if (body != null)
                        throw new InvalidOperationException("技能FSM系统锚点不能拥有StateBody。");
                }
                state.name = name;
                state.position = position;
                BtsmtlSkillFlowGraph previous = state.Body;
                state.SetBody(body);
                if (previous && previous != body && AssetDatabase.IsSubAsset(previous) &&
                    string.Equals(AssetDatabase.GetAssetPath(previous), AssetDatabase.GetAssetPath(machine), StringComparison.Ordinal))
                    Undo.DestroyObjectImmediate(previous);
            });
        }

        public static BtsmtlSkillNativeConnection EnsureConnection(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target,
            string identity)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            if (source == null || target == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("技能FSM转移identity不能为空。", nameof(identity));
            ValidateEndpoints(source, target);
            return BtsmtlSkillFlowEditorMutation.Execute(machine, "配置技能FSM转移", () =>
            {
                BtsmtlSkillNativeConnection connection = ResolveConnection(machine, identity);
                if (connection != null)
                {
                    if (connection.sourceNode != source)
                        connection.SetSourceNode(source);
                    if (connection.targetNode != target)
                        connection.SetTargetNode(target);
                    return connection;
                }
                connection = CreateConnection(machine, source, target);
                if (connection == null)
                    throw new InvalidOperationException($"技能FSM转移identity '{identity}'创建失败。");
                connection.ConfigureAuthoringIdentity(identity);
                return connection;
            });
        }

        public static void RequireConnectionEndpoints(Node sourceNode, Node targetNode)
        {
            if (sourceNode is not BtsmtlSkillNativeState source ||
                targetNode is not BtsmtlSkillNativeState target)
                throw new InvalidOperationException("技能FSM转移端点必须是正式State。");
            ValidateEndpoints(source, target);
        }

        public static BtsmtlSkillNativeConnection CreateConnection(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            if (source == null || target == null)
                throw new ArgumentNullException(nameof(source));
            ValidateEndpoints(source, target);
            return BtsmtlSkillFlowEditorMutation.Execute(machine, "创建技能FSM转移", () =>
            {
                if (!Node.IsNewConnectionAllowed(source, target))
                    throw new InvalidOperationException(
                        $"技能FSM转移端点不允许连接。source={source.UID}; target={target.UID}; " +
                        $"sourceOut={source.outConnections.Count}/{source.maxOutConnections}; " +
                        $"targetIn={target.inConnections.Count}/{target.maxInConnections}");
                BtsmtlSkillNativeConnection connection = machine.ConnectNodes(source, target) as BtsmtlSkillNativeConnection;
                return connection ?? throw new InvalidOperationException("技能FSM转移创建失败。");
            });
        }

        public static void ConfigureConnection(
            BtsmtlSkillNativeConnection connection,
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (connection.graph is not BtsmtlSkillNativeStateMachine machine)
                throw new InvalidOperationException("技能FSM转移没有正式状态机owner。");
            BtsmtlSkillFlowEditorMutation.Execute(machine, "配置技能FSM转移参数", () =>
                connection.Configure(condition, priority, abortPolicy, order));
        }

        public static void PruneConnections(
            BtsmtlSkillNativeStateMachine machine,
            IEnumerable<string> identities)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            var keep = new HashSet<string>(identities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            BtsmtlSkillFlowEditorMutation.Execute(machine, "清理技能FSM转移", () =>
            {
                foreach (BtsmtlSkillNativeConnection connection in machine.allNodes
                             .OfType<BtsmtlSkillNativeState>()
                             .SelectMany(value => value.outConnections.OfType<BtsmtlSkillNativeConnection>())
                             .ToArray())
                    if (!keep.Contains(connection.UID))
                        machine.RemoveConnection(connection, false);
            });
        }

        public static void RequireOwner(
            BtsmtlSkillNativeStateMachine machine,
            string ownerGraphId,
            string ownerNodeId)
        {
            if (machine == null || !string.Equals(machine.OwnerGraphId, ownerGraphId, StringComparison.Ordinal) ||
                !string.Equals(machine.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                throw new InvalidOperationException("技能FSM的调用owner与正式声明不一致。");
        }

        static bool IsSystemAnchor(Type type) =>
            type == typeof(BtsmtlSkillNativeEntryState) ||
            type == typeof(BtsmtlSkillNativeAnyState) ||
            type == typeof(BtsmtlSkillNativeExitState);

        static string AnchorKind(Type type) => type == typeof(BtsmtlSkillNativeEntryState)
            ? "@enter"
            : type == typeof(BtsmtlSkillNativeAnyState)
                ? "@any"
                : "@exit";

        static void ValidateEndpoints(
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target)
        {
            if (source.graph != target.graph ||
                target is BtsmtlSkillNativeEntryState ||
                target is BtsmtlSkillNativeAnyState ||
                source is BtsmtlSkillNativeExitState)
                throw new InvalidOperationException("技能FSM转移必须连接同一状态机中的State到State或Exit。");
        }
    }

    public static class BtsmtlSkillNativeStateMachineContract
    {
        public static void Validate(BtsmtlSkillNativeStateMachine machine, bool requireComplete)
        {
            if (machine == null)
            {
                if (requireComplete)
                    throw new InvalidOperationException("技能缺少原生状态机资产。");
                return;
            }
            if (string.IsNullOrWhiteSpace(machine.AuthoringId))
                throw new InvalidOperationException("原生状态机缺少稳定身份。");
            if (string.IsNullOrWhiteSpace(machine.OwnerGraphId) || string.IsNullOrWhiteSpace(machine.OwnerNodeId))
                throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 缺少唯一调用owner。");
            var states = machine.allNodes.OfType<BtsmtlSkillNativeState>().ToList();
            if (machine.allNodes.Any(value => value is not BtsmtlSkillNativeState))
                throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 包含未登记的NodeCanvas状态节点。");
            if (states.Select(value => value.UID).Distinct(StringComparer.Ordinal).Count() != states.Count)
                throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 的State identity重复。");
            RequireSingleAnchor<BtsmtlSkillNativeEntryState>(states, "Entry", requireComplete);
            RequireSingleAnchor<BtsmtlSkillNativeAnyState>(states, "Any", requireComplete);
            RequireSingleAnchor<BtsmtlSkillNativeExitState>(states, "Exit", requireComplete);
            if (requireComplete && machine.primeNode is not BtsmtlSkillNativeEntryState)
                throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 的Prime必须是Entry。");

            var orders = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillNativeState state in states)
                foreach (Connection value in state.outConnections)
                {
                    if (value is not BtsmtlSkillNativeConnection connection)
                        throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 含有未登记的Connection。");
                    if (connection.sourceNode != state || connection.targetNode is not BtsmtlSkillNativeState target ||
                        target is BtsmtlSkillNativeEntryState || target is BtsmtlSkillNativeAnyState)
                        throw new InvalidOperationException($"原生状态机 {machine.AuthoringId} 的Connection端点不属于Skill FSM。");
                    if (!orders.Add(state.UID + "\0" + connection.Order))
                        throw new InvalidOperationException($"原生状态 {state.UID} 的转移order重复。");
                    if (connection.condition != null ||
                        connection.transitionCallMode != FSM.TransitionCallMode.Normal)
                        throw new InvalidOperationException($"原生转移 {connection.UID} 使用了未登记的NodeCanvas任务或栈调用。");
                    if (connection.Condition != null && connection.Condition.Role != BtsmtlSkillFlowGraphRole.ConditionRule)
                        throw new InvalidOperationException($"原生转移 {connection.UID} 的条件图不是ConditionRule。");
                    if (state is BtsmtlSkillNativeAnyState && connection.Condition == null)
                        throw new InvalidOperationException($"原生Any转移 {connection.UID} 必须有条件图。");
                }

            foreach (BtsmtlSkillNativeState state in states)
                if (state is not BtsmtlSkillNativeEntryState &&
                    state is not BtsmtlSkillNativeAnyState &&
                    state is not BtsmtlSkillNativeExitState &&
                    requireComplete && state.Body == null)
                    throw new InvalidOperationException($"原生状态 {state.UID} 的StateBody引用不完整。");
        }

        public static IEnumerable<BtsmtlSkillFlowGraph> References(BtsmtlSkillNativeStateMachine machine)
        {
            if (machine == null)
                yield break;
            foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
            {
                if (state.Body != null)
                    yield return state.Body;
                foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    if (connection.Condition != null)
                        yield return connection.Condition;
            }
        }

        public static string Fingerprint(BtsmtlSkillNativeStateMachine machine)
        {
            var text = new StringBuilder(machine?.AuthoringId ?? string.Empty);
            text.Append("|owner:").Append(machine?.OwnerGraphId ?? string.Empty).Append(':')
                .Append(machine?.OwnerNodeId ?? string.Empty);
            foreach (BtsmtlSkillNativeState state in machine?.allNodes.OfType<BtsmtlSkillNativeState>()
                         .OrderBy(value => value.UID, StringComparer.Ordinal) ?? Enumerable.Empty<BtsmtlSkillNativeState>())
            {
                text.Append("|state:").Append(state.UID).Append(':').Append(state.GetType().FullName).Append(':')
                    .Append(state.name).Append(':')
                    .Append(state.position.x.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                    .Append(state.position.y.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                    .Append(state.Body?.AuthoringId ?? string.Empty);
                foreach (BtsmtlSkillNativeConnection edge in state.outConnections
                             .OfType<BtsmtlSkillNativeConnection>()
                             .OrderBy(value => value.UID, StringComparer.Ordinal))
                    text.Append("|edge:").Append(edge.UID).Append(':')
                        .Append(edge.sourceNode.UID).Append(':').Append(edge.targetNode.UID).Append(':')
                        .Append(edge.Condition?.AuthoringId ?? string.Empty).Append(':')
                        .Append(edge.Priority).Append(':').Append(edge.Order).Append(':').Append(edge.AbortPolicy);
            }
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        public static void Populate(BtsmtlSkillNativeStateMachine machine)
        {
            if (machine == null || machine.allNodes.Count != 0)
                throw new InvalidOperationException("原生技能状态机只能初始化一次。");
            BtsmtlSkillNativeEntryState entry = machine.AddNode<BtsmtlSkillNativeEntryState>(new Vector2(80, 220));
            machine.AddNode<BtsmtlSkillNativeAnyState>(new Vector2(80, 460));
            machine.AddNode<BtsmtlSkillNativeExitState>(new Vector2(840, 320));
            machine.primeNode = entry;
        }

        static void RequireSingleAnchor<T>(IReadOnlyList<BtsmtlSkillNativeState> states, string name, bool required)
            where T : BtsmtlSkillNativeState
        {
            int count = states.OfType<T>().Count();
            if ((required && count != 1) || (!required && count > 1))
                throw new InvalidOperationException($"原生状态机的{name}锚点数量错误：{count}。");
        }
    }
}
#endif
