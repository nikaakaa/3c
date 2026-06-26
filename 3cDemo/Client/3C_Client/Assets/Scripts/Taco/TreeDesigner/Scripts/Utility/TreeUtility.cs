using UnityEngine;

namespace TreeDesigner
{
    public static class TreeUtility
    {
        public static T Clone<T>(this T tree) where T : BaseTree
        {
            T cloneTree = Object.Instantiate(tree);
            cloneTree.name = tree.name;
            return cloneTree;
        }
    }
}