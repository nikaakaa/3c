using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    [CustomEditor(typeof(TimelineAsset))]
    public sealed class TimelineInspector : UnityEditor.Editor
    {
        VisualElement m_Root;
        string m_ConfigurationError;
        string m_SourceRevision = string.Empty;

        void OnEnable()
        {
            TimelineInspectorSelection.Changed += Rebuild;
        }

        void OnDisable()
        {
            TimelineInspectorSelection.Changed -= Rebuild;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();
            Rebuild();
            return m_Root;
        }

        void Rebuild()
        {
            if (m_Root == null)
                return;
            m_Root.Clear();
            TimelineAsset asset = (TimelineAsset)target;
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
            m_Root.Add(new Button(() => TimelineEditorWindow.Open(asset))
            {
                text = "Open Timeline Editor"
            });

            SerializedProperty data = serializedObject.FindProperty("m_Data");
            PropertyField field = new PropertyField(data, "Timeline Data");
            field.BindProperty(data);
            field.SetEnabled(false);
            m_Root.Add(field);

            if (TimelineInspectorSelection.TryGetElement(asset, out object selected))
            {
                m_Root.Add(new Label("Selected Timeline Element"));
                switch (selected)
                {
                    case Clip clip:
                        m_Root.Add(new IMGUIContainer(() => DrawClipInspector(asset, clip)));
                        break;
                    case Track track:
                        m_Root.Add(new IMGUIContainer(() => DrawTrackInspector(asset, track)));
                        break;
                    case TimelineSection section:
                        m_Root.Add(new IMGUIContainer(() => DrawSectionInspector(asset, section)));
                        break;
                }
            }

            var errors = new List<string>();
            if (!asset.ValidateContent(TimelineTreeContractComposition.Create(), errors))
                m_Root.Add(new HelpBox(string.Join("\n", errors), HelpBoxMessageType.Error));
            if (!string.IsNullOrEmpty(m_ConfigurationError))
                m_Root.Add(new HelpBox(m_ConfigurationError, HelpBoxMessageType.Error));
        }

        void DrawClipInspector(TimelineAsset asset, Clip clip)
        {
            if (!string.IsNullOrEmpty(m_ConfigurationError))
                EditorGUILayout.HelpBox(m_ConfigurationError, MessageType.Error);
            EditorGUILayout.LabelField("Name", clip.Name);
            EditorGUILayout.LabelField("Kind", clip.ContractKind);
            DrawClipMarkerSection(asset, clip);

            int startFrame = clip.StartFrame;
            int endFrame = clip.EndFrame;
            int selfEaseInFrame = clip.SelfEaseInFrame;
            int selfEaseOutFrame = clip.SelfEaseOutFrame;
            int clipInFrame = clip.ClipInFrame;
            bool isTerminalLogicTreeClip = clip is TreeClip treeClip &&
                treeClip.ExecutionDomain == TimelineExecutionDomain.Logic &&
                treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision;
            EditorGUI.BeginChangeCheck();
            startFrame = Mathf.Max(0, EditorGUILayout.IntField("Start Frame", startFrame));
            if (isTerminalLogicTreeClip)
                EditorGUILayout.LabelField("End Frame", $"Timeline End ({endFrame})");
            else
                endFrame = Mathf.Max(startFrame + 1, EditorGUILayout.IntField("End Frame", endFrame));
            selfEaseInFrame = Mathf.Clamp(EditorGUILayout.IntField("Self Ease In", selfEaseInFrame), 0, endFrame - startFrame - 1);
            selfEaseOutFrame = Mathf.Clamp(EditorGUILayout.IntField("Self Ease Out", selfEaseOutFrame), 0, endFrame - startFrame - selfEaseInFrame - 1);
            clipInFrame = Mathf.Max(0, EditorGUILayout.IntField("Clip In", clipInFrame));
            EditorGUILayout.LabelField("Other Ease In", clip.OtherEaseInFrame.ToString());
            EditorGUILayout.LabelField("Other Ease Out", clip.OtherEaseOutFrame.ToString());
            if (EditorGUI.EndChangeCheck())
                ApplyFrames(asset, clip, startFrame, endFrame, selfEaseInFrame, selfEaseOutFrame, clipInFrame);

            TimelineAuthoringClipConfiguration configuration;
            try
            {
                configuration = TimelineAuthoringClipBinding.Read(clip);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }

            PropertyInfo[] properties = typeof(TimelineAuthoringClipConfiguration).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            object[] attributes = clip.GetType().GetCustomAttributes(typeof(TimelineAuthoringPropertyAttribute), true);
            for (int index = 0; index < attributes.Length; index++)
            {
                TimelineAuthoringPropertyAttribute attribute = (TimelineAuthoringPropertyAttribute)attributes[index];
                PropertyInfo property = FindProperty(properties, attribute.PropertyId);
                if (property == null || !property.CanRead || !property.CanWrite)
                    continue;
                object before = property.GetValue(configuration, null);
                object after = DrawProperty(attribute, property, before);
                if (attribute.Trimmed && after is string text)
                    after = text.Trim();
                if (ValuesDiffer(before, after))
                {
                    property.SetValue(configuration, after, null);
                    DrawClipMarkerSection(asset, clip);
                    break;
                }
            }
        }

        void DrawPresentationMarkers(TimelineAsset asset, TreeClip tree)
        {
            var bindings = asset.Data.ExternalBindings
                .Where(value => value != null &&
                    value.Access == TimelineBindingAccess.Input &&
                    value.Lifetime == TimelineBindingLifetime.Call)
                .ToArray();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presentation Markers", EditorStyles.boldLabel);
            TimelinePresentationMarker[] markers = tree.PresentationMarkers.ToArray();
            for (int index = markers.Length - 1; index >= 0; index-- )
            {
                TimelinePresentationMarker marker = markers[index];
                EditorGUILayout.BeginHorizontal();
                int frame = Mathf.Max(0, EditorGUILayout.IntField("Frame", marker.Frame));
                var lifetime = (TimelinePresentationMarkerLifetime)EditorGUILayout.EnumPopup(marker.Lifetime);
                bool stateful = lifetime == TimelinePresentationMarkerLifetime.Stateful;
                int endFrame = stateful ? Mathf.Max(frame + 1, marker.EndFrame) : frame;
                using (new EditorGUI.DisabledScope(!stateful))
                    endFrame = EditorGUILayout.IntField("End", endFrame);
                string[] options = bindings
                    .Select(value => string.IsNullOrEmpty(value.DisplayName) ? value.BindingId : value.DisplayName)
                    .ToArray();
                int selected = Array.FindIndex(bindings, value => string.Equals(value.BindingId, marker.PayloadBinding.BindingId, StringComparison.Ordinal));
                int next = EditorGUILayout.Popup(Mathf.Max(0, selected), options);
                bool remove = GUILayout.Button("-", GUILayout.Width(22));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    MutatePresentationMarkers(asset, () => tree.RemovePresentationMarker(marker));
                    return;
                }
                TimelineExternalBindingUse bindingUse = next != selected && next >= 0
                    ? new TimelineExternalBindingUse(
                        bindings[next].BindingId,
                        bindings[next].ValueKind,
                        TimelineBindingAccess.Input,
                        TimelineBindingLifetime.Call)
                    : null;
                if (frame != marker.Frame ||
                    lifetime != marker.Lifetime ||
                    endFrame != marker.EndFrame ||
                    bindingUse != null)
                {
                    TimelinePresentationMarker target = marker;
                    TimelineExternalBindingUse payload = bindingUse ?? marker.PayloadBinding;
                    MutatePresentationMarkers(asset, () => target.Configure(frame, lifetime, endFrame, payload));
                    return;
                }
            }
            if (bindings.Length == 0)
            {
                EditorGUILayout.HelpBox("先在 Timeline Data 外部绑定表创建 Input/Call 绑定，Marker 才能引用 payload。", MessageType.Info);
                return;
            }
            if (GUILayout.Button("Add Marker"))
            {
                TimelineExternalBindingDeclaration first = bindings[0];
                MutatePresentationMarkers(asset, () => tree.AddPresentationMarker(
                    tree.StartFrame,
                    TimelinePresentationMarkerLifetime.Pulse,
                    tree.StartFrame,
                    new TimelineExternalBindingUse(
                        first.BindingId,
                        first.ValueKind,
                        TimelineBindingAccess.Input,
                        TimelineBindingLifetime.Call)));
            }
        }

        void MutatePresentationMarkers(TimelineAsset asset, Action mutation)
        {
            if (!TryBeginMutation(asset))
                return;
            try
            {
                asset.Data.ApplyModify(mutation, "Edit Timeline Presentation Markers");
                m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
                m_ConfigurationError = null;
            }
            catch (Exception exception)
            {
                m_ConfigurationError = exception.Message;
            }
        }

        void DrawTrackInspector(TimelineAsset asset, Track track)
        {
            EditorGUILayout.LabelField("Contract", track.ContractKind);
            string name = EditorGUILayout.TextField("Name", track.Name ?? string.Empty);
            bool muted = EditorGUILayout.Toggle("Muted", track.PersistentMuted);
            if (name == track.Name && muted == track.PersistentMuted)
                return;
            if (!TryBeginMutation(asset))
                return;
            try
            {
                asset.Data.ApplyModify(() =>
                {
                    track.Name = name;
                    track.PersistentMuted = muted;
                    asset.Data.Init();
                }, "Edit Timeline Track");
                m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
                m_ConfigurationError = null;
            }
            catch (Exception exception)
            {
                m_ConfigurationError = exception.Message;
            }
        }

        void DrawSectionInspector(TimelineAsset asset, TimelineSection section)
        {
            string name = EditorGUILayout.TextField("Name", section.Name);
            int frame = Mathf.Max(0, EditorGUILayout.IntField("Frame", section.Frame));
            if (name == section.Name && frame == section.Frame)
                return;
            if (!TryBeginMutation(asset))
                return;
            try
            {
                asset.Data.ApplyModify(() =>
                {
                    asset.Data.ConfigureSection(section, name, frame);
                    asset.Data.Init();
                }, "Edit Timeline Section");
                m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
                m_ConfigurationError = null;
            }
            catch (Exception exception)
            {
                m_ConfigurationError = exception.Message;
            }
        }

        void DrawClipMarkerSection(TimelineAsset asset, Clip clip)
        {
            if (clip is TreeClip presentationTree &&
                presentationTree.ExecutionDomain == TimelineExecutionDomain.Presentation)
                DrawPresentationMarkers(asset, presentationTree);
        }

        void ApplyFrames(TimelineAsset asset, Clip clip, int startFrame, int endFrame, int selfEaseInFrame, int selfEaseOutFrame, int clipInFrame)
        {
            if (!TryBeginMutation(asset))
                return;
            try
            {
                asset.Data.ApplyModify(() =>
                {
                    clip.StartFrame = startFrame;
                    clip.EndFrame = endFrame;
                    clip.SelfEaseInFrame = selfEaseInFrame;
                    clip.SelfEaseOutFrame = selfEaseOutFrame;
                    clip.ClipInFrame = clipInFrame;
                    clip.Track.UpdateMix();
                    asset.Data.Init();
                }, "Edit Timeline Clip Frames");
                m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
                m_ConfigurationError = null;
            }
            catch (Exception exception)
            {
                m_ConfigurationError = exception.Message;
            }
        }

        void ApplyConfiguration(TimelineAsset asset, Clip clip, TimelineAuthoringClipConfiguration configuration)
        {
            if (!TryBeginMutation(asset))
                return;
            try
            {
                ITimelineAuthoringClipResolver resolver = new TimelineInspectorClipResolver(asset.Data);
                asset.Data.ApplyModify(() =>
                {
                    TimelineAuthoringClipBinding.Configure(asset.Data, clip, configuration, resolver);
                    asset.Data.Init();
                }, "Edit Timeline Clip");
                m_SourceRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
                m_ConfigurationError = null;
            }
            catch (Exception exception)
            {
                m_ConfigurationError = exception.Message;
            }
        }

        bool TryBeginMutation(TimelineAsset asset)
        {
            string currentRevision = TimelineAuthoringFingerprint.Compute(asset.Data);
            if (string.IsNullOrEmpty(m_SourceRevision) ||
                string.Equals(m_SourceRevision, currentRevision, StringComparison.Ordinal))
                return true;
            m_ConfigurationError = "Timeline 内容已被外部修改，当前 Inspector 修改未提交。";
            Rebuild();
            return false;
        }

        static PropertyInfo FindProperty(PropertyInfo[] properties, string propertyId)
        {
            if (string.IsNullOrEmpty(propertyId))
                return null;
            string name = char.ToUpperInvariant(propertyId[0]) + propertyId.Substring(1);
            for (int index = 0; index < properties.Length; index++)
                if (string.Equals(properties[index].Name, name, StringComparison.Ordinal))
                    return properties[index];
            return null;
        }

        static object DrawProperty(TimelineAuthoringPropertyAttribute attribute, PropertyInfo property, object value)
        {
            string label = ObjectNames.NicifyVariableName(attribute.PropertyId);
            switch (attribute.Kind)
            {
                case TimelineAuthoringPropertyKind.Text:
                    return EditorGUILayout.TextField(label, value as string ?? string.Empty);
                case TimelineAuthoringPropertyKind.Boolean:
                    return EditorGUILayout.Toggle(label, value is bool boolean && boolean);
                case TimelineAuthoringPropertyKind.Integer:
                    return EditorGUILayout.IntField(label, value is int integer ? integer : 0);
                case TimelineAuthoringPropertyKind.Float:
                    float floatValue = value is float number ? number : 0f;
                    floatValue = EditorGUILayout.FloatField(label, floatValue);
                    if (attribute.HasMinimum)
                        floatValue = Mathf.Max((float)attribute.Minimum, floatValue);
                    if (attribute.HasMaximum)
                        floatValue = Mathf.Min((float)attribute.Maximum, floatValue);
                    return floatValue;
                case TimelineAuthoringPropertyKind.Enum:
                    Enum enumValue = value as Enum;
                    if (enumValue == null && attribute.EnumType != null)
                        enumValue = (Enum)Enum.ToObject(attribute.EnumType, 0);
                    return enumValue == null ? value : EditorGUILayout.EnumPopup(label, enumValue);
                case TimelineAuthoringPropertyKind.Vector2:
                    return EditorGUILayout.Vector2Field(label, value is Vector2 vector ? vector : Vector2.zero);
                case TimelineAuthoringPropertyKind.Object:
                    if (property.PropertyType == typeof(AnimationCurve))
                    {
                        AnimationCurve curve = value as AnimationCurve;
                        AnimationCurve copy = curve == null ? new AnimationCurve() : new AnimationCurve(curve.keys)
                        {
                            preWrapMode = curve.preWrapMode,
                            postWrapMode = curve.postWrapMode
                        };
                        return EditorGUILayout.CurveField(label, copy);
                    }
                    return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, property.PropertyType, false);
                default:
                    return value;
            }
        }

        static bool ValuesDiffer(object left, object right)
        {
            if (left is AnimationCurve leftCurve && right is AnimationCurve rightCurve)
                return !TimelineCurveAuthoring.AreEquivalent(leftCurve, rightCurve);
            return !Equals(left, right);
        }

        sealed class TimelineInspectorClipResolver : ITimelineAuthoringClipResolver
        {
            readonly TimelineData m_Timeline;

            public TimelineInspectorClipResolver(TimelineData timeline)
            {
                m_Timeline = timeline;
            }

            public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
            {
                clip = (timeline ?? m_Timeline)?.Tracks
                    .SelectMany(track => track.Clips)
                    .OfType<MotionCurveClip>()
                    .FirstOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
                return clip != null;
            }
        }
    }
}
