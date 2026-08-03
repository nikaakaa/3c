using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
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
            Content.style.flexGrow = 1f;
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
            row.style.flexDirection = FlexDirection.Row;
            var name = new Label(label);
            name.style.minWidth = 150f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            var content = new Label(value ?? string.Empty);
            content.style.flexGrow = 1f;
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

    sealed class CharacterPosePreviewPanel :
        CharacterPoseReadOnlyPanel
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly ObjectField m_TargetField =
            new ObjectField("Preview Target");
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
                "Motion Phase",
                CharacterPresentationMotionPhase.GroundedStationary);
        readonly Foldout m_ParameterFixture =
            new Foldout
            {
                text = "Capability Parameters",
                value = true
            };
        readonly Label m_Status = new Label();
        readonly List<PoseParameterId> m_ParameterIds =
            new List<PoseParameterId>();
        readonly List<float> m_ParameterValues =
            new List<float>();
        CharacterPipelineHost m_Target;
        Guid m_SessionId;
        bool m_Playing;
        float m_Time;
        ulong m_Tick;
        double m_LastUpdate;
        string m_FixturePlanHash = string.Empty;

        public CharacterPosePreviewPanel(
            CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ??
                throw new ArgumentNullException(nameof(window));
            m_TargetField.objectType = typeof(CharacterPipelineHost);
            m_TargetField.allowSceneObjects = true;
            m_TargetField.RegisterValueChangedCallback(evt =>
                SetTarget(evt.newValue as CharacterPipelineHost));
            m_TimeField.isDelayed = true;
            m_TimeField.RegisterValueChangedCallback(evt =>
                m_Time = Math.Max(0f, evt.newValue));
        }

        public override void Bind(
            IGraphAuthoringDocumentProjection document)
        {
            base.Bind(document);
            Content.Add(m_TargetField);
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
            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.Add(new Button(Play) { text = "Play" });
            controls.Add(new Button(Pause) { text = "Pause" });
            controls.Add(new Button(Step) { text = "Step" });
            controls.Add(new Button(Seek) { text = "Seek" });
            controls.Add(new Button(Reset) { text = "Reset" });
            Content.Add(controls);
            Content.Add(m_Status);
            EditorApplication.update += Update;
            Refresh();
        }

        public override void Refresh()
        {
            bool published = m_Window.TryGetPublishedPosePlan(
                out CharacterPresentationPosePlan plan,
                out string status);
            string revision = published
                ? $"{plan.PoseGraphId}@{plan.ContentRevision} / {plan.PlanHash}"
                : status;
            AddOrReplaceSummary(revision);
            if (published &&
                !string.Equals(
                    m_FixturePlanHash,
                    plan.PlanHash,
                    StringComparison.Ordinal))
            {
                RebuildParameterFixture(plan);
            }
        }

        public override void Unbind()
        {
            EditorApplication.update -= Update;
            Stop(string.Empty);
            m_TargetField.SetValueWithoutNotify(null);
            m_Target = null;
            m_ParameterIds.Clear();
            m_ParameterValues.Clear();
            m_FixturePlanHash = string.Empty;
            base.Unbind();
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
                CharacterPresentationPoseOperation operation =
                    plan.Operations[i];
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
                CharacterPresentationPoseParameterEntry parameter =
                    plan.Parameters[index];
                if (!CharacterPresentationProgramParameterFrame.Supports(
                        parameter.ParameterId))
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
            {
                m_ParameterFixture.Add(
                    new Label(
                        "This graph has no capability-registered direct parameters."));
            }
        }

        void SetTarget(CharacterPipelineHost target)
        {
            if (ReferenceEquals(m_Target, target))
                return;
            Stop(string.Empty);
            m_Target = target;
            Refresh();
        }

        void Play()
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

        void Pause()
        {
            m_Playing = false;
            m_Status.text = m_SessionId == Guid.Empty
                ? "Preview stopped."
                : $"Paused at {m_Time:0.###}s.";
        }

        void Step()
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

        void Seek()
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

        void Reset()
        {
            Stop(string.Empty);
            m_Time = 0f;
            m_TimeField.SetValueWithoutNotify(0f);
            Refresh();
        }

        void Update()
        {
            if (!m_Playing)
                return;
            double now = EditorApplication.timeSinceStartup;
            float delta =
                Mathf.Clamp((float)(now - m_LastUpdate), 0f, 0.1f);
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
            target.TrySetPreviewPoseWatchInterests(
                m_SessionId,
                m_Window.PoseWatchOwnerId,
                m_Window.PoseWatchIdentities);
            m_Status.text = BuildFrameStatus(target);
            m_Window.RefreshBottomDock();
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
            CharacterFootPlacementFrameSnapshot footPlacement =
                target.PreviewFootPlacementSnapshot;
            string foot = footPlacement.IsValid
                ? $" · Foot Placement weight {footPlacement.FootPlacementWeight:0.###}, support {footPlacement.SupportFoot}, solver {footPlacement.SolverResult.Applied}"
                : string.Empty;
            return $"{(m_Playing ? "Playing" : "Paused")} {m_Time:0.###}s · completion {target.PreviewAnimationDebugView.CompletionIdentity}{foot}";
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
            if (m_Target && m_SessionId != Guid.Empty)
            {
                m_Target.RemovePreviewPoseWatchInterests(
                    m_Window.PoseWatchOwnerId);
                m_Target.ClearPoseGraphPreview(m_SessionId);
            }
            m_SessionId = Guid.Empty;
            if (!string.IsNullOrEmpty(status))
                m_Status.text = status;
        }

        void AddOrReplaceSummary(string revision)
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
            summary.text = $"Published Pose Plan: {revision}";
        }
    }

    sealed class CharacterPoseWatchPanel :
        CharacterPoseReadOnlyPanel
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public CharacterPoseWatchPanel(
            CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ??
                throw new ArgumentNullException(nameof(window));
        }

        public override void Refresh()
        {
            Content.Clear();
            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.Add(new Button(m_Window.WatchSelectedNode)
            {
                text = "Watch Selected"
            });
            controls.Add(new Button(m_Window.ClearPoseWatches)
            {
                text = "Clear"
            });
            Content.Add(controls);
            AddValue(
                Content,
                "Capacity",
                $"{m_Window.PoseWatchIdentities.Count}/{AnimationPoseWatchCapacity.PerWindow}");
            if (m_Window.PoseWatchIdentities.Count == 0)
            {
                AddStatus(
                    Content,
                    "Select a compiled Pose-output node and explicitly add a Pose Watch.");
                return;
            }

            m_Window.SynchronizePoseWatchInterests();
            bool available = m_Window.TryGetPoseWatchSnapshot(
                out AnimationPresentationRuntimeSnapshot snapshot,
                out string status);
            if (!available)
                AddStatus(Content, status);

            for (int identityIndex = 0;
                 identityIndex < m_Window.PoseWatchIdentities.Count;
                 identityIndex++)
            {
                AnimationPoseWatchIdentity identity =
                    m_Window.PoseWatchIdentities[identityIndex];
                var card = new VisualElement();
                card.style.marginTop = 4f;
                card.style.paddingBottom = 4f;
                AddValue(card, "Pose Watch", identity.ToString());
                AnimationPoseWatchSnapshot watch = default;
                bool found = false;
                if (available)
                {
                    for (int watchIndex = 0;
                         watchIndex < snapshot.PoseWatches.Count;
                         watchIndex++)
                    {
                        AnimationPoseWatchSnapshot candidate =
                            snapshot.PoseWatches[watchIndex];
                        if (!candidate.Identity.Equals(identity))
                            continue;
                        watch = candidate;
                        found = true;
                        break;
                    }
                }
                AddValue(
                    card,
                    "Result",
                    found
                        ? $"{watch.Availability} · weight {watch.OutputWeight:0.###} · contributions {watch.ContributionCount} · completion {watch.CompletionIdentity}"
                        : available
                            ? "Not published for this completed frame."
                            : status);
                if (found)
                {
                    AddValue(
                        card,
                        "Execution",
                        $"{watch.OperationCode} · stage {watch.StageIndex} · {watch.ExecutionDomain} · {watch.OutputPoseSpace} Pose");
                    if (watch.InvalidReason != AnimationPoseNativeInvalidReason.None)
                        AddValue(card, "Invalid Reason", watch.InvalidReason.ToString());
                    AnimationFootPlacementSolvedPoseSnapshot solved =
                        watch.FootPlacementSolvedPose;
                    if (solved.IsValid)
                    {
                        AddValue(card, "Solved Pelvis", FormatPosition(solved.Pelvis.Position));
                        AddValue(
                            card,
                            "Solved Left Leg",
                            $"hip {FormatPosition(solved.LeftHip.Position)} · knee {FormatPosition(solved.LeftKnee.Position)} · ankle {FormatPosition(solved.LeftAnkle.Position)}");
                        AddValue(
                            card,
                            "Solved Right Leg",
                            $"hip {FormatPosition(solved.RightHip.Position)} · knee {FormatPosition(solved.RightKnee.Position)} · ankle {FormatPosition(solved.RightAnkle.Position)}");
                    }
                }
                int removeIndex = identityIndex;
                var buttons = new VisualElement();
                buttons.style.flexDirection = FlexDirection.Row;
                buttons.Add(new Button(() =>
                    m_Window.FocusNode(identity.NodeId))
                {
                    text = "Locate Node"
                });
                buttons.Add(new Button(() =>
                    m_Window.RemovePoseWatch(removeIndex))
                {
                    text = "Remove"
                });
                card.Add(buttons);
                Content.Add(card);
            }
        }

        static string FormatPosition(Vector3 value) =>
            $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";

        public override void Unbind()
        {
            m_Window.ReleasePoseWatchInterests();
            base.Unbind();
        }
    }

    sealed class CharacterPoseLiveDebugPanel :
        CharacterPoseReadOnlyPanel
    {
        readonly CharacterPoseRuntimeTraceProjection m_Trace;

        public CharacterPoseLiveDebugPanel(
            CharacterPoseRuntimeTraceProjection trace)
        {
            m_Trace = trace ??
                throw new ArgumentNullException(nameof(trace));
        }

        public override void Refresh()
        {
            Content.Clear();
            if (Document == null)
                return;
            if (!m_Trace.TryGetSnapshot(
                    out AnimationPresentationRuntimeSnapshot snapshot,
                    out string status))
            {
                AddStatus(Content, status);
                return;
            }

            AddValue(
                Content,
                "Completion",
                snapshot.CompletionIdentity.ToString());
            AddValue(
                Content,
                "Final Pose",
                $"{snapshot.FinalAvailability} · {snapshot.FinalInvalidReason} · continuity {snapshot.ContinuityIdentity}");
            if (m_Trace.TryGetPosePlanStages(
                    out CharacterPosePlanStageSnapshot stages,
                    out string stageStatus))
            {
                bool hasWorldStage = false;
                bool worldAvailable = true;
                for (int i = 0; i < stages.Stages.Count; i++)
                {
                    CharacterPoseExecutionStageSnapshot stage = stages.Stages[i];
                    if (stage.ExecutionDomain == CharacterPoseExecutionDomain.WorldAwarePose)
                    {
                        hasWorldStage = true;
                        worldAvailable &= stage.Status == CharacterPoseStageStatus.Completed;
                    }
                    AddValue(
                        Content,
                        $"Stage {stage.StageIndex}",
                        $"{stage.Status} · {stage.ExecutionDomain} · {stage.InputPoseSpace} -> {stage.OutputPoseSpace} · completion {stage.CompletionIdentity} · {stage.UnavailableReason}");
                }
                AddValue(
                    Content,
                    "World Capability",
                    hasWorldStage
                        ? worldAvailable ? "Available" : "Unavailable"
                        : "Not Required");
            }
            else
            {
                AddValue(Content, "Pose Stages", stageStatus);
            }
            if (m_Trace.TryGetFootPlacement(
                    out CharacterFootPlacementFrameSnapshot footPlacement,
                    out string footStatus))
            {
                AddValue(
                    Content,
                    "Foot Planner",
                    $"weight {footPlacement.FootPlacementWeight:0.###} · support {footPlacement.SupportFoot} · pelvis {footPlacement.PelvisHeightDecision}/{footPlacement.PelvisHeightReason} · offset {footPlacement.PelvisCurrentOffset:0.###}");
                AddValue(Content, "Left Foot Plan", FormatFoot(footPlacement.Left));
                AddValue(Content, "Right Foot Plan", FormatFoot(footPlacement.Right));
                AddValue(
                    Content,
                    "Foot Solver",
                    $"applied {footPlacement.SolverResult.Applied} · duplicate {footPlacement.SolverResult.DuplicateRejected} · {footPlacement.SolverResult.Detail}");
            }
            else
            {
                AddValue(Content, "Foot Placement Trace", footStatus);
            }
            IReadOnlyList<GraphAuthoringRuntimeTraceProjection> traces =
                m_Trace.GetRuntimeTrace(Document);
            for (int i = 0; i < traces.Count; i++)
            {
                GraphAuthoringRuntimeTraceProjection trace = traces[i];
                AddValue(
                    Content,
                    trace.ElementId.Value,
                    $"{trace.Status} · {trace.Detail}");
            }
            for (int i = 0; i < snapshot.PoseStateMachines.Count; i++)
            {
                PoseStateMachineRuntimeSnapshot machine =
                    snapshot.PoseStateMachines[i];
                AddValue(
                    Content,
                    $"State Machine {machine.StateMachineId}",
                    $"{machine.ActiveStateId} -> {machine.TargetStateId} · transition {machine.ActiveTransitionId} · {machine.TransitionProgress:0.###}");
            }
            for (int i = 0; i < snapshot.AnimationSlots.Count; i++)
            {
                AnimationSlotRuntimeSnapshot slot =
                    snapshot.AnimationSlots[i];
                AddValue(
                    Content,
                    $"Slot {slot.SlotId}",
                    $"{slot.AnimationChannelId} · {slot.ActionAvailability} · weight {slot.ActionOutputWeight:0.###} · {slot.TransitionExecution}");
            }
        }

        static string FormatFoot(FootPlacementFootFrameSnapshot foot) =>
            $"{foot.ConstraintState}/{foot.TransitionReason} · target ({foot.TargetPosition.x:0.###}, {foot.TargetPosition.y:0.###}, {foot.TargetPosition.z:0.###}) · position {foot.PositionWeight:0.###} · rotation {foot.RotationWeight:0.###} · extension {foot.LegExtensionRatio:0.###} · bend {foot.BendDecisionReason} · candidates {foot.CandidateCount}/{foot.QueryCount}";
    }
}
