#if UNITY_EDITOR
using Slate;
using UnityEditor;

namespace BTSMTL.Timeline.Editor
{
    [CustomEditor(typeof(BtsmtlSlateActionClip), true)]
    sealed class BtsmtlSlateActionClipInspector : ActionClipInspector<BtsmtlSlateActionClip>
    {
        protected override bool RequiresTargetActor => false;
    }
}
#endif
