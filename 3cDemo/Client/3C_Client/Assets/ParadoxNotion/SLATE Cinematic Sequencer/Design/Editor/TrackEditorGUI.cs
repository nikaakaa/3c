#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Slate
{
    static class TrackEditorGUI
    {
        public static void DrawDefaultInfoGUI(
            Rect trackRect,
            string name,
            string info,
            Texture icon,
            bool selected,
            bool active,
            bool locked,
            bool showCurves,
            Action<bool> setActive,
            Action<bool> setLocked,
            Action<bool> setShowCurves)
        {
            const float boxWidth = 30f;
            var iconBGRect = new Rect(0, 0, boxWidth, 32f).ExpandBy(-1);
            var textInfoRect = Rect.MinMaxRect(iconBGRect.xMax + 2, 0, trackRect.width - boxWidth - 2, 32f);
            var curveButtonRect = new Rect(trackRect.width - boxWidth, 0, boxWidth, 32f);

            GUI.color = Color.black.WithAlpha(UnityEditor.EditorGUIUtility.isProSkin ? 0.1f : 0.1f);
            GUI.DrawTexture(iconBGRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (icon != null)
            {
                var iconRect = new Rect(0, 0, 16, 16);
                iconRect.center = iconBGRect.center;
                GUI.color = selected ? Color.white : new Color(1, 1, 1, 0.8f);
                GUI.DrawTexture(iconRect, icon);
                GUI.color = Color.white;
            }

            GUI.color = active ? Color.white : Color.grey;
            GUI.Label(textInfoRect, string.Format("<size=11>{0}</size>\n<size=9><color=#909090>{1}</color></size>", name, info));
            GUI.color = Color.white;

            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var curveIconRect = new Rect(0, 0, 16, 16);
            curveIconRect.center = curveButtonRect.center - new Vector2(0, 1);
            var curveIconColor = UnityEditor.EditorGUIUtility.isProSkin ? Color.white : Color.black;
            curveIconColor.a = showCurves ? 1 : 0.3f;

            if (GUI.Button(curveButtonRect, string.Empty, GUIStyle.none))
                setShowCurves(!showCurves);

            curveButtonRect = curveButtonRect.ExpandBy(-4);
            GUI.color = ColorUtility.Grey(UnityEditor.EditorGUIUtility.isProSkin ? 0.2f : 1f).WithAlpha(0.2f);
            GUI.Box(curveButtonRect, string.Empty, Styles.clipBoxStyle);

            GUI.color = curveIconColor;
            GUI.DrawTexture(curveIconRect, Styles.curveIcon);
            GUI.color = Color.grey;

            if (!active)
            {
                var hiddenRect = new Rect(0, 0, 16, 16);
                hiddenRect.center = curveButtonRect.center - new Vector2(curveButtonRect.width, 0);
                if (GUI.Button(hiddenRect, Styles.hiddenIcon, GUIStyle.none))
                    setActive(!active);
            }

            if (locked)
            {
                var lockRect = new Rect(0, 0, 16, 16);
                lockRect.center = curveButtonRect.center - new Vector2(curveButtonRect.width, 0);
                if (!active)
                    lockRect.center -= new Vector2(16, 0);
                if (GUI.Button(lockRect, Styles.lockIcon, GUIStyle.none))
                    setLocked(!locked);
            }

            GUI.color = Color.white;
            GUI.enabled = wasEnabled;
        }
    }
}
#endif
