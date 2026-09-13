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

        public static void DrawParametersInfoGUI(
            Event e,
            Rect trackRect,
            IKeyable keyable,
            bool showAddPropertyButton,
            float defaultHeight,
            Func<float> finalHeight,
            Func<float> getCustomHeight,
            Action<float> setCustomHeight,
            Func<bool> isSelectedKeyable,
            ref bool isResizingHeight,
            ref float proposedHeight,
            ref int inspectedParameterIndex)
        {
            var expansionRect = Rect.MinMaxRect(5, defaultHeight, trackRect.width - 3, finalHeight() - 3);
            GUI.color = UnityEditor.EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.DrawTexture(expansionRect, Styles.whiteTexture);
            GUI.color = new Color(0, 0, 0, 0.05f);
            GUI.Box(expansionRect, string.Empty, Styles.shadowBorderStyle);
            GUI.color = Color.white;

            if (inspectedParameterIndex >= 0)
            {
                var resizeRect = Rect.MinMaxRect(0, finalHeight() - 4, trackRect.width, finalHeight());
                UnityEditor.EditorGUIUtility.AddCursorRect(resizeRect, UnityEditor.MouseCursor.ResizeVertical);
                GUI.color = Color.grey;
                GUI.DrawTexture(resizeRect, Styles.whiteTexture);
                GUI.color = Color.white;
                if (e.type == EventType.MouseDown && e.button == 0 && resizeRect.Contains(e.mousePosition))
                {
                    isResizingHeight = true;
                    e.Use();
                }
                if (e.type == EventType.MouseDrag && isResizingHeight)
                    setCustomHeight(getCustomHeight() + e.delta.y);
                if (e.rawType == EventType.MouseUp)
                    isResizingHeight = false;
            }

            proposedHeight = 0f;

            if (!isSelectedKeyable())
            {
                GUI.Label(expansionRect, "No Clip Selected", Styles.centerLabel);
                inspectedParameterIndex = -1;
                return;
            }

            if (!showAddPropertyButton && keyable is ActionClip action && !action.isValid)
            {
                GUI.Label(expansionRect, "Clip Is Invalid", Styles.centerLabel);
                return;
            }

            if (keyable == null || keyable.animationData == null || !keyable.animationData.isValid)
            {
                if (keyable is ActionClip)
                {
                    GUI.Label(expansionRect, "Clip Has No Animatable Parameters", Styles.centerLabel);
                    return;
                }
            }

            proposedHeight = defaultHeight + 5f;
            if (keyable.animationData != null && keyable.animationData.animatedParameters != null)
            {
                if (inspectedParameterIndex >= keyable.animationData.animatedParameters.Count)
                    inspectedParameterIndex = -1;

                var paramsCount = keyable.animationData.animatedParameters.Count;
                for (var i = 0; i < paramsCount; i++)
                {
                    var animParam = keyable.animationData.animatedParameters[i];
                    var paramRect = new Rect(expansionRect.xMin + 4, proposedHeight, expansionRect.width - 8, 18f);
                    proposedHeight += 18f + 2f;
                    GUI.color = inspectedParameterIndex == i ? new Color(0.5f, 0.5f, 1f, 0.4f) : new Color(0, 0.5f, 0.5f, 0.5f);
                    GUI.Box(paramRect, string.Empty, Styles.headerBoxStyle);
                    GUI.color = Color.white;

                    var paramName = string.Format(" <size=10><color=#252525>{0}</color></size>", animParam);
                    paramName = inspectedParameterIndex == i ? string.Format("<b>{0}</b>", paramName) : paramName;
                    GUI.Label(paramRect, paramName, Styles.leftLabel);

                    var gearRect = new Rect(paramRect.xMax - 16 - 4, paramRect.y, 16, 16);
                    gearRect.center = new Vector2(gearRect.center.x, paramRect.y + (paramRect.height / 2) - 1);
                    GUI.enabled = true;
                    GUI.color = Color.white.WithAlpha(animParam.enabled ? 1 : 0.5f);
                    if (GUI.Button(gearRect, Styles.gearIcon, GUIStyle.none))
                        AnimatableParameterEditor.DoParamGearContextMenu(animParam, keyable);
                    GUI.enabled = animParam.enabled;
                    if (GUI.Button(paramRect, string.Empty, GUIStyle.none))
                    {
                        inspectedParameterIndex = inspectedParameterIndex == i ? -1 : i;
                        CurveEditor.FrameAllCurvesOf(animParam);
                    }
                    GUI.color = Color.white;
                    GUI.enabled = true;
                }

                proposedHeight += 5f;
                if (inspectedParameterIndex >= 0)
                {
                    var controlRect = Rect.MinMaxRect(expansionRect.x + 6, proposedHeight + 5, expansionRect.xMax - 6, proposedHeight + 50);
                    var animParam = keyable.animationData.animatedParameters[inspectedParameterIndex];
                    GUILayout.BeginArea(controlRect);
                    AnimatableParameterEditor.ShowMiniParameterKeyControls(animParam, keyable);
                    GUILayout.EndArea();
                    proposedHeight = controlRect.yMax + 10;
                }
            }

            if (showAddPropertyButton && inspectedParameterIndex == -1)
            {
                var buttonRect = Rect.MinMaxRect(expansionRect.x + 6, proposedHeight + 5, expansionRect.xMax - 6, proposedHeight + 25);
                var go = keyable?.animatedParametersTarget as GameObject;
                GUI.enabled = go != null && keyable.root != null && keyable.root.currentTime <= 0;
                if (GUI.Button(buttonRect, "Add Property"))
                    EditorTools.ShowAnimatedPropertySelectionMenu(go, keyable.TryAddParameter);
                GUI.enabled = true;
                proposedHeight = buttonRect.yMax + 10;
            }

            if (e.type == EventType.MouseDown && expansionRect.Contains(e.mousePosition))
                e.Use();
        }
    }
}
#endif
