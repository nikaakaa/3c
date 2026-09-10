#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{
    public interface IBlackboardEditorAdapter
    {
        bool IsReadOnly { get; }
        void DrawBlackboardExtensions(IBlackboard blackboard, UnityEngine.Object contextObject);
        GenericMenu GetAddVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject);
        GenericMenu GetVariableMenu(IBlackboard blackboard, UnityEngine.Object contextObject, Variable variable, int index);
        void ExecuteMutation(string title, Action mutation);
        void ApplyVariableList(IBlackboard blackboard, IReadOnlyList<Variable> variables);
    }
}
#endif
