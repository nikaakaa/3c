using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    public static class Debugger
    {
        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void Log(this BaseNode node, object message)
        {
#if UNITY_EDITOR
            Debug.Log($"{node.Owner.name}.{node.GetAttribute<NodeNameAttribute>().Name}:{message}");
#else
            Log(message);
#endif
        }
    }
}