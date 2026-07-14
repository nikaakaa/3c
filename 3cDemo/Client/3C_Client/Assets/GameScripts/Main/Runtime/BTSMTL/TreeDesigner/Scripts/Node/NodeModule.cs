using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public abstract class NodeModule
    {
        [SerializeField]
        string m_ModuleId;

        [NonSerialized]
        BaseNode m_Owner;

        public string ModuleId => m_ModuleId;
        public BaseNode Owner => m_Owner;
        public virtual string DefaultModuleId => GetType().Name;

        public virtual void Init(BaseNode owner, string defaultModuleId)
        {
            m_Owner = owner;
            if (string.IsNullOrEmpty(m_ModuleId))
                m_ModuleId = defaultModuleId;
        }

        public virtual void RebindReadOnlyViewOwner(BaseNode owner)
        {
            m_Owner = owner;
        }

        public virtual IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield break;
        }

        public virtual IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            yield break;
        }

        public virtual IEnumerable<BaseTree> GetReferencedTrees()
        {
            foreach (var reference in GetGraphReferences())
            {
                if (reference.Tree != null)
                    yield return reference.Tree;
            }
        }

        public virtual void Dispose()
        {
            m_Owner = null;
        }
    }
}
