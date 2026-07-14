using System;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEngine;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.Logic
{
    sealed class BehaviorTreeRuntime : IDisposable
    {
        readonly RunnableTree m_RootTreeAsset;
        readonly CharacterGraphContext m_GraphContext;

        RunnableTree m_RuntimeTree;
        bool m_Spawned;

        public BehaviorTreeRuntime(RunnableTree rootTreeAsset, CharacterGraphContext graphContext)
        {
            m_RootTreeAsset = rootTreeAsset;
            m_GraphContext = graphContext;
        }

        public RunnableTree RuntimeTree => m_RuntimeTree;

        public void Activate()
        {
            if (m_RuntimeTree != null)
                return;

            if (m_RootTreeAsset == null)
            {
                Debug.LogError("BehaviorTreeRuntime requires a RootTree asset.");
                return;
            }

            m_RuntimeTree = m_RootTreeAsset.Clone();
            m_RuntimeTree.InitTree(m_GraphContext);
            m_RuntimeTree.OnSpawn();
            m_Spawned = true;
        }

        public void Tick(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            if (m_RuntimeTree == null)
                return;

            m_RuntimeTree.UpdateTree(context.FixedDeltaSeconds);
        }

        public void Deactivate()
        {
            if (m_RuntimeTree == null)
                return;

            if (m_Spawned)
            {
                m_RuntimeTree.OnUnspawn();
                m_Spawned = false;
            }

            m_RuntimeTree.DisposeTree();
            m_RuntimeTree = null;
        }

        public void Dispose()
        {
            Deactivate();
        }
    }
}
