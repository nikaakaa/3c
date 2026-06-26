using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public static class InputActionNodeDragFactory
    {
        static readonly Vector2 NodeSpacing = new Vector2(260f, 120f);

        public static bool CanCreateFromDrag(BaseTreeView treeView)
        {
            if (treeView?.Tree == null)
                return false;

            foreach (var action in EnumerateDraggedActions())
            {
                if (TryGetNodeType(action, out Type nodeType, out _) && treeView.Tree.CanCreateNodeType(nodeType))
                    return true;
            }

            return false;
        }

        public static void CreateFromDrag(BaseTreeView treeView, DragPerformEvent evt)
        {
            if (treeView?.Tree == null)
                return;

            Vector2 origin = evt.localMousePosition;
            int createdCount = 0;

            foreach (var action in EnumerateDraggedActions())
            {
                if (!TryGetNodeType(action, out Type nodeType, out string reason))
                {
                    Debug.LogWarning($"InputAction '{ActionLabel(action)}' skipped: {reason}", action.actionMap?.asset);
                    continue;
                }

                if (!treeView.Tree.CanCreateNodeType(nodeType))
                {
                    Debug.LogWarning($"{treeView.Tree.GetType().Name} cannot create node type {nodeType.Name}.", treeView.Tree);
                    continue;
                }

                Vector2 position = origin + new Vector2(createdCount % 4 * NodeSpacing.x, createdCount / 4 * NodeSpacing.y);
                BaseNode node = treeView.CreateNode(nodeType, position);
                if (node is InputActionValueNode inputActionNode)
                {
                    inputActionNode.ApplyModify("Bind InputAction", () =>
                    {
                        inputActionNode.BindAction(action);
                    });
                    createdCount++;
                }
            }

            if (createdCount > 0)
                evt.StopPropagation();
        }

        static IEnumerable<InputAction> EnumerateDraggedActions()
        {
            UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
            if (objectReferences == null)
                yield break;

            foreach (UnityEngine.Object reference in objectReferences)
            {
                if (reference is InputActionReference actionReference)
                {
                    InputAction action = actionReference.action;
                    if (action != null)
                        yield return action;
                }
                else if (reference is InputActionAsset actionAsset)
                {
                    foreach (InputActionMap actionMap in actionAsset.actionMaps)
                    {
                        foreach (InputAction action in actionMap.actions)
                            yield return action;
                    }
                }
            }
        }

        static bool TryGetNodeType(InputAction action, out Type nodeType, out string reason)
        {
            nodeType = null;
            reason = string.Empty;

            if (action == null)
            {
                reason = "action is missing.";
                return false;
            }

            string expectedControlType = action.expectedControlType ?? string.Empty;
            if (action.type == InputActionType.Button || IsButtonType(expectedControlType))
            {
                nodeType = typeof(InputActionButtonNode);
                return true;
            }

            if (IsVector2Type(expectedControlType))
            {
                nodeType = typeof(InputActionVector2Node);
                return true;
            }

            if (IsFloatType(expectedControlType))
            {
                nodeType = typeof(InputActionFloatNode);
                return true;
            }

            reason = string.IsNullOrEmpty(expectedControlType) ? "expected control type is empty." : $"expected control type '{expectedControlType}' is not supported.";
            return false;
        }

        static bool IsButtonType(string expectedControlType)
        {
            return string.Equals(expectedControlType, "Button", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expectedControlType, "Key", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsVector2Type(string expectedControlType)
        {
            return string.Equals(expectedControlType, "Vector2", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expectedControlType, "Stick", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expectedControlType, "Dpad", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsFloatType(string expectedControlType)
        {
            return string.Equals(expectedControlType, "Float", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expectedControlType, "Axis", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expectedControlType, "Analog", StringComparison.OrdinalIgnoreCase);
        }

        static string ActionLabel(InputAction action)
        {
            if (action == null)
                return "<missing>";

            string mapName = action.actionMap != null ? action.actionMap.name : string.Empty;
            return string.IsNullOrEmpty(mapName) ? action.name : $"{mapName}/{action.name}";
        }
    }
}
