#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Slate
{
    static class ClipEditorGUI
    {
        public static void UpdateScaledCurves(
            AnimationCurve[] curves,
            Dictionary<int, Keyframe[]> preScaleKeys,
            float preScaleStartTime,
            float preScaleEndTime,
            float newStartTime,
            float newLength,
            bool retime,
            bool trim)
        {
            if (curves == null || preScaleKeys == null)
                return;
            for (int curveIndex = 0; curveIndex < curves.Length; curveIndex++)
            {
                AnimationCurve curve = curves[curveIndex];
                if (!preScaleKeys.TryGetValue(curveIndex, out Keyframe[] keys))
                    continue;
                for (int keyIndex = 0; keyIndex < curve.keys.Length && keyIndex < keys.Length; keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    if (retime)
                    {
                        float preLength = preScaleEndTime - preScaleStartTime;
                        key.time = Mathf.LerpUnclamped(0f, newLength, key.time / preLength);
                    }
                    if (trim)
                        key.time -= newStartTime - preScaleStartTime;
                    curve.MoveKey(keyIndex, key);
                }
                curve.UpdateTangentsFromMode();
            }
        }

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
