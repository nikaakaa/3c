using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    abstract class CharacterPoseReadOnlyPanel :
        IGraphAuthoringReadOnlyPanel
    {
        protected readonly ScrollView Content =
            new ScrollView(ScrollViewMode.Vertical);
        protected IGraphAuthoringDocumentProjection Document;

        protected CharacterPoseReadOnlyPanel()
        {
            Content.AddToClassList("pose-read-only-panel");
        }

        public VisualElement View => Content;

        public virtual void Bind(
            IGraphAuthoringDocumentProjection document)
        {
            Document = document ??
                throw new ArgumentNullException(nameof(document));
        }

        public abstract void Refresh();

        public virtual void Unbind()
        {
            Document = null;
            Content.Clear();
        }

        protected static void AddValue(
            VisualElement parent,
            string label,
            string value)
        {
            var row = new VisualElement();
            row.AddToClassList("pose-applied-row");
            var name = new Label(label);
            name.AddToClassList("pose-applied-label");
            var content = new Label(value ?? string.Empty);
            content.AddToClassList("pose-applied-value");
            row.Add(name);
            row.Add(content);
            parent.Add(row);
        }

        protected static void AddStatus(
            VisualElement parent,
            string status)
        {
            parent.Add(new HelpBox(
                status,
                status.StartsWith("Stale", StringComparison.Ordinal)
                    ? HelpBoxMessageType.Warning
                    : HelpBoxMessageType.Info));
        }
    }

    sealed class CharacterPosePreviewViewport :
        CharacterPoseReadOnlyPanel
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly VisualElement m_PreviewFrame = new VisualElement();
        readonly IMGUIContainer m_PreviewRender = new IMGUIContainer();
        readonly DropdownField m_TargetField =
            new DropdownField("Target");
        readonly FloatField m_TimeField =
            new FloatField("Seek Time");
        readonly Toggle m_GroundedField =
            new Toggle("Grounded") { value = true };
        readonly FloatField m_HorizontalSpeedField =
            new FloatField("Horizontal Speed");
        readonly FloatField m_AccelerationField =
            new FloatField("Horizontal Acceleration");
        readonly FloatField m_VerticalSpeedField =
            new FloatField("Vertical Speed");
        readonly Vector2Field m_MovementDirectionField =
            new Vector2Field("Movement Direction");
        readonly Vector2Field m_DesiredDirectionField =
            new Vector2Field("Desired Direction");
        readonly FloatField m_FacingErrorField =
            new FloatField("Facing Error");
        readonly EnumField m_MotionPhaseField =
            new EnumField(
                "Movement State",
                CharacterPresentationMotionPhase.GroundedStationary);
        readonly VisualElement m_TransportControls =
            new VisualElement();
        readonly Foldout m_ParameterFixture =
            new Foldout
            {
                text = "Capability Parameters",
                value = false
            };
        readonly Label m_Status = new Label();
        readonly List<PoseParameterId> m_ParameterIds =
            new List<PoseParameterId>();
        readonly List<float> m_ParameterValues =
            new List<float>();
        CharacterPipelineHost m_Target;
        CharacterAnimationPreviewFixtureSession m_FixtureSession;
        CharacterAnimationPreviewFixture m_SelectedFixture;
        readonly List<CharacterAnimationPreviewFixture> m_FixtureChoices =
            new List<CharacterAnimationPreviewFixture>();
        readonly List<CharacterPipelineHost> m_LiveChoices =
            new List<CharacterPipelineHost>();
        CharacterPoseTuningLayout m_TuningLayout;
        CharacterPoseTuningParameterBlock m_TuningBlock;
        long m_TargetChoicesRevision = long.MinValue;
        string m_TargetChoicesContextKey = string.Empty;
        readonly Foldout m_ViewportOverlay = new Foldout
        {
            text = "Debug Overlay",
            value = false
        };
        Guid m_SessionId;
        bool m_Playing;
        float m_Time;
        ulong m_Tick;
        double m_LastUpdate;
        string m_FixturePlanHash = string.Empty;
        readonly Foldout m_LinkedPoseOverrideFoldout =
            new Foldout
            {
                text = "Linked Pose Preview Override",
                value = false
            };
        readonly DropdownField m_LinkedPoseGroupField =
            new DropdownField("Group");
        readonly DropdownField m_LinkedPoseImplementationField =
            new DropdownField("Implementation");
        readonly Label m_LinkedPoseOverrideStatus = new Label();
        readonly List<CharacterLinkedPosePreviewGroupOption> m_LinkedPoseCatalog =
            new List<CharacterLinkedPosePreviewGroupOption>();
        string m_LinkedPoseCatalogKey = string.Empty;

        public CharacterPosePreviewViewport(
            CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ??
                throw new ArgumentNullException(nameof(window));
            m_PreviewFrame.AddToClassList("pose-preview-frame");
            m_PreviewRender.name = "pose-preview-render";
            m_PreviewRender.AddToClassList("pose-preview-render");
            m_PreviewRender.focusable = true;
            m_PreviewRender.onGUIHandler = DrawPreview;
            m_PreviewFrame.Add(m_PreviewRender);
            m_ViewportOverlay.AddToClassList("pose-preview-overlay");
            m_PreviewFrame.Add(m_ViewportOverlay);
            m_Status.AddToClassList("pose-preview-status");
            m_TargetField.AddToClassList("pose-target-selector");
            m_TargetField.RegisterValueChangedCallback(evt => SelectTarget(m_TargetField.index));
            m_TimeField.isDelayed = true;
            m_TimeField.RegisterValueChangedCallback(evt =>
                m_Time = Math.Max(0f, evt.newValue));
            m_LinkedPoseGroupField.RegisterValueChangedCallback(evt =>
                RefreshLinkedPoseImplementationChoices());
            m_LinkedPoseOverrideFoldout.Add(m_LinkedPoseGroupField);
            m_LinkedPoseOverrideFoldout.Add(m_LinkedPoseImplementationField);
            var linkedPoseControls = new VisualElement();
            linkedPoseControls.AddToClassList("pose-linked-pose-controls");
            linkedPoseControls.Add(new Button(ApplyLinkedPoseOverride)
            {
                text = "Apply Override"
            });
            linkedPoseControls.Add(new Button(ClearLinkedPoseOverride)
            {
                text = "Clear Override"
            });
            m_LinkedPoseOverrideFoldout.Add(linkedPoseControls);
            m_LinkedPoseOverrideFoldout.Add(m_LinkedPoseOverrideStatus);
            m_TransportControls.AddToClassList("pose-preview-transport");
            m_TransportControls.Add(new Button(PlayPreview) { text = "Play" });
            m_TransportControls.Add(new Button(PausePreview) { text = "Pause" });
            m_TransportControls.Add(new Button(StepPreview) { text = "Step" });
            m_TransportControls.Add(new Button(SeekPreview) { text = "Seek" });
            m_TransportControls.Add(new Button(RestartPreview) { text = "Restart" });
        }

        public DropdownField TargetField => m_TargetField;

        public override void Bind(
            IGraphAuthoringDocumentProjection document)
        {
            base.Bind(document);
            Content.Add(m_PreviewFrame);
            Content.Add(m_TimeField);
            Content.Add(m_GroundedField);
            Content.Add(m_HorizontalSpeedField);
            Content.Add(m_AccelerationField);
            Content.Add(m_VerticalSpeedField);
            Content.Add(m_MovementDirectionField);
            Content.Add(m_DesiredDirectionField);
            Content.Add(m_FacingErrorField);
            Content.Add(m_MotionPhaseField);
            Content.Add(m_ParameterFixture);
            Content.Add(m_LinkedPoseOverrideFoldout);
            Content.Add(m_TransportControls);
            Content.Add(m_Status);
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
            Refresh();
        }

        internal void Rebind(
            IGraphAuthoringDocumentProjection document)
        {
            if (Document == null)
            {
                Bind(document);
                return;
            }
            Document = document ??
                throw new ArgumentNullException(nameof(document));
            Refresh();
        }

        internal void RebuildCandidateAfterUndoRedo()
        {
            RefreshTuningState();
            if (!m_Target || m_TuningLayout == null || m_TuningBlock == null)
            {
                m_TuningBlock = null;
                m_Window.RefreshSelectedDetails();
                return;
            }
            if (!m_Window.TryGetPublishedProjection(
                    out CharacterPresentationProjection projection,
                    out string projectionError))
            {
                m_Target.ClearPoseTuningCandidate();
                m_TuningBlock = null;
                m_Status.text = projectionError;
                m_Window.RefreshSelectedDetails();
                return;
            }
            if (!CharacterPoseTuningAuthoringService.TryCompileCurrentBlock(
                    m_Window.AssetContext,
                    projection,
                    m_TuningLayout,
                    m_TuningBlock,
                    out CharacterPoseTuningParameterBlock block,
                    out string compileError))
            {
                m_Target.ClearPoseTuningCandidate();
                m_TuningBlock = null;
                m_Status.text = compileError;
                m_Window.RefreshSelectedDetails();
                return;
            }
            m_TuningBlock = block;
            string sourceRevision =
                m_Window.AssetContext?.Graph?.ContentRevision ??
                string.Empty;
            bool submitted = m_SelectedFixture != null
                ? m_Target.SubmitPreviewPoseTuningCandidate(
                    sourceRevision,
                    Guid.NewGuid().ToString("N"),
                    m_TuningBlock,
                    out string submitError)
                : m_Target.SubmitLivePoseTuningCandidate(
                    sourceRevision,
                    Guid.NewGuid().ToString("N"),
                    m_TuningBlock,
                    out submitError);
            if (!submitted)
            {
                m_Target.ClearPoseTuningCandidate();
                m_TuningBlock = null;
                m_Status.text = submitError;
                m_Window.RefreshSelectedDetails();
                return;
            }
            m_Window.MarkPoseTuningAuthoringChanged();
            m_Status.text =
                "Undo/Redo tuning queued · applies at the next target frame.";
            m_Window.RefreshSelectedDetails();
        }

        public override void Refresh()
        {
            RefreshTargetChoices();
            bool published = m_Window.TryGetPublishedPosePlan(
                out CharacterPresentationPosePlan plan,
                out string status);
            AddOrReplaceSummary(published ? "Ready" : status);
            if (published &&
                !string.Equals(
                    m_FixturePlanHash,
                    plan.PlanHash,
                    StringComparison.Ordinal))
            {
                RebuildParameterFixture(plan);
            }
            RefreshTuningState();
            RefreshViewportOverlay();
            RefreshLinkedPoseOverrideCatalog();
            RenderPreview();
        }

        public override void Unbind()
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            Stop(string.Empty);
            m_FixtureSession?.Dispose();
            m_FixtureSession = null;
            m_SelectedFixture = null;
            m_TargetChoicesRevision = long.MinValue;
            m_TargetChoicesContextKey = string.Empty;
            m_TargetField.SetValueWithoutNotify(null);
            m_TargetField.choices = new List<string>();
            m_Target = null;
            m_ParameterIds.Clear();
            m_ParameterValues.Clear();
            m_FixturePlanHash = string.Empty;
            m_TuningLayout = null;
            m_TuningBlock = null;
            m_ViewportOverlay.Clear();
            m_LinkedPoseCatalog.Clear();
            m_LinkedPoseCatalogKey = string.Empty;
            m_LinkedPoseGroupField.choices = new List<string>();
            m_LinkedPoseImplementationField.choices = new List<string>();
            m_LinkedPoseOverrideStatus.text = string.Empty;
            base.Unbind();
        }

        void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                Stop(string.Empty);
                m_FixtureSession?.Dispose();
                m_FixtureSession = null;
                m_Target = null;
                RenderPreview();
                return;
            }
            if (state != PlayModeStateChange.EnteredEditMode)
                return;
            m_TargetChoicesRevision = long.MinValue;
            RefreshTargetChoices();
            int fixtureIndex = m_FixtureChoices.IndexOf(
                m_SelectedFixture);
            if (fixtureIndex >= 0)
                SelectTarget(fixtureIndex);
        }

        internal IReadOnlyList<GraphAuthoringReadOnlyDetail> GetAppliedValues(
            GraphAuthoringSelection selection)
        {
            var rows = new List<GraphAuthoringReadOnlyDetail>();
            bool preview = m_SelectedFixture != null;
            CharacterPoseTuningLayout layout = preview
                ? m_Target?.PreviewTuningLayout
                : m_Target?.LiveTuningLayout;
            CharacterPoseTuningParameterBlock block = preview
                ? m_Target?.PreviewActiveTuningBlock
                : m_Target?.LiveActiveTuningBlock;
            CharacterPoseTuningRuntimeState state = preview
                ? m_Target?.PreviewTuningState ?? default
                : m_Target?.LiveTuningState ?? default;
            rows.Add(new GraphAuthoringReadOnlyDetail(
                "Applied Target",
                m_Target ? $"{(preview ? "Preview Instance" : "Live Actor")} · {m_Target.name}" : "No Target"));
            rows.Add(new GraphAuthoringReadOnlyDetail("Status", state.Status.ToString()));
            rows.Add(new GraphAuthoringReadOnlyDetail("Applied Frame", state.AppliedFrame.ToString()));
            if (!m_Target || layout == null || block == null)
                return rows;
            var owners = new HashSet<string>(StringComparer.Ordinal)
            {
                $"pose-node:{selection.ElementId.Value}"
            };
            string fieldPrefix = string.Empty;
            if (selection.Kind == GraphAuthoringSelectionKind.Transition)
            {
                if (!string.IsNullOrEmpty(m_Window.CurrentStateMachineId))
                    owners.Add($"pose-state-machine:{m_Window.CurrentStateMachineId}");
                fieldPrefix = $"/transition:{selection.ElementId.Value}/";
            }
            if (m_Window.TryGetPublishedPosePlan(
                    out CharacterPresentationPosePlan plan,
                    out _))
            {
                for (int i = 0; i < plan.FullBodyIks.Count; i++)
                    if (plan.FullBodyIks[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"full-body-ik-profile:{plan.FullBodyIks[i].ProfileId}");
                for (int i = 0; i < plan.PredictiveFootPlacements.Count; i++)
                    if (plan.PredictiveFootPlacements[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"foot-placement-profile:{plan.PredictiveFootPlacements[i].Profile.ProfileId}");
                for (int i = 0; i < plan.BlendNodes.Count; i++)
                    if (plan.BlendNodes[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"animation-blend-policy:{plan.BlendNodes[i].PolicyId}");
                for (int i = 0; i < plan.Inertializations.Count; i++)
                    if (plan.Inertializations[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"pose-inertialization-policy:{plan.Inertializations[i].PolicyId}");
            }
            int valueCount = 0;
            for (int i = 0; i < layout.Entries.Count; i++)
            {
                CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                if (!owners.Contains(entry.OwnerId))
                    continue;
                if (!string.IsNullOrEmpty(fieldPrefix) &&
                    entry.FieldId.IndexOf(fieldPrefix, StringComparison.Ordinal) < 0)
                    continue;
                rows.Add(new GraphAuthoringReadOnlyDetail(
                    entry.DisplayName,
                    FormatTuningValue(block, entry),
                    entry.ApplyTiming == CharacterPoseTuningApplyTiming.NextActivation
                        ? "Next Activation"
                        : "Live Now"));
                valueCount++;
            }
            if (valueCount == 0)
                rows.Add(new GraphAuthoringReadOnlyDetail(
                    "Values",
                    "No compiled tuning field belongs to this element."));
            if (!string.IsNullOrEmpty(state.RejectionReason))
                rows.Add(new GraphAuthoringReadOnlyDetail("Rejected", state.RejectionReason));
            return rows;
        }


        public bool TryGetSnapshot(
            out AnimationPresentationRuntimeSnapshot snapshot,
            out string status)
        {
            snapshot = default;
            if (!m_Target || m_SessionId == Guid.Empty ||
                !m_Target.HasPreviewAnimationDebugView)
            {
                status =
                    "Unavailable: no completed explicit Preview frame.";
                return false;
            }
            snapshot = m_Target.PreviewAnimationDebugView.PosePlan;
            if (!m_Window.MatchesCurrentPublishedRevision(snapshot))
            {
                snapshot = default;
                status =
                    "Stale: Preview Pose Graph or Projection revision does not match this document.";
                return false;
            }
            status = "Preview";
            return true;
        }

        void RefreshLinkedPoseOverrideCatalog()
        {
            string catalogKey =
                $"{m_Window.ProjectionContext?.ProjectionRevision}|{EditorUtility.IsDirty(m_Window.ProfileContext)}";
            if (string.Equals(catalogKey, m_LinkedPoseCatalogKey, StringComparison.Ordinal))
                return;
            m_LinkedPoseCatalogKey = catalogKey;
            if (!m_Window.TryGetCompiledLinkedPosePreviewCatalog(
                    out IReadOnlyList<CharacterLinkedPosePreviewGroupOption> catalog,
                    out string status))
            {
                m_LinkedPoseOverrideFoldout.style.display = DisplayStyle.None;
                m_LinkedPoseCatalog.Clear();
                m_LinkedPoseGroupField.choices = new List<string>();
                m_LinkedPoseImplementationField.choices = new List<string>();
                m_LinkedPoseOverrideStatus.text = status;
                return;
            }
            m_LinkedPoseCatalog.Clear();
            m_LinkedPoseCatalog.AddRange(catalog);
            m_LinkedPoseGroupField.choices = m_LinkedPoseCatalog
                .Select(value => value.DisplayName)
                .ToList();
            if (m_LinkedPoseCatalog.Count == 0)
            {
                m_LinkedPoseOverrideFoldout.style.display = DisplayStyle.None;
                m_LinkedPoseImplementationField.choices = new List<string>();
                m_LinkedPoseOverrideStatus.text = status;
                return;
            }
            m_LinkedPoseOverrideFoldout.style.display = DisplayStyle.Flex;
            int groupIndex = Math.Max(0, Math.Min(
                m_LinkedPoseGroupField.index,
                m_LinkedPoseCatalog.Count - 1));
            m_LinkedPoseGroupField.SetValueWithoutNotify(
                m_LinkedPoseCatalog[groupIndex].DisplayName);
            RefreshLinkedPoseImplementationChoices();
            m_LinkedPoseOverrideStatus.text = status;
        }

        void RefreshLinkedPoseImplementationChoices()
        {
            if (m_LinkedPoseCatalog.Count == 0)
                return;
            int groupIndex = m_LinkedPoseCatalog.FindIndex(
                value => string.Equals(
                    value.DisplayName,
                    m_LinkedPoseGroupField.value,
                    StringComparison.Ordinal));
            if (groupIndex < 0)
                groupIndex = 0;
            CharacterLinkedPosePreviewGroupOption group =
                m_LinkedPoseCatalog[groupIndex];
            m_LinkedPoseImplementationField.choices = group.Implementations
                .Select(value => value.name)
                .ToList();
            if (group.Implementations.Count > 0)
                m_LinkedPoseImplementationField.SetValueWithoutNotify(
                    group.Implementations[0].name);
            m_LinkedPoseGroupField.SetValueWithoutNotify(group.DisplayName);
            bool enabled = group.SupportsPreview &&
                           group.Implementations.Count > 0 &&
                           !m_Window.IsLinkedPoseReadOnly;
            m_LinkedPoseImplementationField.SetEnabled(enabled);
        }

        bool TryGetSelectedLinkedPoseOverride(
            out CharacterLinkedPosePreviewGroupOption group,
            out CharacterLinkedPoseImplementationAsset implementation,
            out string error)
        {
            group = null;
            implementation = null;
            error = string.Empty;
            int groupIndex = m_LinkedPoseCatalog.FindIndex(
                value => string.Equals(
                    value.DisplayName,
                    m_LinkedPoseGroupField.value,
                    StringComparison.Ordinal));
            if (groupIndex < 0)
            {
                error = "Unavailable: select a compiled Linked Pose Group.";
                return false;
            }
            group = m_LinkedPoseCatalog[groupIndex];
            if (!group.SupportsPreview)
            {
                error = "Unavailable: this selector capability has no formal preview adapter.";
                return false;
            }
            int implementationIndex = group.Implementations
                .Select(value => value.name)
                .ToList()
                .IndexOf(m_LinkedPoseImplementationField.value);
            if (implementationIndex < 0)
            {
                error = "Unavailable: select a compiled candidate Implementation.";
                return false;
            }
            implementation = group.Implementations[implementationIndex];
            return true;
        }

        void ApplyLinkedPoseOverride()
        {
            if (m_Window.IsLinkedPoseReadOnly)
            {
                m_LinkedPoseOverrideStatus.text = "Unavailable: Live Debug is read-only.";
                return;
            }
            if (!TryGetContext(out CharacterPipelineHost target, out string error))
            {
                m_LinkedPoseOverrideStatus.text = error;
                return;
            }
            if (!TryGetSelectedLinkedPoseOverride(
                    out CharacterLinkedPosePreviewGroupOption group,
                    out CharacterLinkedPoseImplementationAsset implementation,
                    out error))
            {
                m_LinkedPoseOverrideStatus.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            try
            {
                target.SetLinkedPosePreviewOverride(
                    m_SessionId,
                    group.GroupId,
                    implementation.ImplementationId);
                Evaluate(target, 0f, false);
                m_LinkedPoseOverrideStatus.text =
                    $"Preview override active: {group.DisplayName} → {implementation.name}.";
            }
            catch (Exception exception)
            {
                m_LinkedPoseOverrideStatus.text = exception.Message;
            }
        }

        void ClearLinkedPoseOverride()
        {
            if (!m_Target || m_SessionId == Guid.Empty)
            {
                m_LinkedPoseOverrideStatus.text = "Preview override cleared.";
                return;
            }
            if (!TryGetSelectedLinkedPoseOverride(
                    out CharacterLinkedPosePreviewGroupOption group,
                    out _,
                    out string error))
            {
                m_LinkedPoseOverrideStatus.text = error;
                return;
            }
            m_Target.ClearLinkedPosePreviewOverride(m_SessionId, group.GroupId);
            if (TryGetContext(out CharacterPipelineHost target, out _))
                Evaluate(target, 0f, false);
            m_LinkedPoseOverrideStatus.text = "Preview override cleared.";
        }

        void RebuildParameterFixture(
            CharacterPresentationPosePlan plan)
        {
            m_ParameterFixture.Clear();
            m_ParameterIds.Clear();
            m_ParameterValues.Clear();
            m_FixturePlanHash = plan.PlanHash;
            var indices = new SortedSet<int>();
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.ProgramParameterInput &&
                    operation.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                    continue;
                if (operation.ParameterIndex >= 0)
                    indices.Add(operation.ParameterIndex);
                if (operation.ParameterIndexB >= 0)
                    indices.Add(operation.ParameterIndexB);
            }
            foreach (int index in indices)
            {
                if ((uint)index >= (uint)plan.Parameters.Count)
                    throw new InvalidOperationException(
                        $"Pose Preview capability parameter index '{index}' is outside the compiled parameter table.");
                CharacterPresentationPoseParameterEntry parameter = plan.Parameters[index];
                if (!CharacterPresentationProgramParameterFrame.Supports(parameter.ParameterId))
                    continue;
                int valueIndex = m_ParameterValues.Count;
                m_ParameterIds.Add(parameter.ParameterId);
                m_ParameterValues.Add(parameter.DefaultValue);
                string label = string.IsNullOrEmpty(parameter.Unit)
                    ? parameter.ParameterId.Value
                    : $"{parameter.ParameterId.Value} ({parameter.Unit})";
                switch (parameter.ValueType)
                {
                    case PoseParameterValueType.Float:
                    {
                        var field = new FloatField(label)
                        {
                            value = parameter.DefaultValue
                        };
                        field.RegisterValueChangedCallback(evt =>
                            m_ParameterValues[valueIndex] = evt.newValue);
                        m_ParameterFixture.Add(field);
                        break;
                    }
                    case PoseParameterValueType.Int:
                    {
                        var field = new IntegerField(label)
                        {
                            value = Mathf.RoundToInt(parameter.DefaultValue)
                        };
                        field.RegisterValueChangedCallback(evt =>
                            m_ParameterValues[valueIndex] = evt.newValue);
                        m_ParameterFixture.Add(field);
                        break;
                    }
                    case PoseParameterValueType.Bool:
                    {
                        var field = new Toggle(label)
                        {
                            value = parameter.DefaultValue > 0.5f
                        };
                        field.RegisterValueChangedCallback(evt =>
                            m_ParameterValues[valueIndex] = evt.newValue ? 1f : 0f);
                        m_ParameterFixture.Add(field);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"Pose Preview parameter '{parameter.ParameterId}' has unsupported type '{parameter.ValueType}'.");
                }
            }
            if (m_ParameterIds.Count == 0)
                m_ParameterFixture.Add(
                    new Label("This graph has no capability-registered direct parameters."));
        }

        void SetTarget(CharacterPipelineHost target)
        {
            if (ReferenceEquals(m_Target, target))
                return;
            Stop(string.Empty);
            m_Target = target;
            m_TuningLayout = null;
            m_TuningBlock = null;
            SetPreviewInputsEnabled(m_SelectedFixture != null);
            Refresh();
        }

        void SetPreviewInputsEnabled(bool enabled)
        {
            m_TimeField.SetEnabled(enabled);
            m_GroundedField.SetEnabled(enabled);
            m_HorizontalSpeedField.SetEnabled(enabled);
            m_AccelerationField.SetEnabled(enabled);
            m_VerticalSpeedField.SetEnabled(enabled);
            m_MovementDirectionField.SetEnabled(enabled);
            m_DesiredDirectionField.SetEnabled(enabled);
            m_FacingErrorField.SetEnabled(enabled);
            m_MotionPhaseField.SetEnabled(enabled);
            m_ParameterFixture.SetEnabled(enabled);
            m_TransportControls.SetEnabled(enabled);
        }

        void RefreshTargetChoices()
        {
            long targetRevision = RuntimeDebugSession.Shared.TargetRevision;
            string previousContextKey = m_TargetChoicesContextKey;
            bool published = m_Window.TryGetPublishedPosePlan(
                out CharacterPresentationPosePlan publishedPlan,
                out string publishedStatus);
            string contextKey = string.Join(
                "|",
                m_Window.DefinitionContext
                    ? m_Window.DefinitionContext.GetInstanceID().ToString()
                    : string.Empty,
                m_Window.ProfileContext
                    ? m_Window.ProfileContext.GetInstanceID().ToString()
                    : string.Empty,
                m_Window.ProjectionContext?.ProjectionRevision ?? string.Empty,
                published ? publishedPlan.PlanHash : publishedStatus);
            if (m_TargetChoicesRevision == targetRevision &&
                string.Equals(
                    m_TargetChoicesContextKey,
                    contextKey,
                    StringComparison.Ordinal) &&
                m_TargetField.choices != null)
                return;
            m_TargetChoicesRevision = targetRevision;
            m_TargetChoicesContextKey = contextKey;

            m_FixtureChoices.Clear();
            foreach (CharacterAnimationPreviewFixture fixture in
                     CharacterAnimationPreviewFixtureCatalog.Load())
            {
                if (fixture &&
                    fixture.Definition == m_Window.DefinitionContext &&
                    fixture.Profile == m_Window.ProfileContext)
                    m_FixtureChoices.Add(fixture);
            }

            m_LiveChoices.Clear();
            foreach (RuntimeDebugTargetInfo info in RuntimeDebugSession.Shared.Targets)
            {
                CharacterPipelineHost host =
                    EditorUtility.InstanceIDToObject(info.HostInstanceId)
                    as CharacterPipelineHost;
                if (!published ||
                    !host ||
                    host.Definition == null ||
                    host.Definition != m_Window.DefinitionContext ||
                    host.Definition.AnimationPresentationProfile !=
                        m_Window.ProfileContext ||
                    host.Registration == null ||
                    !host.Registration.SourceMapRevision.Equals(info.Revision) ||
                    host.Registration.Projection == null ||
                    !string.Equals(
                        host.Registration.Projection.ProjectionRevision,
                        m_Window.ProjectionContext?.ProjectionRevision,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        host.Registration.Projection.PosePlan.PlanHash,
                        publishedPlan.PlanHash,
                        StringComparison.Ordinal))
                    continue;
                m_LiveChoices.Add(host);
            }

            var choices = new List<string>(
                m_FixtureChoices.Count + m_LiveChoices.Count);
            choices.AddRange(
                m_FixtureChoices.Select(value =>
                    $"Preview Instance · {value.name}"));
            choices.AddRange(
                m_LiveChoices.Select(value =>
                    $"Live Actor · {value.name}"));
            m_TargetField.choices = choices;
            if (!m_Target && m_SelectedFixture == null && m_FixtureChoices.Count > 0)
            {
                m_TargetField.SetValueWithoutNotify(choices[0]);
                SelectTarget(0);
                return;
            }

            bool contextChanged =
                !string.IsNullOrEmpty(previousContextKey) &&
                !string.Equals(
                    previousContextKey,
                    contextKey,
                    StringComparison.Ordinal);
            bool targetStillAvailable = m_SelectedFixture
                ? m_FixtureChoices.Contains(m_SelectedFixture)
                : m_Target && m_LiveChoices.Contains(m_Target);
            if (contextChanged ||
                (m_Target && !targetStillAvailable))
            {
                Stop(string.Empty);
                m_FixtureSession?.Dispose();
                m_FixtureSession = null;
                m_SelectedFixture = null;
                SetTarget(null);
                return;
            }

            int selected = -1;
            if (m_SelectedFixture)
                selected = m_FixtureChoices.IndexOf(m_SelectedFixture);
            else if (m_Target)
            {
                int liveIndex = m_LiveChoices.IndexOf(m_Target);
                if (liveIndex >= 0)
                    selected = m_FixtureChoices.Count + liveIndex;
            }
            if (selected >= 0 && selected < choices.Count)
                m_TargetField.SetValueWithoutNotify(choices[selected]);
        }

        void SelectTarget(int index)
        {
            if (index < 0)
                return;
            if (index < m_FixtureChoices.Count)
            {
                CharacterAnimationPreviewFixture fixture =
                    m_FixtureChoices[index];
                if (m_SelectedFixture == fixture &&
                    m_FixtureSession != null)
                    return;
                Stop(string.Empty);
                m_FixtureSession?.Dispose();
                m_FixtureSession = null;
                m_SelectedFixture = fixture;
                try
                {
                    m_FixtureSession =
                        CharacterAnimationPreviewFixtureSession.Create(fixture);
                    SetTarget(m_FixtureSession.Target);
                    RenderPreview();
                }
                catch (Exception exception)
                {
                    m_SelectedFixture = null;
                    m_Status.text = exception.Message;
                    SetTarget(null);
                }
                return;
            }

            int liveIndex = index - m_FixtureChoices.Count;
            if ((uint)liveIndex >= (uint)m_LiveChoices.Count)
                return;
            Stop(string.Empty);
            m_FixtureSession?.Dispose();
            m_FixtureSession = null;
            m_SelectedFixture = null;
            SetTarget(m_LiveChoices[liveIndex]);
        }

        void RefreshTuningState()
        {
            CharacterPoseTuningLayout layout = m_SelectedFixture != null
                ? m_Target?.PreviewTuningLayout
                : m_Target?.LiveTuningLayout;
            CharacterPoseTuningParameterBlock source = m_SelectedFixture != null
                ? m_Target?.PreviewActiveTuningBlock
                : m_Target?.LiveActiveTuningBlock;
            if (layout == null)
            {
                if (!m_Window.TryGetPublishedProjection(
                        out CharacterPresentationProjection projection,
                        out _))
                {
                    m_TuningLayout = null;
                    m_TuningBlock = null;
                    return;
                }
                layout = projection.TuningLayout;
                source = projection.TuningDefaultBlock;
            }
            if (layout == null || source == null)
            {
                m_TuningLayout = null;
                m_TuningBlock = null;
                return;
            }
            if (!CharacterPoseTuningAuthoringService.TryCompileCurrentBlock(
                    m_Window.AssetContext,
                    m_Window.TryGetPublishedProjection(
                        out CharacterPresentationProjection currentProjection,
                        out _)
                        ? currentProjection
                        : null,
                    layout,
                    source,
                    out CharacterPoseTuningParameterBlock block,
                    out string error))
            {
                m_TuningLayout = null;
                m_TuningBlock = null;
                m_Status.text = error;
                return;
            }

            m_TuningLayout = layout;
            m_TuningBlock = block;
        }

        static string FormatTuningValue(
            CharacterPoseTuningParameterBlock block,
            CharacterPoseTuningLayoutEntry entry)
        {
            CharacterPoseTuningValue value = block.GetValue(entry);
            switch (entry.ValueKind)
            {
                case CharacterPoseTuningValueKind.Float:
                    return value.FloatValue.ToString("0.###");
                case CharacterPoseTuningValueKind.Integer:
                    return value.IntegerValue.ToString();
                case CharacterPoseTuningValueKind.Boolean:
                    return value.BooleanValue ? "On" : "Off";
                case CharacterPoseTuningValueKind.Enum:
                    return value.EnumValue.ToString();
                default:
                    return string.Empty;
            }
        }

        bool SubmitTuningValue(
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (m_TuningLayout == null || m_TuningBlock == null)
                return false;
            try
            {
                CharacterPoseTuningParameterBlock nextBlock =
                    CharacterPoseTuningCandidateCompiler.CompileBlock(
                        m_TuningLayout,
                        m_TuningBlock,
                        entry,
                        value);
                if (!CharacterPoseTuningAuthoringService.TryApply(
                        m_Window.AssetContext,
                        m_Window.ProfileContext,
                        entry,
                        value,
                        out string authoringError))
                {
                    m_Status.text = authoringError;
                    return true;
                }

                m_TuningBlock = nextBlock;
                m_Window.MarkPoseTuningAuthoringChanged();
                if (!m_Target)
                {
                    m_Status.text = "Saved · No Preview or Live target is selected.";
                    return true;
                }
                string sourceRevision =
                    m_Window.AssetContext?.Graph?.ContentRevision ??
                    string.Empty;
                bool submitted;
                string error;
                if (m_SelectedFixture != null)
                    submitted = m_Target.SubmitPreviewPoseTuningCandidate(
                        sourceRevision,
                        Guid.NewGuid().ToString("N"),
                        m_TuningBlock,
                        out error);
                else
                    submitted = m_Target.SubmitLivePoseTuningCandidate(
                        sourceRevision,
                        Guid.NewGuid().ToString("N"),
                        m_TuningBlock,
                        out error);
                m_Status.text = submitted
                    ? entry.ApplyTiming == CharacterPoseTuningApplyTiming.NextActivation
                        ? "Saved · queued for the next activation."
                        : "Saved · applies on the next frame."
                    : error;
                return true;
            }
            catch (Exception exception)
            {
                m_Status.text = exception.Message;
                return true;
            }
        }

        internal bool TryApplySelectionTuning(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request)
        {
            if (request.Kind != GraphAuthoringMutationKind.SetField ||
                m_TuningLayout == null ||
                m_TuningBlock == null)
                return false;
            CharacterPoseTuningLayoutEntry entry = null;
            if (document is CharacterTypedPoseGraphDocument)
            {
                string ownerId =
                    $"pose-node:{request.TargetId.Value}";
                string fieldId =
                    $"{ownerId}/{request.FieldId.Value}";
                entry = m_TuningLayout.Entries.SingleOrDefault(value =>
                    value.OwnerId == ownerId &&
                    value.FieldId == fieldId &&
                    value.Interaction ==
                        CharacterPoseTuningInteractionPolicy.TunableDefault);
            }
            else if (document is CharacterPoseStateMachineDocument machine &&
                     request.FieldId.Value == "duration-seconds")
            {
                string ownerId =
                    $"pose-state-machine:{machine.DocumentId}";
                string fieldId =
                    $"{ownerId}/transition:{request.TargetId.Value}/duration";
                entry = m_TuningLayout.Entries.SingleOrDefault(value =>
                    value.OwnerId == ownerId &&
                    value.FieldId == fieldId &&
                    value.Interaction ==
                        CharacterPoseTuningInteractionPolicy.TunableDefault);
            }
            if (entry == null)
                return false;
            CharacterPoseTuningValue value = ToTuningValue(
                entry,
                request.Value);
            bool handled = SubmitTuningValue(entry, value);
            if (handled)
                m_Window.RefreshSelectedDetails();
            return handled;
        }

        internal bool PopulateSelectionTuning(
            GraphAuthoringSelection? selection,
            VisualElement host)
        {
            if (host == null)
                return false;
            host.Clear();
            host.style.display = DisplayStyle.None;
            if (!selection.HasValue)
                return false;
            RefreshTuningState();
            if (m_TuningLayout == null ||
                m_TuningBlock == null)
                return false;
            GraphAuthoringSelection current = selection.Value;
            var owners = new HashSet<string>(StringComparer.Ordinal);
            if (current.Kind == GraphAuthoringSelectionKind.Node)
            {
                owners.Add($"pose-node:{current.ElementId.Value}");
                if (m_Window.TryGetPublishedPosePlan(
                        out CharacterPresentationPosePlan plan,
                        out _))
                {
                    for (int i = 0; i < plan.FullBodyIks.Count; i++)
                        if (plan.FullBodyIks[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"full-body-ik-profile:{plan.FullBodyIks[i].ProfileId}");
                    for (int i = 0;
                         i < plan.PredictiveFootPlacements.Count;
                         i++)
                        if (plan.PredictiveFootPlacements[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"foot-placement-profile:{plan.PredictiveFootPlacements[i].Profile.ProfileId}");
                    for (int i = 0; i < plan.BlendNodes.Count; i++)
                        if (plan.BlendNodes[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"animation-blend-policy:{plan.BlendNodes[i].PolicyId}");
                    for (int i = 0;
                         i < plan.Inertializations.Count;
                         i++)
                        if (plan.Inertializations[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"pose-inertialization-policy:{plan.Inertializations[i].PolicyId}");
                }
            }
            string fieldPrefix = string.Empty;
            if (current.Kind == GraphAuthoringSelectionKind.Transition &&
                !string.IsNullOrEmpty(m_Window.CurrentStateMachineId))
            {
                owners.Add(
                    $"pose-state-machine:{m_Window.CurrentStateMachineId}");
                fieldPrefix =
                    $"/transition:{current.ElementId.Value}/";
            }
            CharacterPoseTuningLayoutEntry[] entries =
                m_TuningLayout.Entries
                    .Where(value =>
                        owners.Contains(value.OwnerId) &&
                        (string.IsNullOrEmpty(fieldPrefix) ||
                         value.FieldId.IndexOf(
                             fieldPrefix,
                             StringComparison.Ordinal) >= 0) &&
                        value.Interaction ==
                            CharacterPoseTuningInteractionPolicy.TunableDefault)
                    .OrderBy(value => value.DisplayName,
                        StringComparer.Ordinal)
                    .ToArray();
            if (entries.Length == 0)
                return false;
            host.style.display = DisplayStyle.Flex;
            var foldout = new Foldout
            {
                text = "Live Tuning",
                value = true
            };
            foldout.Add(new Label(m_Target
                ? $"{(m_SelectedFixture != null ? "Preview" : "Live Actor")} · {CurrentTuningState().Status}"
                : "No Target · edits are saved to the formal owner."));
            for (int i = 0; i < entries.Length; i++)
                foldout.Add(CreateSelectionTuningField(entries[i]));
            host.Add(foldout);
            return true;
        }

        VisualElement CreateSelectionTuningField(
            CharacterPoseTuningLayoutEntry entry)
        {
            string timing = entry.ApplyTiming ==
                            CharacterPoseTuningApplyTiming.NextActivation
                ? "Next Activation"
                : "Live Now";
            string label = string.IsNullOrEmpty(entry.Unit)
                ? entry.DisplayName
                : $"{entry.DisplayName} ({entry.Unit})";
            var row = new VisualElement();
            row.AddToClassList("pose-tuning-row");
            VisualElement authoringField;
            switch (entry.ValueKind)
            {
                case CharacterPoseTuningValueKind.Float:
                {
                    var field = new FloatField(label)
                    {
                        value = m_TuningBlock.Floats[entry.ValueIndex],
                        isDelayed = true
                    };
                    field.RegisterValueChangedCallback(evt =>
                        SubmitSelectionTuningValue(
                            entry,
                            CharacterPoseTuningValue.Float(
                                evt.newValue)));
                    authoringField = field;
                    break;
                }
                case CharacterPoseTuningValueKind.Integer:
                case CharacterPoseTuningValueKind.Enum:
                {
                    int current = entry.ValueKind ==
                                  CharacterPoseTuningValueKind.Integer
                        ? m_TuningBlock.Integers[entry.ValueIndex]
                        : m_TuningBlock.Enums[entry.ValueIndex];
                    var field = new IntegerField(label)
                    {
                        value = current,
                        isDelayed = true
                    };
                    field.RegisterValueChangedCallback(evt =>
                        SubmitSelectionTuningValue(
                            entry,
                            entry.ValueKind ==
                                CharacterPoseTuningValueKind.Integer
                                ? CharacterPoseTuningValue.Integer(
                                    evt.newValue)
                                : CharacterPoseTuningValue.Enum(
                                    evt.newValue)));
                    authoringField = field;
                    break;
                }
                case CharacterPoseTuningValueKind.Boolean:
                {
                    var field = new Toggle(label)
                    {
                        value =
                            m_TuningBlock.Booleans[entry.ValueIndex] != 0
                    };
                    field.RegisterValueChangedCallback(evt =>
                        SubmitSelectionTuningValue(
                            entry,
                            CharacterPoseTuningValue.Boolean(
                                evt.newValue)));
                    authoringField = field;
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Pose tuning field '{entry.FieldId}' has an unsupported value kind.");
            }
            authoringField.AddToClassList("pose-tuning-authoring-field");
            row.Add(authoringField);
            var applied = new Label(CurrentAppliedValue(entry));
            applied.tooltip = "Current value applied by the selected runtime target.";
            applied.AddToClassList("pose-tuning-applied-value");
            row.Add(applied);
            var status = new Label(timing);
            status.AddToClassList("pose-tuning-apply-status");
            row.Add(status);
            return row;
        }

        void SubmitSelectionTuningValue(
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (SubmitTuningValue(entry, value))
                m_Window.RefreshSelectedDetails();
        }

        CharacterPoseTuningRuntimeState CurrentTuningState() =>
            !m_Target
                ? default
                : m_SelectedFixture != null
                    ? m_Target.PreviewTuningState
                    : m_Target.LiveTuningState;

        string CurrentAppliedValue(CharacterPoseTuningLayoutEntry entry)
        {
            if (!m_Target)
                return "No Target";
            CharacterPoseTuningParameterBlock block = m_SelectedFixture != null
                ? m_Target.PreviewActiveTuningBlock
                : m_Target.LiveActiveTuningBlock;
            return block == null
                ? "Unavailable"
                : FormatTuningValue(block, entry);
        }

        static CharacterPoseTuningValue ToTuningValue(
            CharacterPoseTuningLayoutEntry entry,
            object value) => entry.ValueKind switch
        {
            CharacterPoseTuningValueKind.Float =>
                CharacterPoseTuningValue.Float(
                    Convert.ToSingle(value)),
            CharacterPoseTuningValueKind.Integer =>
                CharacterPoseTuningValue.Integer(
                    Convert.ToInt32(value)),
            CharacterPoseTuningValueKind.Boolean =>
                CharacterPoseTuningValue.Boolean(
                    Convert.ToBoolean(value)),
            CharacterPoseTuningValueKind.Enum =>
                CharacterPoseTuningValue.Enum(
                    Convert.ToInt32(value)),
            _ => throw new InvalidOperationException(
                $"Pose tuning field '{entry.FieldId}' has an unsupported value kind.")
        };

        void PlayPreview()
        {
            if (!TryGetContext(
                    out CharacterPipelineHost target,
                    out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = true;
            m_LastUpdate = EditorApplication.timeSinceStartup;
            Evaluate(target, 0f, false);
        }

        void PausePreview()
        {
            m_Playing = false;
            m_Status.text = m_SessionId == Guid.Empty
                ? "Preview stopped."
                : $"Paused at {m_Time:0.###}s.";
        }

        void StepPreview()
        {
            if (!TryGetContext(
                    out CharacterPipelineHost target,
                    out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = false;
            Evaluate(target, 1f / 60f, false);
        }

        void SeekPreview()
        {
            if (!TryGetContext(
                    out CharacterPipelineHost target,
                    out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = false;
            Evaluate(target, 0f, true);
        }

        void RestartPreview()
        {
            Stop(string.Empty);
            m_Time = 0f;
            m_Tick = 0;
            m_TimeField.SetValueWithoutNotify(0f);
            m_GroundedField.SetValueWithoutNotify(true);
            m_HorizontalSpeedField.SetValueWithoutNotify(0f);
            m_AccelerationField.SetValueWithoutNotify(0f);
            m_VerticalSpeedField.SetValueWithoutNotify(0f);
            m_MovementDirectionField.SetValueWithoutNotify(Vector2.zero);
            m_DesiredDirectionField.SetValueWithoutNotify(Vector2.zero);
            m_FacingErrorField.SetValueWithoutNotify(0f);
            m_MotionPhaseField.SetValueWithoutNotify(
                CharacterPresentationMotionPhase.GroundedStationary);
            Refresh();
        }

        void Update()
        {
            if (!m_Playing)
                return;
            double now = EditorApplication.timeSinceStartup;
            float delta = Mathf.Clamp(
                (float)(now - m_LastUpdate),
                0f,
                0.1f);
            m_LastUpdate = now;
            if (!TryGetContext(
                    out CharacterPipelineHost target,
                    out string error))
            {
                Stop(error);
                return;
            }
            Evaluate(target, delta, false);
        }

        void Evaluate(
            CharacterPipelineHost target,
            float delta,
            bool reset)
        {
            if (!reset)
                m_Time += Math.Max(0f, delta);
            m_TimeField.SetValueWithoutNotify(m_Time);
            try
            {
                target.EvaluatePoseGraphPreview(
                    m_SessionId,
                    m_Time,
                    ++m_Tick,
                    Math.Max(0f, delta),
                    reset,
                    m_GroundedField.value,
                    Math.Max(0f, m_HorizontalSpeedField.value),
                    m_AccelerationField.value,
                    m_VerticalSpeedField.value,
                    NormalizeDirection(m_MovementDirectionField.value),
                    NormalizeDirection(m_DesiredDirectionField.value),
                    m_FacingErrorField.value,
                    (CharacterPresentationMotionPhase)m_MotionPhaseField.value,
                    m_ParameterIds,
                    m_ParameterValues);
            }
            catch (Exception exception)
            {
                Stop(exception.Message);
                return;
            }
            m_Status.text = BuildFrameStatus(target);
            RenderPreview();
            RefreshViewportOverlay();
            m_Window.RefreshRuntimeHighlight();
            m_Window.RefreshRuntimeDetails();
        }

        void RenderPreview()
        {
            try
            {
                m_FixtureSession?.RenderPreview();
            }
            catch (Exception exception)
            {
                m_Status.text = $"Preview render failed: {exception.Message}";
            }
            m_PreviewRender.MarkDirtyRepaint();
        }

        void DrawPreview()
        {
            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.055f, 0.055f, 0.065f, 1f));
            HandlePreviewInput(rect);
            if (Event.current.type == EventType.Repaint && m_FixtureSession != null)
            {
                m_FixtureSession.Resize(
                    Mathf.RoundToInt(rect.width),
                    Mathf.RoundToInt(rect.height));
                m_FixtureSession.RenderPreview();
            }
            RenderTexture texture = m_FixtureSession?.PreviewTexture;
            if (texture)
            {
                EditorGUI.DrawPreviewTexture(
                    rect,
                    texture,
                    null,
                    ScaleMode.ScaleToFit);
                GUI.Label(
                    new Rect(
                        rect.x + 8f,
                        rect.yMax - 22f,
                        rect.width - 16f,
                        18f),
                    "LMB Orbit  ·  RMB/MMB Pan  ·  Wheel Zoom  ·  F Focus",
                    EditorStyles.miniLabel);
                return;
            }

            string message = m_Target && m_SelectedFixture == null
                ? "Live Actor · observe in Scene/Game View"
                : "Select a Preview Instance";
            GUI.Label(rect, message, EditorStyles.centeredGreyMiniLabel);
        }

        void HandlePreviewInput(Rect rect)
        {
            Event current = Event.current;
            if (m_FixtureSession == null || !rect.Contains(current.mousePosition))
                return;
            if (current.type == EventType.MouseDown)
            {
                m_PreviewRender.Focus();
                current.Use();
                return;
            }
            if (current.type == EventType.MouseDrag)
            {
                if (current.button == 0)
                    m_FixtureSession.Orbit(current.delta);
                else if (current.button == 1 || current.button == 2)
                    m_FixtureSession.Pan(current.delta);
                else
                    return;
                current.Use();
                m_PreviewRender.MarkDirtyRepaint();
                return;
            }
            if (current.type == EventType.ScrollWheel)
            {
                m_FixtureSession.Zoom(current.delta.y);
                current.Use();
                m_PreviewRender.MarkDirtyRepaint();
                return;
            }
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.F)
            {
                m_FixtureSession.Focus();
                current.Use();
                m_PreviewRender.MarkDirtyRepaint();
            }
        }

        void RefreshViewportOverlay()
        {
            m_ViewportOverlay.Clear();
            if (!m_Window.TryGetRuntimeSnapshot(
                    out AnimationPresentationRuntimeSnapshot snapshot,
                    out string status))
            {
                m_ViewportOverlay.Add(new Label(status));
                return;
            }

            m_ViewportOverlay.Add(new Label(
                $"{status} · frame {snapshot.PoseGraphCompletedAt} · completion {snapshot.CompletionIdentity}"));
        }

        bool TryGetContext(
            out CharacterPipelineHost target,
            out string error)
        {
            target = m_Target;
            if (!m_Window.TryGetPublishedPosePlan(out _, out error))
                return false;
            if (!target)
            {
                error = "Unavailable: select one Preview target.";
                return false;
            }
            if (m_SelectedFixture == null)
            {
                error =
                    "Live Actor is observation-only for Preview transport; use tuning fields or RuntimeDebug controls.";
                return false;
            }
            if (target.Definition != m_Window.DefinitionContext)
            {
                error =
                    "Unavailable: Preview target does not use this exact Character Definition.";
                return false;
            }
            if (target.Definition.AnimationPresentationProfile !=
                m_Window.ProfileContext)
            {
                error =
                    "Unavailable: Preview target does not use this exact Presentation Profile.";
                return false;
            }
            if (!target.CanPreviewPoseGraph)
            {
                error =
                    "Unavailable: Preview target is missing its formal Definition, Projection, Rig, Animancer, VisualRoot or Body fixture.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        string BuildFrameStatus(CharacterPipelineHost target)
        {
            CharacterPosePlanStageSnapshot stages =
                target.PreviewPosePlanStages;
            if (stages.IsValid)
            {
                for (int i = 0; i < stages.Stages.Count; i++)
                {
                    CharacterPoseExecutionStageSnapshot stage =
                        stages.Stages[i];
                    if (stage.Status != CharacterPoseStageStatus.Unavailable)
                        continue;
                    return $"Unavailable at stage {stage.StageIndex} ({stage.ExecutionDomain}, {stage.InputPoseSpace} -> {stage.OutputPoseSpace}): {stage.UnavailableReason}.";
                }
            }
            if (!target.HasPreviewAnimationDebugView)
                return $"Preview evaluated at {m_Time:0.###}s; completed frame unavailable.";
            string linkedPoseStatus = BuildLinkedPoseFrameStatus(
                target.PreviewAnimationDebugView.PosePlan);
            return $"{(m_Playing ? "Playing" : "Paused")} {m_Time:0.###}s" +
                (string.IsNullOrEmpty(linkedPoseStatus) ? string.Empty : $" · {linkedPoseStatus}");
        }

        static string BuildLinkedPoseFrameStatus(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            if (snapshot.LinkedPoseGroups.Count == 0)
                return string.Empty;
            var values = new List<string>();
            for (int groupIndex = 0; groupIndex < snapshot.LinkedPoseGroups.Count; groupIndex++)
            {
                CharacterLinkedPoseRuntimeGroupSnapshot group = snapshot.LinkedPoseGroups[groupIndex];
                int completed = 0;
                int entries = 0;
                int contributionCount = 0;
                for (int entryIndex = 0; entryIndex < snapshot.LinkedPoseEntries.Count; entryIndex++)
                {
                    AnimationLinkedPoseEntryRuntimeSnapshot entry = snapshot.LinkedPoseEntries[entryIndex];
                    if (entry.GroupId != group.GroupId)
                        continue;
                    entries++;
                    if (entry.Completed)
                        completed++;
                    contributionCount += entry.OperationCount;
                }
                values.Add(
                    $"{group.ImplementationId} sel={group.SelectionRevision} gen={group.Generation} " +
                    $"entries={completed}/{entries} contribution={contributionCount} discontinuity={group.StateReset}");
            }
            return "Linked Pose " + string.Join(" | ", values);
        }

        static Vector2 NormalizeDirection(Vector2 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new InvalidOperationException(
                    "Pose Preview direction must be finite.");
            return value.sqrMagnitude > 1f
                ? value.normalized
                : value;
        }

        void Stop(string status)
        {
            m_Playing = false;
            m_Target?.ClearPoseTuningCandidate();
            if (m_Target && m_SessionId != Guid.Empty)
                m_Target.ClearPoseGraphPreview(m_SessionId);
            m_SessionId = Guid.Empty;
            if (!string.IsNullOrEmpty(status))
                m_Status.text = status;
        }

        void AddOrReplaceSummary(string status)
        {
            Label summary = Content.Q<Label>("pose-preview-revision");
            if (summary == null)
            {
                summary = new Label
                {
                    name = "pose-preview-revision"
                };
                Content.Insert(
                    Math.Max(0, Content.childCount - 1),
                    summary);
            }
            summary.text = $"Pose Plan: {status}";
        }
    }

}
