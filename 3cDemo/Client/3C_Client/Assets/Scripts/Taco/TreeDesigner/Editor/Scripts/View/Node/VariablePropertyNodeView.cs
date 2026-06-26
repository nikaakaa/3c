using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using Taco;

namespace TreeDesigner.Editor
{
    public class VariablePropertyNodeView : BaseNodeView
    {
        public VariablePropertyNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow, AssetDatabase.GUIDToAssetPath(DefaultVisualTreeGUID))
        {
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshVirablePropertyPorts();
        }
        public override void SyncSerializedPropertyPathes()
        {
            base.SyncSerializedPropertyPathes();

        }

        public override void OnInputPropertyPortConnected(PropertyPortView inputPropertyPortView)
        {
            base.OnInputPropertyPortConnected(inputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnInputPropertyPortDisconnected(PropertyPortView inputPropertyPortView)
        {
            base.OnInputPropertyPortDisconnected(inputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnOutputPropertyPortConnected(PropertyPortView outputPropertyPortView)
        {
            base.OnOutputPropertyPortConnected(outputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }
        public override void OnOutputPropertyPortDisconnected(PropertyPortView outputPropertyPortView)
        {
            base.OnOutputPropertyPortDisconnected(outputPropertyPortView);
            m_Node.GetNewSerializedTree();
            Refresh();
        }

        protected virtual void RefreshVirablePropertyPorts()
        {
            foreach (var item in InputPropertyPorts)
            {
                if (item.Value is VariablePropertyPortView variablePropertyPortView)
                {
                    variablePropertyPortView.SetPropertyPort(m_Node.PropertyPortMap[item.Key]);
                }
            }
            foreach (var item in OutputPropertyPorts)
            {
                if (item.Value is VariablePropertyPortView variablePropertyPortView)
                {
                    variablePropertyPortView.SetPropertyPort(m_Node.PropertyPortMap[item.Key]);
                }
            }
        }
    }
}
