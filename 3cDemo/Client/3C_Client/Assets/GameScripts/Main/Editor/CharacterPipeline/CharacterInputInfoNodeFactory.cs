using System;
using ThirdPersonCharacter.Pipeline.Input;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterInputInfoNodeFactory
    {
        public static bool CanCreate(BaseTree tree, CharacterInputValueDefinition inputValue)
        {
            return tree != null &&
                   inputValue != null &&
                   !string.IsNullOrEmpty(inputValue.InputValueId) &&
                   TryGetInputValueNodeType(inputValue.ValueType, out Type nodeType) &&
                   tree.CanCreateNodeType(nodeType);
        }

        public static bool CanCreate(BaseTree tree, CharacterActionRequestDefinition request)
        {
            Type nodeType = typeof(CharacterActionRequestInfoNode);
            return tree != null &&
                   request != null &&
                   !string.IsNullOrEmpty(request.RequestId) &&
                   tree.CanCreateNodeType(nodeType);
        }

        public static BaseNode Create(BaseTreeView treeView, CharacterInputValueDefinition inputValue, Vector2 position)
        {
            if (treeView?.Tree == null || inputValue == null)
                return null;

            if (!TryGetInputValueNodeType(inputValue.ValueType, out Type nodeType))
                return null;

            if (!treeView.Tree.CanCreateNodeType(nodeType))
                return null;

            BaseNode node = treeView.CreateNode(nodeType, position);

            if (node is CharacterInputValueInfoNode inputValueNode)
            {
                inputValueNode.ApplyModify("Bind Character Input Value", () =>
                {
                    inputValueNode.BindInputValue(inputValue.InputValueId);
                });
                SelectNode(treeView, node);
            }

            return node;
        }

        public static BaseNode Create(BaseTreeView treeView, CharacterActionRequestDefinition request, Vector2 position)
        {
            if (treeView?.Tree == null || request == null)
                return null;

            Type nodeType = typeof(CharacterActionRequestInfoNode);
            if (!treeView.Tree.CanCreateNodeType(nodeType))
                return null;

            BaseNode node = treeView.CreateNode(nodeType, position);

            if (node is CharacterActionRequestInfoNode requestNode)
            {
                requestNode.ApplyModify("Bind Character Action Request", () =>
                {
                    requestNode.BindActionRequest(request.RequestId);
                });
                SelectNode(treeView, node);
            }

            return node;
        }

        static bool TryGetInputValueNodeType(CharacterInputValueType valueType, out Type nodeType)
        {
            switch (valueType)
            {
                case CharacterInputValueType.Bool:
                    nodeType = typeof(CharacterInputBoolInfoNode);
                    return true;
                case CharacterInputValueType.Float:
                    nodeType = typeof(CharacterInputFloatInfoNode);
                    return true;
                case CharacterInputValueType.Vector2:
                    nodeType = typeof(CharacterInputVector2InfoNode);
                    return true;
                default:
                    nodeType = null;
                    return false;
            }
        }

        static void SelectNode(BaseTreeView treeView, BaseNode node)
        {
            BaseNodeView nodeView = treeView.FindNodeView(node);
            if (nodeView == null)
                return;

            treeView.ClearSelection();
            treeView.AddToSelection(nodeView);
        }
    }
}
