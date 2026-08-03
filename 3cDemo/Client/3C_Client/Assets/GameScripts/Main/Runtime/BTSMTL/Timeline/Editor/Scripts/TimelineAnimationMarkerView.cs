using System;
using System.Collections.Generic;
using BTSMTL.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal sealed class AnimationMarkerSyncTrackInspectorView : VisualElement
    {
        readonly TimelineEditorView m_Editor;
        readonly AnimationTrack m_Track;
        readonly string m_SelectedMarkerId;
        readonly List<AnimationMarkerSyncAuthoringIssue> m_Issues = new List<AnimationMarkerSyncAuthoringIssue>();
        readonly List<TimelineAnimationMarkerSyncAuthoringIssue> m_ContextIssues =
            new List<TimelineAnimationMarkerSyncAuthoringIssue>();
        readonly List<TimelineAnimationMarkerSyncGroupMember> m_GroupMembers =
            new List<TimelineAnimationMarkerSyncGroupMember>();
        readonly List<TimelineAnimationMarkerSyncPreviewCandidate> m_PreviewSources =
            new List<TimelineAnimationMarkerSyncPreviewCandidate>();
        VisualElement m_IssueContainer;
        PopupField<string> m_PreviewSourceField;
        Label m_GroupCoverageLabel;
        Label m_PreviewStateLabel;
        string m_PreviewSourceSignature = string.Empty;
        ITimelineAnimationMarkerSyncAuthoringContext m_LastTopologyContext;

        public AnimationMarkerSyncTrackInspectorView(
            TimelineEditorView editor,
            AnimationTrack track,
            string selectedMarkerId = "")
        {
            m_Editor = editor ?? throw new ArgumentNullException(nameof(editor));
            m_Track = track ?? throw new ArgumentNullException(nameof(track));
            m_SelectedMarkerId = selectedMarkerId ?? string.Empty;
            Rebuild();
            schedule.Execute(UpdatePreviewState).Every(50);
        }

        void Rebuild()
        {
            Clear();
            Add(new Label("Marker Sync") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

            var mode = new PopupField<string>(
                "Sync Mode",
                new List<string> { "Unspecified", "None", "MarkerGroup" },
                (int)m_Track.SyncMode);
            mode.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "None")
                {
                    Apply("Configure Animation Sync None", m_Track.ConfigureNone);
                    return;
                }
                if (evt.newValue == "MarkerGroup")
                    DrawMarkerGroupConfiguration(true);
                else
                    Rebuild();
            });
            Add(mode);

            if (m_Track.SyncMode == AnimationSyncMode.MarkerGroup)
                DrawMarkerGroupConfiguration(false);

            DrawIssues();
            if (m_Track.SyncMode == AnimationSyncMode.MarkerGroup)
                DrawPreview();
        }

        void DrawMarkerGroupConfiguration(bool pending)
        {
            while (childCount > 2)
                RemoveAt(2);
            string initialGroup = m_Track.SyncGroupId;
            AnimationMarkerSequenceTopology initialTopology =
                m_Track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic
                    ? AnimationMarkerSequenceTopology.Cyclic
                    : AnimationMarkerSequenceTopology.Finite;
            AnimationMarkerSyncRole initialRole =
                m_Track.SyncRole == AnimationMarkerSyncRole.AlwaysLeader ||
                m_Track.SyncRole == AnimationMarkerSyncRole.AlwaysFollower
                    ? m_Track.SyncRole
                    : AnimationMarkerSyncRole.CanBeLeader;

            var group = new TextField("Sync Group") { value = initialGroup, isDelayed = true };
            var topology = new EnumField("Topology", initialTopology);
            var role = new EnumField("Sync Role", initialRole);
            Add(group);
            Add(topology);
            Add(role);

            if (pending)
            {
                var apply = new Button(() =>
                {
                    string value = AnimationMarkerSyncAuthoring.NormalizeId(group.value);
                    if (string.IsNullOrEmpty(value))
                        return;
                    Apply("Configure Animation Marker Group", () =>
                        m_Track.ConfigureMarkerGroup(
                            value,
                            (AnimationMarkerSequenceTopology)topology.value,
                            (AnimationMarkerSyncRole)role.value));
                }) { text = "Apply" };
                Add(apply);
                return;
            }

            group.RegisterValueChangedCallback(evt =>
            {
                string value = AnimationMarkerSyncAuthoring.NormalizeId(evt.newValue);
                if (!string.IsNullOrEmpty(value))
                    Apply("Rename Animation Sync Group", () =>
                        m_Track.ConfigureMarkerGroup(value, m_Track.SequenceTopology, m_Track.SyncRole));
            });
            topology.RegisterValueChangedCallback(evt => Apply(
                "Change Animation Marker Topology",
                () => m_Track.ConfigureMarkerGroup(
                    m_Track.SyncGroupId,
                    (AnimationMarkerSequenceTopology)evt.newValue,
                    m_Track.SyncRole)));
            role.RegisterValueChangedCallback(evt => Apply(
                "Change Animation Marker Sync Role",
                () => m_Track.ConfigureMarkerGroup(
                    m_Track.SyncGroupId,
                    m_Track.SequenceTopology,
                    (AnimationMarkerSyncRole)evt.newValue)));

            DrawMarkerList();
        }

        void DrawMarkerList()
        {
            Add(new Label("Markers") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            for (int i = 0; i < m_Track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = m_Track.SyncMarkers[i];
                if (marker == null)
                    continue;
                string markerAuthoringId = marker.AuthoringId;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                if (string.Equals(markerAuthoringId, m_SelectedMarkerId, StringComparison.Ordinal))
                    row.style.backgroundColor = new Color(0.2f, 0.42f, 0.48f, 0.55f);
                var markerId = new TextField { value = marker.MarkerId, isDelayed = true };
                markerId.style.flexGrow = 1f;
                markerId.RegisterValueChangedCallback(evt =>
                {
                    if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue == evt.newValue.Trim())
                        Apply("Rename Animation Marker", () => m_Track.RenameMarker(markerAuthoringId, evt.newValue));
                });
                var frame = new IntegerField { value = marker.Frame, isDelayed = true };
                frame.style.width = 72f;
                frame.RegisterValueChangedCallback(evt => Apply(
                    "Move Animation Marker",
                    () => m_Track.MoveMarker(markerAuthoringId, evt.newValue)));
                var remove = new Button(() => Apply(
                    "Delete Animation Marker",
                    () => m_Track.DeleteMarker(markerAuthoringId))) { text = "×", tooltip = "Delete marker" };
                remove.style.width = 24f;
                row.Add(markerId);
                row.Add(frame);
                row.Add(remove);
                Add(row);
            }

            var addRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var newId = new TextField { value = string.Empty, isDelayed = true };
            newId.style.flexGrow = 1f;
            var newFrame = new IntegerField { value = 0, isDelayed = true };
            newFrame.style.width = 72f;
            var add = new Button(() =>
            {
                if (string.IsNullOrEmpty(newId.value) || newId.value != newId.value.Trim())
                    return;
                Apply("Add Animation Marker", () => m_Track.AddMarker(newId.value, newFrame.value));
            }) { text = "+", tooltip = "Add marker" };
            add.style.width = 24f;
            addRow.Add(newId);
            addRow.Add(newFrame);
            addRow.Add(add);
            Add(addRow);

            var pairs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 1; i < m_Track.SyncMarkers.Count; i++)
                pairs.Add($"{m_Track.SyncMarkers[i - 1].MarkerId} → {m_Track.SyncMarkers[i].MarkerId}");
            if (m_Track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic && m_Track.SyncMarkers.Count > 1)
                pairs.Add($"{m_Track.SyncMarkers[m_Track.SyncMarkers.Count - 1].MarkerId} → {m_Track.SyncMarkers[0].MarkerId}");
            Add(new Label(string.Join("  |  ", pairs)) { tooltip = "Directed marker pair coverage" });
        }

        void DrawIssues()
        {
            m_IssueContainer = new VisualElement();
            Add(m_IssueContainer);
            RefreshIssues();
        }

        void RefreshIssues()
        {
            if (m_IssueContainer == null)
                return;
            m_IssueContainer.Clear();
            ITimelineAnimationMarkerSyncAuthoringContext context = m_Editor.SessionContext?.MarkerTopologyContext;
            if (context != null)
            {
                m_ContextIssues.Clear();
                context.CollectAnimationMarkerSyncAuthoringIssues(
                    m_Track.Timeline,
                    m_Track.AuthoringId,
                    m_ContextIssues);
                for (int i = 0; i < m_ContextIssues.Count; i++)
                {
                    TimelineAnimationMarkerSyncAuthoringIssue issue = m_ContextIssues[i];
                    var box = new HelpBox($"{issue.Code}: {issue.Message}", HelpBoxMessageType.Error)
                    {
                        tooltip = $"{issue.AuthoringPath}\n{issue.RelatedIdentity}"
                    };
                    m_IssueContainer.Add(box);
                }
                return;
            }

            m_Issues.Clear();
            AnimationMarkerSyncAuthoring.ValidateTrack(
                new AnimationMarkerSyncAuthoringInput(
                    $"producer:{m_Track.Timeline.AuthoringId}:{m_Track.AuthoringId}",
                    m_Track.Timeline,
                    m_Track,
                    Array.Empty<AnimationMarkerSyncCallSite>()),
                m_Issues);
            for (int i = 0; i < m_Issues.Count; i++)
                m_IssueContainer.Add(new HelpBox(
                    $"{m_Issues[i].Code}: {m_Issues[i].Message}",
                    HelpBoxMessageType.Error));
        }

        void DrawPreview()
        {
            Add(new Label("Presentation Preview") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            if (m_Editor.IsLiveDebug)
            {
                Add(new Label("Marker Sync runtime state is shown in Live Debug details."));
                return;
            }

            m_Editor.PreviewSession.SetMarkerSyncTargetTrack(m_Track.AuthoringId);
            m_PreviewSourceField = new PopupField<string>(
                "Sync Source",
                new List<string> { "None" },
                0);
            m_PreviewSourceField.RegisterValueChangedCallback(_ => ApplyPreviewSource());
            Add(m_PreviewSourceField);
            m_GroupCoverageLabel = new Label();
            m_GroupCoverageLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(m_GroupCoverageLabel);
            m_PreviewStateLabel = new Label();
            m_PreviewStateLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(m_PreviewStateLabel);
            RefreshPreviewSources();
            UpdatePreviewState();
        }

        void RefreshPreviewSources()
        {
            if (m_PreviewSourceField == null)
                return;
            IReadOnlyList<TimelineAnimationMarkerSyncPreviewCandidate> sources =
                m_Editor.PreviewSession.MarkerSyncSources;
            string signature = string.Empty;
            for (int i = 0; i < sources.Count; i++)
                signature += sources[i].SourceTimelineAuthoringId + "/" + sources[i].SourceTrackAuthoringId + ";";
            if (string.Equals(signature, m_PreviewSourceSignature, StringComparison.Ordinal))
                return;
            m_PreviewSourceSignature = signature;
            m_PreviewSources.Clear();
            for (int i = 0; i < sources.Count; i++)
                m_PreviewSources.Add(sources[i]);

            var choices = new List<string>(m_PreviewSources.Count + 1) { "None" };
            int selectedIndex = 0;
            for (int i = 0; i < m_PreviewSources.Count; i++)
            {
                TimelineAnimationMarkerSyncPreviewCandidate source = m_PreviewSources[i];
                string suffix = source.SourceTrackAuthoringId.Length > 8
                    ? source.SourceTrackAuthoringId.Substring(0, 8)
                    : source.SourceTrackAuthoringId;
                choices.Add($"{source.DisplayName} [{suffix}]");
                if (string.Equals(source.SourceTimelineAuthoringId, m_Editor.PreviewSession.MarkerSyncSourceTimelineId, StringComparison.Ordinal) &&
                    string.Equals(source.SourceTrackAuthoringId, m_Editor.PreviewSession.MarkerSyncSourceTrackId, StringComparison.Ordinal))
                    selectedIndex = i + 1;
            }
            m_PreviewSourceField.choices = choices;
            m_PreviewSourceField.SetValueWithoutNotify(choices[selectedIndex]);
            RefreshGroupCoverage();
        }

        void RefreshGroupCoverage()
        {
            if (m_GroupCoverageLabel == null)
                return;
            m_GroupMembers.Clear();
            ITimelineAnimationMarkerSyncAuthoringContext context = m_Editor.SessionContext?.MarkerTopologyContext;
            if (context == null)
            {
                m_GroupCoverageLabel.text = string.Empty;
                return;
            }
            context.CollectAnimationMarkerSyncGroupMembers(
                m_Track.Timeline,
                m_Track.AuthoringId,
                m_GroupMembers);
            var lines = new List<string>(m_GroupMembers.Count);
            for (int i = 0; i < m_GroupMembers.Count; i++)
            {
                TimelineAnimationMarkerSyncGroupMember member = m_GroupMembers[i];
                lines.Add($"{member.DisplayName}: {member.DirectedPairCoverage}");
            }
            m_GroupCoverageLabel.text = string.Join("\n", lines);
            m_GroupCoverageLabel.tooltip = $"{m_Track.AnimationChannelId}/{m_Track.SyncGroupId}";
        }

        void ApplyPreviewSource()
        {
            int index = m_PreviewSourceField.index - 1;
            if (index < 0 || index >= m_PreviewSources.Count)
            {
                m_Editor.PreviewSession.SetMarkerSyncSource(string.Empty, string.Empty);
                return;
            }
            TimelineAnimationMarkerSyncPreviewCandidate source = m_PreviewSources[index];
            m_Editor.PreviewSession.SetMarkerSyncSource(
                source.SourceTimelineAuthoringId,
                source.SourceTrackAuthoringId);
        }

        void UpdatePreviewState()
        {
            ITimelineAnimationMarkerSyncAuthoringContext topology = m_Editor.SessionContext?.MarkerTopologyContext;
            if (!ReferenceEquals(m_LastTopologyContext, topology))
            {
                m_LastTopologyContext = topology;
                RefreshIssues();
                RefreshGroupCoverage();
            }
            if (m_PreviewStateLabel == null || m_Editor.IsLiveDebug || m_Track.SyncMode != AnimationSyncMode.MarkerGroup)
                return;
            if (!string.Equals(m_Editor.PreviewSession.MarkerSyncTargetTrackId, m_Track.AuthoringId, StringComparison.Ordinal))
                m_Editor.PreviewSession.SetMarkerSyncTargetTrack(m_Track.AuthoringId);
            RefreshPreviewSources();
            m_PreviewSourceField.SetEnabled(m_Editor.PreviewSession.HasTarget && m_PreviewSources.Count > 0);
            if (!m_Editor.PreviewSession.HasTarget)
            {
                SetPreviewState("Select a Timeline preview target.", string.Empty);
                return;
            }
            if (!m_Editor.PreviewSession.TryGetMarkerSyncPreviewState(out TimelineAnimationMarkerSyncPreviewState state))
            {
                SetPreviewState("No playback sample.", string.Empty);
                return;
            }

            string pair = string.IsNullOrEmpty(state.PreviousMarkerId) || string.IsNullOrEmpty(state.NextMarkerId)
                ? "no marker pair"
                : $"{state.PreviousMarkerId}->{state.NextMarkerId} {state.Fraction:0.000}";
            SetPreviewState(
                $"{state.RawTime:0.000}s -> {state.EffectiveTime:0.000}s | {pair} | {state.Reason}",
                $"Channel: {state.AnimationChannelId}\nGroup: {state.SyncGroupId}\n" +
                $"Source: {state.SourceProducerId}\nTarget: {state.TargetProducerId}\n" +
                $"Cycle: {state.EffectiveCycle}\nRelation: {state.RelationId}\n" +
                $"Mapped: {state.Mapped}\nRebased: {state.Rebased}\n" +
                $"Lifecycle: {state.LifecyclePhase}\nReason: {state.Reason}");
        }

        void SetPreviewState(string text, string tooltip)
        {
            m_PreviewStateLabel.text = text ?? string.Empty;
            m_PreviewStateLabel.tooltip = tooltip ?? string.Empty;
        }

        void Apply(string undoName, Action mutation)
        {
            m_Track.Timeline.ApplyModify(mutation, undoName);
            m_Editor.RefreshPreview(true);
        }
    }
}

