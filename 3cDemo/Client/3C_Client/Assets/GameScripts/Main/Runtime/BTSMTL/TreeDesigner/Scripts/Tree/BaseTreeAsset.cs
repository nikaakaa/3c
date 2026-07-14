using UnityEngine;

namespace TreeDesigner
{
    public sealed class BaseTreeAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeReference]
        BaseTree m_Tree = new BaseTree();

        public BaseTree Tree
        {
            get
            {
                EnsureTree(true);
                return m_Tree;
            }
        }

        public void SetTree(BaseTree tree)
        {
            m_Tree = tree ?? new BaseTree();
            BindTree(true);
        }

        void OnEnable()
        {
            EnsureTree(true);
        }

        void OnValidate()
        {
            EnsureTree(true);
        }

        void EnsureTree(bool syncName)
        {
            if (m_Tree == null)
                m_Tree = new BaseTree();
            BindTree(syncName);
        }

        void BindTree(bool syncName)
        {
            if (syncName)
                m_Tree.name = name;
            m_Tree.BindSerializedOwner(this, "m_Tree");
        }

        public void OnBeforeSerialize()
        {
            if (m_Tree != null)
                BindTree(false);
        }

        public void OnAfterDeserialize()
        {
            if (m_Tree != null)
                BindTree(false);
            m_Tree?.OnAfterDeserializeGraph();
        }
    }
}
