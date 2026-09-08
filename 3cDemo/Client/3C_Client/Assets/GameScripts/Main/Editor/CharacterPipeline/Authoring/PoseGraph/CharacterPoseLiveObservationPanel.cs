using System;
using System.Collections.Generic;
using System.Linq;
using TreeDesigner.Authoring;
using TreeDesigner.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseLiveObservationPanel : CharacterPoseReadOnlyPanel
    {
        readonly CharacterPoseGraphWorkspace m_Window;
        readonly VisualElement m_TargetControls = new VisualElement();
        readonly DropdownField m_TargetField = new DropdownField("运行角色");
        readonly ObjectField m_ClipTarget = new ObjectField("Clip编辑目标")
        {
            objectType = typeof(CharacterPipelineHost),
            allowSceneObjects = true
        };
        readonly DropdownField m_CallSiteField = new DropdownField("调用位置");
        string[] m_CallSites = Array.Empty<string>();
        internal CharacterPoseProgramImage PublishedPlan { get; private set; }
        internal string CallSite { get; private set; }
        internal string Scope => CallSite == null ? null : PublishedPlan?.SourceMap.FirstOrDefault(source =>
            source.GraphId == Document?.DocumentId && source.CallSite == CallSite)?.Scope;
        readonly Dictionary<string, string> m_PageCalls = new Dictionary<string, string>(StringComparer.Ordinal);

        internal void SelectScope(string scope)
        {
            string[] matching = PublishedPlan?.SourceMap.Where(source => source.GraphId == Document?.DocumentId && source.Scope == scope)
                .Select(source => source.CallSite).Distinct().ToArray() ?? Array.Empty<string>();
            CallSite = matching.Length == 1 ? matching[0] : null;
            if (Document != null && CallSite != null) m_PageCalls[Document.DocumentId] = CallSite;
            if (Document != null && CallSite == null) m_PageCalls.Remove(Document.DocumentId);
            int index = CallSite == null ? 0 : Array.IndexOf(m_CallSites, CallSite) + 1;
            m_CallSiteField.SetValueWithoutNotify(m_CallSiteField.choices[index]);
            m_LastCompletion = 0;
            m_Window.RefreshObservedRuntime();
        }

        readonly Label m_Status = new Label();
        readonly List<AnimationPoseWatchIdentity> m_Watches = new List<AnimationPoseWatchIdentity>();
        readonly Label m_WatchValues = new Label();
        readonly Label m_Frame = new Label();
        readonly Label m_TuningStatus = new Label();
        readonly CharacterPoseLiveTuningPresenter m_Tuning;
        readonly List<AnimationPresentationRuntimeTarget> m_Choices = new List<AnimationPresentationRuntimeTarget>();
        readonly Guid m_InterestOwner = Guid.NewGuid();
        AnimationPresentationRuntimeTarget m_Target;
        AnimationPresentationRuntimeTarget m_SubscribedTarget;
        bool m_Bound;
        bool m_ChoicesDirty = true;
        bool m_ExplicitSelection;
        bool m_TargetEnded;
        bool m_ObservationEnabled;
        string m_Context = string.Empty;
        string m_LastStatus = string.Empty;
        ulong m_LastCompletion;
        double m_NextUpdate;

        internal CharacterPoseLiveObservationPanel(CharacterPoseGraphWorkspace window)
        {
            m_Window = window;
            m_Tuning = new CharacterPoseLiveTuningPresenter(window, ResolveHost, m_TuningStatus);
            m_TargetControls.AddToClassList("pose-target-controls");
            m_TargetControls.Add(m_TargetField);
            m_TargetControls.Add(m_ClipTarget);
            m_ClipTarget.tooltip = "仅选择已有场景角色，供正式AnimationClip编辑入口使用；不会创建或运行预览角色。";
            m_TargetField.RegisterValueChangedCallback(_ => SelectTarget(m_TargetField.index));
            m_ClipTarget.RegisterValueChangedCallback(evt => m_Window.BindObservationContext(evt.newValue as CharacterPipelineHost));
            Content.Add(m_CallSiteField);
            m_CallSiteField.RegisterValueChangedCallback(_ =>
            {
                int index = m_CallSiteField.index - 1;
                CallSite = index >= 0 && index < m_CallSites.Length ? m_CallSites[index] : null;
                if (Document != null && CallSite != null) m_PageCalls[Document.DocumentId] = CallSite;
                m_LastCompletion = 0;
                m_Window.RefreshObservedRuntime();
            });
            Content.Add(m_Status);
            Content.Add(m_Frame);
            Content.Add(new Button(ClearWatches) { text = "清空 Pose Watch" });
            Content.Add(m_WatchValues);
            Content.Add(m_TuningStatus);
        }

        internal VisualElement TargetField => m_TargetControls;
        internal AnimationPresentationRuntimeTarget RuntimeTarget => m_Target;
        internal GameObject AuthoringSceneTarget => EditorApplication.isPlaying
            ? ResolveHost()?.gameObject
            : (m_ClipTarget.value as CharacterPipelineHost)?.gameObject;

        public override void Bind(IGraphAuthoringDocumentProjection document)
        {
            base.Bind(document);
            if (!m_Bound)
            {
                EditorApplication.update += Update;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                AnimationPresentationRuntimeTargetRegistry.TargetRegistered += OnRegistered;
                AnimationPresentationRuntimeTargetRegistry.TargetUnregistered += OnUnregistered;
                m_Bound = true;
            }
            m_Window.SetRuntimeObservationMode(EditorApplication.isPlaying);
            Refresh();
        }

        internal void Rebind(IGraphAuthoringDocumentProjection document)
        {
            if (!m_Bound) Bind(document);
            else
            {
                Document = document;
                Refresh();
            }
        }

        internal void SetObservationEnabled(bool enabled)
        {
            m_ObservationEnabled = enabled;
            ApplyInterest();
            m_NextUpdate = 0;
        }

        public override void Refresh()
        {
            m_ChoicesDirty = true;
            m_NextUpdate = 0;
            m_Tuning.Refresh();
            PublishedPlan = m_Window.TryGetPublishedPosePlan(out CharacterPoseProgramImage plan, out _) ? plan : null;
            m_CallSites = PublishedPlan?.SourceMap.Where(source => source.GraphId == Document?.DocumentId)
                .Select(source => source.CallSite).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
            if (Document != null && m_PageCalls.TryGetValue(Document.DocumentId, out string savedCall)) CallSite = savedCall;
            if (CallSite == null || !m_CallSites.Contains(CallSite))
                CallSite = m_CallSites.Length == 1 ? m_CallSites[0] : null;
            var labels = new List<string> { "选择调用位置" };
            labels.AddRange(m_CallSites.Select(value => string.IsNullOrEmpty(value) ? "根图" : value));
            m_CallSiteField.choices = labels;
            m_CallSiteField.SetValueWithoutNotify(labels[CallSite == null ? 0 : Array.IndexOf(m_CallSites, CallSite) + 1]);
        }

        public override void Unbind()
        {
            if (m_Bound)
            {
                EditorApplication.update -= Update;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= OnRegistered;
                AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= OnUnregistered;
                m_Bound = false;
            }
            ReleaseInterest();
            m_Target = null;
            m_Choices.Clear();
            PublishedPlan = null;
            CallSite = null;
            Document = null;
            m_LastCompletion = 0;
        }

        void OnRegistered(AnimationPresentationRuntimeTarget target) => m_ChoicesDirty = true;

        void OnUnregistered(AnimationPresentationRuntimeTarget target)
        {
            m_ChoicesDirty = true;
            if (!ReferenceEquals(m_Target, target)) return;
            ReleaseInterest();
            m_Target = null;
            m_TargetEnded = true;
            m_LastCompletion = 0;
            m_NextUpdate = 0;
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ReleaseInterest();
                m_Target = null;
                m_ExplicitSelection = false;
                m_TargetEnded = false;
                m_Window.SetRuntimeObservationMode(false);
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
                m_Window.SetRuntimeObservationMode(true);
            m_ChoicesDirty = true;
            m_LastCompletion = 0;
            m_NextUpdate = 0;
        }

        void RefreshChoices()
        {
            m_ChoicesDirty = false;
            m_Choices.Clear();
            string graphId = m_Window.AssetContext?.Graph?.GraphId.Value;
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(graphId))
                m_Choices.AddRange(AnimationPresentationRuntimeTargetRegistry.Targets.Where(target =>
                    string.Equals(target.ProgramIdentity.PoseGraphId, graphId, StringComparison.Ordinal) && MatchesOwner(target))
                    .OrderBy(target => target.DisplayName, StringComparer.Ordinal).ThenBy(target => target.RuntimeInstanceId));
            if (m_Target != null && !m_Choices.Contains(m_Target))
            {
                ReleaseInterest();
                m_Target = null;
                m_LastCompletion = 0;
            }
            if (m_Target == null && !m_ExplicitSelection && m_Window.DefinitionContext != null)
            {
                AnimationPresentationRuntimeTarget[] matching = m_Choices.Where(target =>
                    string.Equals(target.ProjectionRevision, m_Window.ProjectionContext?.ProjectionRevision, StringComparison.Ordinal)).ToArray();
                if (matching.Length == 1) SetTarget(matching[0]);
            }
            var labels = new List<string> { "选择运行角色" };
            labels.AddRange(m_Choices.Select((target, index) => $"{index + 1} · {target.DisplayName} · {target.RuntimeInstanceId.ToString("N").Substring(0, 8)}"));
            m_TargetField.choices = labels;
            int selected = m_Target == null ? 0 : m_Choices.IndexOf(m_Target) + 1;
            m_TargetField.SetValueWithoutNotify(labels[selected]);
            m_TargetField.SetEnabled(EditorApplication.isPlaying);
            m_ClipTarget.style.display = EditorApplication.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }

        bool MatchesOwner(AnimationPresentationRuntimeTarget target)
        {
            var host = EditorUtility.InstanceIDToObject(target.HostInstanceId) as CharacterPipelineHost;
            return host && host.Definition && host.Definition.AnimationPresentationProfile &&
                host.Definition.AnimationPresentationProfile.PoseGraph == m_Window.AssetContext &&
                (!m_Window.DefinitionContext || host.Definition == m_Window.DefinitionContext);
        }

        void SelectTarget(int index)
        {
            m_ExplicitSelection = true;
            AnimationPresentationRuntimeTarget target = index > 0 && index <= m_Choices.Count ? m_Choices[index - 1] : null;
            SetTarget(target);
            if (target != null) m_Window.BindObservationContext(ResolveHost());
        }

        void SetTarget(AnimationPresentationRuntimeTarget target)
        {
            if (ReferenceEquals(target, m_Target)) return;
            ReleaseInterest();
            m_Watches.Clear();
            m_Target = target;
            m_TargetEnded = false;
            m_LastCompletion = 0;
            m_TuningStatus.text = string.Empty;
            m_Tuning.Refresh();
            ApplyInterest();
            m_NextUpdate = 0;
        }

        CharacterPipelineHost ResolveHost() => m_Target != null &&
            AnimationPresentationRuntimeTargetRegistry.TryGet(m_Target.RuntimeInstanceId, out AnimationPresentationRuntimeTarget current) &&
            ReferenceEquals(current, m_Target)
                ? EditorUtility.InstanceIDToObject(m_Target.HostInstanceId) as CharacterPipelineHost : null;

        void ApplyInterest()
        {
            AnimationPresentationRuntimeTarget desired = m_ObservationEnabled && EditorApplication.isPlaying ? m_Target : null;
            if (ReferenceEquals(desired, m_SubscribedTarget)) return;
            ReleaseInterest();
            if (desired == null) return;
            desired.SetDiagnosticsInterest(m_InterestOwner,
                AnimationPresentationDiagnosticsInterest.LiveState | AnimationPresentationDiagnosticsInterest.OperationDetail);
            m_SubscribedTarget = desired;
            ApplyWatches();
        }

        void ReleaseInterest()
        {
            m_SubscribedTarget?.RemovePoseWatchInterests(m_InterestOwner);
            m_SubscribedTarget?.RemoveDiagnosticsInterest(m_InterestOwner);
            m_SubscribedTarget = null;
        }

        internal void WatchNode(CharacterPoseCanvasNode node)
        {
            if (!m_ObservationEnabled || m_Target == null || PublishedPlan == null || CallSite == null) return;
            var sources = PublishedPlan.SourceMap.Where(source => source.GraphId == ((CharacterPoseCanvasGraph)node.graph).GraphId.Value &&
                source.AuthorNodeId == node.NodeId && source.CallSite == CallSite).ToArray();
            foreach (CharacterPresentationPoseSourceMapEntry source in sources)
            {
                CharacterPoseOperationHeader operation = PublishedPlan.OperationHeaders[source.OperationIndex];
                if (PublishedPlan.OperationPages.FindOutputValueIndex(operation, CharacterPoseValueReferenceKind.Pose) < 0 &&
                    PublishedPlan.OperationPages.FindOutputValueIndex(operation, CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) < 0 &&
                    PublishedPlan.OperationPages.FindOutputValueIndex(operation, CharacterPoseValueReferenceKind.FullBodyIkGoalSet) < 0) continue;
                string revision = operation.LinkedPoseFragmentIndex >= 0
                    ? PublishedPlan.LinkedPoseFragments[operation.LinkedPoseFragmentIndex].GraphRevision : PublishedPlan.ContentRevision;
                var identity = new AnimationPoseWatchIdentity(source.GraphId, revision, source.NodeId, source.CallSite);
                if (m_Watches.Contains(identity)) m_Watches.Remove(identity);
                else if (m_Watches.Count < AnimationPoseWatchCapacity.PerWindow) m_Watches.Add(identity);
                else m_TuningStatus.text = $"Pose Watch 最多 {AnimationPoseWatchCapacity.PerWindow} 项，请先清空或取消已有项。";
            }
            ApplyWatches();
            m_LastCompletion = 0;
        }

        void ClearWatches()
        {
            m_Watches.Clear();
            ApplyWatches();
            m_WatchValues.text = "未采集 Pose Watch";
        }

        void ApplyWatches()
        {
            if (m_SubscribedTarget == null) return;
            m_SubscribedTarget.SetPoseWatchInterests(m_InterestOwner, m_Watches);
            m_SubscribedTarget.SetDiagnosticsInterest(m_InterestOwner,
                AnimationPresentationDiagnosticsInterest.LiveState | AnimationPresentationDiagnosticsInterest.OperationDetail |
                (m_Watches.Count == 0 ? AnimationPresentationDiagnosticsInterest.None : AnimationPresentationDiagnosticsInterest.PoseWatch));
        }

        void UpdateWatchValues(in AnimationPresentationRuntimeSnapshot snapshot)
        {
            var rows = new List<string>();
            foreach (AnimationPoseWatchIdentity identity in m_Watches)
            {
                int found = -1;
                for (int i = 0; i < snapshot.PoseWatches.Count; i++)
                    if (snapshot.PoseWatches[i].Identity.Equals(identity)) { found = i; break; }
                if (found < 0) { rows.Add($"{identity.NodeId} · 等待采集"); continue; }
                AnimationPoseWatchSnapshot watch = snapshot.PoseWatches[found];
                rows.Add($"{identity.NodeId} · {watch.Availability} · 权重 {watch.OutputWeight:0.###} · {watch.BoneCount} 骨骼 · 完成帧 {watch.CompletionIdentity}");
                if (watch.Availability != AnimationPoseWatchAvailability.Pose) continue;
                var bones = snapshot.GetPoseWatchComponentPoses(found);
                for (int i = 0; i < bones.Count; i++)
                    rows.Add($"  骨骼 {i} · 位置 {bones[i].Position:F3} · 旋转 {bones[i].Rotation:F3}");
            }
            m_WatchValues.text = rows.Count == 0 ? "未采集 Pose Watch" : string.Join("\n", rows);
        }

        internal bool TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status)
        {
            snapshot = default;
            if (!EditorApplication.isPlaying)
            {
                status = "未播放";
                return false;
            }
            if (!m_ObservationEnabled)
            {
                status = "作者模式 · 运行观察已关闭";
                return false;
            }
            if (m_Target == null)
            {
                status = m_TargetEnded ? "目标已结束" : "等待运行角色";
                return false;
            }
            try
            {
                if (!m_Target.TryGetDebugView(out AnimationPresentationDebugView view))
                {
                    status = "观察中 · 等待已完成帧";
                    return false;
                }
                snapshot = view.PosePlan;
                if (!m_Window.MatchesCurrentPublishedRevision(snapshot))
                {
                    snapshot = default;
                    status = "版本不匹配 · 当前运行结果不叠加到作者图";
                    return false;
                }
            }
            catch (InvalidOperationException exception)
            {
                status = "版本或结果不可用 · " + exception.Message;
                return false;
            }
            status = EditorApplication.isPaused ? "观察中 · Unity已暂停" : "观察中";
            return true;
        }

        void Update()
        {
            if (!m_Bound || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < m_NextUpdate) return;
            m_NextUpdate = now + 0.1;
            string context = $"{m_Window.AssetContext?.GetInstanceID()}|{m_Window.AssetContext?.Graph?.GraphId}|{m_Window.ProjectionContext?.ProjectionRevision}";
            if (context != m_Context)
            {
                m_Context = context;
                m_ChoicesDirty = true;
            }
            if (m_ChoicesDirty) RefreshChoices();
            ApplyInterest();
            bool ready = TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status);
            m_Status.text = status;
            m_Frame.text = ready ? $"完成帧 {snapshot.CompletionIdentity} · {snapshot.FinalAvailability}" : string.Empty;
            ulong completion = ready ? snapshot.CompletionIdentity : 0;
            if (completion == m_LastCompletion && status == m_LastStatus) return;
            m_LastCompletion = completion;
            m_LastStatus = status;
            if (ready) UpdateWatchValues(in snapshot);
            else m_WatchValues.text = "运行结果不可用";
            m_Window.RefreshObservedRuntime();
        }

        internal void RebuildCandidateAfterUndoRedo() => m_Tuning.RebuildCandidateAfterUndoRedo();
        internal IReadOnlyList<GraphAuthoringReadOnlyDetail> GetAppliedValues(GraphAuthoringSelection selection) => m_Tuning.GetAppliedValues(selection);
        internal bool TryApplySelectionTuning(IGraphAuthoringDocumentProjection document, GraphAuthoringMutationRequest request) => m_Tuning.TryApplySelectionTuning(document, request);
        internal bool PopulateSelectionTuning(GraphAuthoringSelection? selection, VisualElement host) => m_Tuning.PopulateSelectionTuning(selection, host);
    }
}
