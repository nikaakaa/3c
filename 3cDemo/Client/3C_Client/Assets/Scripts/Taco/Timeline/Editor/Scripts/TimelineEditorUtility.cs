using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taco.Timeline.Editor
{
    public static class TimelineEditorUtility
    {
        public static Dictionary<Type, MonoScript> TrackScriptMap = new Dictionary<Type, MonoScript>();
        public static Dictionary<Type, MonoScript> ClipScriptMap = new Dictionary<Type, MonoScript>();
        public static Dictionary<Type, MonoScript> ClipInspectorViewScriptMap = new Dictionary<Type, MonoScript>();
        static TimelineEditorUtility()
        {
            BuildScriptCache();
        }

        static void BuildScriptCache()
        {
            foreach (var trackType in TypeCache.GetTypesDerivedFrom<Track>())
            {
                var trackScriptAsset = FindScriptFromClassName(trackType);
                if (trackScriptAsset != null)
                    TrackScriptMap[trackType] = trackScriptAsset;
            }
            foreach (var clipType in TypeCache.GetTypesDerivedFrom<Clip>())
            {
                var clipScriptAsset = FindScriptFromClassName(clipType);
                if (clipScriptAsset != null)
                    ClipScriptMap[clipType] = clipScriptAsset;
            }
            foreach (var clipInspectorViewType in TypeCache.GetTypesDerivedFrom<TimelineClipInspectorView>())
            {
                var clipInspectorViewScriptAsset = FindScriptFromClassName(clipInspectorViewType);
                if (clipInspectorViewScriptAsset != null)
                    ClipInspectorViewScriptMap[clipInspectorViewType] = clipInspectorViewScriptAsset;
            }
        }
        static MonoScript FindScriptFromClassName(Type type)
        {
            var scriptGUIDs = ScriptGuidAttribute.Guids(type);
            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null)
                    return script;
            }

            scriptGUIDs = AssetDatabase.FindAssets($"t:script {type.Name}");
            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && string.Equals(type.Name, Path.GetFileNameWithoutExtension(assetPath), StringComparison.OrdinalIgnoreCase))
                    return script;
            }


            return null;
        }
        public class MonoScriptInfo
        {
            public MonoScript Mono;
            public int LineNumber;
            public int ColumnNumber;
            public MonoScriptInfo(MonoScript mono, int ln = 0, int cn = 0)
            {
                Mono = mono;
                LineNumber = ln;
                ColumnNumber = cn;
            }
            public MonoScriptInfo() { }
        }

        public static void SelectTrackScript<T>(this T target) where T : Track
        {
            if (TrackScriptMap.TryGetValue(target.GetType(), out MonoScript monoScript))
                Selection.activeObject = monoScript;
        }
        public static void OpenTrackScript<T>(this T target) where T : Track
        {
            if (TrackScriptMap.TryGetValue(target.GetType(), out MonoScript monoScript))
                AssetDatabase.OpenAsset(monoScript.GetInstanceID());
        }
        public static void SelectClipScript<T>(this T target) where T : Clip
        {
            if (ClipScriptMap.TryGetValue(target.GetType(), out MonoScript monoScript))
                Selection.activeObject = monoScript;
        }
        public static void OpenClipScript<T>(this T target) where T : Clip
        {
            if (ClipScriptMap.TryGetValue(target.GetType(), out MonoScript monoScript))
                AssetDatabase.OpenAsset(monoScript.GetInstanceID());
        }

        public static bool ShowIf(this MemberInfo memberInfo, object target)
        {
            if (memberInfo.GetCustomAttribute<ShowIfAttribute>() is ShowIfAttribute showIfAttribute && !showIfAttribute.Show(target))
                return false;
            else
                return true;
        }
        public static bool HideIf(this MemberInfo memberInfo, object target)
        {
            if (memberInfo.GetCustomAttribute<HideIfAttribute>() is HideIfAttribute hideIfAttribute && hideIfAttribute.Hide(target))
                return true;
            else
                return false;
        }
        public static bool ReadOnly(this MemberInfo memberInfo, object target)
        {
            if (memberInfo.GetCustomAttribute<ReadOnlyAttribute>() is ReadOnlyAttribute readOnlyAttribute && readOnlyAttribute.ReadOnly(target))
                return true;
            else
                return false;
        }
        public static void Group(this MemberInfo memberInfo, VisualElement content, float index,ref List<VisualElement> visualElements, ref Dictionary<string, (VisualElement, List<VisualElement>)> groupMap)
        {
            string groupName = string.Empty;
            if (memberInfo.GetCustomAttribute<HorizontalGroupAttribute>() is HorizontalGroupAttribute horizontalGroupAttribute)
                groupName = horizontalGroupAttribute.GroupName;

            SplitLine splitLine = memberInfo.GetCustomAttribute<SplitLine>();

            if (!string.IsNullOrEmpty(groupName))
            {
                if (!groupMap.ContainsKey(groupName))
                {
                    VisualElement group = new VisualElement();
                    group.style.flexDirection = FlexDirection.Row;
                    group.name = index * 10 + visualElements.Count.ToString();
                    visualElements.Add(group);
                    groupMap.Add(groupName, (group, new List<VisualElement>()));

                    if (splitLine != null)
                    {
                        group.style.paddingTop = splitLine.Space;
                        group.style.borderTopColor = new Color(88, 88, 88, 255) / 255;
                        group.style.borderTopWidth = 1;
                    }
                }
                groupMap[groupName].Item2.Add(content);
            }
            else
            {
                visualElements.Add(content);
                if (splitLine != null)
                {
                    content.style.paddingTop = splitLine.Space;
                    content.style.borderTopColor = new Color(88, 88, 88, 255) / 255;
                    content.style.borderTopWidth = 1;
                }
            }
        }
    }
}