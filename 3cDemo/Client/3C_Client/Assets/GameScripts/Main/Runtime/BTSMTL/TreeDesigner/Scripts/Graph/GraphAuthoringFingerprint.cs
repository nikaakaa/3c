using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    public static class GraphAuthoringFingerprint
    {
        public static string Compute(BaseGraph graph)
        {
            return graph == null ? string.Empty : SourceContentHasher.Hash(JsonUtility.ToJson(graph));
        }
    }
}
