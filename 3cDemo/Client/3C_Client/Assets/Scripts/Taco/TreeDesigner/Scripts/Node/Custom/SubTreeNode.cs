using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(255, 209, 102)]
    [NodePath("Base/Custom/SubTree")]
    [NodeView("SubTreeNodeView")]
    [Input("Input"), Output("Output", PortCapacity.Single)]
    public partial class SubTreeNode : RunnableNode
    {
        [SerializeField, ShowInPanel, SearchContext("t:SubAbility")]
        SubTree m_SubTree;
        public SubTree SubTree => m_SubTree;

        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        protected string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [SerializeReference]
        List<PropertyPort> m_InputPropertyPorts = new List<PropertyPort>();
        public List<PropertyPort> InputPropertyPorts => m_InputPropertyPorts;

        [SerializeReference]
        List<PropertyPort> m_OutputPropertyPorts = new List<PropertyPort>();
        public List<PropertyPort> OutputPropertyPorts => m_OutputPropertyPorts;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        protected RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        [NonSerialized]
        protected List<TriggerNode> m_SubTreeTriggerNodes = new List<TriggerNode>();
        public List<TriggerNode> SubTreeTriggerNodes => m_SubTreeTriggerNodes;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
#if UNITY_EDITOR

            if (Application.isPlaying && !UnityEditor.EditorUtility.IsPersistent(m_Owner) && m_SubTree && UnityEditor.EditorUtility.IsPersistent(m_SubTree))
            {
                m_SubTree = UnityEngine.Object.Instantiate(m_SubTree);
                m_SubTree.OnSpawn();
            }
#else
            if(Application.isPlaying && m_SubTree)
            {
                m_SubTree = UnityEngine.Object.Instantiate(m_SubTree);
                m_SubTree.OnSpawn();
            }
#endif
            if (m_SubTree)
            {
                if (tree is not BaseTree ownerTree)
                    throw new InvalidOperationException($"{nameof(SubTreeNode)} requires a {nameof(BaseTree)} owner.");

                m_SubTree.Init(ownerTree, ownerTree.User);
                m_SubTree.Nodes.ForEach(i =>
                {
                    if (i is TriggerNode triggerNode)
                        m_SubTreeTriggerNodes.Add(triggerNode);
                });
            }

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }

            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output"))
            {
                m_OutputEdgeGUID = outputEdge.GUID;
                m_Child = outputEdge.EndNode as RunnableNode;
                break;
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            if (m_SubTree)
            {
                m_SubTree.DisposeTree();
                m_SubTreeTriggerNodes.Clear();
            }

            m_Parent = null;
            m_Child = null;
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
            m_InputPropertyPorts.ForEach(i => i.OnAfterDeserialize());
            m_OutputPropertyPorts.ForEach(i => i.OnAfterDeserialize());
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (m_SubTree)
            {
                if (m_Owner is not RunnableTree runnableTree)
                {
                    m_State = State.Failure;
                    return;
                }

                runnableTree.OnStopCallback += m_SubTree.OnStopCallback;
                foreach (var exposedProperty in m_SubTree.ExposedProperties)
                {
                    string portName = $"{exposedProperty.Name}_Input";
                    if (m_InputPropertyPorts.Find(i => i.Name == portName) is PropertyPort inputPropertyPort)
                        exposedProperty.SetValue(inputPropertyPort.GetValue());
                }
            }
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State == State.Running && m_SubTree)
            {
                if(m_SubTree.State == State.Success || m_SubTree.State == State.Failure)
                    m_SubTree.ResetTree();

                State subTreeState = m_SubTree.UpdateTree(Owner.DeltaTime);
                if (subTreeState == State.Success && m_Child)
                    return m_Child.UpdateNode();
                else
                    return subTreeState;
            }
            else
                return State.None;
        }
        protected override void OnReset()
        {
            base.OnReset();
            m_SubTree?.OnStop();
            m_SubTree?.ResetTree();
        }
        protected override void OutputValue()
        {
            base.OutputValue();
            if (m_SubTree)
            {
                foreach (var exposedProperty in m_SubTree.ExposedProperties)
                {
                    if (m_OutputPropertyPorts.Find(i => i.Name == $"{exposedProperty.Name}_Output") is PropertyPort outputPropertyPort)
                        outputPropertyPort.SetValue(exposedProperty.GetValue());
                }
            }
        }

#if UNITY_EDITOR
        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);
            m_InputEdgeGUID = edge.GUID;
            m_Parent = edge.StartNode as RunnableNode;
        }
        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
        public override void OnOutputLinked(BaseEdge edge)
        {
            base.OnOutputLinked(edge);

            m_OutputEdgeGUID = edge.GUID;
            m_Child = edge.EndNode as RunnableNode;
        }
        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);

            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
        }
        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
#endif
    }
}

