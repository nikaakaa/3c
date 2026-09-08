using System;
using System.Collections.Generic;
using System.Linq;
using TreeDesigner.Authoring;
using TreeDesigner.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseLiveTuningPresenter
    {
        readonly CharacterPoseGraphWorkspace m_Window;
        readonly Func<CharacterPipelineHost> m_GetTarget;
        readonly Label m_Status;
        CharacterPoseTuningLayout m_TuningLayout;
        CharacterPoseTuningParameterBlock m_TuningBlock;
        CharacterPipelineHost m_Target => m_GetTarget();

        internal CharacterPoseLiveTuningPresenter(CharacterPoseGraphWorkspace window, Func<CharacterPipelineHost> getTarget, Label status)
        {
            m_Window = window;
            m_GetTarget = getTarget;
            m_Status = status;
        }

        internal void Refresh() => RefreshTuningBinding();
        internal void RebuildCandidateAfterUndoRedo()
        {
            if (m_Window.IsRuntimeObservation) return;
            RefreshTuningBinding();
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
                    m_Window.ProfileContext,
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
            bool submitted = m_Target.SubmitLivePoseTuningCandidate(
                    sourceRevision,
                    Guid.NewGuid().ToString("N"),
                    m_TuningBlock,
                    out string submitError);
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

        internal IReadOnlyList<GraphAuthoringReadOnlyDetail> GetAppliedValues(
            GraphAuthoringSelection selection)
        {
            var rows = new List<GraphAuthoringReadOnlyDetail>();
            CharacterPoseTuningLayout layout = m_Target?.LiveTuningLayout;
            CharacterPoseTuningParameterBlock block = m_Target?.LiveActiveTuningBlock;
            CharacterPoseTuningRuntimeState state = m_Target?.LiveTuningState ?? default;
            rows.Add(new GraphAuthoringReadOnlyDetail(
                "Applied Target",
                m_Target ? $"Live Actor · {m_Target.name}" : "No Target"));
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
                    out CharacterPoseProgramImage plan,
                    out _))
            {
                for (int i = 0; i < plan.FullBodyIks.Count; i++)
                    if (plan.FullBodyIks[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"full-body-ik-profile:{plan.FullBodyIks[i].ProfileId}");
                for (int i = 0; i < plan.FootPlacements.Count; i++)
                    if (plan.FootPlacements[i].NodeId.Value == selection.ElementId.Value)
                        owners.Add($"foot-placement-profile:{plan.FootPlacements[i].Profile.ProfileId}");
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


        void RefreshTuningBinding()
        {
            CharacterPoseTuningLayout layout = m_Target?.LiveTuningLayout;
            CharacterPoseTuningParameterBlock source = m_Target?.LiveActiveTuningBlock;
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
            m_TuningLayout = layout;
            m_TuningBlock = source;
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
            if (m_Window.IsRuntimeObservation) return false;
            if (m_TuningLayout == null || m_TuningBlock == null)
                return false;
            try
            {
                _ = CharacterPoseTuningCandidateCompiler.CompileBlock(
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

                m_Window.MarkPoseTuningAuthoringChanged();
                if (!m_Target)
                {
                    m_Status.text = "Saved · No live target is selected.";
                    return true;
                }
                if (!m_Window.TryGetPublishedProjection(
                        out CharacterPresentationProjection projection,
                        out string projectionError))
                {
                    m_Status.text = $"Saved · {projectionError}";
                    return true;
                }
                if (!CharacterPoseTuningAuthoringService.TryCompileCurrentBlock(
                        m_Window.AssetContext,
                        m_Window.ProfileContext,
                        projection,
                        m_TuningLayout,
                        m_TuningBlock,
                        out CharacterPoseTuningParameterBlock nextBlock,
                        out string compileError))
                {
                    m_Status.text = $"Saved · {compileError}";
                    return true;
                }
                m_TuningBlock = nextBlock;
                string sourceRevision =
                    m_Window.AssetContext?.Graph?.ContentRevision ??
                    string.Empty;
                bool submitted;
                string error;
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
            if (document is CharacterPoseCanvasGraphDocument)
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
            if (m_Window.IsRuntimeObservation || !selection.HasValue)
                return false;
            RefreshTuningBinding();
            if (m_TuningLayout == null ||
                m_TuningBlock == null)
                return false;
            GraphAuthoringSelection current = selection.Value;
            var owners = new HashSet<string>(StringComparer.Ordinal);
            if (current.Kind == GraphAuthoringSelectionKind.Node)
            {
                owners.Add($"pose-node:{current.ElementId.Value}");
                if (m_Window.TryGetPublishedPosePlan(
                        out CharacterPoseProgramImage plan,
                        out _))
                {
                    for (int i = 0; i < plan.FullBodyIks.Count; i++)
                        if (plan.FullBodyIks[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"full-body-ik-profile:{plan.FullBodyIks[i].ProfileId}");
                    for (int i = 0;
                         i < plan.FootPlacements.Count;
                         i++)
                        if (plan.FootPlacements[i].NodeId.Value ==
                            current.ElementId.Value)
                            owners.Add(
                                $"foot-placement-profile:{plan.FootPlacements[i].Profile.ProfileId}");
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
                        !value.OwnerId.StartsWith("pose-node:", StringComparison.Ordinal) &&
                        !value.OwnerId.StartsWith("pose-state-machine:", StringComparison.Ordinal) &&
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
                text = "策略参数",
                value = true
            };
            for (int i = 0; i < entries.Length; i++)
                foldout.Add(CreateSelectionTuningField(entries[i]));
            host.Add(foldout);
            return true;
        }

        VisualElement CreateSelectionTuningField(
            CharacterPoseTuningLayoutEntry entry)
        {
            if (!CharacterPoseTuningAuthoringService.TryReadCurrentValue(
                    m_Window.AssetContext,
                    m_Window.ProfileContext,
                    entry,
                    out CharacterPoseTuningValue currentValue,
                    out string readError))
            {
                m_Status.text = readError;
                var error = new Label($"{entry.DisplayName}: {readError}");
                error.AddToClassList("pose-tuning-authoring-error");
                return error;
            }
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
                        value = currentValue.FloatValue,
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
                {
                    var field = new IntegerField(label) { value = currentValue.IntegerValue, isDelayed = true };
                    field.RegisterValueChangedCallback(evt => SubmitSelectionTuningValue(entry, CharacterPoseTuningValue.Integer(evt.newValue)));
                    authoringField = field;
                    break;
                }
                case CharacterPoseTuningValueKind.Enum:
                {
                    if (!entry.OwnerId.StartsWith("full-body-ik-profile:", StringComparison.Ordinal) ||
                        (!entry.FieldId.EndsWith("/reach-smoothing", StringComparison.Ordinal) && !entry.FieldId.EndsWith("/push-smoothing", StringComparison.Ordinal)))
                        throw new InvalidOperationException($"Enum editor is not registered for '{entry.FieldId}'.");
                    var field = new EnumField(label, (CharacterFullBodyIkSmoothing)currentValue.EnumValue);
                    field.RegisterValueChangedCallback(evt => SubmitSelectionTuningValue(entry,
                        CharacterPoseTuningValue.Enum(Convert.ToInt32(evt.newValue))));
                    authoringField = field;
                    break;
                }
                case CharacterPoseTuningValueKind.Boolean:
                {
                    var field = new Toggle(label)
                    {
                        value = currentValue.BooleanValue
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
                : m_Target.LiveTuningState;

        string CurrentAppliedValue(CharacterPoseTuningLayoutEntry entry)
        {
            if (!m_Target)
                return "No Target";
            CharacterPoseTuningParameterBlock block = m_Target.LiveActiveTuningBlock;
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

    }
}
