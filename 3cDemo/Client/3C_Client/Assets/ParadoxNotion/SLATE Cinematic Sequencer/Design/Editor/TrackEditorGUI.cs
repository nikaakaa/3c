#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Slate
{
    public static class TrackEditorGUI
    {
        const float PARAMS_TOP_MARGIN = 5f;
        const float PARAMS_LINE_HEIGHT = 18f;
        const float PARAMS_LINE_MARGIN = 2f;
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

            if (keyable == null)
            {
                GUI.Label(expansionRect, "No Clip Selected", Styles.centerLabel);
                inspectedParameterIndex = -1;
                return;
            }

            if (keyable.animationData == null || !keyable.animationData.isValid)
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
        public static void DrawClipCurves(Event e, Rect posRect, Rect timeRect, System.Func<float, float> TimeToPos, IKeyable keyable, System.Func<bool> isSelectedKeyable, ref int inspectedParameterIndex) {

            //track expanded bg
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.Box(posRect, string.Empty, Styles.timeBoxStyle);
            GUI.color = Color.white;

            if ( !isSelectedKeyable() ) {
                GUI.color = Color.white.WithAlpha(0.3f);
                GUI.Label(posRect, "Select a Clip of this Track to view it's Animated Parameters here", Styles.centerLabel);
                GUI.color = Color.white;
                return;
            }

            var finalPosRect = posRect;
            var finalTimeRect = timeRect;

            //adjust rects
            if ( keyable is ActionClip ) {
                finalPosRect.xMin = Mathf.Max(posRect.xMin, TimeToPos(keyable.startTime));
                finalPosRect.xMax = Mathf.Min(posRect.xMax, TimeToPos(keyable.endTime));
                finalTimeRect.xMin = Mathf.Max(timeRect.xMin, keyable.startTime) - keyable.startTime;
                finalTimeRect.xMax = Mathf.Min(timeRect.xMax, keyable.endTime) - keyable.startTime;
            }

            //add some top/bottom margins
            finalPosRect.yMin += 1;
            finalPosRect.yMax -= 3;
            finalPosRect.width = Mathf.Max(finalPosRect.width, 5);

            //dark bg
            GUI.color = Color.black.WithAlpha(0.4f);
            GUI.DrawTexture(posRect, Styles.whiteTexture);
            GUI.color = Color.white;


            //out of view range
            if ( keyable is ActionClip ) {
                if ( keyable.startTime > timeRect.xMax || keyable.endTime < timeRect.xMin ) {
                    return;
                }
            }


            //keyable bg
            GUI.color = UnityEditor.EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.9f);
            GUI.Box(finalPosRect, string.Empty, Styles.clipBoxFooterStyle);
            GUI.color = Color.white;

            //if too small do nothing more
            if ( finalPosRect.width <= 5 ) {
                return;
            }

            if ( keyable is ActionClip && !( keyable as ActionClip ).isValid ) {
                GUI.Label(finalPosRect, "Clip Is Invalid", Styles.centerLabel);
                return;
            }

            if ( keyable.animationData == null || !keyable.animationData.isValid ) {
                if ( keyable is ActionClip ) {
                    GUI.Label(finalPosRect, "Clip has no Animatable Parameters", Styles.centerLabel);
                } else {
                    GUI.Label(finalPosRect, "Track has no Animated Properties. You can add some on the left side", Styles.centerLabel);
                }
                return;
            }

            if ( inspectedParameterIndex >= keyable.animationData.animatedParameters.Count ) {
                inspectedParameterIndex = -1;
            }


            //vertical guides from params to dopesheet
            if ( inspectedParameterIndex == -1 ) {
                var yPos = PARAMS_TOP_MARGIN;
                for ( var i = 0; i < keyable.animationData.animatedParameters.Count; i++ ) {
                    // var animParam = keyable.animationData.animatedParameters[i];
                    var paramRect = new Rect(0, posRect.yMin + yPos, finalPosRect.xMin - 2, PARAMS_LINE_HEIGHT);
                    yPos += PARAMS_LINE_HEIGHT + PARAMS_LINE_MARGIN;
                    paramRect.yMin += 1f;
                    paramRect.yMax -= 1f;
                    GUI.color = new Color(0, 0.5f, 0.5f, 0.1f);
                    GUI.DrawTexture(paramRect, Styles.whiteTexture);
                    GUI.color = Color.white;
                }
            }


            //begin in group and neutralize rect
            GUI.BeginGroup(finalPosRect);
            finalPosRect = new Rect(0, 0, finalPosRect.width, finalPosRect.height);

            if ( inspectedParameterIndex == -1 ) {
                var yPos = PARAMS_TOP_MARGIN;
                for ( var i = 0; i < keyable.animationData.animatedParameters.Count; i++ ) {
                    var animParam = keyable.animationData.animatedParameters[i];
                    var paramRect = new Rect(finalPosRect.xMin, finalPosRect.yMin + yPos, finalPosRect.width, PARAMS_LINE_HEIGHT);
                    yPos += PARAMS_LINE_HEIGHT + PARAMS_LINE_MARGIN;
                    paramRect.yMin += 1f;
                    paramRect.yMax -= 1f;
                    GUI.color = Color.black.WithAlpha(0.05f);
                    GUI.DrawTexture(paramRect, Texture2D.whiteTexture);
                    UnityEditor.Handles.color = Color.black.WithAlpha(0.2f);
                    UnityEditor.Handles.DrawLine(new Vector2(paramRect.xMin, paramRect.yMin), new Vector2(paramRect.xMax, paramRect.yMin));
                    UnityEditor.Handles.DrawLine(new Vector2(paramRect.xMin, paramRect.yMax), new Vector2(paramRect.xMax, paramRect.yMax));
                    UnityEditor.Handles.color = Color.white;
                    GUI.color = Color.white;

                    if ( animParam.enabled ) {
                        DopeSheetEditor.DrawDopeSheet(animParam, keyable, paramRect, finalTimeRect.x, finalTimeRect.width, true);
                    } else {
                        GUI.color = new Color(0, 0, 0, 0.2f);
                        GUI.DrawTextureWithTexCoords(paramRect, Styles.stripes, new Rect(0, 0, paramRect.width / 7, paramRect.height / 7));
                        GUI.color = Color.white;
                    }
                }
            }

            if ( inspectedParameterIndex >= 0 ) {
                var animParam = keyable.animationData.animatedParameters[inspectedParameterIndex];
                var dopeRect = finalPosRect;
                dopeRect.y += 4f;
                dopeRect.height = 16f;
                DopeSheetEditor.DrawDopeSheet(animParam, keyable, dopeRect, finalTimeRect.x, finalTimeRect.width, true);
                var curveRect = finalPosRect;
                curveRect.yMin = dopeRect.yMax + 4;
                UnityEditor.Handles.color = Color.black.WithAlpha(0.5f);
                UnityEditor.Handles.DrawLine(new Vector2(curveRect.xMin, curveRect.yMin), new Vector2(curveRect.xMax, curveRect.yMin));
                UnityEditor.Handles.color = Color.white;
                CurveEditor.DrawCurves(animParam, keyable, curveRect, finalTimeRect);
            }

            //consume event
            if ( e.type == EventType.MouseDown && finalPosRect.Contains(e.mousePosition) ) {
                e.Use();
            }

            GUI.EndGroup();

            /*
                        //darken out of clip range time
                        //will use if I make curve editing full-width
                        if (Prefs.fullWidthCurveEditing){
                            var darkBefore = Rect.MinMaxRect( posRect.xMin, posRect.yMin, Mathf.Max(posRect.xMin, TimeToPos(keyable.startTime)), posRect.yMax );
                            var darkAfter = Rect.MinMaxRect( Mathf.Min(posRect.xMax, TimeToPos(keyable.endTime)), posRect.yMin, posRect.xMax, posRect.yMax );
                            GUI.color = new Color(0.1f,0.1f,0.1f,0.6f);
                            GUI.DrawTexture(darkBefore, Styles.whiteTexture);
                            GUI.DrawTexture(darkAfter, Styles.whiteTexture);
                            GUI.color = Color.white;
                        }
            */

        }

        public static void DrawParametersInfoGUI(
            Event e,
            Rect trackRect,
            IEmbeddedTimelineTrackBinding track,
            bool selected,
            ref int inspectedParameterIndex)
        {
            DrawDefaultInfoGUI(
                trackRect,
                track.DisplayName,
                string.Empty,
                null,
                selected,
                track.IsActive,
                track.IsLocked,
                track.ShowCurves,
                value => CutsceneEditorSurface.current?.ApplyEmbeddedCommand(() => track.IsActive = value, "Track Active"),
                value => track.IsLocked = value,
                value => track.ShowCurves = value);

            if (!track.ShowCurves)
                return;

            var expansionRect = Rect.MinMaxRect(5, track.DefaultHeight, trackRect.width - 3, track.FinalHeight - 3);
            GUI.color = UnityEditor.EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.DrawTexture(expansionRect, Styles.whiteTexture);
            GUI.color = Color.white;

            var clip = track.SelectedClip;
            if (clip == null || clip.Parameters == null || clip.Parameters.Count == 0)
            {
                GUI.Label(expansionRect, "No Clip Selected", Styles.centerLabel);
                inspectedParameterIndex = -1;
                return;
            }

            if (inspectedParameterIndex >= clip.Parameters.Count)
                inspectedParameterIndex = -1;
            float nextY = track.DefaultHeight + 5f;
            for (int index = 0; index < clip.Parameters.Count; index++)
            {
                var parameter = clip.Parameters[index];
                var parameterRect = new Rect(expansionRect.xMin + 4, nextY, expansionRect.width - 8, 18f);
                nextY += 20f;
                GUI.color = inspectedParameterIndex == index ? new Color(0.5f, 0.5f, 1f, 0.4f) : new Color(0, 0.5f, 0.5f, 0.5f);
                GUI.Box(parameterRect, string.Empty, Styles.headerBoxStyle);
                GUI.color = Color.white;
                GUI.Label(parameterRect, string.Format(" <size=10><color=#252525>{0}</color></size>", parameter.DisplayName), Styles.leftLabel);
                if (e.type == EventType.MouseDown && e.button == 0 && parameterRect.Contains(e.mousePosition))
                {
                    inspectedParameterIndex = inspectedParameterIndex == index ? -1 : index;
                    e.Use();
                }
            }
        }

        public static void DrawClipCurves(
            Event e,
            Rect posRect,
            Rect timeRect,
            System.Func<float, float> TimeToPos,
            IEmbeddedTimelineTrackBinding track,
            ref int inspectedParameterIndex)
        {
            if (!track.ShowCurves)
                return;
            var curvesRect = Rect.MinMaxRect(posRect.xMin, posRect.yMin + track.DefaultHeight, posRect.xMax, posRect.yMax);
            DrawClipCurves(e, curvesRect, timeRect, TimeToPos, track.SelectedClip, ref inspectedParameterIndex);
        }

        static void DrawClipCurves(
            Event e,
            Rect posRect,
            Rect timeRect,
            System.Func<float, float> TimeToPos,
            IEmbeddedTimelineClipBinding clip,
            ref int inspectedParameterIndex)
        {
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.Box(posRect, string.Empty, Styles.timeBoxStyle);
            GUI.color = Color.white;
            if (clip == null || clip.Parameters == null || clip.Parameters.Count == 0)
            {
                GUI.color = Color.white.WithAlpha(0.3f);
                GUI.Label(posRect, "Select a Clip of this Track to view its Curves here", Styles.centerLabel);
                GUI.color = Color.white;
                return;
            }

            var finalPosRect = posRect;
            finalPosRect.xMin = Mathf.Max(posRect.xMin, TimeToPos(clip.StartTime));
            finalPosRect.xMax = Mathf.Min(posRect.xMax, TimeToPos(clip.EndTime));
            finalPosRect.yMin += 1f;
            finalPosRect.yMax -= 3f;
            finalPosRect.width = Mathf.Max(finalPosRect.width, 5f);
            var finalTimeRect = Rect.MinMaxRect(
                Mathf.Max(timeRect.xMin, clip.StartTime) - clip.StartTime,
                timeRect.yMin,
                Mathf.Min(timeRect.xMax, clip.EndTime) - clip.StartTime,
                timeRect.yMax);

            GUI.color = Color.black.WithAlpha(0.4f);
            GUI.DrawTexture(posRect, Styles.whiteTexture);
            GUI.color = Color.white;
            if (finalPosRect.width <= 5f)
                return;

            if (inspectedParameterIndex >= clip.Parameters.Count)
                inspectedParameterIndex = -1;
            if (inspectedParameterIndex < 0)
            {
                float y = 5f;
                for (int index = 0; index < clip.Parameters.Count; index++)
                {
                    var parameter = clip.Parameters[index];
                    var parameterRect = new Rect(finalPosRect.xMin, finalPosRect.yMin + y, finalPosRect.width, 18f);
                    y += 20f;
                    GUI.color = Color.black.WithAlpha(0.05f);
                    GUI.DrawTexture(parameterRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(parameterRect, string.Format(" <size=10>{0}</size>", parameter.DisplayName), Styles.leftLabel);
                    DrawKeys(parameter, parameterRect, finalTimeRect);
                    if (e.type == EventType.MouseDown && e.button == 0 && parameterRect.Contains(e.mousePosition))
                    {
                        inspectedParameterIndex = index;
                        e.Use();
                    }
                }
                return;
            }

            var selectedParameter = clip.Parameters[inspectedParameterIndex];
            if (selectedParameter.Curves == null || selectedParameter.Curves.Count == 0)
                return;
            var curveRect = finalPosRect;
            curveRect.yMin += 4f;
            CutsceneEditorSurface currentEditor = CutsceneEditorSurface.current;
            CurveEditor.DrawCurves(
                new[] { selectedParameter.Curves[0].Curve },
                CurveEditor.CreateEmbeddedOwner(
                    currentEditor,
                    string.Concat(clip.AuthoringId, ":", selectedParameter.ParameterId)),
                curveRect,
                finalTimeRect,
                () => currentEditor?.ApplyEmbeddedCommand(() => { }, "Edit Timeline Curve"));
        }

        static void DrawKeys(IEmbeddedTimelineParameterBinding parameter, Rect rect, Rect timeRect)
        {
            if (parameter.Curves == null || parameter.Curves.Count == 0)
                return;
            var curve = parameter.Curves[0].Curve;
            if (curve == null)
                return;
            for (int index = 0; index < curve.length; index++)
            {
                float normalized = timeRect.width <= 0f ? 0f : Mathf.InverseLerp(timeRect.xMin, timeRect.xMax, curve[index].time);
                float x = Mathf.Lerp(rect.xMin, rect.xMax, normalized);
                var keyRect = new Rect(x - 2f, rect.center.y - 2f, 4f, 4f);
                GUI.DrawTexture(keyRect, Styles.whiteTexture);
            }
        }

        public static void DrawTimelineGUI(
            Event e,
            Rect posRect,
            Rect timeRect,
            float cursorTime,
            System.Func<float, float> timeToPosition,
            bool showCurves,
            IKeyable keyable,
            ref int inspectedParameterIndex,
            Action<Event, Rect, float> drawContextMenu,
            Func<UnityEngine.Object, bool> canAcceptDrop,
            Func<UnityEngine.Object, float, bool> acceptDrop)
        {
            var clipsPosRect = Rect.MinMaxRect(posRect.xMin, posRect.yMin, posRect.xMax, posRect.yMin + 32f);
            drawContextMenu?.Invoke(e, clipsPosRect, cursorTime);

            if (showCurves)
            {
                var curvesPosRect = Rect.MinMaxRect(posRect.xMin, clipsPosRect.yMax, posRect.xMax, posRect.yMax);
                DrawClipCurves(e, curvesPosRect, timeRect, timeToPosition, keyable, () => keyable != null, ref inspectedParameterIndex);
            }

            if (e.type == EventType.DragUpdated && posRect.Contains(e.mousePosition) &&
                UnityEditor.DragAndDrop.objectReferences.Length == 1 && canAcceptDrop != null &&
                canAcceptDrop(UnityEditor.DragAndDrop.objectReferences[0]))
                UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Link;

            if (e.type == EventType.DragPerform && posRect.Contains(e.mousePosition) &&
                UnityEditor.DragAndDrop.objectReferences.Length == 1 && acceptDrop != null &&
                acceptDrop(UnityEditor.DragAndDrop.objectReferences[0], cursorTime))
                UnityEditor.DragAndDrop.AcceptDrag();
        }
    }
}
#endif
