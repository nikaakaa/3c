using System;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenSubTreeWindow")]
    public sealed class StateBehaviorSubTree : SubTree
    {
        [SerializeField]
        string m_OnEnterGUID;
        public string OnEnterGUID { get => m_OnEnterGUID; set => m_OnEnterGUID = value; }

        [SerializeField]
        string m_OnExitGUID;
        public string OnExitGUID { get => m_OnExitGUID; set => m_OnExitGUID = value; }

        [NonSerialized]
        StateOnEnterNode m_OnEnter;
        public StateOnEnterNode OnEnter => m_OnEnter;

        [NonSerialized]
        StateOnExitNode m_OnExit;
        public StateOnExitNode OnExit => m_OnExit;

        [NonSerialized]
        bool m_StateEnterComplete;

        [NonSerialized]
        bool m_StateExitComplete;

        public override void InitTree(object user)
        {
            base.InitTree(user);
            ResolveLifecycleNodes();
            ResetStateLifecycle();
        }

        public override void DisposeTree()
        {
            base.DisposeTree();
            m_OnEnter = null;
            m_OnExit = null;
            m_StateEnterComplete = false;
            m_StateExitComplete = false;
        }

        public override bool CanCreateNodeType(Type type)
        {
            if (type == null)
                return false;

            if (type == typeof(StateOnEnterNode))
                return !Nodes.OfType<StateOnEnterNode>().Any();

            if (type == typeof(StateOnExitNode))
                return !Nodes.OfType<StateOnExitNode>().Any();

            if (typeof(StateLifecycleNode).IsAssignableFrom(type))
                return false;

            return base.CanCreateNodeType(type);
        }

        public void BeginStateLifecycle()
        {
            if (Running)
                OnStop();

            ResetTree();
            ResetStateLifecycle();
        }

        public State UpdateStateEnter(float deltaTime)
        {
            SetDeltaTime(deltaTime);

            if (m_StateEnterComplete)
                return State.Success;

            State state = UpdateLifecycleNode(m_OnEnter);
            if (state == State.Running)
                return State.Running;

            if (state == State.Failure)
                return State.Failure;

            m_StateEnterComplete = true;
            m_OnEnter?.ResetNode();
            return State.Success;
        }

        public State UpdateStateRoot(float deltaTime)
        {
            if (!m_StateEnterComplete)
                return State.Running;

            return UpdateTree(deltaTime);
        }

        public State UpdateStateExit(float deltaTime)
        {
            SetDeltaTime(deltaTime);

            if (m_StateExitComplete)
                return State.Success;

            State state = UpdateLifecycleNode(m_OnExit);
            if (state == State.Running)
                return State.Running;

            if (state == State.Failure)
                return State.Failure;

            m_StateExitComplete = true;
            m_OnExit?.ResetNode();
            return State.Success;
        }

        State UpdateLifecycleNode(StateLifecycleNode node)
        {
            return node ? node.UpdateNode() : State.Success;
        }

        void ResetStateLifecycle()
        {
            m_StateEnterComplete = false;
            m_StateExitComplete = false;
            m_OnEnter?.ResetNode();
            m_OnExit?.ResetNode();
        }

        void ResolveLifecycleNodes()
        {
            m_OnEnter = !string.IsNullOrEmpty(m_OnEnterGUID) && GUIDNodeMap.TryGetValue(m_OnEnterGUID, out BaseNode enterNode)
                ? enterNode as StateOnEnterNode
                : Nodes.OfType<StateOnEnterNode>().FirstOrDefault();

            m_OnExit = !string.IsNullOrEmpty(m_OnExitGUID) && GUIDNodeMap.TryGetValue(m_OnExitGUID, out BaseNode exitNode)
                ? exitNode as StateOnExitNode
                : Nodes.OfType<StateOnExitNode>().FirstOrDefault();

            if (m_OnEnter)
                m_OnEnterGUID = m_OnEnter.GUID;
            if (m_OnExit)
                m_OnExitGUID = m_OnExit.GUID;
        }

#if UNITY_EDITOR
        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            dirty |= EnsureLifecycleNode<StateOnEnterNode>(ref m_OnEnterGUID, new Vector2(0, -180));
            dirty |= EnsureLifecycleNode<StateOnExitNode>(ref m_OnExitGUID, new Vector2(0, 180));
            ResolveLifecycleNodes();
            return dirty;
        }

        bool EnsureLifecycleNode<T>(ref string guid, Vector2 position) where T : StateLifecycleNode
        {
            T node = !string.IsNullOrEmpty(guid) && GUIDNodeMap.TryGetValue(guid, out BaseNode existingNode)
                ? existingNode as T
                : Nodes.OfType<T>().FirstOrDefault();

            if (node)
            {
                guid = node.GUID;
                return false;
            }

            node = CreateNode(typeof(T)) as T;
            node.Position = position;
            guid = node.GUID;
            return true;
        }

        [UnityEditor.MenuItem("Assets/Create/TreeDesigner/State Behavior SubTree", false, -997)]
        public static void CreateStateBehaviorSubTree()
        {
            StateBehaviorSubTree tree = ScriptableObject.CreateInstance<StateBehaviorSubTree>();
            RootNode rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
            StateOnEnterNode onEnterNode = tree.CreateNode(typeof(StateOnEnterNode)) as StateOnEnterNode;
            StateOnExitNode onExitNode = tree.CreateNode(typeof(StateOnExitNode)) as StateOnExitNode;
            rootNode.Position = new Vector2(0, 0);
            onEnterNode.Position = new Vector2(0, -180);
            onExitNode.Position = new Vector2(0, 180);
            tree.RootGUID = rootNode.GUID;
            tree.OnEnterGUID = onEnterNode.GUID;
            tree.OnExitGUID = onExitNode.GUID;

            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New State Behavior SubTree.asset");
            if (UnityEditor.Selection.activeObject is BaseTree)
                UnityEditor.AssetDatabase.AddObjectToAsset(tree, path);
            else
                UnityEditor.AssetDatabase.CreateAsset(tree, assetPathAndName);

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = tree;
        }
#endif
    }
}
