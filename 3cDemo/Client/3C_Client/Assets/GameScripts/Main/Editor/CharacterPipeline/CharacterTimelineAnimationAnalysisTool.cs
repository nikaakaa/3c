using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterTimelineEditorComposition
    {
        static CharacterTimelineEditorComposition()
        {
            TimelineEditorToolComposition.SetCatalog(CreateCatalog(null));
            TimelineEditorOpenRequestComposition.SetMarkerTopologyResolver(new MarkerTopologyResolver());
            TimelineEditorOpenRequestComposition.SetToolCatalogResolver(new ToolCatalogResolver());
        }

        static TimelineEditorToolCatalog CreateCatalog(CharacterFootPlacementAnalysisSource source) =>
            new TimelineEditorToolCatalog(new ITimelineEditorToolProvider[]
            {
                new CharacterTimelineAnimationAnalysisToolProvider(source)
            });

        sealed class MarkerTopologyResolver : ITimelineEditorMarkerTopologyResolver
        {
            public ITimelineAnimationMarkerSyncAuthoringContext Resolve(BaseTreeWindow sourceGraphWindow) =>
                sourceGraphWindow?.AuthoringContext as CharacterPipelineAuthoringContext;
        }

        sealed class ToolCatalogResolver : ITimelineEditorToolCatalogResolver
        {
            public TimelineEditorToolCatalog Resolve(BaseTreeWindow sourceGraphWindow)
            {
                CharacterFootPlacementAnalysisSource source = null;
                if (sourceGraphWindow?.AuthoringContext is CharacterPipelineAuthoringContext context &&
                    context.Definition && context.Definition.AnimationPresentationProfile)
                {
                    string guid = context.Definition.AnimationPresentationProfile.FootPlacementAnalysisSourceAssetGuid;
                    string path = CharacterFootPlacementAnalysisSource.IsAssetGuid(guid)
                        ? AssetDatabase.GUIDToAssetPath(guid)
                        : string.Empty;
                    source = string.IsNullOrEmpty(path)
                        ? null
                        : AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                }
                return CreateCatalog(source);
            }
        }
    }

    sealed class CharacterTimelineAnimationAnalysisToolProvider : ITimelineEditorToolProvider
    {
        readonly CharacterFootPlacementAnalysisSource m_InitialSource;

        public CharacterTimelineAnimationAnalysisToolProvider(CharacterFootPlacementAnalysisSource initialSource)
        {
            m_InitialSource = initialSource;
        }

        public string ToolId => "thirdperson.character.animation-analysis";
        public string DisplayName => "Animation Analysis";
        public bool Supports(TimelineEditorSelection selection) =>
            selection.Clip is TimelineAnimationClip clip && clip.Clip;
        public TimelineEditorToolPanel CreatePanel(TimelineEditorSessionContext session) =>
            new CharacterTimelineAnimationAnalysisPanel(session, m_InitialSource);
    }

    enum CharacterFootAnalysisSide
    {
        Left,
        Right
    }

    enum CharacterFootAnalysisMetric
    {
        Speed,
        Height,
        Plant,
        Landing
    }

    enum CharacterFootAnalysisLandingMetric
    {
        Delay,
        OffsetX,
        OffsetZ
    }

    sealed class CharacterTimelineAnimationAnalysisPanel : TimelineEditorToolPanel
    {
        readonly TimelineEditorSessionContext m_Session;
        readonly ObjectField m_ClipField;
        readonly ObjectField m_SourceField;
        readonly EnumField m_SideField;
        readonly EnumField m_MetricField;
        readonly EnumField m_LandingMetricField;
        readonly Label m_Status;
        readonly Label m_Identity;
        readonly Label m_Error;
        readonly Button m_Rebuild;
        readonly Label m_ContactMarkers;
        readonly Button m_ApplyContactMarkers;
        readonly CharacterFootAnalysisCurveCanvas m_Canvas;
        TimelineAnimationClip m_SelectedClip;
        CharacterFootPlacementAnalysisSource m_Source;
        AnimationFootAnalysisArtifact m_Artifact;
        AnimationFootContactCandidateSet m_ContactCandidates;
        TimelineFootContactMarkerProposal m_ContactProposal;

        public CharacterTimelineAnimationAnalysisPanel(
            TimelineEditorSessionContext session,
            CharacterFootPlacementAnalysisSource initialSource)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Source = initialSource;
            style.flexGrow = 1f;
            style.flexDirection = FlexDirection.Row;
            style.paddingLeft = 8f;
            style.paddingRight = 8f;
            style.paddingTop = 6f;
            style.paddingBottom = 6f;

            var controls = new VisualElement();
            controls.style.width = 310f;
            controls.style.flexShrink = 0f;
            controls.style.paddingRight = 10f;
            m_ClipField = new ObjectField("Animation Clip")
            {
                objectType = typeof(UnityEngine.AnimationClip),
                allowSceneObjects = false
            };
            m_ClipField.SetEnabled(false);
            m_SourceField = new ObjectField("Analysis Source")
            {
                objectType = typeof(CharacterFootPlacementAnalysisSource),
                allowSceneObjects = false
            };
            m_SourceField.SetValueWithoutNotify(m_Source);
            m_SourceField.RegisterValueChangedCallback(evt =>
            {
                m_Source = evt.newValue as CharacterFootPlacementAnalysisSource;
                RefreshArtifact();
            });
            m_SideField = new EnumField("Foot", CharacterFootAnalysisSide.Left);
            m_SideField.RegisterValueChangedCallback(_ => RefreshCanvas());
            m_MetricField = new EnumField("Metric", CharacterFootAnalysisMetric.Speed);
            m_MetricField.RegisterValueChangedCallback(_ =>
            {
                RefreshMetricFields();
                RefreshCanvas();
            });
            m_LandingMetricField = new EnumField("Landing", CharacterFootAnalysisLandingMetric.Delay);
            m_LandingMetricField.RegisterValueChangedCallback(_ => RefreshCanvas());
            m_Status = new Label();
            m_Status.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Identity = new Label();
            m_Identity.style.whiteSpace = WhiteSpace.Normal;
            m_Identity.style.fontSize = 10f;
            m_Error = new Label();
            m_Error.style.whiteSpace = WhiteSpace.Normal;
            m_Error.style.color = new Color(1f, 0.48f, 0.36f);
            m_Error.style.fontSize = 10f;
            m_Rebuild = new Button(Rebuild) { text = "Rebuild Selected Clip" };
            m_ContactMarkers = new Label();
            m_ContactMarkers.style.whiteSpace = WhiteSpace.Normal;
            m_ContactMarkers.style.fontSize = 10f;
            m_ApplyContactMarkers = new Button(ApplyContactMarkers) { text = "Apply Foot Contact Markers" };
            controls.Add(m_ClipField);
            controls.Add(m_SourceField);
            controls.Add(m_SideField);
            controls.Add(m_MetricField);
            controls.Add(m_LandingMetricField);
            controls.Add(m_Status);
            controls.Add(m_Identity);
            controls.Add(m_Error);
            controls.Add(m_Rebuild);
            controls.Add(m_ContactMarkers);
            controls.Add(m_ApplyContactMarkers);

            m_Canvas = new CharacterFootAnalysisCurveCanvas();
            m_Canvas.style.flexGrow = 1f;
            m_Canvas.style.minWidth = 180f;
            Add(controls);
            Add(m_Canvas);
            m_Session.SelectionChanged += OnSelectionChanged;
            RefreshMetricFields();
            OnSelectionChanged(m_Session.Selection);
        }

        void RefreshMetricFields()
        {
            m_LandingMetricField.style.display =
                (CharacterFootAnalysisMetric)m_MetricField.value == CharacterFootAnalysisMetric.Landing
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        void OnSelectionChanged(TimelineEditorSelection selection)
        {
            m_SelectedClip = selection.Clip as TimelineAnimationClip;
            m_ClipField.SetValueWithoutNotify(m_SelectedClip?.Clip);
            RefreshArtifact();
        }

        void RefreshArtifact()
        {
            m_Artifact = null;
            m_ContactCandidates = null;
            m_ContactProposal = null;
            m_Error.text = string.Empty;
            m_Identity.text = string.Empty;
            if (m_SelectedClip == null || !m_SelectedClip.Clip)
            {
                SetStatus("Animation Clip Required", false);
                RefreshCanvas();
                return;
            }
            if (!m_Source)
            {
                SetStatus("Analysis Source Required", false);
                RefreshCanvas();
                return;
            }
            try
            {
                AnimationFootAnalysisArtifactIdentity identity =
                    AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(m_SelectedClip.Clip, m_Source);
                AnimationFootAnalysisArtifactInspection inspection =
                    AnimationFootAnalysisArtifactBuilder.Inspect(m_SelectedClip.Clip, m_Source);
                m_Artifact = inspection.Artifact;
                SetStatus(inspection.Status.ToString(), inspection.Status == AnimationFootAnalysisArtifactStatus.Ready);
                m_Identity.text = $"{identity.IdentityHash.Value}\n{inspection.Path}";
                m_Error.text = inspection.Error;
            }
            catch (Exception exception)
            {
                SetStatus("Invalid", false);
                m_Error.text = exception.Message;
            }
            RefreshContactMarkers();
            RefreshCanvas();
        }

        void Rebuild()
        {
            if (m_SelectedClip == null || !m_SelectedClip.Clip || !m_Source)
                return;
            try
            {
                AnimationFootAnalysisArtifactBuilder.Build(m_SelectedClip.Clip, m_Source);
                RefreshArtifact();
            }
            catch (Exception exception)
            {
                m_Error.text = exception.Message;
            }
        }

        void SetStatus(string value, bool ready)
        {
            m_Status.text = value;
            m_Status.style.color = ready
                ? new Color(0.42f, 0.82f, 0.58f)
                : new Color(0.92f, 0.68f, 0.34f);
            m_Rebuild.SetEnabled(m_SelectedClip != null && m_SelectedClip.Clip && m_Source);
        }

        void RefreshContactMarkers()
        {
            m_ContactCandidates = null;
            m_ContactProposal = null;
            m_ContactMarkers.text = string.Empty;
            m_ApplyContactMarkers.SetEnabled(false);
            if (m_Artifact == null || m_SelectedClip?.Track is not AnimationTrack track ||
                track.SyncMode != AnimationSyncMode.MarkerGroup ||
                track.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic)
                return;
            try
            {
                m_ContactCandidates = AnimationFootContactCandidateSet.Build(m_SelectedClip.Clip, m_Artifact);
                string sourceSummary = BuildSourceCandidateSummary(m_ContactCandidates);
                try
                {
                    m_ContactProposal = TimelineFootContactMarkerProposal.Build(
                        m_Session.Timeline,
                        track,
                        m_SelectedClip,
                        m_Artifact);
                    m_ContactMarkers.text =
                        $"Contact candidates {Short(m_ContactProposal.Revision)}\n" +
                        BuildTimelineCandidateSummary(m_ContactProposal);
                    m_ApplyContactMarkers.SetEnabled(!m_Session.IsReadOnly);
                }
                catch (Exception exception)
                {
                    m_ContactMarkers.text = $"Contact candidates\n{sourceSummary}\nApply unavailable: {exception.Message}";
                }
            }
            catch (Exception exception)
            {
                m_ContactMarkers.text = $"Contact candidates unavailable: {exception.Message}";
            }
        }

        void ApplyContactMarkers()
        {
            if (m_ContactProposal == null || m_SelectedClip?.Track is not AnimationTrack track || !m_Source)
                return;
            TimelineFootContactMarkerProposal displayed = m_ContactProposal;
            try
            {
                AnimationFootAnalysisArtifactInspection inspection =
                    AnimationFootAnalysisArtifactBuilder.Inspect(m_SelectedClip.Clip, m_Source);
                if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null)
                    throw new InvalidOperationException($"Foot Analysis artifact is {inspection.Status}; rebuild it before applying markers.");
                TimelineFootContactMarkerProposal current = TimelineFootContactMarkerProposal.Build(
                    m_Session.Timeline,
                    track,
                    m_SelectedClip,
                    inspection.Artifact);
                if (!string.Equals(current.Revision, displayed.Revision, StringComparison.Ordinal))
                    throw new InvalidOperationException("Foot contact marker candidates are stale; review the refreshed proposal before applying.");
                string confirmation =
                    $"Timeline: {current.TimelineAuthoringId}\n" +
                    $"Track: {current.TrackAuthoringId}\n" +
                    $"Clip: {current.ClipAuthoringId}\n" +
                    $"Artifact: {Short(current.Source.ArtifactContentHash)}\n\n" +
                    BuildTimelineCandidateSummary(current) +
                    "\n\nOnly LeftFootContact and RightFootContact markers will be replaced. Other markers will be preserved.";
                if (!EditorUtility.DisplayDialog("Apply Foot Contact Markers", confirmation, "Apply", "Cancel"))
                    return;
                m_Session.Apply(() => current.Apply(track), "Apply Foot Contact Markers");
                RefreshContactMarkers();
            }
            catch (Exception exception)
            {
                RefreshArtifact();
                m_Error.text = exception.Message;
            }
        }

        static string BuildSourceCandidateSummary(AnimationFootContactCandidateSet candidates)
        {
            var values = new List<string>();
            for (int i = 0; i < candidates.Candidates.Count; i++)
            {
                AnimationFootContactCandidate candidate = candidates.Candidates[i];
                values.Add($"{candidate.MarkerId}@{candidate.SourceNormalizedTime:0.000}");
            }
            return string.Join(", ", values);
        }

        static string BuildTimelineCandidateSummary(TimelineFootContactMarkerProposal proposal)
        {
            var values = new List<string>();
            for (int i = 0; i < proposal.Candidates.Count; i++)
            {
                TimelineFootContactMarkerCandidate candidate = proposal.Candidates[i];
                values.Add($"{candidate.MarkerId}@{candidate.TimelineFrame}F");
            }
            return string.Join(", ", values);
        }

        static string Short(string value) =>
            string.IsNullOrEmpty(value) || value.Length <= 12 ? value ?? string.Empty : value.Substring(0, 12);

        void RefreshCanvas()
        {
            AnimationFootFeatureCurveSet curves = null;
            if (m_Artifact != null)
            {
                curves = (CharacterFootAnalysisSide)m_SideField.value == CharacterFootAnalysisSide.Left
                    ? m_Artifact.Features.Left
                    : m_Artifact.Features.Right;
            }
            m_Canvas.SetData(
                curves,
                (CharacterFootAnalysisMetric)m_MetricField.value,
                (CharacterFootAnalysisLandingMetric)m_LandingMetricField.value,
                m_ContactCandidates?.Candidates);
        }

        public override void Dispose()
        {
            m_Session.SelectionChanged -= OnSelectionChanged;
            m_Artifact = null;
            m_Canvas.SetData(
                null,
                CharacterFootAnalysisMetric.Speed,
                CharacterFootAnalysisLandingMetric.Delay,
                null);
            base.Dispose();
        }
    }

    sealed class CharacterFootAnalysisCurveCanvas : VisualElement
    {
        AnimationFootFeatureCurveSet m_Curves;
        CharacterFootAnalysisMetric m_Metric;
        CharacterFootAnalysisLandingMetric m_LandingMetric;
        IReadOnlyList<AnimationFootContactCandidate> m_ContactCandidates = Array.Empty<AnimationFootContactCandidate>();

        public CharacterFootAnalysisCurveCanvas()
        {
            generateVisualContent += Draw;
            style.backgroundColor = new Color(0.105f, 0.105f, 0.105f);
            style.borderLeftWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderTopWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftColor = style.borderRightColor = style.borderTopColor = style.borderBottomColor =
                new Color(0.25f, 0.25f, 0.25f);
        }

        public void SetData(
            AnimationFootFeatureCurveSet curves,
            CharacterFootAnalysisMetric metric,
            CharacterFootAnalysisLandingMetric landingMetric,
            IReadOnlyList<AnimationFootContactCandidate> contactCandidates)
        {
            m_Curves = curves;
            m_Metric = metric;
            m_LandingMetric = landingMetric;
            m_ContactCandidates = contactCandidates ?? Array.Empty<AnimationFootContactCandidate>();
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (m_Curves == null || rect.width < 2f || rect.height < 2f)
                return;
            const int samples = 128;
            var values = new float[samples + 1];
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            for (int i = 0; i <= samples; i++)
            {
                float value = Evaluate(i / (float)samples);
                values[i] = value;
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }
            if (m_Metric == CharacterFootAnalysisMetric.Plant)
            {
                minimum = 0f;
                maximum = 1f;
            }
            if (Mathf.Approximately(minimum, maximum))
            {
                minimum -= 0.5f;
                maximum += 0.5f;
            }
            float padding = Mathf.Max((maximum - minimum) * 0.08f, 0.0001f);
            minimum -= padding;
            maximum += padding;
            Painter2D painter = context.painter2D;
            painter.strokeColor = new Color(0.25f, 0.25f, 0.25f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            for (int i = 1; i < 4; i++)
            {
                float y = rect.yMin + rect.height * i / 4f;
                painter.MoveTo(new Vector2(rect.xMin, y));
                painter.LineTo(new Vector2(rect.xMax, y));
            }
            painter.Stroke();
            painter.strokeColor = new Color(0.35f, 0.85f, 0.55f);
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int i = 0; i <= samples; i++)
            {
                float x = rect.xMin + rect.width * i / samples;
                float y = rect.yMax - Mathf.InverseLerp(minimum, maximum, values[i]) * rect.height;
                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
            DrawContactCandidates(painter, rect);
        }

        void DrawContactCandidates(Painter2D painter, Rect rect)
        {
            painter.lineWidth = 1.5f;
            for (int i = 0; i < m_ContactCandidates.Count; i++)
            {
                AnimationFootContactCandidate candidate = m_ContactCandidates[i];
                float x = rect.xMin + rect.width * candidate.SourceNormalizedTime;
                painter.strokeColor = candidate.Side == TimelineFootContactSide.Left
                    ? new Color(0.25f, 0.72f, 1f)
                    : new Color(1f, 0.66f, 0.24f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, rect.yMin));
                painter.LineTo(new Vector2(x, rect.yMax));
                painter.Stroke();
            }
        }

        float Evaluate(float normalizedTime)
        {
            AnimationFootFeatureSample sample = m_Curves.Sample(normalizedTime);
            return m_Metric switch
            {
                CharacterFootAnalysisMetric.Speed => sample.SoleLocalVelocity.magnitude,
                CharacterFootAnalysisMetric.Height => sample.SoleHeight,
                CharacterFootAnalysisMetric.Plant => sample.PlantConfidence,
                CharacterFootAnalysisMetric.Landing => m_LandingMetric switch
                {
                    CharacterFootAnalysisLandingMetric.Delay => sample.NextLandingDelaySeconds,
                    CharacterFootAnalysisLandingMetric.OffsetX => sample.NextLandingLocalOffset.x,
                    CharacterFootAnalysisLandingMetric.OffsetZ => sample.NextLandingLocalOffset.y,
                    _ => 0f
                },
                _ => 0f
            };
        }
    }

    public sealed partial class CharacterPipelineAuthoringContext : ITimelineAnimationMarkerSyncAuthoringContext
    {
        public void CollectAnimationMarkerSyncAuthoringIssues(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncAuthoringIssue> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!Definition || !Definition.RootTreeAsset || Definition.RootTreeAsset.Tree == null || timeline == null)
                return;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                Definition.RootTreeAsset.Tree,
                topologyErrors);
            for (int i = 0; i < topologyErrors.Count; i++)
            {
                destination.Add(new TimelineAnimationMarkerSyncAuthoringIssue(
                    "character_authoring_topology",
                    topologyErrors[i],
                    Definition.RootTreeAsset.name,
                    string.Empty));
            }
            if (!topology.IsValid)
                return;
            var issues = new List<AnimationMarkerSyncAuthoringIssue>();
            CharacterAnimationMarkerSyncAuthoringContext.ValidateTrackContext(
                topology,
                timeline.AuthoringId,
                targetTrackAuthoringId,
                issues);
            for (int i = 0; i < issues.Count; i++)
            {
                AnimationMarkerSyncAuthoringIssue issue = issues[i];
                destination.Add(new TimelineAnimationMarkerSyncAuthoringIssue(
                    issue.Code,
                    issue.Message,
                    issue.AuthoringPath,
                    issue.RelatedIdentity));
            }
        }

        public void CollectAnimationMarkerSyncGroupMembers(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncGroupMember> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!Definition || !Definition.RootTreeAsset || Definition.RootTreeAsset.Tree == null || timeline == null)
                return;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                Definition.RootTreeAsset.Tree,
                topologyErrors);
            if (!topology.IsValid)
                return;
            CharacterAnimationMarkerSyncAuthoringContext.CollectGroupMembers(
                topology,
                timeline.AuthoringId,
                targetTrackAuthoringId,
                destination);
        }
    }
}
