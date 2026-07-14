using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public class TreeRunner : MonoBehaviour
    {
        [SerializeField]
        protected BaseTreeAsset m_TreeAsset;
        [System.NonSerialized]
        protected RunnableTree m_Tree;
        [SerializeField]
        protected UnityEngine.Object m_RuntimeUser;
        [SerializeField]
        protected bool m_Loop;
        [SerializeField, Min(1)]
        protected float m_LoopInterval;


        bool m_Running;
        float m_CDTime;

        void Update()
        {
            RunnableTree tree = RuntimeTree;
            if (tree == null || !m_Running)
                return;


            if (!tree.Running && m_Loop)
            {
                if(m_CDTime >= m_LoopInterval)
                {
                    m_CDTime = 0;
                    tree.ResetTree();
                    tree.UpdateTree(Time.deltaTime);
                }
                else
                {
                    m_CDTime += Time.deltaTime;
                }
            }
            else
            {
                tree.UpdateTree(Time.deltaTime);
            }
        }

        RunnableTree RuntimeTree => m_Tree ?? (m_TreeAsset ? m_TreeAsset.Tree as RunnableTree : null);

        [ContextMenu("CloneTree")]
        void CloneTree()
        {
            RunnableTree source = m_TreeAsset ? m_TreeAsset.Tree as RunnableTree : m_Tree;
            m_Tree = source?.Clone();
            m_Tree?.OnSpawn();
        }
        [ContextMenu("InitTree")]
        void InitTree()
        {
            RuntimeTree?.InitTree(m_RuntimeUser);
        }
        [ContextMenu("DisposeTree")]
        void Dispose()
        {
            RuntimeTree?.DisposeTree();
        }
        [ContextMenu("UpdateTree")]
        void UpdateTree()
        {
            RuntimeTree?.UpdateTree(0);
            m_Running = true;
        }
        [ContextMenu("ResetTree")]
        void ResetTree()
        {
            RunnableTree tree = RuntimeTree;
            tree?.ResetTree();
            if (tree != null)
                tree.Running = false;
            m_Running = false;
        }
        [ContextMenu("PauseTree")]
        void PauseTree()
        {
            m_Running = false;
        }
        [ContextMenu("ResumeTree")]
        void ResumeTree()
        {
            m_Running = true;
        }
    }
}
