#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Slate
{
    static class ClipEditorGUI
    {
        public static void DrawBlendGraphics(
            Rect rect,
            float blendInPosX,
            float blendOutPosX,
            float blendIn,
            float blendOut,
            float overlapIn,
            float overlapOut)
        {
            if (blendIn > 0)
            {
                Handles.color = Color.black.WithAlpha(0.5f);
                Handles.DrawAAPolyLine(2, new Vector2(0, rect.height), new Vector2(blendInPosX, 0));
                Handles.color = Color.black.WithAlpha(0.3f);
                Handles.DrawAAConvexPolygon(new Vector3(0, 0), new Vector3(0, rect.height), new Vector3(blendInPosX, 0));
            }

            if (blendOut > 0 && overlapOut == 0)
            {
                Handles.color = Color.black.WithAlpha(0.5f);
                Handles.DrawAAPolyLine(2, new Vector2(blendOutPosX, 0), new Vector2(rect.width, rect.height));
                Handles.color = Color.black.WithAlpha(0.3f);
                Handles.DrawAAConvexPolygon(new Vector3(rect.width, 0), new Vector2(blendOutPosX, 0), new Vector2(rect.width, rect.height));
            }

            if (overlapIn > 0)
            {
                Handles.color = Color.black;
                Handles.DrawAAPolyLine(2, new Vector2(blendInPosX, 0), new Vector2(blendInPosX, rect.height));
            }
            Handles.color = Color.white;
        }
    }
}
#endif
